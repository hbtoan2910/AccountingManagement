using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class TaxInstalmentAccountModel
    {
        public Business Business { get; set; }

        public TaxAccountWithInstalment TaxAccount { get; set; }

        public UserAccount UserAccount { get; set; }

        public string ConfirmText { get; set; }

        public string InstalmentConfirmText { get; set; }

        public TaxInstalmentAccountModel(TaxAccountWithInstalment taxAccount)
        {
            TaxAccount = taxAccount;
            Business = taxAccount.Business;
            UserAccount = taxAccount.UserAccount;
        }
    }
}
