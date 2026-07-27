using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvmApi.Models;

[Table("demonstracoes_financeiras")]
public class DemonstracaoFinanceira
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("cnpj_cia")]
    public string Cnpj { get; set; } = string.Empty;

    [Column("dt_refer")]
    public DateTime DataReferencia { get; set; }

    [Column("ordem_exerc")]
    public string OrdemExercicio { get; set; } = string.Empty; // ex: LAST (Último) ou PENULT (Penúltimo)

    [Column("cd_conta")]
    public string CodigoConta { get; set; } = string.Empty; // ex: 3.01 (Receita de Venda)

    [Column("ds_conta")]
    public string DescricaoConta { get; set; } = string.Empty; // ex: Receita Operacional Líquida

    [Column("vl_conta")]
    public decimal ValorConta { get; set; }
}