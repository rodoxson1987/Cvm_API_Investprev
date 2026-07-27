using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvmApi.Migrations
{
    /// <inheritdoc />
    public partial class AtualizaModeloCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_fundos_cadastro",
                table: "fundos_cadastro");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "fundos_cadastro",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_fundos_cadastro",
                table: "fundos_cadastro",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_fundos_cadastro",
                table: "fundos_cadastro");

            migrationBuilder.DropColumn(
                name: "id",
                table: "fundos_cadastro");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fundos_cadastro",
                table: "fundos_cadastro",
                column: "cnpj_fundo");
        }
    }
}
