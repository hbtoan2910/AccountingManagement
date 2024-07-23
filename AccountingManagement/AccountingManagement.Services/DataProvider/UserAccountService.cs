using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.Services
{
    public interface IUserAccountService
    {
        List<UserAccount> GetUserAccounts();
        UserAccount GetUserAccountById(Guid userId);

        bool UpsertUserAccount(UserAccount userAccount);
        bool DeleteUserAccount(Guid userAccountId);
    }

    public class UserAccountService : IUserAccountService
    {
        public List<UserAccount> GetUserAccounts()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.UserAccounts.Where(i => i.IsDeleted == false && i.Active)
                .OrderBy(i => i.DisplayName)
                .AsNoTracking()
                .ToList();
        }

        public UserAccount GetUserAccountById(Guid userId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.UserAccounts.FirstOrDefault(x => x.Id == userId);
        }


        public bool UpsertUserAccount(UserAccount userAccount)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingUser = dbContext.UserAccounts.FirstOrDefault(x => x.Id == userAccount.Id);
            if (existingUser != null)
            {
                existingUser.Username = userAccount.Username;
                existingUser.Password = userAccount.Password;
                existingUser.DisplayName = userAccount.DisplayName;
                existingUser.Email = userAccount.Email;
                existingUser.PhoneNumber = userAccount.PhoneNumber;
                existingUser.Active = userAccount.Active;
                existingUser.Role = userAccount.Role;
            }
            else
            {
                dbContext.UserAccounts.Add(userAccount);
            }

            dbContext.SaveChanges();

            return true;
        }

        public bool DeleteUserAccount(Guid userAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingUser = dbContext.UserAccounts.FirstOrDefault(x => x.Id == userAccountId);
            if (existingUser != null)
            {
                existingUser.IsDeleted = true;

                dbContext.SaveChanges();
            }

            return true;
        }
    }
}
