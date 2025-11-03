using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMapDrawingFieldsToAnswerRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MapCenter",
                table: "AnswerRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapDrawingData",
                table: "AnswerRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapZoom",
                table: "AnswerRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MapDrawingData",
                columns: table => new
                {
                    DrawingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnswerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShapeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CoordinatesJson = table.Column<string>(type: "TEXT", nullable: false),
                    StyleJson = table.Column<string>(type: "TEXT", nullable: true),
                    Label = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapDrawingData", x => x.DrawingId);
                    table.ForeignKey(
                        name: "FK_MapDrawingData_AnswerRecords_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "AnswerRecords",
                        principalColumn: "AnswerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_building",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    city = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    city_cn = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    org_city = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    org_area = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    org_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    org_type = table.Column<byte>(type: "INTEGER", nullable: false),
                    addr = table.Column<string>(type: "TEXT", nullable: true),
                    gps = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    creator = table.Column<int>(type: "INTEGER", nullable: true),
                    create_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    amap = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    location = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_building", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "QuestionBanks",
                keyColumn: "BankId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 1, 23, 52, 28, 46, DateTimeKind.Local).AddTicks(7127), new DateTime(2025, 11, 1, 23, 52, 28, 46, DateTimeKind.Local).AddTicks(7128) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 23, 52, 28, 46, DateTimeKind.Local).AddTicks(7001));

            migrationBuilder.CreateIndex(
                name: "IX_MapDrawingData_AnswerId",
                table: "MapDrawingData",
                column: "AnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_MapDrawingData_CreatedAt",
                table: "MapDrawingData",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MapDrawingData_ShapeType",
                table: "MapDrawingData",
                column: "ShapeType");

            migrationBuilder.CreateIndex(
                name: "IX_t_building_city_cn",
                table: "t_building",
                column: "city_cn");

            migrationBuilder.CreateIndex(
                name: "IX_t_building_city_cn_org_type_deleted",
                table: "t_building",
                columns: new[] { "city_cn", "org_type", "deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_t_building_deleted",
                table: "t_building",
                column: "deleted");

            migrationBuilder.CreateIndex(
                name: "IX_t_building_org_type",
                table: "t_building",
                column: "org_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapDrawingData");

            migrationBuilder.DropTable(
                name: "t_building");

            migrationBuilder.DropColumn(
                name: "MapCenter",
                table: "AnswerRecords");

            migrationBuilder.DropColumn(
                name: "MapDrawingData",
                table: "AnswerRecords");

            migrationBuilder.DropColumn(
                name: "MapZoom",
                table: "AnswerRecords");

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
    }
}
