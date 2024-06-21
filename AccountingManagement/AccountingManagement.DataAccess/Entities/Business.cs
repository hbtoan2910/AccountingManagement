using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.DataAccess.Entities
{
    public class Business
    {
        public Guid Id { get; set; }

        public string LegalName { get; set; }

        public string OperatingName { get; set; }

        public bool IsCorporation { get; set; }

        public string BusinessNumber { get; set; }

        public DateTime? BusinessDate { get; set; }

        public string Email { get; set; }

        public string EmailContact { get; set; }

        public string Address { get; set; }

        public string MailingAddress { get; set; }

        public List<BusinessOwner> BusinessOwners { get; set; }

        public PayrollAccount PayrollAccount { get; set; }

        public List<TaxAccountWithInstalment> TaxAccountWithInstalments { get; set; }

        public TaxAccountWithInstalment HSTAccount =>
            TaxAccountWithInstalments?.FirstOrDefault(x => x.AccountType == TaxAccountType.HST);

        public TaxAccountWithInstalment CorporationTaxAccount =>
            TaxAccountWithInstalments?.FirstOrDefault(x => x.AccountType == TaxAccountType.Corporation);

        public List<TaxAccount> TaxAccounts { get; set; }

        public TaxAccount PSTAccount =>
            TaxAccounts?.FirstOrDefault(x => x.AccountType == TaxAccountType.PST);

        public TaxAccount WSIBAccount =>
            TaxAccounts?.FirstOrDefault(x => x.AccountType == TaxAccountType.WSIB);

        public TaxAccount LIQAccount =>
            TaxAccounts?.FirstOrDefault(x => x.AccountType == TaxAccountType.LIQ);

        public TaxAccount ONTAccount => 
            TaxAccounts?.FirstOrDefault(x => x.AccountType == TaxAccountType.ONT);

        public List<ClientPayment> ClientPayments { get; set; }

        public ClientPayment RegularPayment =>
            ClientPayments?.FirstOrDefault(x => x.PaymentType == ClientPaymentType.Regular);

        public ClientPayment ClientPayment2nd =>
            ClientPayments?.FirstOrDefault(x => x.PaymentType == ClientPaymentType.Secondary);

        public Work Work { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public Business()
        {
            IsActive = true;
            IsDeleted = false;

            BusinessOwners = new List<BusinessOwner>();
        }
    }
}
