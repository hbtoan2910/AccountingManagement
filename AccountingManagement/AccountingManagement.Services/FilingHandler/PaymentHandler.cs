using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Serilog;

namespace AccountingManagement.Services
{
    public interface IPaymentHandler
    {
        void ConfirmClientPayment(ClientPayment clientPayment, string confirmText, DateTime confirmDate, Guid userAccountId);
        void SaveConfirmationText(ClientPayment clientPayment, string confirmText);
        void SaveEmailSentStatus(ClientPayment clientPayment);
    }

    public class PaymentHandler : IPaymentHandler
    {
        public PaymentHandler()
        { }

        public void ConfirmClientPayment(ClientPayment clientPayment, string confirmText, DateTime confirmDate, Guid userAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.ClientPayments.FirstOrDefault(x => x.Id == clientPayment.Id)
                ?? throw new ArgumentException($"ClientPaymentId:{clientPayment.Id} not found.");

            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                dbContext.ClientPaymentLogs.Add(new ClientPaymentLog
                {
                    BusinessId = clientPayment.BusinessId,
                    ClientPaymentId = clientPayment.Id,
                    PaymentAmount = clientPayment.PaymentAmount,
                    BankInfo = clientPayment.BankInfo,
                    DueDate = clientPayment.DueDate,
                    ConfirmationNotes = confirmText ?? string.Empty,
                    Timestamp = confirmDate,
                    UserAccountId = userAccountId,
                });

                if (existingAccount.PaymentCycle == ClientPaymentCycle.Undefined)
                {
                    existingAccount.IsActive = false;
                }
                else
                {
                    var nextDueDate = CalculateNextPaymentDueDates(clientPayment.PaymentType, clientPayment.PaymentCycle,
                        clientPayment.DueDate);

                    existingAccount.DueDate = nextDueDate;
                }

                existingAccount.TmpConfirmationText = string.Empty;
                existingAccount.TmpReceiptEmailSent = false;

                dbContext.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Log.Error(ex, $"Unexpected exception while confirming Payment: {ex.Message}. ClientPaymentId:{clientPayment.Id}, BusinessId:{clientPayment.BusinessId}");
                throw ex;
            }
        }

        private DateTime CalculateNextPaymentDueDates(ClientPaymentType paymentType, ClientPaymentCycle cycle,
            DateTime dueDate)
        {
            switch (cycle)
            {
                case ClientPaymentCycle.Monthly:
                    return dueDate.AddMonths(1);

                case ClientPaymentCycle.BiMonthly:
                    return dueDate.AddMonths(2);

                case ClientPaymentCycle.Quarterly:
                    return dueDate.AddMonths(3);

                case ClientPaymentCycle.Undefined:
                    return dueDate;

                default:
                    throw new ArgumentException($"Invalid Payment cycle [{cycle}].");
            }
        }

        public void SaveConfirmationText(ClientPayment clientPayment, string confirmText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.ClientPayments.FirstOrDefault(x => x.Id == clientPayment.Id)
                ?? throw new ArgumentException($"ClientPaymentId:{clientPayment.Id} not found.");

            existing.TmpConfirmationText = confirmText;

            dbContext.SaveChanges();
        }

        public void SaveEmailSentStatus(ClientPayment clientPayment)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.ClientPayments.FirstOrDefault(x => x.Id == clientPayment.Id)
                ?? throw new ArgumentException($"ClientPaymentId:{clientPayment.Id} not found.");

            existing.TmpReceiptEmailSent = true;

            dbContext.SaveChanges();
        }
    }
}
