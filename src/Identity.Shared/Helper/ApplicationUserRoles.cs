using Identity.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Shared.Helper
{
    public static class ApplicationUserRoles
    {
        public static readonly HashSet<string> UserRoles = new HashSet<string>
        {
            JobName.AuditLogArchiver.ToClaimValue(),
            JobName.UserCleanerJob.ToClaimValue(),
            JobName.DataSyncJob.ToClaimValue()
        };
    }
}
