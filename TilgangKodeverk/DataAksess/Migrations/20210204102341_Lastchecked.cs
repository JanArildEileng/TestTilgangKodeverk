using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TilgangKodeverk.DataAksess.Migrations
{
    public partial class Lastchecked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Lastchecked",
                table: "Klassifikasjon",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lastchecked",
                table: "Klassifikasjon");
        }
    }
}
