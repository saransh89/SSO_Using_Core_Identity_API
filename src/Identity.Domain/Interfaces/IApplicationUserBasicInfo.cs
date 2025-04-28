using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Interfaces
{
    public interface IApplicationUserBasicInfo
    {
        string? FirstName {  get; set; }
        string? LastName { get; set; }  
        string? Phone  { get; set; }
        string? Address { get; set; }
        string? Department { get; set; }
        bool? IsActive { get; set; }
    }
}
