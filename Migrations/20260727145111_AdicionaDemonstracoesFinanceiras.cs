using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvmApi.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaDemonstracoesFinanceiras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demonstracoes_financeiras",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cnpj_cia = table.Column<string>(type: "TEXT", nullable: false),
                    dt_refer = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ordem_exerc = table.Column<string>(type: "TEXT", nullable: false),
                    cd_conta = table.Column<string>(type: "TEXT", nullable: false),
                    ds_conta = table.Column<string>(type: "TEXT", nullable: false),
                    vl_conta = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demonstracoes_financeiras", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demonstracoes_financeiras");
        }
    }
}
