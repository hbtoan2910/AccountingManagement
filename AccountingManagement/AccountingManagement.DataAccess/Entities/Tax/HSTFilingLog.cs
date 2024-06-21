using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class HSTFilingLog
    {
        public int Id { get; set; }

        public Guid HSTAccountId { get; set; }

        public Guid BusinessId { get; set; }

        public DateTime HSTEndingPeriod { get; set; }

        public DateTime HSTDueDate { get; set; }

        public string HSTAmount { get; set; }

        public string Notes { get; set; }

        public DateTime FiledTimestamp { get; set; }

        public Guid UserAccountId { get; set; }

        public HSTFilingLog()
        { }
    }
}
