using ADHUNIK_BARI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADHUNIK_BARI.Controllers
{
    [AllowAnonymous]
    public class PropertyPaymentController : Controller
    {
        private readonly IPropertyPaymentService _propertyPaymentService;

        public PropertyPaymentController(IPropertyPaymentService propertyPaymentService)
        {
            _propertyPaymentService = propertyPaymentService;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            var model = await _propertyPaymentService.GetCheckoutAsync(id);
            if (model == null)
            {
                TempData["Error"] = "This checkout session is invalid, completed, or expired.";
                return RedirectToAction("Index", "Property");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int applicationId, string paymentIntentId)
        {
            if (applicationId <= 0 || string.IsNullOrWhiteSpace(paymentIntentId))
                return BadRequest(new { success = false, message = "Invalid payment confirmation request." });

            var result = await _propertyPaymentService.ConfirmPaymentAsync(applicationId, paymentIntentId);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new
            {
                success = true,
                redirectUrl = Url.Action(nameof(Success), new { id = applicationId })
            });
        }

        [HttpGet]
        public IActionResult Success(int id)
        {
            ViewBag.ApplicationId = id;
            return View();
        }
    }
}
