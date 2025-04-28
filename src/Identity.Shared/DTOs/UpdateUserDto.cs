using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Shared.DTOs
{
    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; } // You could add this if users have a full name
        public bool? IsActive { get; set; }  // For example, Is the user active or not
        // Add more properties here as needed
    }
}
