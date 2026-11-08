using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminTrackingAndLogsToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByAdminId",
                table: "Attendances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate",
                table: "Attendances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EntrySource",
                table: "Attendances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LastModifiedByAdminId",
                table: "Attendances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceEditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttendanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminUsername = table.Column<string>(type: "TEXT", nullable: false),
                    EditDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OldClockInTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OldClockOutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NewClockInTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NewClockOutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceEditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceEditLogs_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEditLogs_AttendanceId",
                table: "AttendanceEditLogs",
                column: "AttendanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceEditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedByAdminId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "EntrySource",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LastModifiedByAdminId",
                table: "Attendances");
        }
    }
}
