using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CvmApi.Data;
using CvmApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CvmApi.Services
{
    public class CvmSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _db;

        public CvmSyncService(HttpClient httpClient, AppDbContext db)
        {
            _httpClient = httpClient;
            _db = db;
        }

        /// <summary>
        /// Sincroniza o arquivo cad_fi.csv mantendo TODAS as situações.
        /// </summary>
        public async Task<int> SincronizarCadastroFundosAsync()
        {
            string url = "https://dados.cvm.gov.br/dados/FI/CAD/DADOS/cad_fi.csv";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var latin1Encoding = Encoding.GetEncoding("iso-8859-1");
            using var reader = new StreamReader(stream, latin1Encoding);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);
            var registros = new List<FundoCadastro>();

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                string rawCnpj = csv.GetField<string>("CNPJ_FUNDO") ?? "";
                string cnpjLimpo = new string(rawCnpj.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(cnpjLimpo)) continue;

                cnpjLimpo = cnpjLimpo.PadLeft(14, '0');

                DateTime? dtIni = null;
                if (DateTime.TryParse(csv.GetField<string>("DT_INI_ACTIV"), out var parsedDt))
                {
                    dtIni = parsedDt;
                }

                registros.Add(new FundoCadastro
                {
                    CnpjFundo = cnpjLimpo,
                    RazaoSocial = csv.GetField<string>("DENOM_SOCIAL") ?? "",
                    NomeComercial = csv.GetField<string>("NM_FANTASIA"),
                    Situacao = csv.GetField<string>("SIT") ?? "NÃO INFORMADO",
                    DataInicioAtividade = dtIni,
                    Administrador = csv.GetField<string>("ADMIN"),
                    CnpjAdministrador = new string((csv.GetField<string>("CPF_CNPJ_ADMIN") ?? "").Where(char.IsDigit).ToArray())
                });
            }

            if (registros.Any())
            {
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM fundos_cadastro");
                await _db.FundosCadastro.AddRangeAsync(registros);
                await _db.SaveChangesAsync();
            }

            return registros.Count;
        }

        /// <summary>
        /// Sincroniza informes diários por data no formato DD-MM-YYYY (Ex: 15-07-2026).
        /// </summary>
        public async Task<int> SincronizarInformeDiarioAsync(string dataStr)
        {
            if (!DateTime.TryParseExact(dataStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataCompt))
            {
                throw new ArgumentException("Formato de data inválido. Use DD-MM-YYYY (Ex: 15-07-2026).");
            }

            string anoMes = dataCompt.ToString("yyyyMM");
            string dataFormatadaArquivo = dataCompt.ToString("yyyyMMdd");
            string url = $"https://dados.cvm.gov.br/dados/FI/DOC/INF_DIARIO/DADOS/inf_diario_fi_{anoMes}.zip";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return 0; // Arquivo não encontrado para o mês/ano
            }

            using var zipStream = await response.Content.ReadAsStreamAsync();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            if (entry == null) return 0;

            using var unzippedStream = entry.Open();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = new StreamReader(unzippedStream, Encoding.GetEncoding("iso-8859-1"));

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);
            var novosInformes = new List<InformeDiario>();

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                string dtArq = csv.GetField<string>("DT_COMPTC") ?? "";
                if (dtArq != dataCompt.ToString("yyyy-MM-dd")) continue;

                string rawCnpj = csv.GetField<string>("CNPJ_FUNDO") ?? "";
                string cnpjLimpo = new string(rawCnpj.Where(char.IsDigit).ToArray()).PadLeft(14, '0');

                novosInformes.Add(new InformeDiario
                {
                    CnpjFundo = cnpjLimpo,
                    DataInforme = dataCompt,
                    ValorCota = csv.GetField<decimal>("VL_QUOTA"),
                    PatrimonioLiquido = csv.GetField<decimal>("VL_PATRIM_LIQ"),
                    CapatacaoDia = csv.GetField<decimal>("CAPTAC_DIA"),
                    ResgateDia = csv.GetField<decimal>("RESG_DIA"),
                    NumeroCotistas = csv.GetField<int>("NR_COTST")
                });
            }

            if (novosInformes.Any())
            {
                // Remove registros antigos da mesma data para não duplicar
                var antigos = _db.InformesDiarios.Where(i => i.DataInforme == dataCompt);
                _db.InformesDiarios.RemoveRange(antigos);

                await _db.InformesDiarios.AddRangeAsync(novosInformes);
                await _db.SaveChangesAsync();
            }

            return novosInformes.Count;
        }

        /// <summary>
        /// Sincroniza cad_cia_aberta.csv.
        /// </summary>
        public async Task<int> SincronizarCompanhiasAbertasAsync()
        {
            string url = "https://dados.cvm.gov.br/dados/CIA_ABERTA/CAD/DADOS/cad_cia_aberta.csv";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1"));

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);
            var lista = new List<CompanhiaAberta>();

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                string rawCnpj = csv.GetField<string>("CNPJ_CIA") ?? "";
                string cnpjLimpo = new string(rawCnpj.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(cnpjLimpo)) continue;

                lista.Add(new CompanhiaAberta
                {
                    Cnpj = cnpjLimpo.PadLeft(14, '0'),
                    RazaoSocial = csv.GetField<string>("DENOM_SOCIAL") ?? "",
                    NomeComercial = csv.GetField<string>("DENOM_COMERC"),
                    CodigoCvm = csv.GetField<string>("CD_CVM"),
                    Situacao = csv.GetField<string>("SIT") ?? ""
                });
            }

            if (lista.Any())
            {
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM companhias_abertas");
                await _db.CompanhiasAbertas.AddRangeAsync(lista);
                await _db.SaveChangesAsync();
            }

            return lista.Count;
        }
    }
}