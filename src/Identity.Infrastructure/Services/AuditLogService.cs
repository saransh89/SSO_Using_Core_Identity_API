using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _db;

        public AuditLogService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DateTime?> GetLastArchivedDateAsync()
        {
            // Check if there are any archived logs
            var lastArchived = await _db.AuditLogArchives
                                        .OrderByDescending(x => x.Timestamp)
                                        .Select(x => x.Timestamp)
                                        .FirstOrDefaultAsync();

            if (lastArchived == DateTime.MinValue)
            {
                lastArchived = DateTime.Now;
            }

            return lastArchived;
        }

        public async Task<int> ArchiveAuditLogsAsync(DateTime? olderThan = null)
        {
            //0) Get the Last Update date in AuditLogsArchive
            if(olderThan== null || olderThan == DateTime.MinValue)
            {
                olderThan = DateTime.UtcNow.AddDays(-1);//await GetLastArchivedDateAsync();
            }
            // 1) Select the rows to archive
            var toArchive = await _db.AuditLogs
                                     .Where(x => x.Timestamp < olderThan)
                                     .ToListAsync();

            if (!toArchive.Any())
            {
                // If no logs found, return 0
                return 0;
            }

            // 2) Project into the archive entity
            var archives = toArchive.Select(x => new AuditLogArchive
            {
                Id = x.Id,
                TableName = x.TableName,
                KeyValues = x.KeyValues,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                Action = x.Action,
                UserId = x.UserId,
                Timestamp = x.Timestamp
            }).ToList();

            // 3) Add to archive table
            _db.AuditLogArchives.AddRange(archives);

            // 4) Remove from live table
            _db.AuditLogs.RemoveRange(toArchive);

            // 5) Commit changes to database
            await _db.SaveChangesAsync();

            // Return the number of archived logs
            return archives.Count;
        }
    }


}
