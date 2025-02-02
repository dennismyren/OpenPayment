using System.ComponentModel.DataAnnotations;

namespace OpenPayment.Models
{
    public class Payment
    {
        public Guid PaymentId { get; set; }
        public Guid ClientId { get; set; }
        public string DebtorAccount { get; set; }
        public string CreditorAccount { get; set; }
        public decimal InstructedAmount { get; set; }
        public string Currency { get; set; }
    }
}
