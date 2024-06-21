using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class EmailTemplate
    {
        public int Id { get; set; }

        public string Template { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public string Keywords { get; set; }

        public DateTime LastUpdated { get; set; }

        public string LastUpdatedBy { get; set; }

        public Guid? EmailSenderId { get; set; }

        public EmailSender EmailSender { get; set; }

        public bool IsDeleted { get; set; }

        public EmailTemplate()
        {
            IsDeleted = false;
        }
    }
}
