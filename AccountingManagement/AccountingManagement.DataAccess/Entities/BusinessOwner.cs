using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingManagement.DataAccess.Entities
{
    public class BusinessOwner
    {
        public int Id { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public Guid OwnerId { get; set; }

        public Owner Owner { get; set; }

        public BusinessOwner(Guid businessId, Guid ownerId)
        {
            Id = 0;
            BusinessId = businessId;
            OwnerId = ownerId;
        }
    }
}
