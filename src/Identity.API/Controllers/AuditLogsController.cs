using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Shared.DTOs;
using Identity.Shared.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "BackgroundJobsOnly")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Archives audit logs older than the provided date.
        /// Accepts input from JSON body.
        /// </summary>
        [HttpPost("archive")]
        public async Task<IActionResult> ArchiveAuditLogs([FromBody] ArchiveAuditLogsRequest request)
        {
            if (request == null || request.OlderThan == null)
            {
                return BadRequest("The 'olderThan' field is required in the request body.");
            }

            var archivedCount = await _auditLogService.ArchiveAuditLogsAsync(request.OlderThan);

            if (archivedCount == 0)
            {
                return NotFound($"No audit logs older than {request.OlderThan} were found to archive.");
            }

            return Ok(new { ArchivedCount = archivedCount });
        }
    }
}
