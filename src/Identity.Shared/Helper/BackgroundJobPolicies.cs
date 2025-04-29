using Identity.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Shared.Helper
{
    public static class BackgroundJobPolicies
    {
        public static readonly HashSet<string> AllowedJobs = new HashSet<string>
        {
            JobName.AuditLogArchiver.ToClaimValue(),
            JobName.UserCleanerJob.ToClaimValue(),
            JobName.DataSyncJob.ToClaimValue()
        };
    }
}
