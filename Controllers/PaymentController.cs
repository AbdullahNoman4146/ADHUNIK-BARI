using ADHUNIK_BARI.Services;
using ADHUNIK_BARI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ADHUNIK_BARI.Models;
using Stripe;

namespace ADHUNIK_BARI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IPropertyPaymentService _propertyPaymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IPropertyPaymentService propertyPaymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _userManager = userManager;
            _configuration = configuration;
            _propertyPaymentService = propertyPaymentService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a Stripe PaymentIntent for dynamically selected bill items.
        /// Requires authentication as Tenant, FlatOwner, Resident, Manager, or Admin.
        /// </summary>
        [HttpPost("create-intent")]
        [Authorize(Roles = "Tenant,FlatOwner,Manager,Admin,Resident")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            if (request == null || !request.SelectedItemIds.Any())
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    Message = "Invalid request: BillId and at least one SelectedItemId required"
                });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new PaymentIntentResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var response = await _paymentService.CreatePaymentIntentAsync(userId, request.BillId, request.SelectedItemIds);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Confirms a successful client-side Stripe payment and updates DB records immediately
        /// </summary>
        [HttpPost("confirm-payment")]
        [Authorize(Roles = "Tenant,FlatOwner,Manager,Admin,Resident")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return BadRequest(new ConfirmPaymentResult
                {
                    Success = false,
                    Message = "PaymentIntent ID is required."
                });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ConfirmPaymentResult
                {
                    Success = false,
                    Message = "User not authenticated."
                });
            }

            var result = await _paymentService.ConfirmPaymentAsync(userId, request.PaymentIntentId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Stripe webhook endpoint for payment_intent.succeeded events.
        /// Verifies webhook signature and processes successful payments.
        /// Must be kept private and configured in Stripe dashboard.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );

                // Handle payment_intent.succeeded event
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        _logger.LogInformation($"Processing payment_intent.succeeded: {paymentIntent.Id}");

                        var isPropertyAdvance = paymentIntent.Metadata.TryGetValue("PaymentPurpose", out var purpose)
                            && string.Equals(purpose, "PropertyAdvance", StringComparison.Ordinal);

                        var success = isPropertyAdvance
                            ? await _propertyPaymentService.ProcessPaymentSuccessAsync(
                                paymentIntent.Id, paymentIntent.Amount, paymentIntent.Metadata)
                            : await _paymentService.ProcessPaymentSuccessAsync(
                                paymentIntent.Id, paymentIntent.Amount, paymentIntent.Metadata);

                        if (!success)
                        {
                            _logger.LogError($"Failed to process payment {paymentIntent.Id}");
                            return StatusCode(500, "Internal server error processing payment");
                        }
                    }
                }

                return Ok(new { received = true });
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe webhook error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook processing error: {ex.Message}");
                return StatusCode(500, new { error = "Webhook processing failed" });
            }
        }
    }
}
