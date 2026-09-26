using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.ViewModels;
using ADHUNIK_BARI.Services;
using Microsoft.EntityFrameworkCore;

namespace ADHUNIK_BARI.Controllers
{
    [Authorize]
    public class ResidentController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;
        private readonly IWebHostEnvironment environment;
        private readonly IPaymentService paymentService;
        private readonly IConfiguration configuration;
        private readonly IGymService gymService;

        public ResidentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment environment,
            IPaymentService paymentService,
            IConfiguration configuration,
            IGymService gymService)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.environment = environment;
            this.paymentService = paymentService;
            this.configuration = configuration;
            this.gymService = gymService;
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        public async Task<IActionResult> Dashboard()
        {
            if (User.IsInRole("ParkingUser") && !User.IsInRole("Tenant") && !User.IsInRole("FlatOwner"))
            {
                return RedirectToAction(nameof(ParkingDashboard));
            }

            var user = await userManager.GetUserAsync(User);
            var assignment = user == null ? null : await dbContext.FlatAssignments
                .Include(item => item.Flat)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == user.Id && item.IsActive);

            ViewBag.Assignment = assignment;

            var residentParking = new ResidentParkingViewModel();

            if (assignment != null)
            {
                try
                {
                    var assignedSpots = await dbContext.ParkingSpots
                        .Where(p => p.FlatId == assignment.FlatId)
                        .Include(p => p.Floor)
                        .AsNoTracking()
                        .ToListAsync();

                    if (assignedSpots.Any())
                    {
                        var parkingBillItems = await dbContext.BillItems
                            .Include(bi => bi.Bill)
                            .Where(bi => bi.ItemType == BillItemTypes.Parking && bi.Bill != null && bi.Bill.AssignmentId == assignment.AssignmentId)
                            .OrderByDescending(bi => bi.CreatedAt)
                            .ToListAsync();

                        var latestBill = await dbContext.Bills
                            .Where(b => b.AssignmentId == assignment.AssignmentId)
                            .OrderByDescending(b => b.BillYear)
                            .ThenByDescending(b => b.BillMonth)
                            .FirstOrDefaultAsync();

                        foreach (var spot in assignedSpots)
                        {
                            string paymentStatus = "Due";
                            var matchingItem = parkingBillItems.FirstOrDefault(bi => bi.PaymentStatus == "Paid");
                            if (matchingItem != null || (latestBill != null && latestBill.BillStatus == "Paid"))
                            {
                                paymentStatus = "Paid";
                            }

                            residentParking.Spots.Add(new ResidentParkingSpotItem
                            {
                                ParkingSpotId = spot.ParkingSpotId,
                                SpotNumber = spot.SpotNumber,
                                FloorName = spot.Floor?.FloorName ?? "Basement",
                                VehicleType = spot.ParkingType ?? "Car",
                                MonthlyFee = spot.ParkingFee,
                                PaymentStatus = paymentStatus,
                                AssignedDate = spot.CreatedAt
                            });
                        }
                    }
                }
                catch (Exception)
                {
                    // Gracefully handle if parking tables or columns are pending migration
                }
            }

            ViewBag.ResidentParking = residentParking;

            if (user != null)
            {
                ViewBag.ResidentName = user.FullName;
                ViewBag.RequirePasswordChange = user.TemporaryPasswordStatus;
            }

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "ParkingUser")]
        public async Task<IActionResult> ParkingDashboard()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var spots = new List<ParkingSpot>();
            var applications = new List<ParkingApplication>();

            try
            {
                spots = await dbContext.ParkingSpots
                    .Include(p => p.Floor)
                    .Where(p => p.AssignedUserId == user.Id)
                    .AsNoTracking()
                    .ToListAsync();

                applications = await dbContext.ParkingApplications
                    .Include(a => a.ParkingSpot)
                        .ThenInclude(p => p!.Floor)
                    .Where(a => (a.CreatedUserId == user.Id || a.Email == user.Email) && a.PaymentStatus == PropertyPaymentStatuses.Succeeded)
                    .OrderByDescending(a => a.PaidAt ?? a.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Gracefully handle if parking tables or columns are pending migration
            }

            ViewBag.User = user;
            ViewBag.Spots = spots;
            ViewBag.Applications = applications;
            ViewBag.RequirePasswordChange = user.TemporaryPasswordStatus;

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        public IActionResult MyParking()
        {
            if (User.IsInRole("ParkingUser"))
            {
                return RedirectToAction(nameof(ParkingDashboard));
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        public async Task<IActionResult> MyBills()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var assignment = await GetActiveAssignment(user.Id);

            var bills = assignment == null ? new List<Bill>() : await dbContext.Bills
                .Include(b => b.BillItems)
                .Include(b => b.Payments)
                .Where(b => b.AssignmentId == assignment.AssignmentId)
                .AsNoTracking()
                .OrderByDescending(b => b.BillYear)
                .ThenByDescending(b => b.BillMonth)
                .ToListAsync();

            var payments = await dbContext.Payments
                .Include(p => p.Bill)
                .Where(p => p.UserId == user.Id)
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var currentBillVms = bills.Select(b => new ResidentBillViewModel
            {
                BillId = b.BillId,
                BillMonth = b.BillMonth,
                BillYear = b.BillYear,
                FlatNumber = assignment?.Flat?.FlatNumber ?? "N/A",
                ResidentType = assignment?.ResidentType ?? "Parking Member",
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                DueAmount = b.DueAmount,
                Deadline = b.Deadline,
                BillStatus = b.BillStatus,
                CreatedAt = b.CreatedAt,
                LatestPaymentId = b.Payments?
                    .Where(p => p.PaymentStatus == "Completed" || p.PaymentStatus == PropertyPaymentStatuses.Succeeded)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => (int?)p.PaymentId)
                    .FirstOrDefault(),
                BillItems = b.BillItems.Select(item => new ResidentBillItemViewModel
                {
                    BillItemId = item.BillItemId,
                    ItemType = item.ItemType,
                    Amount = item.Amount,
                    Description = item.Description,
                    PaymentStatus = item.PaymentStatus
                }).ToList()
            }).ToList();

            var paymentHistoryVms = payments.Select(p => new ResidentPaymentHistoryViewModel
            {
                PaymentId = p.PaymentId,
                AmountPaid = p.AmountPaid,
                PaymentDate = p.PaymentDate != default ? p.PaymentDate : p.CreatedAt,
                PaymentStatus = p.PaymentStatus,
                StripeReceiptUrl = p.StripeReceiptUrl,
                Reference = p.Reference,
                ItemsDescription = p.PaidItemsJson
            }).ToList();

            // Include parking payments for this user (outsider or direct parking applications)
            var parkingApps = await dbContext.ParkingApplications
                .Include(a => a.ParkingSpot)
                .Where(a => (a.CreatedUserId == user.Id || a.Email == user.Email) && a.PaymentStatus == PropertyPaymentStatuses.Succeeded)
                .AsNoTracking()
                .OrderByDescending(a => a.PaidAt ?? a.CreatedAt)
                .ToListAsync();

            foreach (var pa in parkingApps)
            {
                paymentHistoryVms.Add(new ResidentPaymentHistoryViewModel
                {
                    PaymentId = 900000 + pa.ParkingApplicationId,
                    AmountPaid = pa.AdvanceAmount,
                    PaymentDate = pa.PaidAt ?? pa.CreatedAt,
                    PaymentStatus = "Completed",
                    Reference = $"PRK-{pa.ParkingSpot?.SpotNumber ?? ""}-{pa.ParkingApplicationId}",
                    ItemsDescription = $"Parking Bay {pa.ParkingSpot?.SpotNumber ?? ""} ({pa.ApplicationType})"
                });
            }

            var model = new MyBillsViewModel
            {
                ResidentName = user.FullName ?? "Resident",
                FlatNumber = assignment?.Flat?.FlatNumber ?? "Parking Space",
                ResidentType = assignment?.ResidentType ?? "Parking Member",
                CurrentBills = currentBillVms,
                PaymentHistory = paymentHistoryVms.OrderByDescending(p => p.PaymentDate).ToList()
            };

            ViewBag.StripePublicKey = configuration["Stripe:PublishableKey"] ?? "pk_test_placeholder";
            ViewBag.StripePublishableKey = configuration["Stripe:PublishableKey"] ?? "pk_test_placeholder";

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Manager,Admin,Tenant,FlatOwner,ParkingUser")]
        public async Task<IActionResult> Receipt(int id, [FromQuery] int? paymentId)
        {
            var targetPaymentId = id > 0 ? id : paymentId.GetValueOrDefault();
            if (targetPaymentId <= 0)
            {
                return NotFound("Invalid Receipt/Payment ID.");
            }

            var user = await userManager.GetUserAsync(User);
            var isManager = User.IsInRole("Manager") || User.IsInRole("Admin");
            var userId = user?.Id ?? "";

            var receipt = await paymentService.GetReceiptDetailsAsync(targetPaymentId, userId, isManager);
            if (receipt == null)
            {
                return NotFound("Receipt not found or access denied.");
            }

            return View(receipt);
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> Notices()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var assignedFlatId = await dbContext.FlatAssignments
                .Where(assignment => assignment.UserId == user.Id && assignment.IsActive)
                .Select(assignment => (int?)assignment.FlatId)
                .SingleOrDefaultAsync();

            var notices = await dbContext.Notices
                .Include(notice => notice.Targets)
                .Where(notice => notice.NoticeType == "General" ||
                    (assignedFlatId.HasValue && notice.Targets.Any(target => target.FlatId == assignedFlatId.Value)))
                .AsNoTracking()
                .OrderByDescending(notice => notice.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> SubmitComplaint()
        {
            var assignment = await GetActiveAssignment();
            if (assignment == null)
            {
                TempData["Error"] = "You must have an active flat assignment before submitting a complaint.";
                return RedirectToAction(nameof(Dashboard));
            }

            ViewBag.FlatNumber = assignment.Flat?.FlatNumber;
            return View(new SubmitComplaintViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> SubmitComplaint(SubmitComplaintViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            var assignment = user == null ? null : await GetActiveAssignment(user.Id);

            if (user == null)
            {
                return Challenge();
            }

            if (assignment == null)
            {
                ModelState.AddModelError(string.Empty, "You must have an active flat assignment before submitting a complaint.");
            }

            string? imagePath = null;
            if (model.Image != null)
            {
                var extension = Path.GetExtension(model.Image.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.Image), "Only JPG, JPEG, PNG, and WebP images are allowed.");
                }
                else if (model.Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.Image), "The image must be 5 MB or smaller.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FlatNumber = assignment?.Flat?.FlatNumber;
                return View(model);
            }

            if (model.Image != null)
            {
                var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "complaints");
                Directory.CreateDirectory(uploadDirectory);
                var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(model.Image.FileName).ToLowerInvariant()}";
                var filePath = Path.Combine(uploadDirectory, fileName);
                await using var stream = new FileStream(filePath, FileMode.CreateNew);
                await model.Image.CopyToAsync(stream);
                imagePath = $"/uploads/complaints/{fileName}";
            }

            dbContext.Complaints.Add(new Complaint
            {
                FlatId = assignment!.FlatId,
                UserId = user.Id,
                Category = model.Category.Trim(),
                Description = model.Description.Trim(),
                ImagePath = imagePath,
                ComplaintStatus = "Pending",
                CreatedAt = DateTime.Now
            });

            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Your complaint was submitted successfully.";
            return RedirectToAction(nameof(MyComplaints));
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> MyComplaints()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var assignment = await GetActiveAssignment(user.Id);
            var complaints = assignment == null
                ? new List<Complaint>()
                : await dbContext.Complaints
                    .Include(complaint => complaint.Flat)
                    .Where(complaint => complaint.UserId == user.Id && complaint.FlatId == assignment.FlatId)
                    .AsNoTracking()
                    .OrderByDescending(complaint => complaint.CreatedAt)
                    .ToListAsync();

            return View(new MyComplaintsViewModel
            {
                FlatNumber = assignment?.Flat?.FlatNumber,
                Complaints = complaints
            });
        }

        private async Task<FlatAssignment?> GetActiveAssignment(string? userId = null)
        {
            userId ??= userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await dbContext.FlatAssignments
                .Include(assignment => assignment.Flat)
                .AsNoTracking()
                .Where(assignment => assignment.UserId == userId && assignment.IsActive)
                .OrderByDescending(assignment => assignment.AssignmentDate)
                .FirstOrDefaultAsync();
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> Cctv(string? zone = null)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var assignment = await GetActiveAssignment(user.Id);
            ViewBag.Assignment = assignment;

            // Fetch active flat IDs for this resident (supporting single or multiple flat assignments)
            var userFlatIds = await dbContext.FlatAssignments
                .Where(a => a.UserId == user.Id && a.IsActive)
                .Select(a => a.FlatId)
                .ToListAsync();

            // Residents can ONLY view cameras that:
            // 1. Are Online (Status == "Online")
            // 2. AND have AccessType == "All" OR (AccessType == "SpecificFlats" and the resident's flat is granted access)
            var baseQuery = dbContext.CctvCameras
                .Include(c => c.FlatAccesses)
                .AsNoTracking()
                .Where(c => c.Status == "Online")
                .Where(c => c.AccessType == "All" ||
                            (c.AccessType == "SpecificFlats" && c.FlatAccesses.Any(fa => userFlatIds.Contains(fa.FlatId))));

            var availableZones = await baseQuery
                .Select(c => c.Location)
                .Distinct()
                .OrderBy(z => z)
                .ToListAsync();

            var query = baseQuery;
            if (!string.IsNullOrWhiteSpace(zone) && zone != "All")
            {
                query = query.Where(c => c.Location == zone);
            }

            var cameras = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var viewModel = new CctvDashboardViewModel
            {
                Cameras = cameras,
                SelectedZone = zone,
                AvailableZones = availableZones,
                TotalCameras = cameras.Count,
                OnlineCount = cameras.Count
            };

            return View(viewModel);
        }

        #region Gym & Health Club

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        public async Task<IActionResult> Gym()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var model = await gymService.GetResidentGymDashboardAsync(user.Id);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyGymMembership(GymApplicationRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["Error"] = !string.IsNullOrWhiteSpace(firstError) ? firstError : "Please fill in all required fields.";
                return RedirectToAction(nameof(Gym));
            }

            if (request.MemberPhoto == null || request.MemberPhoto.Length == 0)
            {
                TempData["Error"] = "A clear portrait photo is required when applying for a gym membership ID card.";
                return RedirectToAction(nameof(Gym));
            }

            if (request.MemberPhoto != null && request.MemberPhoto.Length > 0)
            {
                try
                {
                    var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "gym");
                    Directory.CreateDirectory(uploadDirectory);
                    var ext = Path.GetExtension(request.MemberPhoto.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadDirectory, fileName);
                    await using var stream = new FileStream(filePath, FileMode.CreateNew);
                    await request.MemberPhoto.CopyToAsync(stream);
                    request.MemberPhotoUrl = $"/uploads/gym/{fileName}";
                }
                catch (Exception)
                {
                    // Photo upload failed gracefully, continue with application
                }
            }

            var result = await gymService.ApplyForGymMembershipAsync(user.Id, request);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Gym));
        }

        [HttpPost]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGymMemberPhoto(GymUpdatePhotoRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (request.MemberPhoto == null || request.MemberPhoto.Length == 0)
            {
                TempData["Error"] = "Please select or capture a valid member photo.";
                return RedirectToAction(nameof(Gym));
            }

            try
            {
                var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "gym");
                Directory.CreateDirectory(uploadDirectory);
                var ext = Path.GetExtension(request.MemberPhoto.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDirectory, fileName);
                await using var stream = new FileStream(filePath, FileMode.CreateNew);
                await request.MemberPhoto.CopyToAsync(stream);
                var photoUrl = $"/uploads/gym/{fileName}";

                var result = await gymService.UpdateMemberPhotoAsync(user.Id, request.GymMembershipId, photoUrl, isManager: false);
                if (result.Success)
                {
                    TempData["Success"] = "Member photo updated successfully for official ID card.";
                }
                else
                {
                    TempData["Error"] = result.Message;
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to upload member photo. Please try again.";
            }

            return RedirectToAction(nameof(Gym));
        }

        [HttpPost]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestGymCancellation(GymCancellationRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await gymService.RequestGymCancellationAsync(user.Id, request);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Gym));
        }

        [HttpPost]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestGymRenewal(int id)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await gymService.RequestGymRenewalAsync(user.Id, id);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Gym));
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        public async Task<IActionResult> GymIdCard(int id)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var cardModel = await gymService.GetGymIdCardDetailsAsync(id, user.Id, isManager: false, baseUrl);
            if (cardModel == null)
            {
                TempData["Error"] = "Official gym membership ID card is only available after your membership is approved and active.";
                return RedirectToAction(nameof(Gym));
            }

            return View(cardModel);
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        public async Task<IActionResult> PendingGymRequestsCount()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { count = 0 });
            }

            var count = await gymService.GetResidentPendingGymCountAsync(user.Id);
            return Json(new { count });
        }

        #endregion

    }
}
