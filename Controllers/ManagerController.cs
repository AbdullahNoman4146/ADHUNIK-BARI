using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;
using ADHUNIK_BARI.Data;
using Microsoft.EntityFrameworkCore;


namespace ADHUNIK_BARI.Controllers
{

    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;


        public ManagerController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
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
                FlatStatus = "Available",
                CreatedAt = DateTime.Now
            });

            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Flat created successfully.";
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
                FlatId = flat.FlatId,
                UserId = resident.Id,
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
                TargetFlatIds = notice.Targets.Select(target => target.FlatId).Where(id => id.HasValue).Select(id => id.Value).ToList()
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





        public async Task<IActionResult> Dashboard()
        {
            ViewBag.RecentComplaints = await dbContext.Complaints
                .Include(complaint => complaint.Flat)
                .Include(complaint => complaint.User)
                .AsNoTracking()
                .OrderByDescending(complaint => complaint.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
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
                ModelState.AddModelError(nameof(model.ComplaintStatus), "Select a valid complaint status.");
            }
            else if (!CanMoveToStatus(complaint.ComplaintStatus, model.ComplaintStatus))
            {
                ModelState.AddModelError(nameof(model.ComplaintStatus), "Complaint status must progress from Pending to In Progress to Solved.");
            }

            if (!ModelState.IsValid)
            {
                var details = await dbContext.Complaints
                    .Include(item => item.Flat)
                    .Include(item => item.User)
                    .Include(item => item.ResolvedByUser)
                    .AsNoTracking()
                    .SingleAsync(item => item.ComplaintId == model.ComplaintId);
                ViewBag.UpdateModel = model;
                return View("ComplaintDetails", details);
            }

            complaint.ComplaintStatus = model.ComplaintStatus;
            complaint.ManagerNote = string.IsNullOrWhiteSpace(model.ManagerNote) ? null : model.ManagerNote.Trim();

            if (model.ComplaintStatus == "Solved" && complaint.ResolvedAt == null)
            {
                complaint.ResolvedAt = DateTime.Now;
                complaint.ResolvedByUserId = userManager.GetUserId(User);
            }

            await dbContext.SaveChangesAsync();
            TempData["Success"] = "Complaint updated successfully.";
            return RedirectToAction(nameof(ComplaintDetails), new { id = complaint.ComplaintId });
        }

        private static bool IsComplaintStatus(string? status)
        {
            return status is "Pending" or "In Progress" or "Solved";
        }

        private static bool CanMoveToStatus(string currentStatus, string requestedStatus)
        {
            return currentStatus == requestedStatus ||
                (currentStatus == "Pending" && requestedStatus == "In Progress") ||
                (currentStatus == "In Progress" && requestedStatus == "Solved");
        }





        // GET CREATE RESIDENT

        [HttpGet]
        public IActionResult CreateResident()
        {
            return View();
        }







        // POST CREATE RESIDENT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResident(
            CreateResidentViewModel model)
        {


            if (!ModelState.IsValid)
            {
                return View(model);
            }





            var existingUser =
                await userManager.FindByEmailAsync(model.Email);



            if (existingUser != null)
            {

                ModelState.AddModelError(
                    "",
                    "Email already exists"
                );


                return View(model);

            }







            ApplicationUser user = new ApplicationUser
            {


                FullName = model.FullName,
                Phone = model.Phone,


                UserName = model.Email,


                Email = model.Email,


                EmailConfirmed = true,


                TemporaryPasswordStatus = true,


                AccountStatus = "Active",


                CreatedAt = DateTime.Now


            };







            var result =
                await userManager.CreateAsync(
                    user,
                    model.TemporaryPassword
                );








            if (result.Succeeded)
            {



                var roleResult =
                    await userManager.AddToRoleAsync(
                        user,
                        model.ResidentType
                    );






                if (!roleResult.Succeeded)
                {


                    foreach (var error in roleResult.Errors)
                    {

                        ModelState.AddModelError(
                            "",
                            error.Description
                        );

                    }



                    return View(model);


                }







                TempData["Success"] =
                    "Resident account created successfully";






                return RedirectToAction(
                    "Dashboard"
                );


            }







            foreach (var error in result.Errors)
            {

                ModelState.AddModelError(
                    "",
                    error.Description
                );

            }






            return View(model);


        }



    }

}