using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExamSystem.Infrastructure.Migrations
{
    public partial class AddIsPublishedToExamPapers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ExamPapers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "QuestionBanks",
                keyColumn: "BankId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 21, 10, 58, 29, 885, DateTimeKind.Local).AddTicks(3725), new DateTime(2025, 10, 21, 10, 58, 29, 885, DateTimeKind.Local).AddTicks(3797) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 21, 10, 58, 29, 884, DateTimeKind.Local).AddTicks(7690));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ExamPapers");

            migrationBuilder.UpdateData(
                table: "QuestionBanks",
                keyColumn: "BankId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 20, 1, 17, 10, 726, DateTimeKind.Local).AddTicks(5189), new DateTime(2025, 10, 20, 1, 17, 10, 726, DateTimeKind.Local).AddTicks(5259) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 20, 1, 17, 10, 725, DateTimeKind.Local).AddTicks(9707));
        }
    }
}
