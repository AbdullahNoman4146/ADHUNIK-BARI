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
        private readonly IGymService gymService;

        public ManagerController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IBillingService billingService,
            IPaymentService paymentService,
            IAIComplaintSummaryService aiService,
            IGymService gymService)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.billingService = billingService;
            this.paymentService = paymentService;
            this.aiService = aiService;
            this.gymService = gymService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var activeFlatsCount = await dbContext.FlatAssignments.CountAsync(a => a.IsActive);
            var pendingComplaintsCount = await dbContext.Complaints.CountAsync(c => c.ComplaintStatus == "Pending");
            var totalBilled = await dbContext.Bills.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            var totalPaid = await dbContext.Bills.SumAsync(b => (decimal?)b.PaidAmount) ?? 0m;

            // Include collected gym membership fees (each ৳500 paid pass) in revenue
            var gymPaidRevenue = await dbContext.GymMemberships
                .Where(m => m.IsFeePaid)
                .SumAsync(m => (decimal?)(m.PaySlipAmount ?? m.MonthlyFee)) ?? 0m;

            var totalCollected = totalPaid + gymPaidRevenue;
            var totalBilledWithGym = totalBilled + gymPaidRevenue;
            var collectionRate = totalBilledWithGym > 0 ? (int)Math.Round((totalCollected / totalBilledWithGym) * 100m) : 100;

            ViewBag.ActiveFlatsCount = activeFlatsCount;
            ViewBag.PendingComplaintsCount = pendingComplaintsCount;
            ViewBag.CollectionRate = collectionRate;
            ViewBag.TotalCollectedRevenue = totalCollected;
            ViewBag.GymCollectedRevenue = gymPaidRevenue;

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

            // Include every collected gym membership fee (৳500 each) in collected revenue
            var gymCollected = await dbContext.GymMemberships
                .Where(m => m.IsFeePaid)
                .SumAsync(m => (decimal?)(m.PaySlipAmount ?? m.MonthlyFee)) ?? 0m;

            totalCollected += gymCollected;
            totalBilled += gymCollected;

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
                TotalGymFeesCollected = gymCollected,
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
                    req.MonthlyRent,
                    req.OtherCharge,
                    req.OtherDescription
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
        public async Task<IActionResult> AppointOtherCharge(int billId, decimal amount, string? description, bool applyToAllMonthBills = false)
        {
            try
            {
                if (amount < 0)
                {
                    TempData["Error"] = "Charge amount cannot be negative.";
                    return RedirectToAction(nameof(Bills));
                }

                if (applyToAllMonthBills)
                {
                    var targetBill = await dbContext.Bills.FindAsync(billId);
                    if (targetBill == null)
                    {
                        TempData["Error"] = "Target invoice not found.";
                        return RedirectToAction(nameof(Bills));
                    }

                    var count = await billingService.AppointOtherChargeForMonthAsync(targetBill.BillMonth, targetBill.BillYear, amount, description);
                    TempData["Success"] = $"Successfully appointed Other charge of ৳{amount:N0} to {count} invoice(s) for {new DateTime(targetBill.BillYear, targetBill.BillMonth, 1):MMMM yyyy}.";
                }
                else
                {
                    var success = await billingService.AppointOtherChargeAsync(billId, amount, description);
                    if (success)
                    {
                        TempData["Success"] = $"Successfully appointed Other charge (৳{amount:N0}) for Bill #{billId}.";
                    }
                    else
                    {
                        TempData["Error"] = $"Bill #{billId} not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to appoint Other charge: {ex.Message}";
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


        // ==========================================
        // PARKING MANAGEMENT & BASEMENT FLOOR PLAN
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Parking(int? floorId = null, string? search = null, string? status = null)
        {
            // Ensure at least one floor exists (e.g. Basement 1)
            var floors = await dbContext.ParkingFloors
                .OrderBy(f => f.FloorCode)
                .ToListAsync();

            if (!floors.Any())
            {
                var defaultFloor = new ParkingFloor
                {
                    FloorName = "Basement 1",
                    FloorCode = "B1",
                    Capacity = 20,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.ParkingFloors.Add(defaultFloor);
                await dbContext.SaveChangesAsync();

                // Generate default 20 spots for B1
                var initialSpots = new List<ParkingSpot>();
                for (int i = 1; i <= 20; i++)
                {
                    initialSpots.Add(new ParkingSpot
                    {
                        ParkingFloorId = defaultFloor.ParkingFloorId,
                        SpotNumber = $"B1-{i:D2}",
                        Status = "Available",
                        IsAvailable = true,
                        ParkingType = "Car",
                        ParkingFee = 1500m,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                dbContext.ParkingSpots.AddRange(initialSpots);
                dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
                {
                    Action = "Floor Created",
                    Details = "System initialized Basement 1 with 20 parking spaces.",
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();

                floors = await dbContext.ParkingFloors.OrderBy(f => f.FloorCode).ToListAsync();
            }

            // Assign any orphaned spots to first floor
            var orphanedSpots = await dbContext.ParkingSpots.Where(s => s.ParkingFloorId == null).ToListAsync();
            if (orphanedSpots.Any())
            {
                var firstFloorId = floors.First().ParkingFloorId;
                foreach (var s in orphanedSpots)
                {
                    s.ParkingFloorId = firstFloorId;
                }
                await dbContext.SaveChangesAsync();
            }

            // Selected active floor
            var currentFloor = floorId.HasValue
                ? floors.FirstOrDefault(f => f.ParkingFloorId == floorId.Value) ?? floors.First()
                : floors.First();

            // Load all spots for current floor
            var floorSpots = await dbContext.ParkingSpots
                .Where(s => s.ParkingFloorId == currentFloor.ParkingFloorId)
                .Include(s => s.Flat)
                .OrderBy(s => s.SpotNumber)
                .ToListAsync();

            // Check if floor has missing spots up to capacity; auto-generate missing tiles if needed
            if (floorSpots.Count < currentFloor.Capacity)
            {
                int nextIndex = floorSpots.Count + 1;
                var missingSpots = new List<ParkingSpot>();
                for (int i = nextIndex; i <= currentFloor.Capacity; i++)
                {
                    var spotNum = $"{currentFloor.FloorCode}-{i:D2}";
                    if (!floorSpots.Any(s => s.SpotNumber.Equals(spotNum, StringComparison.OrdinalIgnoreCase)))
                    {
                        missingSpots.Add(new ParkingSpot
                        {
                            ParkingFloorId = currentFloor.ParkingFloorId,
                            SpotNumber = spotNum,
                            Status = "Available",
                            IsAvailable = true,
                            ParkingType = "Car",
                            ParkingFee = 1500m,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                if (missingSpots.Any())
                {
                    dbContext.ParkingSpots.AddRange(missingSpots);
                    await dbContext.SaveChangesAsync();
                    floorSpots = await dbContext.ParkingSpots
                        .Where(s => s.ParkingFloorId == currentFloor.ParkingFloorId)
                        .Include(s => s.Flat)
                        .OrderBy(s => s.SpotNumber)
                        .ToListAsync();
                }
            }

            // Load active flat assignments for occupied flats to display resident names and compute payments
            var activeAssignments = await dbContext.FlatAssignments
                .Include(a => a.User)
                .Include(a => a.Flat)
                .Where(a => a.IsActive)
                .ToListAsync();

            var assignmentMap = activeAssignments.ToDictionary(a => a.FlatId, a => a);

            // Fetch parking bill items to accurately determine payment status (Paid vs Due)
            var parkingBillItems = await dbContext.BillItems
                .Include(bi => bi.Bill)
                .Where(bi => bi.ItemType == BillItemTypes.Parking && bi.Bill != null)
                .OrderByDescending(bi => bi.CreatedAt)
                .ToListAsync();

            // Map each spot to ParkingSpotTileViewModel
            var spotTiles = floorSpots.Select(s =>
            {
                string residentName = string.Empty;
                string residentType = string.Empty;
                int? flatFloor = null;
                FlatAssignment? assign = null;

                if (s.FlatId.HasValue && assignmentMap.TryGetValue(s.FlatId.Value, out assign))
                {
                    residentName = assign.User?.FullName ?? string.Empty;
                    residentType = assign.ResidentType ?? string.Empty;
                    flatFloor = assign.Flat?.FloorNumber;
                }

                // Determine payment status:
                string paymentStatus = "Due";
                if (s.FlatId.HasValue)
                {
                    var matchingBillItem = parkingBillItems
                        .FirstOrDefault(bi => bi.Bill?.Assignment != null && bi.Bill.Assignment.FlatId == s.FlatId.Value);

                    if (matchingBillItem != null && (matchingBillItem.PaymentStatus == "Paid" || matchingBillItem.Bill?.BillStatus == "Paid"))
                    {
                        paymentStatus = "Paid";
                    }
                    else if (assign != null)
                    {
                        var latestBill = dbContext.Bills
                            .Where(b => b.AssignmentId == assign.AssignmentId)
                            .OrderByDescending(b => b.BillYear)
                            .ThenByDescending(b => b.BillMonth)
                            .FirstOrDefault();

                        if (latestBill != null && latestBill.BillStatus == "Paid")
                        {
                            paymentStatus = "Paid";
                        }
                    }
                }

                return new ParkingSpotTileViewModel
                {
                    ParkingSpotId = s.ParkingSpotId,
                    ParkingFloorId = s.ParkingFloorId,
                    FloorName = currentFloor.FloorName,
                    SpotNumber = s.SpotNumber,
                    Status = s.Status,
                    VehicleType = s.ParkingType ?? "Car",
                    MonthlyFee = s.ParkingFee,
                    ListingPrice = s.ListingPrice,
                    ListingNotes = s.ListingNotes,
                    FlatId = s.FlatId,
                    FlatNumber = s.Flat?.FlatNumber,
                    FlatFloor = flatFloor,
                    ResidentName = residentName,
                    ResidentType = residentType,
                    PaymentStatus = paymentStatus
                };
            }).ToList();

            // Global stats across all floors
            var allSpots = await dbContext.ParkingSpots.AsNoTracking().ToListAsync();
            int totalCap = floors.Sum(f => f.Capacity);
            if (totalCap < allSpots.Count) totalCap = allSpots.Count;

            int availCount = allSpots.Count(s => s.Status == "Available");
            int assignedCount = allSpots.Count(s => s.Status == "Assigned");
            int forSaleCount = allSpots.Count(s => s.Status == "ForSale");
            int toLetCount = allSpots.Count(s => s.Status == "ToLet");

            // Calculate revenue summary
            decimal revenueCollected = parkingBillItems
                .Where(bi => bi.PaymentStatus == "Paid" || (bi.Bill != null && bi.Bill.BillStatus == "Paid"))
                .Sum(bi => bi.Amount);

            decimal revenueDue = spotTiles
                .Where(st => st.Status == "Assigned" && st.PaymentStatus == "Due")
                .Sum(st => st.MonthlyFee);

            if (revenueCollected == 0 && assignedCount > 0)
            {
                revenueDue = allSpots.Where(s => s.Status == "Assigned").Sum(s => s.ParkingFee);
            }

            // Occupied flats for searchable typeahead dropdown
            var occupiedFlats = await dbContext.Flats
                .Where(f => f.FlatStatus == "Occupied")
                .OrderBy(f => f.FlatNumber)
                .Select(f => new FlatLookupItem
                {
                    FlatId = f.FlatId,
                    FlatNumber = f.FlatNumber,
                    FloorNumber = f.FloorNumber,
                    ResidentName = f.Assignments.Where(a => a.IsActive).Select(a => a.User.FullName).FirstOrDefault() ?? "Resident",
                    ResidentType = f.Assignments.Where(a => a.IsActive).Select(a => a.ResidentType).FirstOrDefault() ?? "Tenant"
                })
                .ToListAsync();

            // Recent activity logs
            var recentLogs = await dbContext.ParkingActivityLogs
                .Include(l => l.ParkingSpot)
                .OrderByDescending(l => l.CreatedAt)
                .Take(20)
                .Select(l => new ParkingActivityLogViewModel
                {
                    ActivityId = l.ActivityId,
                    SpotNumber = l.ParkingSpot != null ? l.ParkingSpot.SpotNumber : "—",
                    Action = l.Action,
                    Details = l.Details,
                    CreatedBy = l.CreatedBy,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            var viewModel = new ParkingFloorPlanViewModel
            {
                SelectedFloorId = currentFloor.ParkingFloorId,
                CurrentFloor = currentFloor,
                Floors = floors,
                Spots = spotTiles,
                TotalCapacity = totalCap,
                AvailableCount = availCount,
                AssignedCount = assignedCount,
                ForSaleCount = forSaleCount,
                ToLetCount = toLetCount,
                RevenueCollected = revenueCollected,
                RevenueDue = revenueDue,
                SearchQuery = search,
                StatusFilter = status,
                OccupiedFlats = occupiedFlats,
                RecentActivities = recentLogs
            };

            return View(viewModel);
        }

        // CREATE NEW PARKING FLOOR WITH AUTO-GENERATED SPOTS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFloor(CreateParkingFloorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required floor details correctly.";
                return RedirectToAction(nameof(Parking));
            }

            var cleanCode = model.FloorCode.Trim().ToUpperInvariant();
            if (await dbContext.ParkingFloors.AnyAsync(f => f.FloorCode.ToUpper() == cleanCode))
            {
                TempData["Error"] = $"A parking floor with code '{cleanCode}' already exists.";
                return RedirectToAction(nameof(Parking));
            }

            var floor = new ParkingFloor
            {
                FloorName = model.FloorName.Trim(),
                FloorCode = cleanCode,
                Capacity = model.Capacity,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.ParkingFloors.Add(floor);
            await dbContext.SaveChangesAsync();

            var spots = new List<ParkingSpot>();
            for (int i = 1; i <= model.Capacity; i++)
            {
                spots.Add(new ParkingSpot
                {
                    ParkingFloorId = floor.ParkingFloorId,
                    SpotNumber = $"{cleanCode}-{i:D2}",
                    Status = "Available",
                    IsAvailable = true,
                    ParkingType = model.DefaultVehicleType,
                    ParkingFee = model.DefaultMonthlyFee,
                    CreatedAt = DateTime.UtcNow
                });
            }

            dbContext.ParkingSpots.AddRange(spots);

            dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
            {
                Action = "Floor Created",
                Details = $"Manager created parking floor '{floor.FloorName}' with {model.Capacity} automated spaces.",
                CreatedBy = User.Identity?.Name ?? "Manager",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TempData["Success"] = $"Parking floor '{floor.FloorName}' created with {model.Capacity} spaces generated automatically ({cleanCode}-01 to {cleanCode}-{model.Capacity:D2}).";

            return RedirectToAction(nameof(Parking), new { floorId = floor.ParkingFloorId });
        }

        // AJAX ASSIGN PARKING SPOT TO FLAT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSpotAjax([FromBody] AssignParkingInputModel model)
        {
            if (model == null || model.ParkingSpotId <= 0 || model.FlatId <= 0)
            {
                return Json(new { success = false, message = "Invalid parking spot or flat specified." });
            }

            var spot = await dbContext.ParkingSpots
                .Include(s => s.Floor)
                .FirstOrDefaultAsync(s => s.ParkingSpotId == model.ParkingSpotId);

            if (spot == null)
            {
                return Json(new { success = false, message = "Parking spot not found." });
            }

            var flat = await dbContext.Flats
                .Include(f => f.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(f => f.FlatId == model.FlatId);

            if (flat == null)
            {
                return Json(new { success = false, message = "Flat not found." });
            }

            spot.FlatId = flat.FlatId;
            spot.Status = "Assigned";
            spot.IsAvailable = false;
            spot.ListingPrice = null;
            spot.ListingNotes = null;

            var resident = flat.Assignments.FirstOrDefault()?.User?.FullName ?? "Resident";

            dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
            {
                ParkingSpotId = spot.ParkingSpotId,
                Action = "Parking Assigned",
                Details = $"Manager assigned {spot.SpotNumber} to Flat {flat.FlatNumber} ({resident}).",
                CreatedBy = User.Identity?.Name ?? "Manager",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Space {spot.SpotNumber} assigned to Flat {flat.FlatNumber}.",
                spotId = spot.ParkingSpotId,
                spotNumber = spot.SpotNumber,
                flatNumber = flat.FlatNumber,
                residentName = resident,
                fee = spot.ParkingFee,
                status = "Assigned"
            });
        }

        // AJAX RELEASE PARKING SPOT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseSpotAjax(int spotId)
        {
            var spot = await dbContext.ParkingSpots
                .Include(s => s.Flat)
                .FirstOrDefaultAsync(s => s.ParkingSpotId == spotId);

            if (spot == null)
            {
                return Json(new { success = false, message = "Parking spot not found." });
            }

            var prevFlat = spot.Flat?.FlatNumber ?? "Flat";

            spot.FlatId = null;
            spot.Status = "Available";
            spot.IsAvailable = true;
            spot.ListingPrice = null;
            spot.ListingNotes = null;

            dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
            {
                ParkingSpotId = spot.ParkingSpotId,
                Action = "Parking Released",
                Details = $"Manager released parking spot {spot.SpotNumber} (previously assigned to Flat {prevFlat}).",
                CreatedBy = User.Identity?.Name ?? "Manager",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Space {spot.SpotNumber} released and returned to Available.",
                spotId = spot.ParkingSpotId,
                spotNumber = spot.SpotNumber,
                status = "Available"
            });
        }

        // AJAX UPDATE SPOT STATUS (FOR SALE / TO-LET / AVAILABLE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSpotStatusAjax([FromBody] UpdateSpotStatusInputModel model)
        {
            if (model == null || model.ParkingSpotId <= 0)
            {
                return Json(new { success = false, message = "Invalid spot specified." });
            }

            var spot = await dbContext.ParkingSpots.FirstOrDefaultAsync(s => s.ParkingSpotId == model.ParkingSpotId);
            if (spot == null)
            {
                return Json(new { success = false, message = "Parking spot not found." });
            }

            var validStatuses = new[] { "Available", "ForSale", "ToLet" };
            if (!validStatuses.Contains(model.Status))
            {
                return Json(new { success = false, message = "Invalid status specified." });
            }

            spot.Status = model.Status;
            spot.ListingPrice = model.ListingPrice;
            spot.ListingNotes = model.ListingNotes;

            if (model.Status == "Available")
            {
                spot.IsAvailable = true;
                spot.FlatId = null;
            }
            else
            {
                spot.IsAvailable = false;
                spot.FlatId = null; // Unlink flat so it is cleanly open for booking on the marketplace
            }

            string actionLabel = model.Status switch
            {
                "ForSale" => "Listed For Sale on Marketplace",
                "ToLet" => "Listed To-Let on Marketplace",
                _ => "Status Updated"
            };

            string friendlyStatus = model.Status switch
            {
                "ForSale" => "For Sale",
                "ToLet" => "To-Let",
                _ => "Available"
            };

            dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
            {
                ParkingSpotId = spot.ParkingSpotId,
                Action = actionLabel,
                Details = model.Status == "Available"
                    ? $"Spot {spot.SpotNumber} released and returned to Available."
                    : $"Spot {spot.SpotNumber} published to Marketplace as {friendlyStatus} (৳{(model.ListingPrice ?? spot.ParkingFee):N0}).",
                CreatedBy = User.Identity?.Name ?? "Manager",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            var marketUrl = Url.Action("ParkingDetails", "Property", new { id = spot.ParkingSpotId });
            var responseMessage = model.Status == "Available"
                ? $"Spot {spot.SpotNumber} marked as Available."
                : $"Spot {spot.SpotNumber} is now published to the Marketplace as {friendlyStatus}!";

            return Json(new
            {
                success = true,
                message = responseMessage,
                spotId = spot.ParkingSpotId,
                spotNumber = spot.SpotNumber,
                status = spot.Status,
                listingPrice = spot.ListingPrice ?? spot.ParkingFee,
                marketplaceUrl = marketUrl
            });
        }

        // TYPEAHEAD SEARCH FLATS (FOR MODAL ASSIGNMENT)
        [HttpGet]
        public async Task<IActionResult> SearchFlats(string? term)
        {
            var cleanTerm = term?.Trim() ?? string.Empty;

            var query = dbContext.Flats
                .Where(f => f.FlatStatus == "Occupied");

            if (!string.IsNullOrWhiteSpace(cleanTerm))
            {
                query = query.Where(f => f.FlatNumber.Contains(cleanTerm) || f.FloorNumber.ToString().Contains(cleanTerm));
            }

            var flats = await query
                .OrderBy(f => f.FlatNumber)
                .Take(25)
                .Select(f => new
                {
                    flatId = f.FlatId,
                    flatNumber = f.FlatNumber,
                    floorNumber = f.FloorNumber,
                    residentName = f.Assignments.Where(a => a.IsActive).Select(a => a.User.FullName).FirstOrDefault() ?? "Resident",
                    residentType = f.Assignments.Where(a => a.IsActive).Select(a => a.ResidentType).FirstOrDefault() ?? "Tenant"
                })
                .ToListAsync();

            return Json(flats);
        }

        // LEGACY CREATE PARKING SPOT (PRESERVED)
        [HttpGet]
        public IActionResult CreateParking()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParking(ParkingSpot model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await dbContext.ParkingSpots.AnyAsync(p => p.SpotNumber == model.SpotNumber))
            {
                ModelState.AddModelError(nameof(model.SpotNumber), "Parking spot number already exists.");
                return View(model);
            }

            model.IsAvailable = true;
            model.Status = "Available";
            model.CreatedAt = DateTime.UtcNow;

            // Associate with first floor if not set
            if (!model.ParkingFloorId.HasValue)
            {
                var firstFloor = await dbContext.ParkingFloors.OrderBy(f => f.FloorCode).FirstOrDefaultAsync();
                if (firstFloor != null)
                {
                    model.ParkingFloorId = firstFloor.ParkingFloorId;
                }
            }

            dbContext.ParkingSpots.Add(model);
            dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
            {
                Action = "Spot Created",
                Details = $"Manager created spot {model.SpotNumber}.",
                CreatedBy = User.Identity?.Name ?? "Manager",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TempData["Success"] = "Parking spot created successfully.";
            return RedirectToAction(nameof(Parking));
        }

        // LEGACY ASSIGN PARKING PAGE (PRESERVED)
        [HttpGet]
        public async Task<IActionResult> AssignParking(int id)
        {
            var parking = await dbContext.ParkingSpots
                .Include(p => p.Flat)
                .FirstOrDefaultAsync(p => p.ParkingSpotId == id);

            if (parking == null)
            {
                return NotFound();
            }

            ViewBag.Flats = await dbContext.Flats
                .Where(f => f.FlatStatus == "Occupied")
                .OrderBy(f => f.FlatNumber)
                .ToListAsync();

            return View(parking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignParking(int ParkingSpotId, int FlatId)
        {
            var parking = await dbContext.ParkingSpots
                .FirstOrDefaultAsync(p => p.ParkingSpotId == ParkingSpotId);

            if (parking == null)
            {
                return NotFound();
            }

            var flat = await dbContext.Flats
                .FirstOrDefaultAsync(f => f.FlatId == FlatId);

            if (flat == null)
            {
                TempData["Error"] = "Invalid flat selected.";
                return RedirectToAction(nameof(Parking));
            }

            parking.FlatId = flat.FlatId;
            parking.Status = "Assigned";
            parking.IsAvailable = false;

            dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
            {
                ParkingSpotId = parking.ParkingSpotId,
                Action = "Parking Assigned",
                Details = $"Manager assigned {parking.SpotNumber} to Flat {flat.FlatNumber}.",
                CreatedBy = User.Identity?.Name ?? "Manager",
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            TempData["Success"] = $"Parking {parking.SpotNumber} assigned successfully.";
            return RedirectToAction(nameof(Parking));
        }

        // ==========================================
        // CCTV controller kaj
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Cctv(string? zone = null)
        {
            var query = dbContext.CctvCameras
                .Include(c => c.FlatAccesses)
                .ThenInclude(fa => fa.Flat)
                .AsNoTracking();

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

            var availableFlats = await dbContext.Flats
                .AsNoTracking()
                .OrderBy(f => f.FloorNumber)
                .ThenBy(f => f.FlatNumber)
                .ToListAsync();

            var viewModel = new CctvDashboardViewModel
            {
                Cameras = cameras,
                SelectedZone = zone,
                AvailableZones = availableZones,
                TotalCameras = totalCameras,
                OnlineCount = onlineCount,
                OfflineCount = offlineCount,
                AvailableFlats = availableFlats
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCctv()
        {
            var flats = await dbContext.Flats
                .AsNoTracking()
                .OrderBy(f => f.FloorNumber)
                .ThenBy(f => f.FlatNumber)
                .ToListAsync();

            var model = new CctvCameraViewModel
            {
                Location = "Main Gate",
                Status = "Online",
                AccessType = "All",
                AvailableFlats = flats
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCctv(CctvCameraViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CameraName))
            {
                ModelState.AddModelError(nameof(model.CameraName), "Camera Name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.StreamUrl))
            {
                ModelState.AddModelError(nameof(model.StreamUrl), "Stream URL is required.");
            }

            if (model.AccessType == "SpecificFlats")
            {
                if (model.TargetFlatIds == null || model.TargetFlatIds.Count == 0)
                {
                    ModelState.AddModelError(nameof(model.TargetFlatIds), "Please select at least one flat for specific access.");
                }
            }
            else
            {
                model.AccessType = "All";
                model.TargetFlatIds?.Clear();
            }

            if (!ModelState.IsValid)
            {
                model.AvailableFlats = await dbContext.Flats
                    .AsNoTracking()
                    .OrderBy(f => f.FloorNumber)
                    .ThenBy(f => f.FlatNumber)
                    .ToListAsync();
                return View(model);
            }

            var camera = new CctvCamera
            {
                CameraName = model.CameraName.Trim(),
                Location = string.IsNullOrWhiteSpace(model.Location) ? "Main Gate" : model.Location.Trim(),
                // Direct URL as input by user - NO auto format, NO appending!
                StreamUrl = model.StreamUrl.Trim(),
                Status = string.IsNullOrWhiteSpace(model.Status) ? "Online" : model.Status.Trim(),
                AccessType = model.AccessType,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.CctvCameras.AddAsync(camera);
            await dbContext.SaveChangesAsync();

            if (model.AccessType == "SpecificFlats" && model.TargetFlatIds != null && model.TargetFlatIds.Count > 0)
            {
                var distinctFlatIds = model.TargetFlatIds.Distinct().ToList();
                var accesses = distinctFlatIds.Select(flatId => new CctvCameraFlatAccess
                {
                    CameraId = camera.CameraId,
                    FlatId = flatId,
                    AssignedAt = DateTime.UtcNow
                });
                await dbContext.CctvCameraFlatAccesses.AddRangeAsync(accesses);
                await dbContext.SaveChangesAsync();
            }

            TempData["Success"] = $"Camera '{camera.CameraName}' added successfully.";
            return RedirectToAction(nameof(Cctv));
        }

        [HttpGet]
        public async Task<IActionResult> EditCctv(int id)
        {
            var camera = await dbContext.CctvCameras
                .Include(c => c.FlatAccesses)
                .FirstOrDefaultAsync(c => c.CameraId == id);

            if (camera == null)
            {
                TempData["Error"] = "Camera not found.";
                return RedirectToAction(nameof(Cctv));
            }

            var flats = await dbContext.Flats
                .AsNoTracking()
                .OrderBy(f => f.FloorNumber)
                .ThenBy(f => f.FlatNumber)
                .ToListAsync();

            var model = new CctvCameraViewModel
            {
                CameraId = camera.CameraId,
                CameraName = camera.CameraName,
                Location = camera.Location,
                StreamUrl = camera.StreamUrl,
                Status = camera.Status,
                AccessType = string.IsNullOrWhiteSpace(camera.AccessType) ? "All" : camera.AccessType,
                TargetFlatIds = camera.FlatAccesses.Select(fa => fa.FlatId).ToList(),
                AvailableFlats = flats,
                CreatedAt = camera.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCctv(int id, CctvCameraViewModel model)
        {
            if (id != model.CameraId)
            {
                return NotFound();
            }

            var camera = await dbContext.CctvCameras
                .Include(c => c.FlatAccesses)
                .FirstOrDefaultAsync(c => c.CameraId == id);

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

            if (model.AccessType == "SpecificFlats")
            {
                if (model.TargetFlatIds == null || model.TargetFlatIds.Count == 0)
                {
                    ModelState.AddModelError(nameof(model.TargetFlatIds), "Please select at least one flat for specific access.");
                }
            }
            else
            {
                model.AccessType = "All";
                model.TargetFlatIds?.Clear();
            }

            if (!ModelState.IsValid)
            {
                model.AvailableFlats = await dbContext.Flats
                    .AsNoTracking()
                    .OrderBy(f => f.FloorNumber)
                    .ThenBy(f => f.FlatNumber)
                    .ToListAsync();
                return View(model);
            }

            camera.CameraName = model.CameraName.Trim();
            camera.Location = string.IsNullOrWhiteSpace(model.Location) ? "Main Gate" : model.Location.Trim();
            // Direct URL as input by user - NO auto format, NO appending!
            camera.StreamUrl = model.StreamUrl.Trim();
            camera.Status = string.IsNullOrWhiteSpace(model.Status) ? "Online" : model.Status.Trim();
            camera.AccessType = model.AccessType;

            // Update flat accesses
            dbContext.CctvCameraFlatAccesses.RemoveRange(camera.FlatAccesses);

            if (model.AccessType == "SpecificFlats" && model.TargetFlatIds != null && model.TargetFlatIds.Count > 0)
            {
                var distinctFlatIds = model.TargetFlatIds.Distinct().ToList();
                foreach (var flatId in distinctFlatIds)
                {
                    dbContext.CctvCameraFlatAccesses.Add(new CctvCameraFlatAccess
                    {
                        CameraId = camera.CameraId,
                        FlatId = flatId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

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

        #region Gym Management

        [HttpGet]
        public async Task<IActionResult> Gym()
        {
            var model = await gymService.GetManagerGymDashboardAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGymSettings(GymSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors in the gym settings form.";
                var dashboardModel = await gymService.GetManagerGymDashboardAsync();
                dashboardModel.GymSettings = model;
                return View("Gym", dashboardModel);
            }

            var user = await userManager.GetUserAsync(User);
            var success = await gymService.UpdateGymSettingsAsync(model, user?.Id);

            if (success)
            {
                TempData["Success"] = "Gym facility settings, rules, and monthly fee updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update gym settings. Please try again.";
            }

            return RedirectToAction(nameof(Gym));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveGymMembership(GymApprovalRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide valid start and expiry dates.";
                return RedirectToAction(nameof(Gym));
            }

            var user = await userManager.GetUserAsync(User);
            var result = await gymService.ApproveMembershipAsync(user?.Id ?? "", request);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectGymMembership(GymRejectionRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide a rejection reason.";
                return RedirectToAction(nameof(Gym));
            }

            var user = await userManager.GetUserAsync(User);
            var result = await gymService.RejectMembershipAsync(user?.Id ?? "", request);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelGymMembership(int id, string? reason)
        {
            var user = await userManager.GetUserAsync(User);
            var result = await gymService.CancelMembershipByManagerAsync(user?.Id ?? "", id, reason);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenewGymMembership(int id, int durationMonths = 1)
        {
            var user = await userManager.GetUserAsync(User);
            var result = await gymService.RenewMembershipByManagerAsync(user?.Id ?? "", id, durationMonths);

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
        public async Task<IActionResult> GymIdCard(int id)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var cardModel = await gymService.GetGymIdCardDetailsAsync(id, null, isManager: true, baseUrl);
            if (cardModel == null)
            {
                TempData["Error"] = "Gym membership record not found.";
                return RedirectToAction(nameof(Gym));
            }

            return View(cardModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkGymFeePaid(int id)
        {
            var user = await userManager.GetUserAsync(User);
            var result = await gymService.MarkGymFeePaidAsync(user?.Id ?? "", id);

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

        #endregion

    }
}