using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Shared.Enums
{
    public class Enums
    {
    }


    public enum JobName
    {
        AuditLogArchiver,
        UserCleanerJob,
        DataSyncJob
    }
    public static class JobNameExtensions
    {
        public static string ToClaimValue(this JobName job)
        {
            return job.ToString(); // Or customize if needed
        }
    }

    public enum EnumUserRoles
    {
        Guest1,
        WalkInUser,
        ManagerTest,
        Manager,
        Admin,
        [Display(Name = "Machine Jobs Runner")]
        MachineJobsRunner,
        ReadOnly,
        OnlineUser,
        Support,
        Developer
    }

    public static class UserRolesExtensions
    {
        public static string ToClaimValue(this EnumUserRoles userRoles)
        {
            return userRoles.ToString(); // Or customize if needed
        }
    }

    /// <summary>
    /// This will be extension method on the enum to get the display Value
    /// </summary>
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            if (enumValue == null) throw new ArgumentNullException(nameof(enumValue));

            var memberInfo = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
            if (memberInfo == null) return enumValue.ToString();

            var attribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
            return attribute?.Name ?? enumValue.ToString();
        }
    }


}
