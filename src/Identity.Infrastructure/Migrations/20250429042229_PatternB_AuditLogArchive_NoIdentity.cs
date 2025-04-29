using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PatternB_AuditLogArchive_NoIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) rename the old table
            migrationBuilder.Sql("EXEC sp_rename N'dbo.AuditLogArchives', N'AuditLogArchives_old';");

            // 2) create the new table without IDENTITY on Id
            migrationBuilder.Sql(@"
      CREATE TABLE dbo.AuditLogArchives
      (
        Id         INT          PRIMARY KEY,    -- no IDENTITY
        TableName  NVARCHAR(200) NOT NULL,
        KeyValues  NVARCHAR(MAX) NULL,
        OldValues  NVARCHAR(MAX) NULL,
        NewValues  NVARCHAR(MAX) NULL,
        Action     NVARCHAR(50)  NULL,
        UserId     NVARCHAR(128) NULL,
        Timestamp  DATETIME2     NOT NULL
      );
    ");

            // 3) copy all rows from the old table
            migrationBuilder.Sql(@"
      INSERT INTO dbo.AuditLogArchives
        (Id, TableName, KeyValues, OldValues, NewValues, Action, UserId, Timestamp)
      SELECT
        Id, TableName, KeyValues, OldValues, NewValues, Action, UserId, Timestamp
      FROM dbo.AuditLogArchives_old;
    ");

            // 4) drop the old table
            migrationBuilder.Sql("DROP TABLE dbo.AuditLogArchives_old;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // rename the archive we just built out of the way
            migrationBuilder.Sql("EXEC sp_rename N'dbo.AuditLogArchives', N'AuditLogArchives_new';");

            // recreate the original table with IDENTITY on Id
            migrationBuilder.Sql(@"
      CREATE TABLE dbo.AuditLogArchives
      (
        Id         INT IDENTITY PRIMARY KEY,
        TableName  NVARCHAR(200) NOT NULL,
        KeyValues  NVARCHAR(MAX) NULL,
        OldValues  NVARCHAR(MAX) NULL,
        NewValues  NVARCHAR(MAX) NULL,
        Action     NVARCHAR(50)  NULL,
        UserId     NVARCHAR(128) NULL,
        Timestamp  DATETIME2     NOT NULL
      );
    ");

            // copy back
            migrationBuilder.Sql(@"
      INSERT INTO dbo.AuditLogArchives
        (TableName, KeyValues, OldValues, NewValues, Action, UserId, Timestamp)
      SELECT
        TableName, KeyValues, OldValues, NewValues, Action, UserId, Timestamp
      FROM dbo.AuditLogArchives_new;
    ");

            // drop the new helper table
            migrationBuilder.Sql("DROP TABLE dbo.AuditLogArchives_new;");
        }
    }
}
