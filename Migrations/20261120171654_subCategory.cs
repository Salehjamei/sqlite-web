using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class subCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "AdminNotificationLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "AdminNotificationLogs");
        }
    }
}
