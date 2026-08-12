using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLab.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoReferentiProgrammi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Referenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Attivo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programmi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programmi", x => x.Id);
                });

            migrationBuilder.DropColumn(
                name: "Referente",
                table: "Clienti");

            migrationBuilder.AddColumn<int>(
                name: "ReferenteId",
                table: "Clienti",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgrammaId",
                table: "Clienti",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_ReferenteId",
                table: "Clienti",
                column: "ReferenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_ProgrammaId",
                table: "Clienti",
                column: "ProgrammaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clienti_Referenti_ReferenteId",
                table: "Clienti",
                column: "ReferenteId",
                principalTable: "Referenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clienti_Programmi_ProgrammaId",
                table: "Clienti",
                column: "ProgrammaId",
                principalTable: "Programmi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_Fatture_Clienti_ClienteId",
                table: "Fatture");

            migrationBuilder.DropIndex(
                name: "IX_Fatture_ClienteId",
                table: "Fatture");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Fatture");

            migrationBuilder.AddColumn<int>(
                name: "ReferenteId",
                table: "Fatture",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fatture_ReferenteId",
                table: "Fatture",
                column: "ReferenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fatture_Referenti_ReferenteId",
                table: "Fatture",
                column: "ReferenteId",
                principalTable: "Referenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fatture_Referenti_ReferenteId",
                table: "Fatture");

            migrationBuilder.DropIndex(
                name: "IX_Fatture_ReferenteId",
                table: "Fatture");

            migrationBuilder.DropColumn(
                name: "ReferenteId",
                table: "Fatture");

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "Fatture",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Fatture_ClienteId",
                table: "Fatture",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fatture_Clienti_ClienteId",
                table: "Fatture",
                column: "ClienteId",
                principalTable: "Clienti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_Clienti_Programmi_ProgrammaId",
                table: "Clienti");

            migrationBuilder.DropForeignKey(
                name: "FK_Clienti_Referenti_ReferenteId",
                table: "Clienti");

            migrationBuilder.DropIndex(
                name: "IX_Clienti_ProgrammaId",
                table: "Clienti");

            migrationBuilder.DropIndex(
                name: "IX_Clienti_ReferenteId",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "ProgrammaId",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "ReferenteId",
                table: "Clienti");

            migrationBuilder.AddColumn<string>(
                name: "Referente",
                table: "Clienti",
                type: "TEXT",
                nullable: true);

            migrationBuilder.DropTable(
                name: "Programmi");

            migrationBuilder.DropTable(
                name: "Referenti");
        }
    }
}
