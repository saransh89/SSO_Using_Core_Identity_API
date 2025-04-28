using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Shared.DTOs
{
    public class AuthResponseDto
    {
        public bool IsSuccessful { get; set; }
        public string? Token { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public IEnumerable<string>? Message { get; set; }
    }
}
