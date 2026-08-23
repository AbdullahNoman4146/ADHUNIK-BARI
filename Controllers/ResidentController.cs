using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.ViewModels;
using Microsoft.EntityFrameworkCore;


namespace ADHUNIK_BARI.Controllers
{


    [Authorize(Roles = "Tenant,FlatOwner")]
    public class ResidentController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;
        private readonly IWebHostEnvironment environment;



        public ResidentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment environment)
        {

            this.userManager = userManager;
            this.dbContext = dbContext;
            this.environment = environment;

        }




        public async Task<IActionResult> Dashboard()
        {


            var user =
                await userManager.GetUserAsync(User);

            var assignment = user == null ? null : await dbContext.FlatAssignments
                .Include(item => item.Flat)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == user.Id && item.IsActive);

            ViewBag.Assignment = assignment;



            if (user != null)
            {

                ViewBag.RequirePasswordChange =
                    user.TemporaryPasswordStatus;

            }



            return View();

        }

        [HttpGet]
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



    }


}