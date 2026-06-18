using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLab.Migrations
{
    /// <inheritdoc />
    public partial class StatoClienteReferentePrincipale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Attivo",
                table: "Clienti",
                newName: "Stato");

            migrationBuilder.AddColumn<bool>(
                name: "Principale",
                table: "Contatti",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Referente",
                table: "Clienti",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Principale",
                table: "Contatti");

            migrationBuilder.DropColumn(
                name: "Referente",
                table: "Clienti");

            migrationBuilder.RenameColumn(
                name: "Stato",
                table: "Clienti",
                newName: "Attivo");
        }
    }
}
