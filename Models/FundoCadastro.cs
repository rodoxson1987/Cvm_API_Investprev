namespace CvmApi.Models
{
    public class FundoCadastro
    {
        public int Id { get; set; }
        public string CnpjFundo { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? NomeComercial { get; set; }
        public string Situacao { get; set; } = string.Empty;
        public DateTime? DataInicioAtividade { get; set; }
        public string? Administrador { get; set; }
        public string? CnpjAdministrador { get; set; }
    }
}