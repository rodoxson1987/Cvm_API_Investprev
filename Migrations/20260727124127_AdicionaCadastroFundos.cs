using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvmApi.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCadastroFundos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fundos_cadastro",
                columns: table => new
                {
                    cnpj_fundo = table.Column<string>(type: "TEXT", nullable: false),
                    denom_social = table.Column<string>(type: "TEXT", nullable: false),
                    denom_comercial = table.Column<string>(type: "TEXT", nullable: true),
                    sit = table.Column<string>(type: "TEXT", nullable: false),
                    dt_ini_activ = table.Column<DateTime>(type: "TEXT", nullable: true),
                    admin = table.Column<string>(type: "TEXT", nullable: true),
                    cpf_cnpj_admin = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundos_cadastro", x => x.cnpj_fundo);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fundos_cadastro");
        }
    }
}
