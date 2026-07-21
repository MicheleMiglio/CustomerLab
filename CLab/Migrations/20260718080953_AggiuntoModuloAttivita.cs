using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLab.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoModuloAttivita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attivita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Periodicita = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoCampo = table.Column<int>(type: "INTEGER", nullable: false),
                    TestoLunghezzaMassima = table.Column<int>(type: "INTEGER", nullable: true),
                    NumeroEImporto = table.Column<bool>(type: "INTEGER", nullable: false),
                    TendinaRichiedeImporto = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attivita", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RitenuteAcconto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroFattura = table.Column<string>(type: "TEXT", nullable: false),
                    DataFattura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataPagamentoFattura = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImportoRitenuta = table.Column<decimal>(type: "TEXT", nullable: false),
                    ScadenzaVersamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImportoVersato = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RitenuteAcconto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RitenuteAcconto_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiAttivita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttivitaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataAssegnazione = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiAttivita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientiAttivita_Attivita_AttivitaId",
                        column: x => x.AttivitaId,
                        principalTable: "Attivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiAttivita_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Compilazioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttivitaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Anno = table.Column<int>(type: "INTEGER", nullable: false),
                    Periodo = table.Column<int>(type: "INTEGER", nullable: false),
                    ValoreBooleano = table.Column<bool>(type: "INTEGER", nullable: true),
                    ValoreTesto = table.Column<string>(type: "TEXT", nullable: true),
                    ValoreNumero = table.Column<decimal>(type: "TEXT", nullable: true),
                    Commento = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compilazioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compilazioni_Attivita_AttivitaId",
                        column: x => x.AttivitaId,
                        principalTable: "Attivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Compilazioni_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpzioniAttivita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttivitaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Testo = table.Column<string>(type: "TEXT", nullable: false),
                    Ordine = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpzioniAttivita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpzioniAttivita_Attivita_AttivitaId",
                        column: x => x.AttivitaId,
                        principalTable: "Attivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivita_AttivitaId",
                table: "ClientiAttivita",
                column: "AttivitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivita_ClienteId_AttivitaId",
                table: "ClientiAttivita",
                columns: new[] { "ClienteId", "AttivitaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compilazioni_AttivitaId",
                table: "Compilazioni",
                column: "AttivitaId");

            migrationBuilder.CreateIndex(
                name: "IX_Compilazioni_ClienteId_AttivitaId_Anno_Periodo",
                table: "Compilazioni",
                columns: new[] { "ClienteId", "AttivitaId", "Anno", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpzioniAttivita_AttivitaId",
                table: "OpzioniAttivita",
                column: "AttivitaId");

            migrationBuilder.CreateIndex(
                name: "IX_RitenuteAcconto_ClienteId",
                table: "RitenuteAcconto",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientiAttivita");

            migrationBuilder.DropTable(
                name: "Compilazioni");

            migrationBuilder.DropTable(
                name: "OpzioniAttivita");

            migrationBuilder.DropTable(
                name: "RitenuteAcconto");

            migrationBuilder.DropTable(
                name: "Attivita");
        }
    }
}
