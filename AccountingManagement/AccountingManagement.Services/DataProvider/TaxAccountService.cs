using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.Services
{
    public interface ITaxAccountService
    {
        List<TaxAccount> GetTaxAccounts();
        List<TaxAccount> GetTaxAccountsByAccountType(TaxAccountType type);
        TaxAccount GetTaxAccountById(Guid id);
        bool UpsertTaxAccount(TaxAccount taxAccount);

        List<TaxAccountWithInstalment> GetTaxAccountWithInstalments();
        List<TaxAccountWithInstalment> GetTaxAccountWithInstalmentRequiredAndActive();
        List<TaxAccountWithInstalment> GetTaxAccountWithInstalmentsByAccountType(TaxAccountType type);
        TaxAccountWithInstalment GetTaxAccountWithInstalmentById(Guid id);
        bool UpsertTaxAccountWithInstalment(TaxAccountWithInstalment taxAccount);

        List<TaxFilingLog> GetTaxFilingLogs(DateTime cutoffDate);

        // Personal Tax // T1
        List<PersonalTaxAccount> GetPersonalTaxAccounts();
        List<PersonalTaxAccount> GetPersonalTaxAccountsByType(PersonalTaxType taxType);
        PersonalTaxAccount GetPersonalTaxAccountById(Guid id);
        bool UpsertPersonalTaxAccount(PersonalTaxAccount pta);

        List<PersonalTaxAccountLog> GetPersonalTaxAccountLogsByType(PersonalTaxType taxType);
    }

    public class TaxAccountService : ITaxAccountService
    {
        public TaxAccountService()
        { }

        public List<TaxAccount> GetTaxAccounts()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccounts.Where(a => a.IsActive)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public List<TaxAccount> GetTaxAccountsByAccountType(TaxAccountType type)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccounts.Where(a => a.IsActive && a.AccountType == type)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public TaxAccount GetTaxAccountById(Guid id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccounts.Where(x => x.Id == id)
                .Include(x => x.Business)
                .FirstOrDefault();
        }

        public bool UpsertTaxAccount(TaxAccount taxAccount)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.TaxAccounts.FirstOrDefault(x => x.Id == taxAccount.Id);
            if (existingAccount == null)
            {
                dbContext.TaxAccounts.Add(taxAccount);
            }
            else
            {
                existingAccount.AccountNumber = taxAccount.AccountNumber;
                existingAccount.AccountType = taxAccount.AccountType;
                existingAccount.Cycle = taxAccount.Cycle;
                existingAccount.EndingPeriod = taxAccount.EndingPeriod;
                existingAccount.DueDate = taxAccount.DueDate;
                existingAccount.Notes = taxAccount.Notes;
                existingAccount.IsActive = taxAccount.IsActive;
            }

            dbContext.SaveChanges();
            return true;
        }


        public List<TaxAccountWithInstalment> GetTaxAccountWithInstalments()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccountWithInstalments.Where(a => a.IsActive)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }
        //RYAN: new method for Instalment view
        public List<TaxAccountWithInstalment> GetTaxAccountWithInstalmentRequiredAndActive()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccountWithInstalments.Where(a => a.IsActive && a.InstalmentRequired)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public List<TaxAccountWithInstalment> GetTaxAccountWithInstalmentsByAccountType(TaxAccountType type)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccountWithInstalments.Where(a => a.IsActive && a.AccountType == type)
                .Include(a => a.Business)
                .Where(a => a.Business.IsDeleted == false)
                .Include(a => a.UserAccount)
                .AsNoTracking()
                .ToList();
        }

        public TaxAccountWithInstalment GetTaxAccountWithInstalmentById(Guid id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxAccountWithInstalments.Where(x => x.Id == id)
                .Include(x => x.Business)
                .Include(x => x.UserAccount)
                .FirstOrDefault();
        }

        public bool UpsertTaxAccountWithInstalment(TaxAccountWithInstalment taxAccount)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.TaxAccountWithInstalments.FirstOrDefault(x => x.Id == taxAccount.Id);
            if (existingAccount == null)
            {
                dbContext.TaxAccountWithInstalments.Add(taxAccount);
            }
            else
            {
                existingAccount.AccountNumber = taxAccount.AccountNumber;
                existingAccount.AccountType = taxAccount.AccountType;
                existingAccount.Cycle = taxAccount.Cycle;
                existingAccount.EndingPeriod = taxAccount.EndingPeriod;
                existingAccount.DueDate = taxAccount.DueDate;
                existingAccount.Notes = taxAccount.Notes;
                existingAccount.UserAccountId = taxAccount.UserAccountId;
                existingAccount.ProgressNotes = taxAccount.ProgressNotes;
                existingAccount.IsActive = taxAccount.IsActive;

                existingAccount.InstalmentRequired = taxAccount.InstalmentRequired;
                existingAccount.InstalmentAmount = taxAccount.InstalmentAmount;
                existingAccount.InstalmentDueDate = taxAccount.InstalmentDueDate;
            }

            dbContext.SaveChanges();
            return true;
        }

        public List<TaxFilingLog> GetTaxFilingLogs(DateTime cutoffDate)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.TaxFilingLogs
                .Include(log => log.Business)
                .Include(log => log.UserAccount)
                .Where(log => log.Timestamp >= cutoffDate)
                .AsNoTracking()
                .ToList();
        }

        public List<PersonalTaxAccount> GetPersonalTaxAccounts()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PersonalTaxAccounts.Where(a => a.IsActive)
                .Include(a => a.Owner)
                .Where(a => a.Owner.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public List<PersonalTaxAccount> GetPersonalTaxAccountsByType(PersonalTaxType taxType)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PersonalTaxAccounts.Where(a => a.IsActive && a.TaxType == taxType)
                .Include(a => a.Owner)
                .Where(a => a.Owner.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public PersonalTaxAccount GetPersonalTaxAccountById(Guid id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PersonalTaxAccounts.Where(a => a.Id == id)
                .Include(a => a.Owner)
                .FirstOrDefault();
        }

        public bool UpsertPersonalTaxAccount(PersonalTaxAccount pta)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.PersonalTaxAccounts.FirstOrDefault(i => i.Id == pta.Id);
            if (existing != null)
            {
                existing.TaxType = pta.TaxType;
                existing.TaxNumber = pta.TaxNumber;
                existing.TaxYear = pta.TaxYear;
                existing.Description = pta.Description;
                existing.Notes = pta.Notes;
                existing.TaxFilingProgress = pta.TaxFilingProgress;
                existing.Step1Completed = pta.Step1Completed;
                existing.Step2Completed = pta.Step2Completed;
                existing.Step3Completed = pta.Step3Completed;
                existing.Step4Completed = pta.Step4Completed;
                existing.IsHighPriority = pta.IsHighPriority;
                existing.IsActive = pta.IsActive;

                dbContext.SaveChanges();
            }
            else
            {
                var account = new PersonalTaxAccount
                {
                    Id = pta.Id,
                    OwnerId = pta.OwnerId,
                    TaxType = pta.TaxType,
                    TaxNumber = pta.TaxNumber,
                    TaxYear = pta.TaxYear,
                    Description = pta.Description,
                    Notes = pta.Notes,
                    TaxFilingProgress = pta.TaxFilingProgress,
                    Step1Completed = pta.Step1Completed,
                    Step2Completed = pta.Step2Completed,
                    Step3Completed = pta.Step3Completed,
                    Step4Completed = pta.Step4Completed,
                    IsHighPriority = pta.IsHighPriority,
                    IsActive = pta.IsActive
                };

                dbContext.PersonalTaxAccounts.Add(account);
                dbContext.SaveChanges();
            }

            return true;
        }

        public List<PersonalTaxAccountLog> GetPersonalTaxAccountLogsByType(PersonalTaxType taxType)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PersonalTaxAccountLogs
                // .Where(a => a.TaxType == taxType)
                .Include(a => a.Owner)
                .Where(a => a.Owner.IsDeleted == false)
                .Include(a => a.UserAccount)
                .AsNoTracking()
                .ToList();
        }
    }
}
