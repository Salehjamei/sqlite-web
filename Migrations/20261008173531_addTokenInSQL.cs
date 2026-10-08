using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sqlite_web.Migrations
{
    /// <inheritdoc />
    public partial class addTokenInSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentToken",
                table: "Admins",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                table: "Admins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTokenStopped",
                table: "Admins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpireTime",
                table: "Admins",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentToken",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsTokenStopped",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "TokenExpireTime",
                table: "Admins");
        }
    }
}
