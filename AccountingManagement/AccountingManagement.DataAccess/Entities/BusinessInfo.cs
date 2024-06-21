using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class BusinessInfo
    {
        public int Id { get; set; }

        public Guid BusinessId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime ModifiedTime { get; set; }

        public string LastUpdated { get; set; }

        public bool IsDeleted { get; set; }

        public BusinessInfo()
        {
            IsDeleted = false;
        }
    }
}
