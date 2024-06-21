using System;
using System.Collections.Generic;

namespace AccountingManagement.DataAccess.Entities
{
    public class Work
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public string Description { get; set; }

        public WorkStatus WorkStatus { get; set; }

        public int Priority { get; set; }

        public string Notes { get; set; }

        public bool IsDeleted { get; set; }

        public List<Task> Tasks { get; set; }
    }

    public enum WorkStatus : byte
    {
        New,
        Close,
    }
}
