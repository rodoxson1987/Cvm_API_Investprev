using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvmApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "informes_diarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tp_fundo = table.Column<string>(type: "TEXT", nullable: false),
                    cnpj_fundo = table.Column<string>(type: "TEXT", nullable: false),
                    dt_comptc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    vl_total = table.Column<decimal>(type: "TEXT", nullable: false),
                    vl_quota = table.Column<decimal>(type: "TEXT", nullable: false),
                    vl_patrim_liq = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_informes_diarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_informes_diarios_cnpj_fundo_dt_comptc",
                table: "informes_diarios",
                columns: new[] { "cnpj_fundo", "dt_comptc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "informes_diarios");
        }
    }
}
