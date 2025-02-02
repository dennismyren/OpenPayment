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

        public async Task<(Guid? PaymentId, PaymentProcessStatus PaymentProcessStatus)> AddPaymentToProcessing(PaymentRequestDTO paymentRequest, Guid clientId)
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

            // Check if there is a payment processing with client id present
            if (!_paymentsInProcess.TryAdd(payment.ClientId, payment))
            {
                return (null, PaymentProcessStatus.Conflict);
            }

            await _paymentProcessingChannel.Writer.WriteAsync(payment);
            return (payment.PaymentId, PaymentProcessStatus.Success);
        }

        public bool RemovePaymentFromProcessing(Guid clientId)
        {
            try
            {
                if (_paymentsInProcess.TryRemove(clientId, out Payment? payment))
                {
                    _logger.LogInformation("Processing complete: Payment with clientid: {ClientId}", clientId);
                    return true;
                }

                _logger.LogInformation("Payment with clientid: {ClientId} was not processing", clientId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not remove payment from processing with id: {ClientId}", clientId);
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
