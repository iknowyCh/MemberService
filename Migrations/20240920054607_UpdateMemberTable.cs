using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMemberTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Members");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Members",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "Identity",
                table: "Members",
                newName: "IP");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Members",
                newName: "HWID");

            migrationBuilder.AddColumn<int>(
                name: "ChangeTime",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Creator",
                table: "Members",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Members",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Locked",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Plan",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Times",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Members",
                table: "Members",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Members",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ChangeTime",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Creator",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Locked",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Times",
                table: "Members");

            migrationBuilder.RenameTable(
                name: "Members",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "IP",
                table: "Users",
                newName: "Identity");

            migrationBuilder.RenameColumn(
                name: "HWID",
                table: "Users",
                newName: "Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");
        }
    }
}
