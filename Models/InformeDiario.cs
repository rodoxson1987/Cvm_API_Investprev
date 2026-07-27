namespace CvmApi.Models
{
    public class InformeDiario
    {
        public int Id { get; set; }
        public string CnpjFundo { get; set; } = string.Empty;
        public DateTime DataInforme { get; set; }
        public decimal ValorCota { get; set; }
        public decimal PatrimonioLiquido { get; set; }
        public decimal CapatacaoDia { get; set; }
        public decimal ResgateDia { get; set; }
        public int NumeroCotistas { get; set; }
    }
}