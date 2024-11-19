using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WindowsServerDnsUpdater.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Logger = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MikrotikIp = table.Column<string>(type: "TEXT", nullable: false),
                    MikrotikLogin = table.Column<string>(type: "TEXT", nullable: false),
                    MikrotikPassword = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultDomain = table.Column<string>(type: "TEXT", nullable: false),
                    LeaseUpdateDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CacheUpdateIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    VpnSitesListName = table.Column<string>(type: "TEXT", nullable: false),
                    VpnSitesListUpdateDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
