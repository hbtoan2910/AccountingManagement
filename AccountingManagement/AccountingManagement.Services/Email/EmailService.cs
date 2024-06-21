using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Mail;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Core;
using System.Text.Json;

namespace AccountingManagement.Services.Email
{
    public interface IEmailService
    {
        void SendCorporationTaxReminderEmail(Dictionary<string, string> parameters, string recipient);
        void SendCorporationInstalmentReminderEmail(Dictionary<string, string> parameters, string recipient);

        void SendHSTReminderEmail(Dictionary<string, string> parameters, string recipients);
        void SendHSTInstalmentReminderEmail(Dictionary<string, string> parameters, string recipients);

        void SendPayrollReminderEmail(Business business, PayrollAccountRecord payrollRecord);
        void SendTimesheetRequestEmail(Dictionary<string, string> parameters, string recipients);

        void SendPaymentReceiptEmail(Business business, ClientPayment clientPayment);

        void SendPersonalContactConfirmationEmail(Dictionary<string, string> parameters, string recipients);
        void SendT1ConfirmationEmail(Dictionary<string, string> parameters, string recipients);
    }

    public class EmailService : IEmailService
    {
        private static readonly EmailCredential SampleCredential = new EmailCredential
        {
            Server = "smtp.office365.com",
            // ServerTargetName = "STARTTLS/smtp.office365.com",
            ServerTargetName = "smtp.office365.com",
            Port = "587",
            EnableSsl = true,
            Email = "payroll@hrkaccounting.com",
            Password = "HRKPayroll101",
        };

        private readonly IEmailSenderQueryService _emailSenderQueryService;
        private readonly IEmailTemplateQueryService _emailTemplateQueryService;

        public EmailService(IEmailSenderQueryService emailSenderQueryService, IEmailTemplateQueryService emailTemplateRepository)
        {
            _emailSenderQueryService = emailSenderQueryService ?? throw new ArgumentNullException(nameof(emailSenderQueryService));
            _emailTemplateQueryService = emailTemplateRepository ?? throw new ArgumentNullException(nameof(emailTemplateRepository));
        }

        private void SendEmail(string templateName, Dictionary<string, string> parameters, string recipients)
        {
            var emailTemplate = _emailTemplateQueryService.GetEmailTemplateByTemplateId(templateName)
                ?? throw new ArgumentNullException($"Email Template [{templateName}] not found!");

            var credential = JsonSerializer.Deserialize<EmailCredential>(emailTemplate.EmailSender.Credential);

            using var client = CreateSmtpClient(credential);

            using var message = BuildEmailMessage(credential.Email, emailTemplate.Subject, emailTemplate.Content, parameters);

            message.To.Add(recipients);

            client.Send(message);
        }

        public void SendCorporationTaxReminderEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("CorporationTaxReminder", parameters, recipients);
        }

        public void SendCorporationInstalmentReminderEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("CorporationInstalmentReminder", parameters, recipients);
        }

        public void SendHSTReminderEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("HSTReminder", parameters, recipients);
        }

        public void SendHSTInstalmentReminderEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("HSTInstalmentReminder", parameters, recipients);
        }

        public void SendPayrollReminderEmail(Business business, PayrollAccountRecord payrollRecord)
        {
            var parameters = new Dictionary<string, string>
            {
                { "{LegalName}", business.LegalName },
                { "{OperatingName}", business.OperatingName },
                { "{EmailContact}", business.EmailContact },
                { "{PD7AConfirmation}", payrollRecord.PD7AConfirmation ?? "N/A" },
                { "{PD7ACycle}", payrollRecord.PayrollAccount.PD7ACycle.ToString() },
                { "{PD7ADueDate}", payrollRecord.PD7ADueDate?.ToString(Constant.DateFormat) ?? "[ ]" },
            };

            SendEmail("PD7AReminder", parameters, business.Email);
        }

        public void SendTimesheetRequestEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("TimesheetRequest", parameters, recipients);
        }

        public void SendPaymentReceiptEmail(Business business, ClientPayment clientPayment)
        {
            var parameters = new Dictionary<string, string>
            {
                { "{LegalName}", business.LegalName },
                { "{OperatingName}", business.OperatingName },
                { "{EmailContact}", business.EmailContact },
                { "{PaymentCycle}", clientPayment.PaymentCycle.ToString() },
                { "{PaymentAmount}", clientPayment.PaymentAmount.ToString("C") },
                { "{DueDate}", clientPayment.DueDate.ToString("MMM-dd-yyyy") },
                { "{PaymentNotes}", clientPayment.Notes },
                { "{SpecialText}", clientPayment.TmpConfirmationText },
            };

            SendEmail("ClientPaymentReceipt", parameters, business.Email);
        }

        public void SendPersonalContactConfirmationEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("T1PersonalContactConfirmation", parameters, recipients);
        }

        public void SendT1ConfirmationEmail(Dictionary<string, string> parameters, string recipients)
        {
            SendEmail("T1Confirmation", parameters, recipients);
        }

        private MailMessage BuildEmailMessage(string sender, string subject, string content, Dictionary<string, string> parameters)
        {
            if (parameters != null && string.IsNullOrWhiteSpace(content) == false)
            {
                foreach (var parameter in parameters)
                {
                    if (content.IndexOf(parameter.Key) >= 0)
                    {
                        content = content.Replace(parameter.Key, parameter.Value);
                    }
                }
            }

            return new MailMessage()
            {
                From = new MailAddress(sender),
                Subject = subject,
                IsBodyHtml = false,
                Body = content,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
            };
        }

        private SmtpClient CreateSmtpClient()
        {
            var deserialized = JsonSerializer.Serialize(SampleCredential);

            return new SmtpClient()
            {
                Host = SampleCredential.Server,
                Port = Convert.ToInt32(SampleCredential.Port),
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(SampleCredential.Email, SampleCredential.Password),
                TargetName = SampleCredential.ServerTargetName,
                EnableSsl = SampleCredential.EnableSsl,
            };
        }

        private SmtpClient CreateSmtpClient(EmailCredential credential)
        {
            return new SmtpClient
            {
                Host = credential.Server,
                Port = Convert.ToInt32(credential.Port),
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(credential.Email, credential.Password),
                TargetName = credential.ServerTargetName,
                EnableSsl = credential.EnableSsl,
            };
        }

        public void SendTestEmail()
        {
            using var client = CreateSmtpClient(SampleCredential);

            using var mailMessage = BuildEmailMessage(SampleCredential.Email, "TEST-SUBJECT", "<h1>Test Email</h1><div>Amount: {Amount}</div>",
                new Dictionary<string, string> { { "{Amount}", "777,777.77" } });

            mailMessage.To.Add("charles.nguyen.dc@gmail.com");

            try
            {
                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending test email. {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
