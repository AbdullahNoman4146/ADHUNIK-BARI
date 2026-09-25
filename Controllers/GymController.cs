using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Services;
using ADHUNIK_BARI.ViewModels;

namespace ADHUNIK_BARI.Controllers
{
    public class GymController : Controller
    {
        private readonly IGymService _gymService;
        private readonly UserManager<ApplicationUser> _userManager;

        public GymController(IGymService gymService, UserManager<ApplicationUser> userManager)
        {
            _gymService = gymService;
            _userManager = userManager;
        }

        // PUBLIC QR CODE VERIFICATION ENDPOINT
        // Any smartphone camera scanning the QR code opens this page directly!
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ViewBag.ErrorCode = "EMPTY";
                return View(new GymVerificationViewModel
                {
                    IsValid = false,
                    StatusBadge = "No Code",
                    Message = "No Gym Member Pass code was provided for verification."
                });
            }

            var result = await _gymService.VerifyGymPassAsync(code);
            return View(result);
        }

        // RESIDENT: VIEW PRINTABLE PAY SLIP
        [HttpGet]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser,Manager,Admin")]
        public async Task<IActionResult> PaySlip(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var isManager = User.IsInRole("Manager") || User.IsInRole("Admin");
            var paySlip = await _gymService.GetGymPaySlipDetailsAsync(id, user.Id, isManager);

            if (paySlip == null)
            {
                TempData["Error"] = "Pay slip record not found or access unauthorized.";
                return RedirectToAction("Gym", isManager ? "Manager" : "Resident");
            }

            return View(paySlip);
        }

        // RESIDENT: PAY GYM FEE PAY SLIP
        [HttpPost]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaySlipPayment(GymPaymentRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please select a valid payment method.";
                return RedirectToAction(nameof(PaySlip), new { id = request.MembershipId });
            }

            var result = await _gymService.PayGymPaySlipAsync(user.Id, request);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction("GymIdCard", "Resident", new { id = request.MembershipId });
            }

            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PaySlip), new { id = request.MembershipId });
        }

        // RESIDENT: CONFIRM STRIPE PAYMENT FOR GYM PAY SLIP
        [HttpPost]
        [Authorize(Roles = "Tenant,FlatOwner,ParkingUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmStripePayment([FromForm] int membershipId, [FromForm] string? paymentIntentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            if (membershipId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid gym membership ID." });
            }

            var result = await _gymService.ConfirmGymStripePaymentAsync(user.Id, membershipId, paymentIntentId);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            TempData["Success"] = result.Message;
            return Ok(new
            {
                success = true,
                redirectUrl = Url.Action("GymIdCard", "Resident", new { id = membershipId })
            });
        }
    }
}
