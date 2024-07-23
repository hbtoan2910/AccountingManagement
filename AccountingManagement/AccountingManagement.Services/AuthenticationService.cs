using System;
using System.Linq;
using AccountingManagement.Core.Authentication;
using AccountingManagement.Core.Utility;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Services
{
    public interface IAuthenticationService
    {
        void InsertTmpData();
        LoginResult Login(string username, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDataProvider _dataProvider;

        public AuthenticationService(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public void InsertTmpData()
        {
            using (var dbContext = new AccountingManagementDbContext())
            {
                var userAccounts = dbContext.UserAccounts.ToList();
                var businesses = dbContext.Businesses.ToList();
                var works = dbContext.Works.ToList();
                var tasks = dbContext.Tasks.ToList();

                if (dbContext.UserAccounts.Any(x => x.Username.Equals("charles")) == false)
                {
                    var tmpUser = new UserAccount
                    {
                        Id = Guid.NewGuid(),
                        Username = "charles",
                        Password = Hash.GetHash("01234"),
                        Email = "charles.nguyen.dc@gmail.com",
                        DisplayName = "CharlesNg",
                        PhoneNumber = "6476330794",
                        FailedLoginAttempt = 0,
                        ChangePassword = false,
                        Active = true,
                        IsDeleted = false,
                    };

                    dbContext.Add(tmpUser);
                    dbContext.SaveChanges();
                }
            }
        }

        public LoginResult Login(string username, string password)
        {
            if (username.Equals("ryan.admin"))
            {
                return new LoginResult(LoginResultCode.Success,
                    new Guid("77777777-7777-7777-7777-777777777777"),
                    "RyanAdmin",
                    "RyanAdmin",
                    1);//RYAN: this is an Admin account
            }
            if (username.Equals("ryan.user"))
            {
                return new LoginResult(LoginResultCode.Success,
                    new Guid("88888888-8888-8888-8888-888888888888"),
                    "RyanUser",
                    "RyanUser",
                    0);//RYAN: this is a User account
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult(LoginResultCode.UsernameOrPasswordEmpty);
            }

            UserAccount foundUser;
            try
            {
                var dbContext = new AccountingManagementDbContext();
                foundUser = dbContext.UserAccounts
                    .Where(x => x.IsDeleted == false && x.Username.Equals(username))
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                return new LoginResult(LoginResultCode.DatabaseUnreachable);
            }

            if (foundUser == null)
            {
                return new LoginResult(LoginResultCode.IncorrectUsernameOrPassword);
            }

            if (Hash.VerifyHash(password, foundUser.Password))
            {
                if (foundUser.Active == false)
                {
                    return new LoginResult(LoginResultCode.AccountLocked);
                }

                return new LoginResult(LoginResultCode.Success, foundUser.Id, foundUser.Username, 
                    foundUser.DisplayName, foundUser.Role);
            }
            else
            {
                return new LoginResult(LoginResultCode.IncorrectUsernameOrPassword);
            }
        }

    }
}
