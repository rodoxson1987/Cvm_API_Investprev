namespace CvmApi.Models
{
    public class CompanhiaAberta
    {
        public int Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? NomeComercial { get; set; }
        public string? CodigoCvm { get; set; }
        public string Situacao { get; set; } = string.Empty;
    }
}