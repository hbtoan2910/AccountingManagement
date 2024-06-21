using System.Windows.Media;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class OwnerPersonalTaxAccountModel
    {
        public Owner Owner { get; set; }

        public PersonalTaxAccount PersonalTaxAccount { get; set; }

        public string ConfirmText { get; set; }

        public SolidColorBrush Step1StatusColor => PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step1)
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush Step2StatusColor => PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step2)
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush Step3StatusColor => PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step3)
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush Step4StatusColor => PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step4)
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush Step5StatusColor => PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step5)
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public OwnerPersonalTaxAccountModel(PersonalTaxAccount account)
        {
            PersonalTaxAccount = account;
            Owner = account.Owner;
        }
    }
}
