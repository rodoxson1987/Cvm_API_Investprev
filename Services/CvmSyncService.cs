using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CvmApi.Data;
using CvmApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CvmApi.Services;

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
    /// Sincroniza os informes diários (cotas e patrimônio) para uma data específica.
    /// </summary>
    public async Task<int> SincronizarDataDiariaAsync(DateTime data)
    {
        string anoMes = data.ToString("yyyyMM");
        string url = $"https://dados.cvm.gov.br/dados/FI/DOC/INF_DIARIO/DADOS/inf_diario_fi_{anoMes}.zip";

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        
        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        using var zipStream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        string nomeArquivoCsv = $"inf_diario_fi_{anoMes}.csv";
        var entry = archive.GetEntry(nomeArquivoCsv);

        if (entry == null)
        {
            return 0;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        var registrosLidos = new List<InformeDiario>();
        string dataAlvoStr = data.ToString("yyyy-MM-dd");

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            string dtComptc = csv.GetField<string>("DT_COMPTC") ?? "";
            
            if (dtComptc != dataAlvoStr) continue;

            string rawCnpj = csv.GetField<string>("CNPJ_FUNDO") ?? csv.GetField<string>("CNPJ_FUNDO_CLASSE") ?? "";
            string cnpjLimpo = new string(rawCnpj.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(cnpjLimpo)) continue;

            cnpjLimpo = cnpjLimpo.PadLeft(14, '0');
            string tpFundo = csv.GetField<string>("TP_FUNDO") ?? "";
            
            decimal.TryParse(csv.GetField<string>("VL_QUOTA"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vlQuota);
            decimal.TryParse(csv.GetField<string>("VL_PATRIM_LIQ"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vlPatrimLiq);
            decimal.TryParse(csv.GetField<string>("VL_TOTAL"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vlTotal);

            registrosLidos.Add(new InformeDiario
            {
                CnpjFundo = cnpjLimpo,
                TipoFundo = tpFundo,
                DataCompetencia = data.Date,
                ValorCota = vlQuota,
                PatrimonioLiquido = vlPatrimLiq,
                ValorTotal = vlTotal
            });
        }

        var novosRegistros = registrosLidos
            .DistinctBy(r => new { r.CnpjFundo, r.DataCompetencia })
            .ToList();

        if (novosRegistros.Any())
        {
            var cnpjsParaInserir = novosRegistros.Select(r => r.CnpjFundo).ToList();
            
            var antigos = await _db.InformesDiarios
                .Where(i => i.DataCompetencia.Date == data.Date && cnpjsParaInserir.Contains(i.CnpjFundo))
                .ToListAsync();

            if (antigos.Any())
            {
                _db.InformesDiarios.RemoveRange(antigos);
                await _db.SaveChangesAsync();
            }

            await _db.InformesDiarios.AddRangeAsync(novosRegistros);
            await _db.SaveChangesAsync();
        }

        return novosRegistros.Count;
    }

    /// <summary>
    /// Baixa o arquivo oficial de cadastro da CVM (cad_fi.csv) e atualiza a base de cadastros de fundos.
    /// </summary>
    public async Task<int> SincronizarCadastroFundosAsync()
    {
        string url = "https://dados.cvm.gov.br/dados/FI/CAD/DADOS/cad_fi.csv";

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var latin1Encoding = Encoding.GetEncoding("iso-8859-1");
        
        using var reader = new StreamReader(stream, latin1Encoding, detectEncodingFromByteOrderMarks: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        var registrosLidos = new List<FundoCadastro>();

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            string rawCnpj = csv.GetField<string>("CNPJ_FUNDO") ?? "";
            string cnpjLimpo = new string(rawCnpj.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(cnpjLimpo)) continue;

            cnpjLimpo = cnpjLimpo.PadLeft(14, '0');

            string razaoSocial = csv.GetField<string>("DENOM_SOCIAL") ?? "";
            string nomeComercial = csv.GetField<string>("NM_FANTASIA") ?? "";
            string situacao = csv.GetField<string>("SIT") ?? "";
            string admin = csv.GetField<string>("ADMIN") ?? "";
            string cnpjAdmin = csv.GetField<string>("CPF_CNPJ_ADMIN") ?? "";

            DateTime? dtIni = null;
            if (DateTime.TryParse(csv.GetField<string>("DT_INI_ACTIV"), out var parsedDt))
            {
                dtIni = parsedDt;
            }

            registrosLidos.Add(new FundoCadastro
            {
                CnpjFundo = cnpjLimpo,
                RazaoSocial = razaoSocial,
                NomeComercial = string.IsNullOrWhiteSpace(nomeComercial) ? null : nomeComercial,
                Situacao = situacao,
                DataInicioAtividade = dtIni,
                Administrador = string.IsNullOrWhiteSpace(admin) ? null : admin,
                CnpjAdministrador = new string(cnpjAdmin.Where(char.IsDigit).ToArray())
            });
        }

        if (registrosLidos.Any())
        {
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM fundos_cadastro");
            await _db.FundosCadastro.AddRangeAsync(registrosLidos);
            await _db.SaveChangesAsync();
        }

        return registrosLidos.Count;
    }

    /// <summary>
    /// Baixa o cadastro oficial de Companhias Abertas (Empresas/Ações) da CVM (cad_cia_aberta.csv).
    /// </summary>
    public async Task<int> SincronizarCompanhiasAbertasAsync()
    {
        string url = "https://dados.cvm.gov.br/dados/CIA_ABERTA/CAD/DADOS/cad_cia_aberta.csv";

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var latin1Encoding = Encoding.GetEncoding("iso-8859-1");
        
        using var reader = new StreamReader(stream, latin1Encoding, detectEncodingFromByteOrderMarks: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        var empresasLidas = new List<CompanhiaAberta>();

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            string rawCnpj = csv.GetField<string>("CNPJ_CIA") ?? "";
            string cnpjLimpo = new string(rawCnpj.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(cnpjLimpo)) continue;

            cnpjLimpo = cnpjLimpo.PadLeft(14, '0');

            string denomSocial = csv.GetField<string>("DENOM_SOCIAL") ?? "";
            string denomComercial = csv.GetField<string>("DENOM_COMER") ?? "";
            string codCvm = csv.GetField<string>("CD_CVM") ?? "";
            string situacao = csv.GetField<string>("SIT") ?? "";
            string setor = csv.GetField<string>("SETOR_ATIV") ?? "";

            DateTime? dtReg = null;
            if (DateTime.TryParse(csv.GetField<string>("DT_REG"), out var parsedDt))
            {
                dtReg = parsedDt;
            }

            empresasLidas.Add(new CompanhiaAberta
            {
                Cnpj = cnpjLimpo,
                RazaoSocial = denomSocial,
                NomeComercial = string.IsNullOrWhiteSpace(denomComercial) ? null : denomComercial,
                CodigoCvm = codCvm,
                Situacao = situacao,
                SetorAtividade = string.IsNullOrWhiteSpace(setor) ? null : setor,
                DataRegistro = dtReg
            });
        }

        if (empresasLidas.Any())
        {
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM companhias_abertas");
            await _db.CompanhiasAbertas.AddRangeAsync(empresasLidas);
            await _db.SaveChangesAsync();
        }

        return empresasLidas.Count;
    }

    /// <summary>
    /// Busca histórico de cotações de uma ação na B3 por Ticker (ex: PETR4) ou por CNPJ da empresa.
    /// </summary>
    public async Task<(string tickerIdentificado, List<object> cotacoes)> ObterCotacoesAcaoAsync(string termoBusca)
    {
        var resultList = new List<object>();

        if (string.IsNullOrWhiteSpace(termoBusca)) 
            return (string.Empty, resultList);

        string termoLimpo = System.Net.WebUtility.UrlDecode(termoBusca).Trim();
        string apenasDigitos = new string(termoLimpo.Where(char.IsDigit).ToArray());
        string tickerFinal = termoLimpo.ToUpper();

        if (apenasDigitos.Length == 14 || (apenasDigitos.Length > 8 && !termoLimpo.Contains(".")))
        {
            string cnpjFormatado = apenasDigitos.PadLeft(14, '0');
            
            var empresa = await _db.CompanhiasAbertas
                .FirstOrDefaultAsync(e => e.Cnpj == cnpjFormatado);

            if (empresa != null)
            {
                tickerFinal = MapearNomeParaTicker(empresa.NomeComercial ?? empresa.RazaoSocial);
            }
        }

        string tickerYahoo = tickerFinal.EndsWith(".SA") ? tickerFinal : $"{tickerFinal}.SA";

        long period1 = DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds();
        long period2 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{tickerYahoo}?period1={period1}&period2={period2}&interval=1d";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) 
            return (tickerFinal, resultList);

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(jsonString);

        if (!doc.RootElement.TryGetProperty("chart", out var chartElement)) return (tickerFinal, resultList);
        if (!chartElement.TryGetProperty("result", out var resultElement) || resultElement.ValueKind != System.Text.Json.JsonValueKind.Array) return (tickerFinal, resultList);
        if (resultElement.GetArrayLength() == 0) return (tickerFinal, resultList);

        var chartResult = resultElement[0];

        if (!chartResult.TryGetProperty("timestamp", out var timestamps)) return (tickerFinal, resultList);
        if (!chartResult.TryGetProperty("indicators", out var indicators)) return (tickerFinal, resultList);
        if (!indicators.TryGetProperty("quote", out var quoteArray) || quoteArray.GetArrayLength() == 0) return (tickerFinal, resultList);

        var quotes = quoteArray[0];

        if (!quotes.TryGetProperty("close", out var closes)) return (tickerFinal, resultList);
        quotes.TryGetProperty("open", out var opens);
        quotes.TryGetProperty("high", out var highs);
        quotes.TryGetProperty("low", out var lows);
        quotes.TryGetProperty("volume", out var volumes);

        for (int i = 0; i < timestamps.GetArrayLength(); i++)
        {
            if (i >= closes.GetArrayLength() || closes[i].ValueKind == System.Text.Json.JsonValueKind.Null) continue;

            var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).DateTime;

            decimal valorFechamento = closes[i].GetDecimal();
            decimal valorAbertura = (opens.ValueKind != System.Text.Json.JsonValueKind.Undefined && opens[i].ValueKind != System.Text.Json.JsonValueKind.Null) ? opens[i].GetDecimal() : valorFechamento;
            decimal valorMaxima = (highs.ValueKind != System.Text.Json.JsonValueKind.Undefined && highs[i].ValueKind != System.Text.Json.JsonValueKind.Null) ? highs[i].GetDecimal() : valorFechamento;
            decimal valorMinima = (lows.ValueKind != System.Text.Json.JsonValueKind.Undefined && lows[i].ValueKind != System.Text.Json.JsonValueKind.Null) ? lows[i].GetDecimal() : valorFechamento;
            long volumeNegociado = (volumes.ValueKind != System.Text.Json.JsonValueKind.Undefined && volumes[i].ValueKind != System.Text.Json.JsonValueKind.Null) ? volumes[i].GetInt64() : 0;

            resultList.Add(new
            {
                data = date.ToString("dd/MM/yyyy"),
                abertura = valorAbertura,
                maxima = valorMaxima,
                minima = valorMinima,
                fechamento = valorFechamento,
                volume = volumeNegociado
            });
        }

        return (tickerFinal, resultList);
    }

    private string MapearNomeParaTicker(string nomeEmpresa)
    {
        string nome = nomeEmpresa.ToUpper();

        if (nome.Contains("PETROBRAS") || nome.Contains("PETROLEO BRASILEIRO")) return "PETR4";
        if (nome.Contains("VALE")) return "VALE3";
        if (nome.Contains("ITAU") || nome.Contains("ITAÚ")) return "ITUB4";
        if (nome.Contains("BRADESCO")) return "BBDC4";
        if (nome.Contains("AMBEV")) return "ABEV3";
        if (nome.Contains("MAGAZINE LUIZA") || nome.Contains("MAGALU")) return "MGLU3";
        if (nome.Contains("WEG")) return "WEGE3";
        if (nome.Contains("BANCO DO BRASIL")) return "BBAS3";

        string letras = new string(nome.Where(char.IsLetter).ToArray());
        return letras.Length >= 4 ? $"{letras.Substring(0, 4)}3" : nome;
    }
}