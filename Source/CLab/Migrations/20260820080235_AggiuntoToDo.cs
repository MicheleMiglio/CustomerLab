using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLab.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoToDo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToDo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titolo = table.Column<string>(type: "TEXT", nullable: false),
                    Descrizione = table.Column<string>(type: "TEXT", nullable: true),
                    Completato = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCompletamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataScadenza = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Priorita = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordine = table.Column<int>(type: "INTEGER", nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReferenteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClienteNomeStorico = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToDo_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToDo_Referenti_ReferenteId",
                        column: x => x.ReferenteId,
                        principalTable: "Referenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToDoSottoAttivita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToDoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Testo = table.Column<string>(type: "TEXT", nullable: false),
                    Completato = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ordine = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDoSottoAttivita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToDoSottoAttivita_ToDo_ToDoId",
                        column: x => x.ToDoId,
                        principalTable: "ToDo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToDo_ClienteId",
                table: "ToDo",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDo_ReferenteId",
                table: "ToDo",
                column: "ReferenteId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDoSottoAttivita_ToDoId",
                table: "ToDoSottoAttivita",
                column: "ToDoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToDoSottoAttivita");

            migrationBuilder.DropTable(
                name: "ToDo");
        }
    }
}
