using System.Diagnostics;
using System.Net.Sockets;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Services;
using ADHUNIK_BARI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADHUNIK_BARI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;
        private readonly IBillingService billingService;
        private readonly IPaymentService paymentService;
        private readonly IAIComplaintSummaryService aiService;

        public ManagerController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IBillingService billingService,
    IPaymentService paymentService,
    IAIComplaintSummaryService aiService)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.billingService = billingService;
            this.paymentService = paymentService;
            this.aiService = aiService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var activeFlatsCount = await dbContext.FlatAssignments.CountAsync(a => a.IsActive);
            var pendingComplaintsCount = await dbContext.Complaints.CountAsync(c => c.ComplaintStatus == "Pending");
            var totalBilled = await dbContext.Bills.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            var totalPaid = await dbContext.Bills.SumAsync(b => (decimal?)b.PaidAmount) ?? 0m;
            var collectionRate = totalBilled > 0 ? (int)Math.Round((totalPaid / totalBilled) * 100m) : 100;

            ViewBag.ActiveFlatsCount = activeFlatsCount;
            ViewBag.PendingComplaintsCount = pendingComplaintsCount;
            ViewBag.CollectionRate = collectionRate;
            ViewBag.TotalCollectedRevenue = totalPaid;

            ViewBag.RecentComplaints = await dbContext.Complaints
                .Include(complaint => complaint.Flat)
                .Include(complaint => complaint.User)
                .AsNoTracking()
                .OrderByDescending(complaint => complaint.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPayments = await dbContext.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                    .ThenInclude(b => b!.Assignment)
                        .ThenInclude(a => a!.Flat)
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Flats()
        {
            var flats = await dbContext.Flats
                .Include(flat => flat.Assignments.Where(assignment => assignment.IsActive))
                .AsNoTracking()
                .OrderBy(flat => flat.FloorNumber)
                .ThenBy(flat => flat.FlatNumber)
                .ToListAsync();

            return View(flats);
        }

        [HttpGet]
        public IActionResult CreateFlat()
        {
            return View(new CreateFlatViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlat(CreateFlatViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await dbContext.Flats.AnyAsync(flat => flat.FlatNumber == model.FlatNumber))
            {
                ModelState.AddModelError(nameof(model.FlatNumber), "A flat with this number already exists.");
                return View(model);
            }

            dbContext.Flats.Add(new Flat
            {
                FlatNumber = model.FlatNumber,
                FloorNumber = model.FloorNumber,
                MonthlyRent = model.MonthlyRent,
                FlatStatus = "Available",
                CreatedAt = DateTime.Now
            });

            await dbContext.SaveChangesAsync();
            TempData["Success"] = $"Flat {model.FlatNumber} created successfully with monthly rent ৳{model.MonthlyRent:N0}.";
            return RedirectToAction(nameof(Flats));
        }

        [HttpGet]
        public async Task<IActionResult> EditFlat(int id)
        {
            var flat = await dbContext.Flats.FindAsync(id);
            if (flat == null)
            {
                return NotFound();
            }

            return View(new EditFlatViewModel
            {
                FlatId = flat.FlatId,
                FlatNumber = flat.FlatNumber,
                FloorNumber = flat.FloorNumber,
                MonthlyRent = flat.MonthlyRent,
                FlatStatus = flat.FlatStatus
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFlat(EditFlatViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var flat = await dbContext.Flats.FindAsync(model.FlatId);
            if (flat == null)
            {
                return NotFound();
            }

            flat.FlatNumber = model.FlatNumber;
            flat.FloorNumber = model.FloorNumber;
            flat.MonthlyRent = model.MonthlyRent;
            flat.FlatStatus = model.FlatStatus;

            await dbContext.SaveChangesAsync();
            TempData["Success"] = $"Flat {flat.FlatNumber} updated successfully with monthly rent ৳{flat.MonthlyRent:N0}.";
            return RedirectToAction(nameof(Flats));
        }

        [HttpGet]
        public async Task<IActionResult> AssignFlat()
        {
            return View(await BuildAssignFlatViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignFlat(AssignFlatViewModel model)
        {
            var flat = await dbContext.Flats.FindAsync(model.FlatId);
            var resident = await userManager.FindByIdAsync(model.UserId);

            if (flat == null || resident == null ||
                !(await userManager.IsInRoleAsync(resident, "Tenant") ||
                  await userManager.IsInRoleAsync(resident, "FlatOwner")))
            {
                ModelState.AddModelError(string.Empty, "Select a valid flat and resident.");
            }
            else if (flat.FlatStatus == "Occupied" ||
                     await dbContext.FlatAssignments.AnyAsync(assignment =>
                         assignment.FlatId == model.FlatId && assignment.IsActive))
            {
                ModelState.AddModelError(string.Empty, "This flat is already occupied.");
            }
            else if (await dbContext.FlatAssignments.AnyAsync(assignment =>
                         assignment.UserId == model.UserId && assignment.IsActive))
            {
                ModelState.AddModelError(string.Empty, "This resident already has an active flat assignment.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableFlats = await dbContext.Flats
                    .Where(item => item.FlatStatus == "Available" &&
                        !dbContext.FlatAssignments.Any(assignment => assignment.FlatId == item.FlatId && assignment.IsActive))
                    .OrderBy(item => item.FlatNumber).ToListAsync();
                model.Residents = await GetResidents();
                return View(model);
            }

            dbContext.FlatAssignments.Add(new FlatAssignment
            {
                FlatId = flat!.FlatId,
                UserId = resident!.Id,
                ResidentType = model.ResidentType,
                AssignmentDate = DateTime.Now,
                IsActive = true
            });
            flat.FlatStatus = "Occupied";
            await dbContext.SaveChangesAsync();

            TempData["Success"] = "Resident assigned to flat successfully.";
            return RedirectToAction(nameof(Flats));
        }

        private async Task<AssignFlatViewModel> BuildAssignFlatViewModel()
        {
            return new AssignFlatViewModel
            {
                AvailableFlats = await dbContext.Flats
                    .Where(flat => flat.FlatStatus == "Available" &&
                        !dbContext.FlatAssignments.Any(assignment => assignment.FlatId == flat.FlatId && assignment.IsActive))
                    .OrderBy(flat => flat.FlatNumber).ToListAsync(),
                Residents = await GetResidents()
            };
        }

        private async Task<IList<ApplicationUser>> GetResidents()
        {
            var tenants = await userManager.GetUsersInRoleAsync("Tenant");
            var owners = await userManager.GetUsersInRoleAsync("FlatOwner");
            return tenants.Concat(owners).GroupBy(user => user.Id).Select(group => group.First()).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Notices()
        {
            var notices = await dbContext.Notices
                .Include(notice => notice.Targets)
                    .ThenInclude(target => target.Flat)
                .AsNoTracking()
                .OrderByDescending(notice => notice.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        [HttpGet]
        public async Task<IActionResult> CreateNotice()
        {
            return View(await PopulateNoticeModel(new NoticeViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotice(NoticeViewModel model)
        {
            await ValidateNoticeModel(model);
            if (!ModelState.IsValid)
            {
                return View(await PopulateNoticeModel(model));
            }

            var manager = await userManager.GetUserAsync(User);
            if (manager == null)
            {
                return Challenge();
            }

            var notice = new Notice
            {
                CreatedByUserId = manager.Id,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                NoticeType = model.NoticeType,
                CreatedAt = DateTime.Now,
                Targets = model.NoticeType == "General"
                    ? new List<NoticeTarget>()
                    : model.TargetFlatIds.Distinct().Select(flatId => new NoticeTarget { FlatId = flatId }).ToList()
            };

            dbContext.Notices.Add(notice);
            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Notice published successfully.";
            return RedirectToAction(nameof(Notices));
        }

        [HttpGet]
        public async Task<IActionResult> EditNotice(int id)
        {
            var notice = await dbContext.Notices
                .Include(item => item.Targets)
                .SingleOrDefaultAsync(item => item.NoticeId == id);
            if (notice == null)
            {
                return NotFound();
            }

            return View(await PopulateNoticeModel(new NoticeViewModel
            {
                NoticeId = notice.NoticeId,
                Title = notice.Title,
                Description = notice.Description,
                NoticeType = notice.NoticeType,
                TargetFlatIds = notice.Targets
    .Select(target => target.FlatId)
    .Where(id => id.HasValue)
    .Select(id => id!.Value)
    .ToList()
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotice(NoticeViewModel model)
        {
            var notice = await dbContext.Notices
                .Include(item => item.Targets)
                .SingleOrDefaultAsync(item => item.NoticeId == model.NoticeId);
            if (notice == null)
            {
                return NotFound();
            }

            await ValidateNoticeModel(model);
            if (!ModelState.IsValid)
            {
                return View(await PopulateNoticeModel(model));
            }

            notice.Title = model.Title.Trim();
            notice.Description = model.Description.Trim();
            notice.NoticeType = model.NoticeType;
            dbContext.NoticeTargets.RemoveRange(notice.Targets);
            notice.Targets = model.NoticeType == "General"
                ? new List<NoticeTarget>()
                : model.TargetFlatIds.Distinct().Select(flatId => new NoticeTarget { NoticeId = notice.NoticeId, FlatId = flatId }).ToList();

            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Notice updated successfully.";
            return RedirectToAction(nameof(Notices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotice(int id)
        {
            var notice = await dbContext.Notices.FindAsync(id);
            if (notice == null)
            {
                return NotFound();
            }

            dbContext.Notices.Remove(notice);
            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Notice deleted successfully.";
            return RedirectToAction(nameof(Notices));
        }

        private async Task ValidateNoticeModel(NoticeViewModel model)
        {
            if (model.NoticeType != "General" && model.NoticeType != "SpecificFlats")
            {
                ModelState.AddModelError(nameof(model.NoticeType), "Select a valid notice type.");
                return;
            }

            if (model.NoticeType == "SpecificFlats")
            {
                if (model.TargetFlatIds.Count == 0)
                {
                    ModelState.AddModelError(nameof(model.TargetFlatIds), "Select at least one flat.");
                    return;
                }

                var validFlatCount = await dbContext.Flats.CountAsync(flat => model.TargetFlatIds.Contains(flat.FlatId));
                if (validFlatCount != model.TargetFlatIds.Distinct().Count())
                {
                    ModelState.AddModelError(nameof(model.TargetFlatIds), "One or more selected flats are invalid.");
                }
            }
        }

        private async Task<NoticeViewModel> PopulateNoticeModel(NoticeViewModel model)
        {
            model.Flats = await dbContext.Flats
                .AsNoTracking()
                .OrderBy(flat => flat.FloorNumber)
                .ThenBy(flat => flat.FlatNumber)
                .ToListAsync();
            return model;
        }

        [HttpGet]
        public async Task<IActionResult> Complaints(string? flatNumber, string? status)
        {
            var query = dbContext.Complaints
                .Include(complaint => complaint.Flat)
                .Include(complaint => complaint.User)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(flatNumber))
            {
                var search = flatNumber.Trim();
                query = query.Where(complaint => complaint.Flat != null && complaint.Flat.FlatNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && IsComplaintStatus(status))
            {
                query = query.Where(complaint => complaint.ComplaintStatus == status);
            }

            var model = new ManagerComplaintListViewModel
            {
                FlatNumber = flatNumber,
                Status = status,
                Complaints = await query
                    .OrderByDescending(complaint => complaint.CreatedAt)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ComplaintDetails(int id)
        {
            var complaint = await dbContext.Complaints
                .Include(item => item.Flat)
                .Include(item => item.User)
                .Include(item => item.ResolvedByUser)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.ComplaintId == id);

            if (complaint == null)
            {
                return NotFound();
            }

            return View(complaint);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComplaint(UpdateComplaintViewModel model)
        {
            var complaint = await dbContext.Complaints
                .SingleOrDefaultAsync(item => item.ComplaintId == model.ComplaintId);

            if (complaint == null)
            {
                return NotFound();
            }

            if (!IsComplaintStatus(model.ComplaintStatus))
            {
                ModelState.AddModelError(nameof(model.ComplaintStatus), "Select a valid status.");
                return RedirectToAction(nameof(ComplaintDetails), new { id = model.ComplaintId });
            }

            var manager = await userManager.GetUserAsync(User);
            if (manager == null)
            {
                return Challenge();
            }

            complaint.ComplaintStatus = model.ComplaintStatus;
            complaint.ManagerNote = string.IsNullOrWhiteSpace(model.ManagerNote) ? null : model.ManagerNote.Trim();

            if (model.ComplaintStatus == "Resolved" || model.ComplaintStatus == "Closed")
            {
                complaint.ResolvedByUserId = manager.Id;
                complaint.ResolvedAt = DateTime.Now;
            }
            else
            {
                complaint.ResolvedByUserId = null;
                complaint.ResolvedAt = null;
            }

            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Complaint status updated successfully.";
            return RedirectToAction(nameof(ComplaintDetails), new { id = model.ComplaintId });
        }

        [HttpGet]
        public IActionResult CreateResident()
        {
            return View(new CreateResidentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResident(CreateResidentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                PhoneNumber = model.Phone,
                Phone = model.Phone,
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                TemporaryPasswordStatus = true,
                AccountStatus = "Active",
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, model.TemporaryPassword);

            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, model.ResidentType);
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                TempData["Success"] = "Resident account created successfully";
                return RedirectToAction("Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ==========================================
        // BILLING & INVOICE MANAGEMENT ACTIONS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Bills()
        {
            var activeAssignments = await dbContext.FlatAssignments
                .Include(a => a.Flat)
                .Include(a => a.User)
                .Where(a => a.IsActive)
                .AsNoTracking()
                .OrderBy(a => a.Flat != null ? a.Flat.FlatNumber : "")
                .ToListAsync();

            var overviewList = await billingService.GetBillingOverviewForManagerAsync();

            var recentBills = await dbContext.Bills
                .Include(b => b.BillItems)
                .Include(b => b.Assignment)
                    .ThenInclude(a => a!.Flat)
                .Include(b => b.Assignment)
                    .ThenInclude(a => a!.User)
                .AsNoTracking()
                .OrderByDescending(b => b.BillYear)
                .ThenByDescending(b => b.BillMonth)
                .ThenByDescending(b => b.CreatedAt)
                .Take(50)
                .ToListAsync();

            var recentPayments = await dbContext.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                    .ThenInclude(b => b!.Assignment)
                        .ThenInclude(a => a!.Flat)
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .Take(50)
                .ToListAsync();

            var totalBilled = await dbContext.Bills.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            var totalCollected = await dbContext.Bills.SumAsync(b => (decimal?)b.PaidAmount) ?? 0m;
            var totalDue = await dbContext.Bills.SumAsync(b => (decimal?)b.DueAmount) ?? 0m;
            var totalUnpaidBills = await dbContext.Bills.CountAsync(b => b.BillStatus != "Paid");

            var model = new ManagerBillsPageViewModel
            {
                GenerateRequest = new GenerateMonthlyBillsRequest
                {
                    Month = DateTime.Now.Month,
                    Year = DateTime.Now.Year
                },
                ActiveAssignments = activeAssignments,
                OverviewList = overviewList,
                RecentBills = recentBills,
                RecentPayments = recentPayments,
                TotalBilledAmount = totalBilled,
                TotalCollectedAmount = totalCollected,
                TotalDueAmount = totalDue,
                TotalUnpaidBills = totalUnpaidBills,
                TotalActiveAssignments = activeAssignments.Count
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyBills(ManagerBillsPageViewModel model)
        {
            var req = model.GenerateRequest ?? new GenerateMonthlyBillsRequest();
            try
            {
                var count = await billingService.GenerateMonthlyBillsAsync(
                    req.Month,
                    req.Year,
                    req.ServiceCharge,
                    req.GasCharge,
                    req.WaterCharge,
                    req.ElectricityCharge,
                    req.MaintenanceCharge,
                    req.TargetAssignmentId > 0 ? req.TargetAssignmentId : null,
                    req.MonthlyRent
                );

                if (count > 0)
                {
                    TempData["Success"] = $"Successfully generated and issued {count} monthly invoice(s) for {new DateTime(req.Year, req.Month, 1):MMMM yyyy}.";
                }
                else
                {
                    TempData["Error"] = $"No new bills were generated. Bills may already exist for {new DateTime(req.Year, req.Month, 1):MMMM yyyy} or no active resident matches.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to generate bills: {ex.Message}";
            }

            return RedirectToAction(nameof(Bills));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBill(int billId)
        {
            var bill = await dbContext.Bills
                .Include(b => b.Payments)
                .Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction(nameof(Bills));
            }

            if (bill.PaidAmount > 0 || bill.Payments.Any())
            {
                TempData["Error"] = "Cannot delete a bill with recorded payment transactions for audit compliance.";
                return RedirectToAction(nameof(Bills));
            }

            dbContext.BillItems.RemoveRange(bill.BillItems);
            dbContext.Bills.Remove(bill);
            await dbContext.SaveChangesAsync();

            TempData["Success"] = $"Bill #{billId} was removed successfully.";
            return RedirectToAction(nameof(Bills));
        }

        [HttpGet]
        public IActionResult ComplaintAISummary()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> ComplaintAISummary(int months)
        {

            var startDate =
                DateTime.Now.AddMonths(-months);



            var complaints =
                await dbContext.Complaints
                .Where(c => c.CreatedAt >= startDate)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();



            if (!complaints.Any())
            {
                ViewBag.Error =
                "No complaints found for this period.";

                return View();
            }



            var complaintText =
                string.Join("\n\n",
                complaints.Select(c =>
                $@"
Category:
{c.Category}

Status:
{c.ComplaintStatus}

Complaint:
{c.Description}

Date:
{c.CreatedAt}
"
                ));



            AIComplaintReport report;

            try
            {
                report =
                    await aiService.GenerateComplaintSummary(
                        complaintText);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "AI service error: " + ex.Message;

                return View();
            }



            var model =
            new ComplaintAISummaryViewModel
            {
                Months = months,
                TotalComplaints = complaints.Count,
                Report = report
            };


            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id, [FromQuery] int? paymentId)
        {
            var targetPaymentId = id > 0 ? id : paymentId.GetValueOrDefault();
            if (targetPaymentId <= 0)
            {
                return NotFound("Invalid Receipt/Payment ID.");
            }

            var user = await userManager.GetUserAsync(User);
            var userId = user?.Id ?? "";

            var receipt = await paymentService.GetReceiptDetailsAsync(targetPaymentId, userId, isManagerOrAdmin: true);
            if (receipt == null)
            {
                return NotFound("Receipt not found or access denied.");
            }

            return View("~/Views/Resident/Receipt.cshtml", receipt);
        }

        private static bool IsComplaintStatus(string status)
        {
            return status is "Pending" or "In Progress" or "Resolved" or "Closed";
        }
        // ==========================================
        // PARKING MANAGEMENT
        // ==========================================


        [HttpGet]
        public async Task<IActionResult> Parking()
        {
            var parkingSpots = await dbContext.ParkingSpots
                .Include(p => p.Flat)
                .AsNoTracking()
                .OrderBy(p => p.SpotNumber)
                .ToListAsync();


            return View(parkingSpots);
        }




        // CREATE PARKING PAGE

        [HttpGet]
        public IActionResult CreateParking()
        {
            return View();
        }





        // SAVE PARKING SPOT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParking(ParkingSpot model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }



            if (await dbContext.ParkingSpots
                .AnyAsync(p => p.SpotNumber == model.SpotNumber))
            {
                ModelState.AddModelError(
                    nameof(model.SpotNumber),
                    "Parking spot number already exists."
                );

                return View(model);
            }



            model.IsAvailable = true;
            model.CreatedAt = DateTime.Now;



            dbContext.ParkingSpots.Add(model);


            await dbContext.SaveChangesAsync();



            TempData["Success"] =
                "Parking spot created successfully.";


            return RedirectToAction(nameof(Parking));

        }





        // ASSIGN PARKING PAGE

        [HttpGet]
        public async Task<IActionResult> AssignParking(int id)
        {

            var parking =
                await dbContext.ParkingSpots
                .Include(p => p.Flat)
                .FirstOrDefaultAsync(
                    p => p.ParkingSpotId == id);



            if (parking == null)
            {
                return NotFound();
            }



            ViewBag.Flats =
                await dbContext.Flats
                .Where(f =>
                    f.FlatStatus == "Occupied")
                .OrderBy(f => f.FlatNumber)
                .ToListAsync();



            return View(parking);

        }






        // SAVE PARKING ASSIGNMENT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignParking(
            int ParkingSpotId,
            int FlatId)
        {


            var parking =
                await dbContext.ParkingSpots
                .FirstOrDefaultAsync(
                    p => p.ParkingSpotId == ParkingSpotId);



            if (parking == null)
            {
                return NotFound();
            }



            var flat =
                await dbContext.Flats
                .FirstOrDefaultAsync(
                    f => f.FlatId == FlatId);



            if (flat == null)
            {
                TempData["Error"] =
                    "Invalid flat selected.";

                return RedirectToAction(nameof(Parking));
            }



            parking.FlatId = flat.FlatId;

            parking.IsAvailable = false;



            await dbContext.SaveChangesAsync();



            TempData["Success"] =
                $"Parking {parking.SpotNumber} assigned successfully.";

            return RedirectToAction(nameof(Parking));
        }

        // ==========================================
        // CCTV SURVEILLANCE MANAGEMENT (CLEAN & DIRECT)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Cctv(string? zone = null)
        {
            var query = dbContext.CctvCameras.AsNoTracking();

            var availableZones = await dbContext.CctvCameras
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

            var totalCameras = await dbContext.CctvCameras.CountAsync();
            var onlineCount = await dbContext.CctvCameras.CountAsync(c => c.Status == "Online");
            var offlineCount = totalCameras - onlineCount;

            var viewModel = new CctvDashboardViewModel
            {
                Cameras = cameras,
                SelectedZone = zone,
                AvailableZones = availableZones,
                TotalCameras = totalCameras,
                OnlineCount = onlineCount,
                OfflineCount = offlineCount
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateCctv()
        {
            var model = new CctvCamera
            {
                Location = "Main Gate",
                Status = "Online"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCctv(CctvCamera model)
        {
            if (string.IsNullOrWhiteSpace(model.CameraName))
            {
                ModelState.AddModelError(nameof(model.CameraName), "Camera Name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.StreamUrl))
            {
                ModelState.AddModelError(nameof(model.StreamUrl), "Stream URL is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.CameraName = model.CameraName.Trim();
            model.Location = string.IsNullOrWhiteSpace(model.Location) ? "Main Gate" : model.Location.Trim();
            // Direct URL as input by user - NO auto format, NO appending!
            model.StreamUrl = model.StreamUrl.Trim();
            model.Status = string.IsNullOrWhiteSpace(model.Status) ? "Online" : model.Status.Trim();
            model.CreatedAt = DateTime.UtcNow;

            await dbContext.CctvCameras.AddAsync(model);
            await dbContext.SaveChangesAsync();

            TempData["Success"] = $"Camera '{model.CameraName}' added successfully.";
            return RedirectToAction(nameof(Cctv));
        }

        [HttpGet]
        public async Task<IActionResult> EditCctv(int id)
        {
            var camera = await dbContext.CctvCameras.FindAsync(id);
            if (camera == null)
            {
                TempData["Error"] = "Camera not found.";
                return RedirectToAction(nameof(Cctv));
            }

            return View(camera);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCctv(int id, CctvCamera model)
        {
            if (id != model.CameraId)
            {
                return NotFound();
            }

            var camera = await dbContext.CctvCameras.FindAsync(id);
            if (camera == null)
            {
                TempData["Error"] = "Camera not found.";
                return RedirectToAction(nameof(Cctv));
            }

            if (string.IsNullOrWhiteSpace(model.CameraName))
            {
                ModelState.AddModelError(nameof(model.CameraName), "Camera Name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.StreamUrl))
            {
                ModelState.AddModelError(nameof(model.StreamUrl), "Stream URL is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            camera.CameraName = model.CameraName.Trim();
            camera.Location = string.IsNullOrWhiteSpace(model.Location) ? "Main Gate" : model.Location.Trim();
            // Direct URL as input by user - NO auto format, NO appending!
            camera.StreamUrl = model.StreamUrl.Trim();
            camera.Status = string.IsNullOrWhiteSpace(model.Status) ? "Online" : model.Status.Trim();

            await dbContext.SaveChangesAsync();

            TempData["Success"] = $"Camera '{camera.CameraName}' updated successfully.";
            return RedirectToAction(nameof(Cctv));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCctvStatus(int id)
        {
            var camera = await dbContext.CctvCameras.FindAsync(id);
            if (camera == null)
            {
                return Json(new { success = false, message = "Camera not found." });
            }

            camera.Status = (camera.Status == "Online") ? "Offline" : "Online";
            await dbContext.SaveChangesAsync();

            return Json(new { success = true, status = camera.Status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCctv(int id)
        {
            var camera = await dbContext.CctvCameras.FindAsync(id);
            if (camera == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Camera not found." });
                }
                TempData["Error"] = "Camera not found.";
                return RedirectToAction(nameof(Cctv));
            }

            var cameraName = camera.CameraName;
            dbContext.CctvCameras.Remove(camera);
            await dbContext.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                var total = await dbContext.CctvCameras.CountAsync();
                var online = await dbContext.CctvCameras.CountAsync(c => c.Status == "Online");
                var offline = total - online;
                return Json(new
                {
                    success = true,
                    message = $"Camera '{cameraName}' has been removed.",
                    totalCameras = total,
                    onlineCount = online,
                    offlineCount = offline
                });
            }

            TempData["Success"] = $"Camera '{cameraName}' has been removed.";
            return RedirectToAction(nameof(Cctv));
        }

    }
}