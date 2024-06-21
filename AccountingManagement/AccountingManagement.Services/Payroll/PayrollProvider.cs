using System;
using System.Collections.Generic;
using System.Linq;
using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Serilog;

namespace AccountingManagement.Services
{
    public interface IPayrollProvider
    {
        void UpdatePayrollRecordDueDate1Status(long recordId, PayrollStatus status, string updatedText);
        void UpdatePayrollRecordDueDate2Status(long recordId, PayrollStatus status, string updatedText);
        void UpdatePayrollRecordDueDate3Status(long recordId, PayrollStatus status, string updatedText);
        void UpdatePayrollRecordPD7APrinted(long recordId, bool printed, string updatedText);
        void UpdatePayrollRecordPD7AReminder(long recordId, EmailReminder reminderStatus, string confirmText, string updatedText);
        void UpdatePayrollRecordPD7AStatus(long recordId, PayrollStatus status, string confirmText, string updatedText);

        // Payroll Payout
        void UpdatePayoutRecordDueDate1Status(long recordId, PayrollStatus status, string updatedText);
        void UpdatePayoutRecordDueDate2Status(long recordId, PayrollStatus status, string updatedText);
        void UpdatePayoutRecordDueDate3Status(long recordId, PayrollStatus status, string updatedText);

        // Year End
        int UpdateYearEndRecordT4Reconciliation(int recordId, bool reconciliation, string updatedBy);
        int UpdateYearEndRecordT4FormReady(int recordId, bool formReady, string updatedBy);
        int UpdateYearEndRecordT4FilingStatus(int recordId, PayrollStatus status, string confirmText, string updatedBy);
        int UpdateYearEndRecordT4AReconciliation(int recordId, bool reconciliation, string updatedBy);
        int UpdateYearEndRecordT4AFilingStatus(int recordId, PayrollStatus status, string confirmText, string updatedBy);
        int UpdateYearEndRecordT5Reconciliation(int recordId, bool reconciliation, string updatedBy);
        int UpdateYearEndRecordT5FilingStatus(int recordId, PayrollStatus status, string confirmText, string updatedBy);

        void GeneratePayrollYearEndRecords(string yearEndPeriod);
    }

    public class PayrollProvider : IPayrollProvider
    {
        public void UpdatePayrollRecordDueDate1Status(long recordId, PayrollStatus status, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payrollRecord = dbContext.PayrollAccountRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollRecordId:{recordId} not found.");

            payrollRecord.Payroll1Status = status;
            payrollRecord.Payroll1UpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayrollRecordDueDate2Status(long recordId, PayrollStatus status, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payrollRecord = dbContext.PayrollAccountRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollRecordId:{recordId} not found.");

            payrollRecord.Payroll2Status = status;
            payrollRecord.Payroll2UpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayrollRecordDueDate3Status(long recordId, PayrollStatus status, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payrollRecord = dbContext.PayrollAccountRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollRecordId:{recordId} not found.");

            payrollRecord.Payroll3Status = status;
            payrollRecord.Payroll3UpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayrollRecordPD7APrinted(long recordId, bool printed, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payrollRecord = dbContext.PayrollAccountRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollRecordId:{recordId} not found.");

            payrollRecord.PD7APrinted = printed;
            payrollRecord.PD7AUpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayrollRecordPD7AReminder(long recordId, EmailReminder reminderStatus, string confirmText, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payrollRecord = dbContext.PayrollAccountRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollRecordId:{recordId} not found.");

            payrollRecord.PD7AReminder = reminderStatus;
            payrollRecord.PD7AConfirmation = confirmText;
            payrollRecord.PD7AUpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayrollRecordPD7AStatus(long recordId, PayrollStatus status, string confirmText, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payrollRecord = dbContext.PayrollAccountRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollRecordId:{recordId} not found.");

            payrollRecord.PD7AStatus = status;
            payrollRecord.PD7AConfirmation = confirmText;
            payrollRecord.PD7AUpdatedBy = updatedText;

            dbContext.SaveChanges();
        }


        public void UpdatePayoutRecordDueDate1Status(long recordId, PayrollStatus status, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payoutRecord = dbContext.PayrollPayoutRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayoutRecordId:{recordId} not found.");

            payoutRecord.PayrollPayout1Status = status;
            payoutRecord.PayrollPayout1UpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayoutRecordDueDate2Status(long recordId, PayrollStatus status, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payoutRecord = dbContext.PayrollPayoutRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayoutRecordId:{recordId} not found.");

            payoutRecord.PayrollPayout2Status = status;
            payoutRecord.PayrollPayout2UpdatedBy = updatedText;

            dbContext.SaveChanges();
        }

        public void UpdatePayoutRecordDueDate3Status(long recordId, PayrollStatus status, string updatedText)
        {
            using var dbContext = new AccountingManagementDbContext();

            var payoutRecord = dbContext.PayrollPayoutRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollPayoutId:{recordId} not found.");

            payoutRecord.PayrollPayout3Status = status;
            payoutRecord.PayrollPayout3UpdatedBy = updatedText;

            dbContext.SaveChanges();
        }


        public void GeneratePayrollYearEndRecords(string yearEndPeriod)
        {
            Log.Information($"Generating records for Year End Payroll Period:{yearEndPeriod}");

            using var dbContext = new AccountingManagementDbContext();

            try
            {
                dbContext.Database.BeginTransaction();

                var payrollAccounts = dbContext.PayrollAccounts
                        .Where(x => x.IsActive && (x.IsRunT4 || x.IsRunT4A || x.IsRunT5));

                var existingRecords = dbContext.PayrollYearEndRecords
                        .Where(x => x.YearEndPeriod == yearEndPeriod)
                        .Select(x => x.PayrollAccountId)
                        .ToList();

                foreach (var payrollAccount in payrollAccounts.Where(x => existingRecords.Contains(x.Id) == false))
                {
                    var newYearEndRecord = new PayrollYearEndRecord
                    {
                        PayrollAccountId = payrollAccount.Id,
                        YearEndPeriod = yearEndPeriod,
                        T4Reconciliation = false,
                        T4FormReady = false,
                        T4Confirmation = string.Empty,
                        T4Status = PayrollStatus.None,
                        T4UpdatedBy = string.Empty,
                        T4AReconciliation = false,
                        T4AConfirmation = string.Empty,
                        T4AStatus = PayrollStatus.None,
                        T4AUpdatedBy = string.Empty,
                        T5Reconciliation = false,
                        T5Confirmation = string.Empty,
                        T5Status = PayrollStatus.None,
                        T5UpdatedBy = string.Empty,
                        Notes = string.Empty
                    };

                    dbContext.PayrollYearEndRecords.Add(newYearEndRecord);
                }

                dbContext.SaveChanges();
                dbContext.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                dbContext.Database.RollbackTransaction();
                Log.Error($"Fail to generate Year End Payroll records for period: {yearEndPeriod}. {ex}");

                throw;
            }
        }

        public int UpdateYearEndRecordT4Reconciliation(int recordId, bool reconciliation, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T4Reconciliation = reconciliation;
            yearEndRecord.T4UpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }

        public int UpdateYearEndRecordT4FormReady(int recordId, bool formReady, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T4FormReady = formReady;
            yearEndRecord.T4UpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }

        public int UpdateYearEndRecordT4FilingStatus(int recordId, PayrollStatus status, string confirmText, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T4Confirmation = confirmText;
            yearEndRecord.T4Status = status;
            yearEndRecord.T4UpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }

        public int UpdateYearEndRecordT4AReconciliation(int recordId, bool reconciliation, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T4AReconciliation = reconciliation;
            yearEndRecord.T4AUpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }

        public int UpdateYearEndRecordT4AFilingStatus(int recordId, PayrollStatus status, string confirmText, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T4AConfirmation = confirmText;
            yearEndRecord.T4AStatus = status;
            yearEndRecord.T4AUpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }

        public int UpdateYearEndRecordT5Reconciliation(int recordId, bool reconciliation, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T5Reconciliation = reconciliation;
            yearEndRecord.T5UpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }

        public int UpdateYearEndRecordT5FilingStatus(int recordId, PayrollStatus status, string confirmText, string updatedBy)
        {
            using var dbContext = new AccountingManagementDbContext();

            var yearEndRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(x => x.Id == recordId)
                ?? throw new KeyNotFoundException($"PayrollYearEndRecordId:{recordId} not found.");

            yearEndRecord.T5Confirmation = confirmText;
            yearEndRecord.T5Status = status;
            yearEndRecord.T5UpdatedBy = updatedBy;

            return dbContext.SaveChanges();
        }
    }
}
