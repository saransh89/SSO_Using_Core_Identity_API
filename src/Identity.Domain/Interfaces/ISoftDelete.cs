using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Interfaces
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set;}
        DateTime? DeletedAt { get; set; }

        Guid? DeletedByUserId { get; set; }
    }
}
