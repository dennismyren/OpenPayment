﻿using OpenPayment.Models;
using OpenPayment.Models.DTOs;
using OpenPayment.Services.PaymentService;

namespace OpenPayment.Services.PaymentProcessor
{
    public class PaymentProcessor : BackgroundService
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger _logger;

        public PaymentProcessor(IPaymentService paymentService, ILogger<PaymentProcessor> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var payment in _paymentService.GetPaymentQueueReader().ReadAllAsync(stoppingToken))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessPayment(payment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing payment {payment.PaymentId}: {ex.Message}");
                    }
                });
            }
        }

        private async Task ProcessPayment(Payment payment)
        {
            _logger.LogInformation($"Processing payment {payment.PaymentId} for Client {payment.ClientId}...");

            await Task.Delay(2000);

            bool paymentWasRemovedFromProcessing = _paymentService.RemovePaymentFromProcessing(payment.ClientId);

            if (paymentWasRemovedFromProcessing)
            {
                Transaction transaction = new Transaction()
                {
                    PaymentId = payment.PaymentId,
                    CreditorAccount = payment.CreditorAccount,
                    DebtorAccount = payment.DebtorAccount,
                    Currency = payment.Currency,
                    TransactionAmount = payment.InstructedAmount
                };
                _paymentService.AddTransaction(transaction.DebtorAccount, transaction);
                _paymentService.AddTransaction(transaction.CreditorAccount, transaction);
            }

            _logger.LogInformation($"Payment {payment.PaymentId} for Client {payment.ClientId} processed successfully.");
        }
    }
}
