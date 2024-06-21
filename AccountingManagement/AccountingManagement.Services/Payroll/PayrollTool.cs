using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Serilog;

namespace AccountingManagement.Services
{
    public interface IPayrollTool
    {
        void GeneratePayrollRecords(string payrollPeriod, bool overwrite);
        IEnumerable<string> GetNextPayrollPeriods(string payrollPeriod, int count);
        IEnumerable<string> GetPreviousPayrollPeriods(string payrollPeriod, int count);
    }

    public class PayrollTool : IPayrollTool
    {
        public static readonly DateTime FirstBiWeeklyPayrollDateOfTheYear = new DateTime(2021, 03, 12);

        public PayrollTool()
        { }

        public void GeneratePayrollRecords(string payrollPeriod, bool overwrite)
        {
            Log.Information($"Generating records for Payroll Period:{payrollPeriod}");

            using var dbContext = new AccountingManagementDbContext();

            try
            {
                dbContext.Database.BeginTransaction();

                var ppLookup = dbContext.PayrollPeriodLookups
                        .FirstOrDefault(x => x.PayrollPeriod == payrollPeriod);

                if (ppLookup == null)
                {
                    ppLookup = GeneratePayrollPeriodLookupEntry(payrollPeriod);
                    dbContext.PayrollPeriodLookups.Add(ppLookup);
                }

                var payrollAccounts = dbContext.PayrollAccounts
                        .Where(x => x.IsActive && x.PayrollCycle != FilingCycle.None)
                        .Include(p => p.PayrollAccountRecords)
                        .Include(p => p.PayrollPayoutRecords);

                foreach (var payrollAccount in payrollAccounts)
                {
                    GeneratePayrollRecordForPeriod(payrollAccount, ppLookup, overwrite);
                    GeneratePayrollPayoutRecordForPeriod(payrollAccount, ppLookup);
                }

                dbContext.SaveChanges();
                dbContext.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                dbContext.Database.RollbackTransaction();
                Log.Error($"Fail to generate payroll records for period: {payrollPeriod}. {ex}");

                throw;
            }
        }

        private PayrollPeriodLookup GeneratePayrollPeriodLookupEntry(string payrollPeriod)
        {
            var ppLookupEntry = new PayrollPeriodLookup
            {
                PayrollPeriod = payrollPeriod,
                PreviousPayrollPeriod = GetPreviousPayrollPeriod(payrollPeriod),
            };

            var monthlyDueDates = GetPayrollDueDatesForPeriod(payrollPeriod, FilingCycle.Monthly);
            ppLookupEntry.MonthlyDueDate = monthlyDueDates[0];

            var semiMonthlyDueDates = GetPayrollDueDatesForPeriod(payrollPeriod, FilingCycle.SemiMonthly);
            ppLookupEntry.SemiMonthlyDueDate1 = semiMonthlyDueDates[0];
            ppLookupEntry.SemiMonthlyDueDate2 = semiMonthlyDueDates[1];

            var biWeeklyDueDates = GetPayrollDueDatesForPeriod(payrollPeriod, FilingCycle.BiWeekly);
            ppLookupEntry.BiWeeklyDueDate1 = biWeeklyDueDates[0];
            ppLookupEntry.BiWeeklyDueDate2 = biWeeklyDueDates[1];
            if (biWeeklyDueDates.Length > 2)
            {
                ppLookupEntry.BiWeeklyDueDate3 = biWeeklyDueDates[2];
            }

            ppLookupEntry.MonthlyPayoutDueDate = CalculatePayoutDueDate(ppLookupEntry.MonthlyDueDate, FilingCycle.Monthly);
            ppLookupEntry.BiWeeklyPayoutDueDate1 = CalculatePayoutDueDate(ppLookupEntry.BiWeeklyDueDate1, FilingCycle.BiWeekly);
            ppLookupEntry.BiWeeklyPayoutDueDate2 = CalculatePayoutDueDate(ppLookupEntry.BiWeeklyDueDate2, FilingCycle.BiWeekly);
            if (ppLookupEntry.BiWeeklyDueDate3 != null)
            {
                ppLookupEntry.BiWeeklyPayoutDueDate3 = CalculatePayoutDueDate(ppLookupEntry.BiWeeklyDueDate3, FilingCycle.BiWeekly);
            }

            var pd7aQuarterlyDueDate = GetPD7ADueDateForPeriod(payrollPeriod, FilingCycle.Quarterly);
            ppLookupEntry.PD7AQuarterlyDueDate = pd7aQuarterlyDueDate;

            var pd7aMonthlyDueDate = GetPD7ADueDateForPeriod(payrollPeriod, FilingCycle.Monthly);
            ppLookupEntry.PD7AMonthlyDueDate = pd7aMonthlyDueDate;

            return ppLookupEntry;
        }

        private void GeneratePayrollPayoutRecordForPeriod(PayrollAccount payrollAccount, PayrollPeriodLookup ppLookup)
        {
            var payoutRecord = payrollAccount.PayrollPayoutRecords
                    .FirstOrDefault(x => x.PayrollPeriod.Equals(ppLookup.PayrollPeriod));

            if (payoutRecord != null)
            {
                return;
            }

            var newPayoutRecord = new PayrollPayoutRecord
            {
                PayrollAccountId = payrollAccount.Id,
                PayrollAccount = payrollAccount,
                PayrollPeriod = ppLookup.PayrollPeriod,
            };

            switch (payrollAccount.PayrollCycle)
            {
                case FilingCycle.Monthly:
                    newPayoutRecord.PayrollPayout1DueDate = ppLookup.MonthlyPayoutDueDate;
                    break;

                case FilingCycle.SemiMonthly:
                    newPayoutRecord.PayrollPayout1DueDate = ppLookup.SemiMonthlyDueDate1;
                    newPayoutRecord.PayrollPayout2DueDate = ppLookup.SemiMonthlyDueDate2;
                    break;

                case FilingCycle.BiWeekly:
                    newPayoutRecord.PayrollPayout1DueDate = ppLookup.BiWeeklyPayoutDueDate1;
                    newPayoutRecord.PayrollPayout2DueDate = ppLookup.BiWeeklyPayoutDueDate2;
                    newPayoutRecord.PayrollPayout3DueDate = ppLookup.BiWeeklyPayoutDueDate3;
                    break;

                default:
                    break;
            }

            payrollAccount.PayrollPayoutRecords.Add(newPayoutRecord);
        }

        private void GeneratePayrollRecordForPeriod(PayrollAccount payrollAccount, PayrollPeriodLookup ppLookup, bool overwrite)
        {
            var existingRecord = payrollAccount.PayrollAccountRecords
                    .FirstOrDefault(x => x.PayrollPeriod.Equals(ppLookup.PayrollPeriod));

            if (existingRecord != null)
            {
                if (overwrite == false)
                {
                    return;
                }

                payrollAccount.PayrollAccountRecords.Remove(existingRecord);
            }

            var newRecord = new PayrollAccountRecord
            {
                PayrollAccountId = payrollAccount.Id,
                PayrollAccount = payrollAccount,
                PayrollPeriod = ppLookup.PayrollPeriod,
            };

            switch (payrollAccount.PayrollCycle)
            {
                case FilingCycle.Monthly:
                    newRecord.Payroll1DueDate = ppLookup.MonthlyDueDate;
                    break;

                case FilingCycle.SemiMonthly:
                    newRecord.Payroll1DueDate = ppLookup.SemiMonthlyDueDate1;
                    newRecord.Payroll2DueDate = ppLookup.SemiMonthlyDueDate2;
                    break;

                case FilingCycle.BiWeekly:
                    newRecord.Payroll1DueDate = ppLookup.BiWeeklyDueDate1;
                    newRecord.Payroll2DueDate = ppLookup.BiWeeklyDueDate2;
                    newRecord.Payroll3DueDate = ppLookup.BiWeeklyDueDate3;
                    break;

                default:
                    break;
            }

            switch (payrollAccount.PD7ACycle)
            {
                case FilingCycle.Quarterly:
                    newRecord.PD7ADueDate = ppLookup.PD7AQuarterlyDueDate;
                    break;

                case FilingCycle.Monthly:
                    newRecord.PD7ADueDate = ppLookup.PD7AMonthlyDueDate;
                    break;

                case FilingCycle.BiMonthly:
                    break;

                default:
                    break;
            }

            payrollAccount.PayrollAccountRecords.Add(newRecord);
        }

        private string GetPreviousPayrollPeriod(string payrollPeriod)
        {
            var (year, month) = ParsePayrollPeriod(payrollPeriod);
            
            if (month == 1)
            {
                return $"{year - 1}-12";
            }
            else
            {
                return $"{year}-{month - 1:D2}";
            }
        }

        public IEnumerable<string> GetNextPayrollPeriods(string payrollPeriod, int count)
        {
            var (year, month) = ParsePayrollPeriod(payrollPeriod);

            for (int i = 0; i < count; i++)
            {
                if (month >= 12)
                {
                    year++;
                    month = 1;
                }
                else
                {
                    month++;
                }

                yield return $"{year}-{month:D2}";
            }
        }

        public IEnumerable<string> GetPreviousPayrollPeriods(string payrollPeriod, int count)
        {
            var (year, month) = ParsePayrollPeriod(payrollPeriod);

            for (int i = 0; i < count; i++)
            {
                if (month <= 1)
                {
                    year--;
                    month = 12;
                }
                else
                {
                    month--;
                }

                yield return $"{year}-{month:D2}";
            }
        }

        public DateTime? GetPD7ADueDateForPeriod(string payrollPeriod, FilingCycle pd7aCycle)
        {
            var (year, month) = ParsePayrollPeriod(payrollPeriod);

            switch (pd7aCycle)
            {
                case FilingCycle.Monthly:
                    return new DateTime(year, month, DateTime.DaysInMonth(year, month) / 2);

                case FilingCycle.Quarterly:
                    if (month == 1 || month == 4 || month == 7 || month == 10)
                    {
                        return new DateTime(year, month, 15);
                    }
                    return null;

                default:
                    return null;
            }
        }

        public DateTime? CalculatePayoutDueDate(DateTime? payrollDueDate, FilingCycle payrollCycle)
        {
            if (payrollDueDate == null)
            {
                return null;
            }

            switch (payrollCycle)
            {
                case FilingCycle.Weekly:
                case FilingCycle.BiWeekly:
                    // Payout DueDate is the next Friday from Payroll Record DueDate
                    var nextFriday = payrollDueDate.Value.AddDays(1);
                    while (nextFriday.DayOfWeek != DayOfWeek.Friday)
                    {
                        nextFriday = nextFriday.AddDays(1);
                    }
                    return nextFriday;

                case FilingCycle.SemiMonthly:
                case FilingCycle.Monthly:
                    // DueDate for Payroll with Monthly cycle is the last day of the month,
                    // And it's the same for Payroll Payout
                    return payrollDueDate;

                default:
                    throw new ArgumentException($"Invalid PayrollCycle: {payrollCycle}");
            }
        }

        public DateTime[] GetPayrollDueDatesForPeriod(string payrollPeriod, FilingCycle payrollCycle)
        {
            var (year, month) = ParsePayrollPeriod(payrollPeriod);

            switch (payrollCycle)
            {
                case FilingCycle.Weekly:
                case FilingCycle.BiWeekly:
                    var firstPayrollDateOfMonth = GetFirstPayrollDateInBiWeeklyCycle(year, month);
                    if (firstPayrollDateOfMonth.Day + 28 <= DateTime.DaysInMonth(year, month))
                    {
                        return new DateTime[]
                        {
                            firstPayrollDateOfMonth,
                            firstPayrollDateOfMonth.AddDays(14),
                            firstPayrollDateOfMonth.AddDays(28),
                        };
                    }
                    else
                    {
                        return new DateTime[]
                        {
                            firstPayrollDateOfMonth,
                            firstPayrollDateOfMonth.AddDays(14)
                        };
                    }

                case FilingCycle.SemiMonthly:
                    return new DateTime[]
                    {
                        new DateTime(year, month, DateTime.DaysInMonth(year, month) / 2),
                        new DateTime(year, month, DateTime.DaysInMonth(year, month)),
                    };

                case FilingCycle.Monthly:
                    return new DateTime[]
                    {
                        new DateTime(year, month, DateTime.DaysInMonth(year, month))
                    };

                default:
                    throw new ArgumentException($"Invalid PayrollCycle: {payrollCycle}");
            }
        }

        public DateTime GetFirstPayrollDateInBiWeeklyCycle(int year, int month)
        {
            var firstPayrollDateOfMonth = FirstBiWeeklyPayrollDateOfTheYear;

            while (firstPayrollDateOfMonth.Year < year || firstPayrollDateOfMonth.Month < month)
            {
                firstPayrollDateOfMonth = firstPayrollDateOfMonth.AddDays(14);
            }

            return firstPayrollDateOfMonth;
        }

        public (int, int) ParsePayrollPeriod(string payrollPeriod)
        {
            if (int.TryParse(payrollPeriod.Substring(0, 4), out var year) == false
                || int.TryParse(payrollPeriod.Substring(5), out var month) == false)
            {
                throw new ArgumentException($"Invalid PayrollPeriod: {payrollPeriod}. Correct format is: yyyy-MM");
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentException($"Invalid month: {month}. Must be between 1 - 12");
            }

            return (year, month);
        }
    }
}
