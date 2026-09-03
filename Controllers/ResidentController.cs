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

        public ResidentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment environment,
            IPaymentService paymentService,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.environment = environment;
            this.paymentService = paymentService;
            this.configuration = configuration;
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await userManager.GetUserAsync(User);
            var assignment = user == null ? null : await dbContext.FlatAssignments
                .Include(item => item.Flat)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == user.Id && item.IsActive);

            ViewBag.Assignment = assignment;

            if (user != null)
            {
                ViewBag.ResidentName = user.FullName;
                ViewBag.RequirePasswordChange = user.TemporaryPasswordStatus;
            }

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner")]
        public async Task<IActionResult> MyBills()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var assignment = await GetActiveAssignment(user.Id);
            if (assignment == null)
            {
                return View(new MyBillsViewModel
                {
                    ResidentName = user.FullName ?? "Resident",
                    FlatNumber = "N/A",
                    ResidentType = "Resident"
                });
            }

            var bills = await dbContext.Bills
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
                FlatNumber = assignment.Flat?.FlatNumber ?? "N/A",
                ResidentType = assignment.ResidentType,
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                DueAmount = b.DueAmount,
                Deadline = b.Deadline,
                BillStatus = b.BillStatus,
                CreatedAt = b.CreatedAt,
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

            var model = new MyBillsViewModel
            {
                ResidentName = user.FullName ?? "Resident",
                FlatNumber = assignment.Flat?.FlatNumber ?? "N/A",
                ResidentType = assignment.ResidentType,
                CurrentBills = currentBillVms,
                PaymentHistory = paymentHistoryVms
            };

            ViewBag.StripePublicKey = configuration["Stripe:PublishableKey"] ?? "pk_test_placeholder";
            ViewBag.StripePublishableKey = configuration["Stripe:PublishableKey"] ?? "pk_test_placeholder";

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Manager,Admin,Tenant,FlatOwner")]
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
            var assignment = user == null ? null : await GetActiveAssignment(user.Id);
            ViewBag.Assignment = assignment;

            var query = dbContext.CctvCameras
                .AsNoTracking()
                .Where(c => c.Status == "Online");

            var availableZones = await dbContext.CctvCameras
                .Where(c => c.Status == "Online")
                .Select(c => c.Location)
                .Distinct()
                .OrderBy(z => z)
                .ToListAsync();

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
    }
}
