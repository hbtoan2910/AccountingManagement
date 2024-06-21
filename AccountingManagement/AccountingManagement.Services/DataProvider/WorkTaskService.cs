using AccountingManagement.DataAccess;
using AccountingManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Task = AccountingManagement.DataAccess.Entities.Task;

namespace AccountingManagement.Services
{
    public interface IWorkTaskService
    {
        List<Work> GetPendingWorks();
        List<Work> GetPendingWorksByUserAccount(Guid userId);
        Work GetWorkById(Guid workId);
        Work GetWorkByBusinessId(Guid businessId);
        List<Task> GetTasks(DateTime cutoffDate);
        List<Task> GetTasksByWorkId(Guid workId);
        Task GetTaskById(int taskId);

        bool UpsertTask(Task task);
        int SetWorkPriorityToTop(Guid workId);
        int SetWorkPriorityUp(Guid workId);
        int SetWorkPriorityDown(Guid workId);
    }

    public class WorkTaskService : IWorkTaskService
    {
        public List<Work> GetPendingWorks()
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Works.Where(x => x.IsDeleted == false)
                .Include(work => work.Business)
                .Include(work => work.Tasks.Where(t => t.TaskStatus != TaskStatus.Closed).OrderByDescending(t => t.LastUpdatedTime))
                .ThenInclude(task => task.UserAccount)
                // .OrderByDescending(work => work.Priority)
                .AsNoTracking()
                .ToList();
        }

        public List<Work> GetPendingWorksByUserAccount(Guid userId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Works.Where(x => x.IsDeleted == false)
                .Include(work => work.Business)
                .Include(work => work.Tasks.Where(t => t.UserAccountId == userId && t.TaskStatus != TaskStatus.Closed).OrderByDescending(t => t.LastUpdatedTime))
                .ThenInclude(task => task.UserAccount)
                .OrderByDescending(work => work.Priority)
                .AsNoTracking()
                .ToList();
        }

        public Work GetWorkByBusinessId(Guid businessId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Works.Where(x => x.BusinessId == businessId)
                .Include(work => work.Business)
                .Include(work => work.Tasks)
                .ThenInclude(task => task.UserAccount)
                .FirstOrDefault();
        }

        public Work GetWorkById(Guid workId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Works.Where(x => x.Id == workId)
                .Include(work => work.Business)
                .Include(work => work.Tasks)
                .ThenInclude(task => task.UserAccount)
                .FirstOrDefault();
        }

        public List<Task> GetTasks(DateTime cutoffDate)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Tasks.Where(t => t.LastUpdatedTime >= cutoffDate)
                .Include(t => t.UserAccount)
                .Include(t => t.Work)
                .AsNoTracking()
                .ToList();
        }

        public List<Task> GetTasksByWorkId(Guid workId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Tasks.Where(t => t.WorkId == workId && t.TaskStatus != TaskStatus.Closed)
                .Include(t => t.UserAccount)
                .Include(t => t.Work)
                .ToList();
        }

        public Task GetTaskById(int taskId)
        {
            using var dbContext = new AccountingManagementDbContext();

            return dbContext.Tasks.Where(x => x.Id == taskId)
                .Include(task => task.Work)
                .ThenInclude(work => work.Business)
                .Include(task => task.UserAccount)
                .FirstOrDefault();
        }

        public bool UpsertTask(Task task)
        {
            using var dbContext = new AccountingManagementDbContext();

            var existingTask = dbContext.Tasks.FirstOrDefault(i => i.Id == task.Id);
            if (existingTask != null)
            {
                existingTask.Description = task.Description;
                existingTask.Notes = task.Notes;
                existingTask.TaskStatus = task.TaskStatus;
                existingTask.UserAccountId = task.UserAccountId;
                existingTask.LastUpdated = task.LastUpdated;
                existingTask.LastUpdatedTime = task.LastUpdatedTime;

                dbContext.SaveChanges();
            }
            else
            {
                // Create new Task object and DO NOT assign Work or UserAccount property
                var newTask = new Task
                {
                    WorkId = task.Work.Id,
                    Description = task.Description,
                    Notes = task.Notes,
                    TaskStatus = task.TaskStatus,
                    UserAccountId = task.UserAccountId,
                    LastUpdated = task.LastUpdated,
                    LastUpdatedTime = task.LastUpdatedTime
                };

                dbContext.Tasks.Add(newTask);
                dbContext.SaveChanges();
            }

            return true;
        }

        public int SetWorkPriorityToTop(Guid workId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var work = dbContext.Works.FirstOrDefault(i => i.Id == workId)
                ?? throw new KeyNotFoundException($"WorkId:{workId} not found.");

            var maxPriority = dbContext.Works.Max(x => x.Priority);
            var topPriorityWork = dbContext.Works.FirstOrDefault(x => x.Priority == maxPriority);

            if (topPriorityWork != null)
            {
                work.Priority = topPriorityWork.Priority + 1;

                dbContext.SaveChanges();

                return work.Priority;
            }

            return int.MinValue;
        }

        public int SetWorkPriorityUp(Guid workId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var work = dbContext.Works.FirstOrDefault(i => i.Id == workId)
                ?? throw new KeyNotFoundException($"WorkId:{workId} not found.");

            var higherPriorityWork = dbContext.Works
                    .OrderBy(x => x.Priority)
                    .FirstOrDefault(x => x.Priority > work.Priority);

            if (higherPriorityWork != null)
            {
                work.Priority = higherPriorityWork.Priority + 1;

                dbContext.SaveChanges();

                return work.Priority;
            }

            return int.MinValue;
        }

        public int SetWorkPriorityDown(Guid workId)
        {
            using var dbContext = new AccountingManagementDbContext();

            var work = dbContext.Works.FirstOrDefault(i => i.Id == workId)
                ?? throw new KeyNotFoundException($"WorkId:{workId} not found.");

            var lowerPriorityWork = dbContext.Works
                    .OrderByDescending(x => x.Priority)
                    .FirstOrDefault(x => x.Priority < work.Priority);

            if (lowerPriorityWork != null)
            {
                work.Priority = lowerPriorityWork.Priority - 1;

                dbContext.SaveChanges();

                return work.Priority;
            }

            return int.MinValue;
        }
    }
}
