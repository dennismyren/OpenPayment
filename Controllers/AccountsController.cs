using Microsoft.AspNetCore.Mvc;
using OpenPayment.Models;
using OpenPayment.Services.PaymentService;

namespace OpenPayment.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public AccountsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet(Name = "GetTransactions")]
        [Route("{iban}/transactions")]
        public IActionResult GetTransactions(string iban)
        {
            var transactions = _paymentService.GetTransactionsByIban(iban);

            if (transactions.Any())
            {
                return Ok(transactions);
            }
            else
            {
                return NoContent();
            }
        }
    }
}
