using Microsoft.AspNetCore.Mvc;
using OpenPayment.Models;
using OpenPayment.Models.DTOs;
using OpenPayment.Services.PaymentService;

namespace OpenPayment.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ILogger<PaymentsController> _logger;
        private readonly IPaymentService _paymentService;

        public PaymentsController(ILogger<PaymentsController> logger, IPaymentService paymentService)
        {
            _logger = logger;
            _paymentService = paymentService;
        }

        [HttpPost(Name = "InitiatePayment")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequestDTO initiatePaymentRequest)
        {
            if (!Request.Headers.TryGetValue("Client-ID", out var clientId))
            {
                return BadRequest("Missing required header: Client-ID");
            }
            if (!Guid.TryParse(clientId, out Guid parsedClientId))
            {
                return BadRequest("Invalid Client-ID header: expected a GUID in the format 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'.");
            }

            var paymentInfo = await _paymentService.AddPaymentToProcessing(initiatePaymentRequest, parsedClientId);

            // If there is no payment id, then there is a payment already processing with the same client id
            if (paymentInfo.PaymentProcessStatus == PaymentProcessStatus.Conflict)
            {
                return Conflict($"There is already a payment processing with client id: {parsedClientId}");
            }

            return CreatedAtAction(nameof(InitiatePayment).ToLower(), paymentInfo.PaymentId);
        }
    }
}
