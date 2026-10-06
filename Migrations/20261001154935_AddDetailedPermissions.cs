using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAddEmployee",
                table: "Admins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDeleteEmployee",
                table: "Admins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEditEmployee",
                table: "Admins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "Admins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAddEmployee",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "CanDeleteEmployee",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "CanEditEmployee",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "Admins");
        }
    }
}
