using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLab.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoModuloPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasterSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    RecoverySalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DekCifrataMaster = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DekNonceMaster = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DekTagMaster = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DekCifrataRecovery = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DekNonceRecovery = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DekTagRecovery = table.Column<byte[]>(type: "BLOB", nullable: false),
                    IterazioniPbkdf2 = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfiguratoIl = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Passwords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PayloadCifrato = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Tag = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passwords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordConfig_Id",
                table: "PasswordConfig",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordConfig");

            migrationBuilder.DropTable(
                name: "Passwords");
        }
    }
}
