using OpenPayment.Models;
using OpenPayment.Models.DTOs;
using System.Threading.Channels;

namespace OpenPayment.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<Guid?> AddPaymentToProcessing(PaymentRequestDTO paymentRequest, Guid clientId);
        ChannelReader<Payment> GetPaymentQueueReader();
        bool RemovePaymentFromProcessing(Guid clientId);
        List<Transaction> GetTransactionsByIban(string iban);
        void AddTransaction(string iban, Transaction transaction);
    }
}   