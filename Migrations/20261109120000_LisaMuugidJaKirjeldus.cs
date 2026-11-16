using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooded_EFCore.Migrations
{
    [DbContext(typeof(ToodedContext))]
    [Migration("20261109120000_LisaMuugidJaKirjeldus")]
    public partial class LisaMuugidJaKirjeldus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kirjeldus",
                table: "Toodetabel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Muugid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToodeId = table.Column<int>(type: "int", nullable: false),
                    Kogus = table.Column<int>(type: "int", nullable: false),
                    Aeg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Demo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Muugid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Muugid_Toodetabel_ToodeId",
                        column: x => x.ToodeId,
                        principalTable: "Toodetabel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Muugid_ToodeId",
                table: "Muugid",
                column: "ToodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Muugid");

            migrationBuilder.DropColumn(
                name: "Kirjeldus",
                table: "Toodetabel");
        }
    }
}
