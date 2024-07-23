using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Serilog;

namespace AccountingManagement.Services
{
    public interface IFilingHandler
    {
        void ConfirmTaxFiling(TaxAccount taxAccount, string confirmText, DateTime confirmDate, Guid userAccountId);

        void ConfirmTaxFiling(TaxAccountWithInstalment taxAccount, string confirmText, DateTime confirmDate, Guid userAccountId);

        void ConfirmInstalment(TaxAccountWithInstalment taxAccount, string confirmText, DateTime confirmDate, Guid userAccountId);

        void GenerateMissingHSTAccounts();

        //RYAN add
        DateTime CalculateNextInstalmentDueDateNew(DateTime currentEndingPeriod);
        DateTime CalculateNextPersonalInstalmentDueDate();
    }

    public class FilingHandler : IFilingHandler
    {
        private readonly IDataProvider _dataProvider;

        public FilingHandler(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }


        public void ConfirmTaxFiling(TaxAccount taxAccount, string confirmText, DateTime confirmDate, Guid userAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.TaxAccounts.FirstOrDefault(x => x.Id == taxAccount.Id)
                ?? throw new ArgumentException($"TaxAccountId:{taxAccount.Id} not found.");

            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                dbContext.TaxFilingLogs.Add(new TaxFilingLog
                {
                    BusinessId = taxAccount.BusinessId,
                    TaxAccountId = taxAccount.Id,
                    AccountType = taxAccount.AccountType,
                    AccountNumber = taxAccount.AccountNumber,
                    Cycle = taxAccount.Cycle,
                    EndingPeriod = taxAccount.EndingPeriod,
                    DueDate = taxAccount.DueDate,
                    Notes = taxAccount.Notes,
                    ConfirmationNotes = confirmText,
                    Timestamp = confirmDate,
                    UserAccountId = userAccountId,
                });

                (DateTime nextEndingPeriod, DateTime nextDueDate) =
                    CalculateNextTaxDueDates(taxAccount.AccountType, taxAccount.EndingPeriod, taxAccount.Cycle, taxAccount.Business.IsSoleProprietorship);

                existingAccount.EndingPeriod = nextEndingPeriod;
                existingAccount.DueDate = nextDueDate;

                dbContext.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Log.Error(ex, $"Unexpected exception while confirming Tax Filing: {ex.Message}. TaxAccount:{taxAccount.Id}, BusinessId:{taxAccount.BusinessId}");

                throw ex;
            }
        }

        public void ConfirmTaxFiling(TaxAccountWithInstalment taxAccount, string confirmText, DateTime confirmDate, Guid userAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.TaxAccountWithInstalments.FirstOrDefault(x => x.Id == taxAccount.Id)
                ?? throw new ArgumentException($"TaxAccountId:{taxAccount.Id} not found.");

            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                dbContext.TaxFilingLogs.Add(new TaxFilingLog
                {
                    BusinessId = taxAccount.BusinessId,
                    TaxAccountId = taxAccount.Id,
                    AccountType = taxAccount.AccountType,
                    AccountNumber = taxAccount.AccountNumber,
                    Cycle = taxAccount.Cycle,
                    EndingPeriod = taxAccount.EndingPeriod,
                    DueDate = taxAccount.DueDate,
                    Notes = taxAccount.Notes,
                    ConfirmationNotes = $"{confirmText}\r\n{taxAccount.ProgressNotes}",
                    Timestamp = confirmDate,
                    UserAccountId = userAccountId,
                });

                (DateTime nextEndingPeriod, DateTime nextDueDate) =
                    CalculateNextTaxDueDates(taxAccount.AccountType, taxAccount.EndingPeriod, taxAccount.Cycle, taxAccount.Business.IsSoleProprietorship);

                existingAccount.UserAccountId = null;
                existingAccount.UserAccount = null;
                existingAccount.EndingPeriod = nextEndingPeriod;
                existingAccount.DueDate = nextDueDate;

                existingAccount.UserAccountId = null;
                existingAccount.ProgressNotes = string.Empty;

                dbContext.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Log.Error(ex, $"Unexpected exception while confirming Tax Filing: {ex.Message}. TaxAccount:{taxAccount.Id}, BusinessId:{taxAccount.BusinessId}");

                throw ex;
            }
        }

        public void ConfirmInstalment(TaxAccountWithInstalment taxAccount, string confirmText, DateTime confirmDate, Guid userAccountId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.TaxAccountWithInstalments.FirstOrDefault(x => x.Id == taxAccount.Id);
            if (existingAccount == null)
            {
                throw new ArgumentException($"AccountId:{taxAccount.Id} not found.");
            }

            if (taxAccount.InstalmentDueDate == null)
            {
                throw new Exception($"{nameof(taxAccount.InstalmentDueDate)} must not be NULL.");
            }

            var transaction = dbContext.Database.BeginTransaction();
            try
            {
                dbContext.TaxInstalmentLogs.Add(new TaxInstalmentLog
                {
                    BusinessId = taxAccount.BusinessId,
                    TaxAccountId = taxAccount.Id,
                    Cycle = FilingCycle.None,
                    InstalmentDueDate = taxAccount.InstalmentDueDate.Value,
                    ConfirmationNotes = confirmText,
                    Timestamp = confirmDate,
                    UserAccountId = userAccountId
                });

                var nextInstalmentDate = CalculateNextInstalmentDueDate(taxAccount.InstalmentDueDate.Value);

                existingAccount.InstalmentDueDate = nextInstalmentDate;

                dbContext.SaveChanges();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Log.Error(ex, $"Unexpected exception while confirming Instalment:{ex.Message}, AccountId:{taxAccount.Id}, BusinessId:{taxAccount.BusinessId}");

                throw ex;
            }
        }


        private (DateTime, DateTime) CalculateNextTaxDueDates(TaxAccountType taxAccountType, DateTime endingPeriod,
            FilingCycle cycle, bool isCorp)
        {
            switch (taxAccountType)
            {
                case TaxAccountType.Corporation:
                    return CalculateNextCorporationTaxDueDates(endingPeriod, cycle, isCorp);

                case TaxAccountType.HST:
                    return CalculateNextHSTDueDates(endingPeriod, cycle, isCorp);

                // Same rules as PST
                case TaxAccountType.LIQ:
                    return CalculateNextPSTDueDates(endingPeriod, cycle, isCorp);

                case TaxAccountType.ONT:
                    return CalculateNextONTTaxDueDates(endingPeriod, cycle, isCorp);

                case TaxAccountType.PST:
                    return CalculateNextPSTDueDates(endingPeriod, cycle, isCorp);

                case TaxAccountType.WSIB:
                    return CalculateNextWSIBDueDates(endingPeriod, cycle, isCorp);

                default:
                    throw new ArgumentException($"Invalid TaxAccountType: {taxAccountType}");
            }
        }

        private DateTime CalculateNextInstalmentDueDate(DateTime currentDueDate)
        {
            var nextMonth = currentDueDate.AddMonths(3);

            return new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        }
        //RYAN: Add new method for quarterly instalment of TaxAccount (4months)
        public DateTime CalculateNextInstalmentDueDateNew(DateTime currentEndingPeriod)
        {
            var nextMonth = currentEndingPeriod.AddMonths(4);

            return new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        }
        //RYAN: Add new method for calculating instalmentDueDate of PersonalTaxAccount (logic: 15Mar of next year)
        public DateTime CalculateNextPersonalInstalmentDueDate()
        {
            // Get the current year and add one year
            int nextYear = DateTime.Now.Year + 1;

            // Set the specific month (March) and day (15th)
            int month = 3; // March
            int day = 15;

            // Create the new DateTime object
            DateTime newDateTime = new DateTime(nextYear, month, day);

            return newDateTime;
        }
        private (DateTime, DateTime) CalculateNextHSTDueDates(DateTime endingPeriod, FilingCycle cycle, bool isCorp)
        {
            DateTime nextEndingPeriod;
            DateTime nextDueDate;

            switch (cycle)
            {
                // HST Annually, Update 2021-10-20:
                // NextEndingPeriod = endingPeriod plus 1 year
                // NextDueDate = NextEndingPeriod plus 3 months
                case FilingCycle.Annually:
                    nextEndingPeriod = endingPeriod.AddYears(1);
                    nextDueDate = nextEndingPeriod.AddMonths(3);
                    break;

                // Quarterly:
                // NextEndingPeriod: based on endingPeriod plus 3 months
                // Due Date: is the last day of the next month
                case FilingCycle.Quarterly:
                    var nextQuarter = endingPeriod.AddMonths(3);
                    nextEndingPeriod = GetLastDateOfCurrentMonth(nextQuarter);
                    nextDueDate = GetLastDateOfNextMonth(nextQuarter);
                    break;

                // Monthly:
                // NextEndingPeriod: based on endingPeriod plus 1 month
                // Due Date: is the last day of the next month
                case FilingCycle.Monthly:
                    nextEndingPeriod = GetLastDateOfNextMonth(endingPeriod);
                    nextDueDate = GetLastDateOfNextMonth(nextEndingPeriod);
                    break;

                default:
                    throw new ArgumentException($"Invalid HST Filing cycle [{cycle}].");
            };

            return (nextEndingPeriod, nextDueDate);
        }

        private (DateTime, DateTime) CalculateNextCorporationTaxDueDates(DateTime endingPeriod, FilingCycle cycle, bool _)
        {
            DateTime nextEndingPeriod;
            DateTime nextDueDate;

            switch (cycle)
            {
                // Corporation Tax Cycle is always Annually
                // NextEndingPeriod = endingPeriod.(Year + 1);
                // Due Date is 3 months from NextEndingPeriod
                case FilingCycle.Annually:
                    nextEndingPeriod = endingPeriod.AddYears(1);
                    nextDueDate = GetLastDateOfCurrentMonth(nextEndingPeriod.AddMonths(3));
                    break;

                default:
                    throw new ArgumentException($"Corporate Tax Filing cycle must always be [{FilingCycle.Annually}]. Please update and try again.");
            };

            return (nextEndingPeriod, nextDueDate);
        }

        private (DateTime, DateTime) CalculateNextONTTaxDueDates(DateTime endingPeriod, FilingCycle cycle, bool _)
        {
            DateTime nextEndingPeriod;
            DateTime nextDueDate;

            switch (cycle)
            {
                // 2023-10-12: Change ONT's next EndingPeriod to exactly one year later
                // Change next DueDate to exactly one month from the next EndingPeriod
                case FilingCycle.Annually:
                    nextEndingPeriod = endingPeriod.AddYears(1);
                    nextDueDate = nextEndingPeriod.AddMonths(1);
                    break;

                default:
                    throw new ArgumentException($"ONT Tax Filing cycle must always be [{FilingCycle.Annually}]. Please update and try again.");
            };

            return (nextEndingPeriod, nextDueDate);
        }

        private (DateTime, DateTime) CalculateNextPSTDueDates(DateTime endingPeriod, FilingCycle cycle, bool isCorp)
        {
            DateTime nextEndingPeriod;
            DateTime nextDueDate;

            // PST cycle can be monthly, quarterly or annually
            // Due Date is the 20th day of the month following the reporting period
            switch (cycle)
            {
                case FilingCycle.Annually:
                    nextEndingPeriod = new DateTime(endingPeriod.Year + 1, 12, 31);
                    nextDueDate = new DateTime(nextEndingPeriod.Year + 1, 1, 20);
                    break;

                case FilingCycle.Quarterly:
                    nextEndingPeriod = GetLastDateOfCurrentMonth(endingPeriod.AddMonths(3));
                    nextDueDate = GetThe20thDayOfNextMonth(nextEndingPeriod);
                    break;

                case FilingCycle.Monthly:
                    nextEndingPeriod = GetLastDateOfNextMonth(endingPeriod);
                    nextDueDate = GetThe20thDayOfNextMonth(nextEndingPeriod);
                    break;

                default:
                    throw new ArgumentException($"Invalid PST Filing cycle [{cycle}].");
            }

            return (nextEndingPeriod, nextDueDate);
        }

        private (DateTime, DateTime) CalculateNextWSIBDueDates(DateTime endingPeriod, FilingCycle cycle, bool isCorp)
        {
            DateTime nextEndingPeriod;
            DateTime nextDueDate;

            // PST cycle can be monthly, quarterly or annually
            // Annually: Due Date is Apr 30 of the year following the reporting period
            // Quarterly: Due Date is the last of of the month following the reporting period
            // Monthly: Due Date is last day of the month following the reporting period
            switch (cycle)
            {
                case FilingCycle.Annually:
                    nextEndingPeriod = new DateTime(endingPeriod.Year + 1, 12, 31);
                    nextDueDate = new DateTime(nextEndingPeriod.Year + 1, 4, 30);
                    break;

                case FilingCycle.Quarterly:
                    nextEndingPeriod = GetLastDateOfCurrentMonth(endingPeriod.AddMonths(3));
                    nextDueDate = GetLastDateOfNextMonth(nextEndingPeriod);
                    break;

                case FilingCycle.Monthly:
                    nextEndingPeriod = GetLastDateOfNextMonth(endingPeriod);
                    nextDueDate = GetLastDateOfNextMonth(nextEndingPeriod);
                    break;

                default:
                    throw new ArgumentException($"Invalid WSIB Filing cycle [{cycle}].");
            }

            return (nextEndingPeriod, nextDueDate);
        }


        public DateTime GetLastDateOfCurrentMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        public DateTime GetLastDateOfNextMonth(DateTime date)
        {
            var nextMonth = date.AddMonths(1);

            return new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        }

        public DateTime GetThe20thDayOfNextMonth(DateTime date)
        {
            var nextMonth = date.AddMonths(1);

            return new DateTime(nextMonth.Year, nextMonth.Month, 20);
        }


        public void GenerateMissingHSTAccounts()
        {
            using var dbContext = new AccountingManagementDbContext();

            var businesses = dbContext.Businesses.Where(x => x.IsDeleted == false)
                .Include(b => b.TaxAccountWithInstalments.Where(x => x.AccountType == TaxAccountType.HST))
                .ToList();

            var placeholderDate = new DateTime(DateTime.Now.Year, 12, 31);

            var newHSTAccounts = new List<HSTAccount>();

            foreach (var business in businesses.Where(x => x.HSTAccount == null))
            {
                var hstAccount = new HSTAccount
                {
                    Id = Guid.NewGuid(),
                    BusinessId = business.Id,
                    HSTNumber = string.IsNullOrWhiteSpace(business.BusinessNumber) == false
                        ? business.BusinessNumber + "RT0001"
                        : string.Empty,
                    IsRunHST = true,
                    IsActive = true,
                    HSTCycle = FilingCycle.Annually,
                    HSTEndingPeriod = placeholderDate,
                    HSTDueDate = placeholderDate,
                    InstalmentRequired = false,
                    InstalmentAmount = 0,
                    InstalmentDueDate = placeholderDate,
                    Notes = "To be updated!!!",
                };

                newHSTAccounts.Add(hstAccount);
            }

            // TODO: Fix
            // dbContext.HSTAccounts.AddRange(newHSTAccounts);

            dbContext.SaveChanges();
        }

    }
}
