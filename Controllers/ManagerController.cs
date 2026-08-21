using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ADHUNIK_BARI.Controllers
{

    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {


        public IActionResult Dashboard()
        {

            return View();

        }


    }

}