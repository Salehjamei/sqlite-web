using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceTrackingAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplyAutoLogout",
                table: "WorkSettings");

            migrationBuilder.DropColumn(
                name: "AutoLogoutTime",
                table: "WorkSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApplyAutoLogout",
                table: "WorkSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AutoLogoutTime",
                table: "WorkSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
