﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Shared.Requests
{
    public class ArchiveAuditLogsRequest
    {
        public DateTime? OlderThan { get; set; }
    }

}
