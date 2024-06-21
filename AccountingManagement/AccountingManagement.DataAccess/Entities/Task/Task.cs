using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class Task
    {
        public int Id { get; set; }

        public Work Work { get; set; }

        public Guid WorkId { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        public UserAccount UserAccount { get; set; }

        public Guid UserAccountId { get; set; }

        public TaskStatus TaskStatus { get; set; }

        public string LastUpdated { get; set; }

        public DateTime LastUpdatedTime { get; set; }
    }

    public enum TaskStatus : byte
    {
        New = 0,
        InProgress = 1,
        Blocked = 2,
        Done = 3,
        Closed = 4
    }
}
