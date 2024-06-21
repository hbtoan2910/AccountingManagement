using System;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class BusinessTaxAccountModel
    {
        public Business Business { get; set; }

        public TaxAccount TaxAccount { get; set; }

        public string ConfirmText { get; set; }

        public BusinessTaxAccountModel(TaxAccount taxAccount)
        {
            TaxAccount = taxAccount;
            Business = taxAccount.Business;
        }
    }
}
