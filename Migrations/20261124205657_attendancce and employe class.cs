using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class attendancceandemployeclass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MainCategoryId",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubCategory1Id",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubCategory2Id",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockInLocation",
                table: "Attendances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockOutLocation",
                table: "Attendances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Attendances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_MainCategoryId",
                table: "Employees",
                column: "MainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SubCategory1Id",
                table: "Employees",
                column: "SubCategory1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SubCategory2Id",
                table: "Employees",
                column: "SubCategory2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_PersonelCategorys_MainCategoryId",
                table: "Employees",
                column: "MainCategoryId",
                principalTable: "PersonelCategorys",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_PersonelCategorys_SubCategory1Id",
                table: "Employees",
                column: "SubCategory1Id",
                principalTable: "PersonelCategorys",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_PersonelCategorys_SubCategory2Id",
                table: "Employees",
                column: "SubCategory2Id",
                principalTable: "PersonelCategorys",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_PersonelCategorys_MainCategoryId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_PersonelCategorys_SubCategory1Id",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_PersonelCategorys_SubCategory2Id",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_MainCategoryId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SubCategory1Id",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SubCategory2Id",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MainCategoryId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SubCategory1Id",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SubCategory2Id",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ClockInLocation",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ClockOutLocation",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Attendances");
        }
    }
}
