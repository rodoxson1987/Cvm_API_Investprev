using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvmApi.Models;

[Table("fundos_cadastro")]
public class FundoCadastro
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; } // <--- Nova Chave Primária Única (Permite CNPJs repetidos)

    [Column("cnpj_fundo")]
    public string CnpjFundo { get; set; } = string.Empty;

    [Column("denom_social")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Column("denom_comercial")]
    public string? NomeComercial { get; set; }

    [Column("sit")]
    public string Situacao { get; set; } = string.Empty;

    [Column("dt_ini_activ")]
    public DateTime? DataInicioAtividade { get; set; }

    [Column("admin")]
    public string? Administrador { get; set; }

    [Column("cpf_cnpj_admin")]
    public string? CnpjAdministrador { get; set; }
}