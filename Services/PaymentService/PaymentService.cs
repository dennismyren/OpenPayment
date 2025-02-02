using OpenPayment.Models;
using OpenPayment.Models.DTOs;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace OpenPayment.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private static readonly ConcurrentDictionary<Guid, Payment> _paymentsInProcess = new();
        private readonly ConcurrentDictionary<string, List<Transaction>> _transactionsByIban = new();
        private static readonly Channel<Payment> _paymentProcessingChannel = Channel.CreateUnbounded<Payment>();

        public PaymentService(ILogger<PaymentService> logger)
        {
            _logger = logger;
        }

        public async Task<Guid?> AddPaymentToProcessing(PaymentRequestDTO paymentRequest, Guid clientId)
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
            if (!_paymentsInProcess.TryAdd(payment.ClientId, payment))
            {
                return null;
            }

            _paymentsInProcess[payment.ClientId] = payment;

            await _paymentProcessingChannel.Writer.WriteAsync(payment);
            return payment.PaymentId;
        }

        public bool RemovePaymentFromProcessing(Guid clientId)
        {
            try
            {
                if (_paymentsInProcess.TryRemove(clientId, out Payment? payment))
                {
                    _logger.LogInformation($"Dequeued payment request with clientid: {payment.ClientId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"Payment request with clientid: {clientId} was not on queue");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not remove payment from processing: {ex.Message}");
                return false;
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
