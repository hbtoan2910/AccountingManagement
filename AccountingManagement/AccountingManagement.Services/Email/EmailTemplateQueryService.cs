using System.Collections.Generic;
using System.Linq;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountingManagement.Services.Email
{
    public interface IEmailTemplateQueryService
    {
        List<EmailTemplate> GetEmailTemplates();
        EmailTemplate GetEmailTemplateByTemplateId(string templateId);

        bool UpsertEmailTemplate(EmailTemplate emailTemplate);
    }

    public class EmailTemplateQueryService : IEmailTemplateQueryService
    {
        public EmailTemplateQueryService()
        { }

        public List<EmailTemplate> GetEmailTemplates()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.EmailTemplates
                    .Where(x => x.IsDeleted == false)
                    .ToList();
        }

        public EmailTemplate GetEmailTemplateByTemplateId(string templateId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.EmailTemplates.Where(x => x.Template == templateId && x.IsDeleted == false)
                .Include(x => x.EmailSender)
                .FirstOrDefault();
        }

        public bool UpsertEmailTemplate(EmailTemplate emailTemplate)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existing = dbContext.EmailTemplates.FirstOrDefault(x => x.Id == emailTemplate.Id);
            if (existing != null)
            {
                existing.Subject = emailTemplate.Subject;
                existing.Content = emailTemplate.Content;
                existing.EmailSenderId = emailTemplate.EmailSenderId;
                existing.LastUpdated = emailTemplate.LastUpdated;
                existing.LastUpdatedBy = emailTemplate.LastUpdatedBy;
            }
            else
            {
                dbContext.EmailTemplates.Add(emailTemplate);
            }

            dbContext.SaveChanges();
            return true;
        }
    }
}
