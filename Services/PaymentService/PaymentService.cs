using OpenPayment.Models;
using OpenPayment.Models.DTOs;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace OpenPayment.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private static readonly ConcurrentDictionary<Guid, Payment> _queuedPayments = new();
        private readonly ConcurrentDictionary<string, List<Transaction>> _transactionsByIban = new();
        private static readonly Channel<Payment> _paymentProcessingChannel = Channel.CreateUnbounded<Payment>();

        public async Task<Guid?> EnqueuePayment(PaymentRequestDTO paymentRequest, Guid clientId)
        {
            var payment = new Payment()
            {
                PaymentId = Guid.NewGuid(),
                ClientId = clientId,
                CreditorAccount = paymentRequest.CreditorAccount,
                DebtorAccount = paymentRequest.DebtorAccount,
                Currency = paymentRequest.Currency,
                InstructedAmount = paymentRequest.InstructedAmount,
            };

            // Check if there is a payment with client id present on the queue.
            if (!_queuedPayments.TryAdd(payment.ClientId, payment))
            {
                return null;
            }

            _queuedPayments[payment.ClientId] = payment;

            await _paymentProcessingChannel.Writer.WriteAsync(payment);
            return payment.PaymentId;
        }

        public Payment? DequeuePayment(Guid clientId)
        {
            if (_queuedPayments.TryRemove(clientId, out Payment? payment))
            {
                Console.WriteLine($"Dequeued payment request with clientid: {payment.ClientId}");
                return payment;
            }
            else
            {
                Console.WriteLine($"Payment request with clientid: {clientId} was not on queue");
                return null;
            }
        }

        public void AddTransaction(string iban, Transaction transaction)
        {
            _transactionsByIban.AddOrUpdate(iban, new List<Transaction> { transaction },
            (key, existingList) =>
            {
                existingList.Add(transaction);
                return existingList;
            });
        }
        public List<Transaction> GetTransactionsByIban(string iban)
        {
            return _transactionsByIban.TryGetValue(iban, out var transactions) ? transactions : new List<Transaction>();
        }

        public ChannelReader<Payment> GetPaymentQueueReader() => _paymentProcessingChannel.Reader;
    }
}
