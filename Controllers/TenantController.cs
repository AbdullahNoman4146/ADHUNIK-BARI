using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ADHUNIK_BARI.Controllers
{

    [Authorize(Roles = "Tenant")]
    public class TenantController : Controller
    {


        public IActionResult Dashboard()
        {

            return View();

        }


    }

}