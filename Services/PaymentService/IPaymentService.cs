using OpenPayment.Models;
using OpenPayment.Models.DTOs;
using System.Threading.Channels;

namespace OpenPayment.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<Guid?> EnqueuePayment(PaymentRequestDTO paymentRequest, Guid clientId);
        ChannelReader<Payment> GetPaymentQueueReader();
        Payment? DequeuePayment(Guid clientId);
        List<Transaction> GetTransactionsByIban(string iban);
        void AddTransaction(string iban, Transaction transaction);
    }
}   