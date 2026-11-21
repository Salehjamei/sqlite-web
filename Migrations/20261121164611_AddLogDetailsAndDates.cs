using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class AddLogDetailsAndDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedDate",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedTime",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventDate",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventTime",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageArgs",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageKey",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "AdminNotificationLogs");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "AdminNotificationLogs");

            migrationBuilder.DropColumn(
                name: "EventDate",
                table: "AdminNotificationLogs");

            migrationBuilder.DropColumn(
                name: "EventTime",
                table: "AdminNotificationLogs");

            migrationBuilder.DropColumn(
                name: "MessageArgs",
                table: "AdminNotificationLogs");

            migrationBuilder.DropColumn(
                name: "MessageKey",
                table: "AdminNotificationLogs");
        }
    }
}
