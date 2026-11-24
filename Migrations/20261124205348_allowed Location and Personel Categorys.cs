using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class allowedLocationandPersonelCategorys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonelCategorys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelCategorys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelCategorys_PersonelCategorys_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PersonelCategorys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AllowedLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    RadiusMeters = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowedMainCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowedSubCategory1Id = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowedSubCategory2Id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllowedLocations_PersonelCategorys_AllowedMainCategoryId",
                        column: x => x.AllowedMainCategoryId,
                        principalTable: "PersonelCategorys",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllowedLocations_PersonelCategorys_AllowedSubCategory1Id",
                        column: x => x.AllowedSubCategory1Id,
                        principalTable: "PersonelCategorys",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllowedLocations_PersonelCategorys_AllowedSubCategory2Id",
                        column: x => x.AllowedSubCategory2Id,
                        principalTable: "PersonelCategorys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllowedLocations_AllowedMainCategoryId",
                table: "AllowedLocations",
                column: "AllowedMainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedLocations_AllowedSubCategory1Id",
                table: "AllowedLocations",
                column: "AllowedSubCategory1Id");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedLocations_AllowedSubCategory2Id",
                table: "AllowedLocations",
                column: "AllowedSubCategory2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelCategorys_ParentId",
                table: "PersonelCategorys",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowedLocations");

            migrationBuilder.DropTable(
                name: "PersonelCategorys");
        }
    }
}
