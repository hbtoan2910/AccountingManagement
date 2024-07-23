using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.Services
{
    public interface IPayrollService
    {
        List<Business> GetBusinessPayrollRecordsForPeriod(string payrollPeriod);
        PayrollAccountRecord GetPayrollRecordById(long id);
        bool UpsertPayrollAccount(PayrollAccount payrollAccount);
        bool UpsertPayrollRecord(PayrollAccountRecord payrollRecord);

        List<PayrollYearEndRecord> GetPayrollYearEndRecordsByPeriod(string yearEndPeriod);
        PayrollYearEndRecord GetPayrollYearEndRecordById(int id);
        bool UpsertPayrollYearEndRecord(PayrollYearEndRecord yearEndRecord);
        List<string> GetPayrollYearEndPeriods();

        List<Business> GetBusinessPayrollPayoutRecordsForPeriod(string payrollPeriod);
        PayrollPayoutRecord GetPayrollPayoutRecordById(long id);
        bool UpsertPayrollPayoutRecord(PayrollPayoutRecord payrollPayoutRecord);

        List<PayrollPeriodLookup> GetLatestPayrollPeriodLookups(int count);
    }

    public class PayrollService : IPayrollService
    {
        public List<Business> GetBusinessPayrollRecordsForPeriod(string payrollPeriod)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Businesses.Where(x => x.IsDeleted == false)
                .Include(b => b.BusinessOwners)
                .ThenInclude(bo => bo.Owner)
                .Include(b => b.PayrollAccount)
                .ThenInclude(p => p.PayrollAccountRecords
                    .Where(r => r.PayrollPeriod.Equals(payrollPeriod)))
                .Where(b => b.PayrollAccount.IsActive && b.PayrollAccount.PayrollCycle != FilingCycle.None)
                .AsNoTracking()
                .ToList();
            
            /*  RYAN: equivalent with this query:
                DECLARE @FilingCycleNoneValue INT;
                DECLARE @PayrollPeriod NVARCHAR(50);

                SET @FilingCycleNoneValue = 0;
                SET @PayrollPeriod = '2024-09';

                SELECT DISTINCT b.*
                FROM Business b
                INNER JOIN PayrollAccount pa ON b.Id = pa.BusinessId
                INNER JOIN PayrollAccountRecord par ON pa.Id = par.PayrollAccountId
                LEFT JOIN BusinessOwner bo ON b.Id = bo.BusinessId
                LEFT JOIN Owner o ON bo.OwnerId = o.Id
                WHERE b.IsDeleted = 0
                  AND pa.IsActive = 1
                  AND pa.PayrollCycle != @FilingCycleNoneValue
                  AND par.PayrollPeriod = @PayrollPeriod
                ORDER BY b.Id;
            */
        }

        public PayrollAccountRecord GetPayrollRecordById(long id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PayrollAccountRecords.Where(x => x.Id == id)
                .Include(r => r.PayrollAccount)
                .ThenInclude(pa => pa.Business)
                .FirstOrDefault();
        }

        public bool UpsertPayrollAccount(PayrollAccount payrollAccount)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingAccount = dbContext.PayrollAccounts.FirstOrDefault(x => x.Id == payrollAccount.Id);
            if (existingAccount == null)
            {
                dbContext.PayrollAccounts.Add(payrollAccount);
            }
            else
            {
                existingAccount.PayrollNumber = payrollAccount.PayrollNumber;
                existingAccount.PayrollCycle = payrollAccount.PayrollCycle;
                existingAccount.PD7ACycle = payrollAccount.PD7ACycle;
                existingAccount.IsRunPayroll = payrollAccount.IsRunPayroll;
                existingAccount.IsRunPayout = payrollAccount.IsRunPayout;
                existingAccount.IsPayPD7A = payrollAccount.IsPayPD7A;
                existingAccount.Notes = payrollAccount.Notes;
                existingAccount.PayoutNotes = payrollAccount.PayoutNotes;
                existingAccount.IsActive = payrollAccount.IsActive;

                existingAccount.IsRunT4 = payrollAccount.IsRunT4;
                existingAccount.IsRunT4A = payrollAccount.IsRunT4A;
                existingAccount.IsRunT5 = payrollAccount.IsRunT5;
                existingAccount.Timesheet = payrollAccount.Timesheet;
                existingAccount.YearEndNotes = payrollAccount.YearEndNotes;
            }

            dbContext.SaveChanges();
            return true;
        }

        public bool UpsertPayrollRecord(PayrollAccountRecord payrollRecord)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingRecord = dbContext.PayrollAccountRecords.FirstOrDefault(i => i.Id == payrollRecord.Id);
            if (existingRecord == null)
            {
                dbContext.PayrollAccountRecords.Add(payrollRecord);
            }
            else
            {
                existingRecord.Payroll1DueDate = payrollRecord.Payroll1DueDate;
                existingRecord.Payroll1Status = payrollRecord.Payroll1Status;
                existingRecord.Payroll1UpdatedBy = payrollRecord.Payroll1UpdatedBy;

                existingRecord.Payroll2DueDate = payrollRecord.Payroll2DueDate;
                existingRecord.Payroll2Status = payrollRecord.Payroll2Status;
                existingRecord.Payroll2UpdatedBy = payrollRecord.Payroll2UpdatedBy;

                existingRecord.Payroll3DueDate = payrollRecord.Payroll3DueDate;
                existingRecord.Payroll3Status = payrollRecord.Payroll3Status;
                existingRecord.Payroll3UpdatedBy = payrollRecord.Payroll3UpdatedBy;

                existingRecord.PD7AConfirmation = payrollRecord.PD7AConfirmation;
                existingRecord.PD7ADueDate = payrollRecord.PD7ADueDate;
                existingRecord.PD7APrinted = payrollRecord.PD7APrinted;
                existingRecord.PD7AReminder = payrollRecord.PD7AReminder;
                existingRecord.PD7AStatus = payrollRecord.PD7AStatus;
                existingRecord.PD7AUpdatedBy = payrollRecord.PD7AUpdatedBy;

                existingRecord.Notes = payrollRecord.Notes;
                dbContext.SaveChanges();
            }

            return true;
        }


        public List<PayrollYearEndRecord> GetPayrollYearEndRecordsByPeriod(string yearEndPeriod)
        {
            using var dbContext = new AccountingManagementDbContext();

            // TODO: Evaluate bottom-up query vs. top-down query
            return dbContext.PayrollYearEndRecords.Where(x => x.YearEndPeriod.Equals(yearEndPeriod))
                .Include(r => r.PayrollAccount)
                .ThenInclude(pa => pa.Business)
                .Where(x => x.PayrollAccount.Business.IsDeleted == false)
                .AsNoTracking()
                .ToList();
        }

        public PayrollYearEndRecord GetPayrollYearEndRecordById(int id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PayrollYearEndRecords.Where(x => x.Id == id)
                .Include(r => r.PayrollAccount)
                .ThenInclude(pa => pa.Business)
                .FirstOrDefault();
        }

        public bool UpsertPayrollYearEndRecord(PayrollYearEndRecord yearEndRecord)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingRecord = dbContext.PayrollYearEndRecords.FirstOrDefault(i => i.Id == yearEndRecord.Id);
            if (existingRecord == null)
            {
                dbContext.PayrollYearEndRecords.Add(yearEndRecord);
            }
            else
            {
                existingRecord.YearEndPeriod = yearEndRecord.YearEndPeriod;

                existingRecord.T4Confirmation = yearEndRecord.T4Confirmation;
                existingRecord.T4FormReady = yearEndRecord.T4FormReady;
                existingRecord.T4Reconciliation = yearEndRecord.T4Reconciliation;
                existingRecord.T4Status = yearEndRecord.T4Status;
                existingRecord.T4UpdatedBy = yearEndRecord.T4UpdatedBy;

                existingRecord.T4AConfirmation = yearEndRecord.T4AConfirmation;
                existingRecord.T4AReconciliation = yearEndRecord.T4AReconciliation;
                existingRecord.T4AStatus = yearEndRecord.T4AStatus;
                existingRecord.T4AUpdatedBy = yearEndRecord.T4AUpdatedBy;

                existingRecord.T5Confirmation = yearEndRecord.T5Confirmation;
                existingRecord.T5Reconciliation = yearEndRecord.T5Reconciliation;
                existingRecord.T5Status = yearEndRecord.T5Status;
                existingRecord.T5UpdatedBy = yearEndRecord.T5UpdatedBy;

                existingRecord.Notes = yearEndRecord.Notes;

                dbContext.SaveChanges();
            }

            return true;
        }

        public List<string> GetPayrollYearEndPeriods()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PayrollYearEndRecords.Select(x => x.YearEndPeriod)
                .Distinct()
                .OrderByDescending(x => x)
                .Take(10)
                .AsNoTracking()
                .ToList();
        }


        public List<Business> GetBusinessPayrollPayoutRecordsForPeriod(string payrollPeriod)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Businesses.Where(x => x.IsDeleted == false)
                .Include(b => b.PayrollAccount)
                .ThenInclude(p => p.PayrollPayoutRecords
                    .Where(r => r.PayrollPeriod.Equals(payrollPeriod)))
                .Where(b => b.PayrollAccount.IsActive
                            && b.PayrollAccount.IsRunPayout
                            && b.PayrollAccount.PayrollCycle != FilingCycle.None)
                .AsNoTracking()
                .ToList();
        }

        public PayrollPayoutRecord GetPayrollPayoutRecordById(long id)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PayrollPayoutRecords.Where(x => x.Id == id)
                .Include(r => r.PayrollAccount)
                .ThenInclude(pa => pa.Business)
                .FirstOrDefault();
        }

        public bool UpsertPayrollPayoutRecord(PayrollPayoutRecord payoutRecord)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingRecord = dbContext.PayrollPayoutRecords.FirstOrDefault(i => i.Id == payoutRecord.Id);
            if (existingRecord == null)
            {
                dbContext.PayrollPayoutRecords.Add(payoutRecord);
            }
            else
            {
                existingRecord.PayrollPayout1DueDate = payoutRecord.PayrollPayout1DueDate;
                existingRecord.PayrollPayout1Status = payoutRecord.PayrollPayout1Status;
                existingRecord.PayrollPayout1UpdatedBy = payoutRecord.PayrollPayout1UpdatedBy;

                existingRecord.PayrollPayout2DueDate = payoutRecord.PayrollPayout2DueDate;
                existingRecord.PayrollPayout2Status = payoutRecord.PayrollPayout2Status;
                existingRecord.PayrollPayout2UpdatedBy = payoutRecord.PayrollPayout2UpdatedBy;

                existingRecord.PayrollPayout3DueDate = payoutRecord.PayrollPayout3DueDate;
                existingRecord.PayrollPayout3Status = payoutRecord.PayrollPayout3Status;
                existingRecord.PayrollPayout3UpdatedBy = payoutRecord.PayrollPayout3UpdatedBy;

                existingRecord.Notes = payoutRecord.Notes;
                dbContext.SaveChanges();
            }

            return true;
        }


        public List<PayrollPeriodLookup> GetLatestPayrollPeriodLookups(int count)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.PayrollPeriodLookups
                .OrderByDescending(x => x.PayrollPeriod)
                .Take(count)
                .ToList();
        }
    }
}
