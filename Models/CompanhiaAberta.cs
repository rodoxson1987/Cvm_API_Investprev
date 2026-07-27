using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvmApi.Models;

[Table("companhias_abertas")]
public class CompanhiaAberta
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("cnpj_cia")]
    public string Cnpj { get; set; } = string.Empty;

    [Column("denom_social")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Column("denom_comercial")]
    public string? NomeComercial { get; set; }

    [Column("codigo_cvm")]
    public string CodigoCvm { get; set; } = string.Empty;

    [Column("sit")]
    public string Situacao { get; set; } = string.Empty;

    [Column("setor_ativ")]
    public string? SetorAtividade { get; set; }

    [Column("dt_reg")]
    public DateTime? DataRegistro { get; set; }
}