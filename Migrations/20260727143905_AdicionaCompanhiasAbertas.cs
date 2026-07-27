using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvmApi.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCompanhiasAbertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companhias_abertas",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cnpj_cia = table.Column<string>(type: "TEXT", nullable: false),
                    denom_social = table.Column<string>(type: "TEXT", nullable: false),
                    denom_comercial = table.Column<string>(type: "TEXT", nullable: true),
                    codigo_cvm = table.Column<string>(type: "TEXT", nullable: false),
                    sit = table.Column<string>(type: "TEXT", nullable: false),
                    setor_ativ = table.Column<string>(type: "TEXT", nullable: true),
                    dt_reg = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companhias_abertas", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companhias_abertas");
        }
    }
}
