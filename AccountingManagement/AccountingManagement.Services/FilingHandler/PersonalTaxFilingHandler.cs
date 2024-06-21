using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Serilog;

namespace AccountingManagement.Services
{
    public interface IPersonalTaxFilingHandler
    {
        void ConfirmTaxFiled(PersonalTaxAccount account, string confirmText, DateTime confirmDate, Guid userAccountId);

        void UpdateTaxFilingProgress(PersonalTaxAccount account);

        void UpdateStep1Status(PersonalTaxAccount account);
        void UpdateStep2Status(PersonalTaxAccount account);
        void UpdateStep3Status(PersonalTaxAccount account);
        void UpdateStep4Status(PersonalTaxAccount account);
    }

    public class PersonalTaxFilingHandler : IPersonalTaxFilingHandler
    {
        public void UpdateStep1Status(PersonalTaxAccount account)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.PersonalTaxAccounts.FirstOrDefault(x => x.Id == account.Id)
                ?? throw new ArgumentException($"PersonalTaxAccountId:{account.Id} not found.");

            existing.Step1Completed = account.Step1Completed;
            existing.TaxFilingProgress = account.TaxFilingProgress;

            dbContext.SaveChanges();
        }

        public void UpdateStep2Status(PersonalTaxAccount account)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.PersonalTaxAccounts.FirstOrDefault(x => x.Id == account.Id)
                ?? throw new ArgumentException($"PersonalTaxAccountId:{account.Id} not found.");

            existing.Step2Completed = account.Step2Completed;
            existing.TaxFilingProgress = account.TaxFilingProgress;

            dbContext.SaveChanges();
        }

        public void UpdateStep3Status(PersonalTaxAccount account)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.PersonalTaxAccounts.FirstOrDefault(x => x.Id == account.Id)
                ?? throw new ArgumentException($"PersonalTaxAccountId:{account.Id} not found.");

            existing.Step3Completed = account.Step3Completed;
            existing.TaxFilingProgress = account.TaxFilingProgress;

            dbContext.SaveChanges();
        }

        public void UpdateStep4Status(PersonalTaxAccount account)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.PersonalTaxAccounts.FirstOrDefault(x => x.Id == account.Id)
                ?? throw new ArgumentException($"PersonalTaxAccountId:{account.Id} not found.");

            existing.Step4Completed = account.Step4Completed;
            existing.TaxFilingProgress = account.TaxFilingProgress;

            dbContext.SaveChanges();
        }

        public void UpdateTaxFilingProgress(PersonalTaxAccount account)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.PersonalTaxAccounts.FirstOrDefault(x => x.Id == account.Id)
                ?? throw new ArgumentException($"PersonalTaxAccountId:{account.Id} not found.");

            existing.TaxFilingProgress = account.TaxFilingProgress;

            dbContext.SaveChanges();
        }

        public void ConfirmTaxFiled(PersonalTaxAccount account, string confirmText, DateTime confirmDate, Guid userAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.PersonalTaxAccounts.FirstOrDefault(x => x.Id == account.Id)
                ?? throw new ArgumentException($"PersonalTaxAccountId:{account.Id} not found.");

            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                dbContext.PersonalTaxAccountLogs.Add(new PersonalTaxAccountLog
                {
                    PersonalTaxAccountId = account.Id,
                    OwnerId = account.OwnerId,
                    TaxType = account.TaxType,
                    TaxYear = account.TaxYear,
                    ConfirmationNotes = confirmText,
                    Timestamp = confirmDate,
                    UserAccountId = userAccountId,
                });

                var nextTaxYear = CalculateNextPersonalTaxYear(account.TaxYear, account.TaxType);

                existingAccount.TaxYear = nextTaxYear;
                existingAccount.Step1Completed = false;
                existingAccount.Step2Completed = false;
                existingAccount.Step3Completed = false;
                existingAccount.Step4Completed = false;
                existingAccount.TaxFilingProgress = PersonalTaxFilingProgress.None;
                existingAccount.IsHighPriority = false;

                dbContext.SaveChanges();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Log.Error(ex, $"Unexpected exception while confirming Tax Filing: {ex.Message}. PersonalTaxAccount:{account.Id}, OwnerId:{account.OwnerId}");

                throw ex;
            }
        }

        private string CalculateNextPersonalTaxYear(string taxYear, PersonalTaxType taxType)
        {
            if (int.TryParse(taxYear, out var iTaxYear) == false)
            {
                throw new ArgumentException("");
            }

            switch (taxType)
            {
                case PersonalTaxType.T1:
                    iTaxYear++;
                    break;

                default:
                    break;
            }

            return iTaxYear.ToString();
        }
    }
}
