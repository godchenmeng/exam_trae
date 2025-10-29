using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "QuestionBanks",
                keyColumn: "BankId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 27, 1, 34, 3, 353, DateTimeKind.Local).AddTicks(311), new DateTime(2025, 10, 27, 1, 34, 3, 353, DateTimeKind.Local).AddTicks(312) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 27, 1, 34, 3, 353, DateTimeKind.Local).AddTicks(227));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "QuestionBanks",
                keyColumn: "BankId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 27, 1, 25, 34, 554, DateTimeKind.Local).AddTicks(464), new DateTime(2025, 10, 27, 1, 25, 34, 554, DateTimeKind.Local).AddTicks(465) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 27, 1, 25, 34, 554, DateTimeKind.Local).AddTicks(306));
        }
    }
}
