using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class Note
    {
        public int Id { get; set; }

        public Guid BusinessId { get; set; }

        public string Header { get; set; }

        public string Content { get; set; }

        public DateTime Created { get; set; }

        public string LastUpdated { get; set; }

        public bool IsDeleted { get; set; }

        public Note()
        {
            IsDeleted = false;
        }
    }
}
