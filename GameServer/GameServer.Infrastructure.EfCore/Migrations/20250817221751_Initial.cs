using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameServer.Infrastructure.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gameserver");

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "gameserver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true, comment: "Flag para soft delete"),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                schema: "gameserver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true, comment: "Flag para soft delete"),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false, comment: "ID da conta proprietária do personagem"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, comment: "Nome do personagem (3-20 caracteres, case-insensitive)"),
                    Vocation = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0, comment: "Vocação do personagem "),
                    Gender = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0, comment: "Sexo do personagem"),
                    Direction = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)5, comment: "Direção que o personagem está virado"),
                    Position = table.Column<string>(type: "TEXT", nullable: false, comment: "Posição do personagem no mapa (X,Y,Z)"),
                    Speed = table.Column<float>(type: "REAL", nullable: false, defaultValue: 40f, comment: "Velocidade de movimento do personagem")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "gameserver",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsActive",
                schema: "gameserver",
                table: "Accounts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_AccountId",
                schema: "gameserver",
                table: "Characters",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_AccountId_IsActive",
                schema: "gameserver",
                table: "Characters",
                columns: new[] { "AccountId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Class",
                schema: "gameserver",
                table: "Characters",
                column: "Vocation");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name_Unique",
                schema: "gameserver",
                table: "Characters",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters",
                schema: "gameserver");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "gameserver");
        }
    }
}
