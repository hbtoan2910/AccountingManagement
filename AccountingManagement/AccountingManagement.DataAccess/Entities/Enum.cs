namespace AccountingManagement.DataAccess.Entities
{
    // DO NOT edit existing enum value because the numeric values are stored in DB
    public enum FilingCycle
    {
        None = 0,
        Weekly = 1,
        BiWeekly = 2,
        SemiMonthly = 3,
        Monthly = 4,
        Quarterly = 5,
        Annually = 6,
        BiMonthly = 7, // Special Filing Cycle for PD7A. Due Dates are always 10th and 25th of the month
    }

    // DO NOT edit existing enum value because the numeric values are stored in DB
    public enum TaxAccountType
    {
        Undefined = 0,
        HST = 1,
        Corporation = 2,
        PST = 3,
        WCB = 4, // Not in use
        WSIB = 5,
        LIQ = 6,
        ONT = 7,
    }

    public enum ClientPaymentType
    {
        Undefined = 0,
        Regular = 1,
        Secondary = 2,
        Tertiary = 3,
    }

    public enum ClientPaymentCycle
    {
        Undefined = 0,
        Monthly = 1,
        BiMonthly = 2,
        Quarterly = 3,
    }

    public enum PersonalTaxType
    {
        Undefined = 0,
        T1 = 1,
    }

    public enum PersonalTaxFilingProgress : byte
    {
        None = 0,
        Step1 = 1,
        Step2 = 2,
        Step3 = 4,
        Step4 = 8,
        Step5 = 16,
        Step6 = 32,
        Step7 = 64,
        Step8 = 128
    }
}
