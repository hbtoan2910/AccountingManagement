using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.Services
{
    public interface IClientPaymentService
    {
        List<ClientPayment> GetClientPayments();
        List<ClientPayment> GetClientPaymentsByPaymentType(ClientPaymentType type);
        ClientPayment GetClientPaymentById(Guid id);
        bool UpsertClientPayment(ClientPayment clientPayment);

        List<ClientPaymentLog> GetClientPaymentLogsByType(ClientPaymentType type);
    }

    public class ClientPaymentService : IClientPaymentService
    {
        public List<ClientPayment> GetClientPayments()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.ClientPayments.Where(a => a.IsActive)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public List<ClientPayment> GetClientPaymentsByPaymentType(ClientPaymentType type)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.ClientPayments.Where(a => a.IsActive && a.PaymentType == type)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .ToList();
        }

        public ClientPayment GetClientPaymentById(Guid id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.ClientPayments.Where(x => x.Id == id)
                .Include(x => x.Business)
                .FirstOrDefault();
        }

        public bool UpsertClientPayment(ClientPayment clientPayment)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingPayment = dbContext.ClientPayments.FirstOrDefault(x => x.Id == clientPayment.Id);
            if (existingPayment == null)
            {
                dbContext.ClientPayments.Add(clientPayment);
            }
            else
            {
                existingPayment.BankInfo = clientPayment.BankInfo;
                existingPayment.PaymentType = clientPayment.PaymentType;
                existingPayment.PaymentCycle = clientPayment.PaymentCycle;
                existingPayment.PaymentAmount = clientPayment.PaymentAmount;
                existingPayment.DueDate = clientPayment.DueDate;
                existingPayment.Notes = clientPayment.Notes;
                existingPayment.TmpConfirmationText = clientPayment.TmpConfirmationText;
                existingPayment.TmpReceiptEmailSent = clientPayment.TmpReceiptEmailSent;
                existingPayment.IsActive = clientPayment.IsActive;
            }

            dbContext.SaveChanges();
            return true;
        }

        public List<ClientPaymentLog> GetClientPaymentLogsByType(ClientPaymentType type)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.ClientPaymentLogs
                .Include(a => a.ClientPayment)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .Include(a => a.UserAccount)
                .AsNoTracking()
                .ToList();
        }
    }
}
