using System.ComponentModel.DataAnnotations;

namespace OpenPayment.Models.DTOs
{
    public class PaymentRequestDTO
    {
        [Required]
        [StringLength(34)]
        public string DebtorAccount { get; set; }
        [Required]
        [StringLength(34)]
        public string CreditorAccount { get; set; }
        // An positive or negative number with 1-14 digits followed by 1-3 digits as decimals
        [Required]
        [RegularExpression(@"-?[0-9]{1,14}(\.[0-9]{1,3})?", ErrorMessage = "Invalid amount format.")]
        public decimal InstructedAmount { get; set; }

        [Required]
        public string Currency { get; set; }
    }
}
