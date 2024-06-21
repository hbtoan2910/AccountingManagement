using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class PersonalTaxAccountLog
    {
        public int Id { get; set; }

        public Guid PersonalTaxAccountId { get; set; }

        public PersonalTaxAccount PersonalTaxAccount { get; set; }

        public Guid OwnerId { get; set; }

        public Owner Owner { get; set; }

        public PersonalTaxType TaxType { get; set; }

        public string TaxYear { get; set; }

        public string ConfirmationNotes { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid UserAccountId { get; set; }

        public UserAccount UserAccount { get; set; }

        public PersonalTaxAccountLog()
        { }
    }
}
