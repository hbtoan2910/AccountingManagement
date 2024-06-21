using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class HSTInstalmentLog
    {
        public int Id { get; set; }

        public Guid HSTAccountId { get; set; }

        public Guid BusinessId { get; set; }

        public DateTime InstalmentDueDate { get; set; }

        public string InstalmentConfirm { get; set; }

        public DateTime FiledTimestamp { get; set; }

        public Guid UserAccountId { get; set; }

        public HSTInstalmentLog()
        { }
    }
}
