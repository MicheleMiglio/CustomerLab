using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLab.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoFatture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fatture",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    DataEmissione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Importo = table.Column<decimal>(type: "TEXT", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataPagamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Annullata = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fatture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fatture_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fatture_ClienteId",
                table: "Fatture",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fatture");
        }
    }
}
