using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using CvmApi.Data;
using CvmApi.Models;

namespace CvmApi.Services;

public class CvmSyncService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _client;

    public CvmSyncService(AppDbContext db, HttpClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<int> SincronizarDataDiariaAsync(DateTime dataAlvo)
    {
        string anoMes = dataAlvo.ToString("yyyyMM");
        string url = $"https://dados.cvm.gov.br/dados/FI/DOC/INF_DIARIO/DADOS/inf_diario_fi_{anoMes}.zip";

        byte[] zipBytes = await _client.GetByteArrayAsync(url);
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
        if (entry == null) return 0;

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, System.Text.Encoding.GetEncoding("iso-8859-1"));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var csv = new CsvReader(reader, config);
        var registros = csv.GetRecords<CvmRecordMap>().ToList();

        string dataFormatada = dataAlvo.ToString("yyyy-MM-dd");

        // 1. Limpamos e agrupamos os registros do dia
        var registrosDoDia = registros
            .Where(r => r.DT_COMPTC == dataFormatada)
            .GroupBy(r => LimparCnpj(r.CNPJ_FUNDO))
            .Select(g => g.First())
            .Select(r => new InformeDiario
            {
                TipoFundo = r.TP_FUNDO,
                CnpjFundo = LimparCnpj(r.CNPJ_FUNDO),
                DataCompetencia = DateTime.SpecifyKind(DateTime.Parse(r.DT_COMPTC), DateTimeKind.Utc),
                ValorTotal = r.VL_TOTAL,
                ValorCota = r.VL_QUOTA,
                PatrimonioLiquido = r.VL_PATRIM_LIQ
            })
            .ToList();

        if (!registrosDoDia.Any()) return 0;

        // 2. Verificamos o que já existe no banco SQLite
        var dataUtc = DateTime.SpecifyKind(dataAlvo.Date, DateTimeKind.Utc);
        var jaExistemNoBanco = await _db.InformesDiarios
            .Where(i => i.DataCompetencia == dataUtc)
            .Select(i => i.CnpjFundo)
            .ToListAsync();

        // 3. Filtramos apenas os novos
        var novos = registrosDoDia
            .Where(r => !jaExistemNoBanco.Contains(r.CnpjFundo))
            .ToList();

        if (novos.Any())
        {
            await _db.InformesDiarios.AddRangeAsync(novos);
            await _db.SaveChangesAsync();
        }

        return novos.Count;
    }

    // Função auxiliar limpa para remover caracteres do CNPJ sem dar erro de compilador
    private static string LimparCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj)) return string.Empty;
        return cnpj.Replace(".", "").Replace("/", "").Replace("-", "").Trim();
    }
}

// Mapeamento das colunas com suporte a nomes antigos e novos
public class CvmRecordMap
{
    [Name("TP_FUNDO_CLASSE", "TP_FUNDO")]
    public string TP_FUNDO { get; set; } = string.Empty;

    [Name("CNPJ_FUNDO_CLASSE", "CNPJ_FUNDO")]
    public string CNPJ_FUNDO { get; set; } = string.Empty;

    public string DT_COMPTC { get; set; } = string.Empty;
    public decimal VL_TOTAL { get; set; }
    public decimal VL_QUOTA { get; set; }
    public decimal VL_PATRIM_LIQ { get; set; }
}