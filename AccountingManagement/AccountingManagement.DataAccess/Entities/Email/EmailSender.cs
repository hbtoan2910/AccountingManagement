using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class EmailSender
    {
        public Guid Id { get; set; }

        public string Sender { get; set; }

        public string Email { get; set; }

        public string Credential { get; set; }

        public EmailSender()
        { }
    }

    public class EmailCredential
    {
        public string Server { get; set; }
        public string ServerTargetName { get; set; }
        public string Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public EmailCredential()
        { }
    }
}
