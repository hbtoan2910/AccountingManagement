using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.Services.Email
{
    public interface IEmailSenderQueryService
    {
        List<EmailSender> GetEmailSenders();
        EmailSender GetEmailSenderById(Guid id);

        bool UpsertEmailSender(EmailSender emailSender);
    }

    public class EmailSenderQueryService : IEmailSenderQueryService
    {
        public List<EmailSender> GetEmailSenders()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.EmailSenders
                .AsNoTracking()
                .ToList();
        }

        public EmailSender GetEmailSenderById(Guid id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.EmailSenders.Where(x => x.Id == id)
                .FirstOrDefault();
        }

        // TODO:
        public bool UpsertEmailSender(EmailSender emailSender)
        {
            throw new NotImplementedException();
        }
    }
}
