using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Employees");
        }
    }
}
