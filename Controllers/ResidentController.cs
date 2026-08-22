using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Data;
using Microsoft.EntityFrameworkCore;


namespace ADHUNIK_BARI.Controllers
{


    [Authorize(Roles = "Tenant,FlatOwner")]
    public class ResidentController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;



        public ResidentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {

            this.userManager = userManager;
            this.dbContext = dbContext;

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



    }


}