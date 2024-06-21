using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.Services
{
    public interface IBusinessOwnerService
    {
        List<Business> GetBusinesses();
        List<Business> GetBusinessListOnly();
        Business GetBusinessById(Guid businessId);
        Business GetBusinessByIdWithFullDetails(Guid businessId);
        List<Owner> GetOwners();
        List<Owner> GetOwnersByBusinessId(Guid businessId);
        Owner GetOwnerById(Guid ownerId);
        List<Note> GetNotesByBusinessId(Guid businessId);
        Note GetNoteById(int noteId);
        List<BusinessInfo> GetBusinessInfosByBusinessId(Guid businessId);
        BusinessInfo GetBusinessInfoById(int businessInfoId);
        List<BankAccount> GetBankAccountsByBusinessId(Guid businessId);
        BankAccount GetBankAccountById(Guid bankAccountId);


        bool HardDeleteBusiness(Guid businessId);
        bool SoftDeleteBusiness(Guid businessId);
        bool UnDeleteBusiness(Guid businessId);

        bool DeleteOwner(Guid ownerId);
        bool UndoDeleteOwner(Guid ownerId);

        bool DeleteNote(int noteId);
        bool DeleteBankAccount(Guid bankAccountId);
        bool UpsertBusiness(Business business);
        bool UpsertOwner(Owner owner);
        bool UpsertNote(Note note);
        bool UpsertBusinessInfo(BusinessInfo businessInfo);
        bool UpsertBankAccount(BankAccount bankAccount);
        bool InsertBusinessOwner(Guid businessId, Guid ownerId);
        bool RemoveBusinessOwner(Guid businessId, Guid ownerId);
        bool RemoveBusinessOwner(int businessOwnerId);
    }

    public class BusinessOwnerService : IBusinessOwnerService
    {
        public List<Business> GetBusinesses()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Businesses
                .Include(b => b.BusinessOwners)
                .ThenInclude(bo => bo.Owner)
                .Include(b => b.TaxAccounts)
                .Include(b => b.TaxAccountWithInstalments)
                .ThenInclude(u => u.UserAccount)
                .Include(b => b.PayrollAccount)
                .Include(b => b.ClientPayments)
                .Include(b => b.Work)
                .ThenInclude(w => w.Tasks)
                .ThenInclude(t => t.UserAccount)
                .AsNoTracking()
                .ToList();
        }

        public List<Business> GetBusinessListOnly()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Businesses.Where(x => x.IsDeleted == false)
                .ToList();
        }

        public Business GetBusinessById(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Businesses.Where(b => b.Id == businessId)
                .Include(b => b.BusinessOwners)
                .ThenInclude(bo => bo.Owner)
                .Include(b => b.TaxAccounts)
                .Include(b => b.TaxAccountWithInstalments)
                .Include(b => b.PayrollAccount)
                .Include(b => b.ClientPayments)
                .FirstOrDefault();
        }

        public Business GetBusinessByIdWithFullDetails(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Businesses.Where(b => b.Id == businessId)
                .Include(b => b.BusinessOwners)
                .ThenInclude(bo => bo.Owner)
                .Include(b => b.TaxAccounts)
                .Include(b => b.TaxAccountWithInstalments)
                .Include(b => b.PayrollAccount)
                .Include(b => b.ClientPayments)
                .FirstOrDefault();
        }


        public List<Owner> GetOwners()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Owners
                .Include(b => b.BusinessOwners)
                .ThenInclude(bo => bo.Business)
                .Include(x => x.PersonalTaxAccounts)
                .AsNoTracking()
                .ToList();
        }

        public List<Owner> GetOwnersByBusinessId(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Owners
                .Where(o => o.BusinessOwners.Any(bo => bo.BusinessId == businessId))
                .ToList();
        }

        public Owner GetOwnerById(Guid ownerId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Owners.Where(o => o.Id == ownerId)
                .Include(o => o.BusinessOwners)
                .ThenInclude(bo => bo.Business)
                .Include(x => x.PersonalTaxAccounts)
                .FirstOrDefault();
        }


        public List<Note> GetNotesByBusinessId(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Notes
                .Where(x => x.BusinessId == businessId && x.IsDeleted == false)
                .OrderByDescending(x => x.Created)
                .ToList();
        }

        public Note GetNoteById(int noteId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Notes.FirstOrDefault(x => x.Id == noteId);
        }

        public List<BusinessInfo> GetBusinessInfosByBusinessId(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.BusinessInfos
                .Where(x => x.BusinessId == businessId && x.IsDeleted == false)
                .OrderByDescending(x => x.ModifiedTime)
                .ToList();
        }

        public BusinessInfo GetBusinessInfoById(int businessInfoId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.BusinessInfos.FirstOrDefault(x => x.Id == businessInfoId);
        }

        public List<BankAccount> GetBankAccountsByBusinessId(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.BankAccounts
                .Where(x => x.BusinessId == businessId && x.IsDeleted == false)
                .ToList();
        }

        public BankAccount GetBankAccountById(Guid bankAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.BankAccounts.FirstOrDefault(x => x.Id == bankAccountId);
        }

        public bool SoftDeleteBusiness(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingBusiness = dbContext.Businesses.FirstOrDefault(x => x.Id == businessId);
            if (existingBusiness == null)
            {
                return false;
            }

            existingBusiness.IsDeleted = true;

            dbContext.SaveChanges();

            return true;
        }

        public bool UnDeleteBusiness(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingBusiness = dbContext.Businesses.FirstOrDefault(x => x.Id == businessId);
            if (existingBusiness == null)
            {
                return false;
            }

            existingBusiness.IsDeleted = false;

            dbContext.SaveChanges();

            return true;
        }

        public bool HardDeleteBusiness(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingBusiness = dbContext.Businesses.FirstOrDefault(x => x.Id == businessId);
            if (existingBusiness == null)
            {
                return false;
            }

            var transaction = dbContext.Database.BeginTransaction();
            try
            {
                var businessOwnersToDelete = dbContext.BusinessOwners.Where(x => x.BusinessId == businessId);
                dbContext.BusinessOwners.RemoveRange(businessOwnersToDelete);

                var bankAccountsToDelete = dbContext.BankAccounts.Where(x => x.BusinessId == businessId);
                dbContext.BankAccounts.RemoveRange(bankAccountsToDelete);

                // TODO: Hard delete a business

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }

            return true;
        }

        public bool DeleteOwner(Guid ownerId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingOwner = dbContext.Owners.FirstOrDefault(x => x.Id == ownerId);
            if (existingOwner != null)
            {
                dbContext.Database.BeginTransaction();

                var existingRelationship = dbContext.BusinessOwners.Where(x => x.OwnerId == ownerId);
                if (existingRelationship.Count() > 0)
                {
                    dbContext.BusinessOwners.RemoveRange(existingRelationship);
                }

                // dbContext.Owners.Remove(existingOwner);

                // Deactive T1 Account together with the Owner
                var personalTaxAccounts = dbContext.PersonalTaxAccounts.Where(x => x.OwnerId == ownerId);
                if (personalTaxAccounts != null && personalTaxAccounts.Count() > 0)
                {
                    foreach (var account in personalTaxAccounts)
                    {
                        account.IsActive = false;
                    }
                }

                existingOwner.IsDeleted = true;

                dbContext.SaveChanges();
                dbContext.Database.CommitTransaction();
            }
            else
            {
                Log.Warning($"Warning - OwnerId:{ownerId} not found while deleting");
            }

            return true;
        }

        public bool UndoDeleteOwner(Guid ownerId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingOwner = dbContext.Owners.FirstOrDefault(x => x.Id == ownerId);
            if (existingOwner != null)
            {
                existingOwner.IsDeleted = false;
                dbContext.SaveChanges();
            }
            else
            {
                Log.Warning($"Warning - OwnerId:{ownerId} not found while re-activating");
            }

            return true;
        }

        public bool DeleteNote(int noteId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingNote = dbContext.Notes.FirstOrDefault(x => x.Id == noteId);
            if (existingNote != null)
            {
                dbContext.Notes.Remove(existingNote);
                dbContext.SaveChanges();
            }

            return true;
        }

        public bool DeleteBankAccount(Guid bankAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.BankAccounts.FirstOrDefault(x => x.Id == bankAccountId);
            if (existing != null)
            {
                dbContext.BankAccounts.Remove(existing);
                dbContext.SaveChanges();
            }

            return true;
        }

        public bool UpsertBusiness(Business business)
        {
            using var dbContext = new AccountingManagementDbContext();

            dbContext.Database.BeginTransaction();

            var existingBusiness = dbContext.Businesses.FirstOrDefault(b => b.Id == business.Id);
            if (existingBusiness != null)
            {
                Log.Information($"Updating BusinessId:{business.Id}");

                existingBusiness.OperatingName = business.OperatingName;
                existingBusiness.LegalName = business.LegalName;
                existingBusiness.BusinessNumber = business.BusinessNumber;
                existingBusiness.BusinessDate = business.BusinessDate;
                existingBusiness.IsCorporation = business.IsCorporation;
                existingBusiness.Address = business.Address;
                existingBusiness.MailingAddress = business.MailingAddress;
                existingBusiness.Email = business.Email;
                existingBusiness.EmailContact = business.EmailContact;
                existingBusiness.IsActive = business.IsActive;
            }
            else
            {
                Log.Information($"Adding new BusinessId:{business.Id}");

                dbContext.Businesses.Add(new Business
                {
                    Id = business.Id,
                    OperatingName = business.OperatingName,
                    LegalName = business.LegalName,
                    BusinessNumber = business.BusinessNumber,
                    BusinessDate = business.BusinessDate,
                    IsCorporation = business.IsCorporation,
                    Address = business.Address,
                    MailingAddress = business.MailingAddress,
                    Email = business.Email,
                    EmailContact = business.EmailContact,
                    IsActive = business.IsActive,
                });
            }

            if (dbContext.Works.Any(x => x.BusinessId == business.Id) == false)
            {
                dbContext.Works.Add(new Work
                {
                    Id = Guid.NewGuid(),
                    BusinessId = business.Id,
                    Description = $"{business.OperatingName} Work",
                    WorkStatus = WorkStatus.New,
                    Priority = 0,
                    Notes = string.Empty,
                    IsDeleted = false
                });
            }

            dbContext.SaveChanges();
            dbContext.Database.CommitTransaction();

            return true;
        }

        public bool UpsertOwner(Owner owner)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingOwner = dbContext.Owners.FirstOrDefault(i => i.Id == owner.Id);
            if (existingOwner != null)
            {
                existingOwner.Name = owner.Name;
                existingOwner.DOB = owner.DOB;
                existingOwner.SIN = owner.SIN;
                existingOwner.Address = owner.Address;
                existingOwner.Address2 = owner.Address2;
                existingOwner.PhoneNumber = owner.PhoneNumber;
                existingOwner.PhoneNumber2 = owner.PhoneNumber2;
                existingOwner.Email = owner.Email;
                existingOwner.Notes = owner.Notes;

                dbContext.SaveChanges();
            }
            else
            {
                // Create new Task object and DO NOT assign Work or UserAccount property
                var newOwner = new Owner
                {
                    Id = owner.Id,
                    Name = owner.Name,
                    DOB = owner.DOB,
                    SIN = owner.SIN,
                    Address = owner.Address,
                    Address2 = owner.Address2,
                    PhoneNumber = owner.PhoneNumber,
                    PhoneNumber2 = owner.PhoneNumber2,
                    Email = owner.Email,
                    Notes = owner.Notes,
                    IsDeleted = false,
                };

                dbContext.Owners.Add(newOwner);
                dbContext.SaveChanges();
            }

            return true;
        }

        public bool UpsertNote(Note note)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.Notes.FirstOrDefault(x => x.Id == note.Id);
            if (existing != null)
            {
                existing.Header = note.Header;
                existing.Content = note.Content;
                existing.LastUpdated = note.LastUpdated;
            }
            else
            {
                note.Created = DateTime.Now;

                dbContext.Notes.Add(note);
            }

            dbContext.SaveChanges();
            return true;
        }

        public bool UpsertBusinessInfo(BusinessInfo businessInfo)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.BusinessInfos.FirstOrDefault(x => x.Id == businessInfo.Id);
            if (existing != null)
            {
                existing.Title = businessInfo.Title;
                existing.Content = businessInfo.Content;
                existing.ModifiedTime = businessInfo.ModifiedTime;
                existing.LastUpdated = businessInfo.LastUpdated;
            }
            else
            {
                dbContext.BusinessInfos.Add(businessInfo);
            }

            dbContext.SaveChanges();
            return true;
        }

        public bool UpsertBankAccount(BankAccount bankAccount)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.BankAccounts.FirstOrDefault(x => x.Id == bankAccount.Id);
            if (existing != null)
            {
                existing.Title = bankAccount.Title;
                existing.Description = bankAccount.Description;
                existing.Website = bankAccount.Website;
                existing.AccountUsername = bankAccount.AccountUsername;
                existing.AccountPassword = bankAccount.AccountPassword;
                existing.AccountNote = bankAccount.AccountNote;
            }
            else
            {
                dbContext.BankAccounts.Add(bankAccount);
            }

            dbContext.SaveChanges();
            return true;
        }

        public bool InsertBusinessOwner(Guid businessId, Guid ownerId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingOwner = dbContext.BusinessOwners
                    .FirstOrDefault(bo => bo.BusinessId == businessId && bo.OwnerId == ownerId);

            if (existingOwner == null)
            {
                var newBO = new BusinessOwner(businessId, ownerId);

                dbContext.BusinessOwners.Add(newBO);
                dbContext.SaveChanges();

                return true;
            }

            return false;
        }

        public bool RemoveBusinessOwner(Guid businessId, Guid ownerId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingBO = dbContext.BusinessOwners
                    .FirstOrDefault(bo => bo.BusinessId == businessId && bo.OwnerId == ownerId);

            if (existingBO != null)
            {
                dbContext.BusinessOwners.Remove(existingBO);
                dbContext.SaveChanges();

                return true;
            }

            return false;
        }

        public bool RemoveBusinessOwner(int businessOwnerId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingBO = dbContext.BusinessOwners.FirstOrDefault(bo => bo.Id == businessOwnerId);

            if (existingBO != null)
            {
                dbContext.BusinessOwners.Remove(existingBO);
                dbContext.SaveChanges();

                return true;
            }

            return false;
        }
    }
}
