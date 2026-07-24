using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CvmApi.Models;

[Table("informes_diarios")]
[Index(nameof(CnpjFundo), nameof(DataCompetencia), IsUnique = true)]
public class InformeDiario
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("tp_fundo")]
    public string TipoFundo { get; set; } = string.Empty;

    [Column("cnpj_fundo")]
    public string CnpjFundo { get; set; } = string.Empty;

    [Column("dt_comptc")]
    public DateTime DataCompetencia { get; set; }

    [Column("vl_total")]
    public decimal ValorTotal { get; set; }

    [Column("vl_quota")]
    public decimal ValorCota { get; set; }

    [Column("vl_patrim_liq")]
    public decimal PatrimonioLiquido { get; set; }
}