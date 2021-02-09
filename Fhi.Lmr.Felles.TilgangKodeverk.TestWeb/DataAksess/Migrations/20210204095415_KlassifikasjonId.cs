using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TilgangKodeverk.DataAksess.Migrations
{
    public partial class KlassifikasjonId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Klassifikasjon",
                columns: table => new
                {
                    KlassifikasjonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OId = table.Column<int>(type: "int", nullable: false),
                    Nedlasted = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klassifikasjon", x => x.KlassifikasjonId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Klassifikasjon");
        }
    }
}
