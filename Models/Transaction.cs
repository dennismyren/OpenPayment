namespace OpenPayment.Models
{
    public class Transaction
    {
        public Guid PaymentId { get; set; }
        public string DebtorAccount { get; set; }
        public string CreditorAccount { get; set; }
        public decimal TransactionAmount { get; set; }
        public string Currency { get; set; }
    }
}
