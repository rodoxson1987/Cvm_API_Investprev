using Microsoft.EntityFrameworkCore;
using CvmApi.Data;
using CvmApi.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuração dos Serviços (EF Core + SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=cvm_database.db"));

// Injeção do Serviço CvmSyncService com HttpClient
builder.Services.AddHttpClient<CvmSyncService>();

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "📊 CVM - API de Fundos de Investimento", 
        Version = "v1",
        Description = "API para sincronização diária e consulta de cotas e patrimônio de Fundos de Investimento da CVM."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Executa as migrations automaticamente ao iniciar a aplicação
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CVM Fundos API v1");
        c.DocumentTitle = "CVM Fundos - Swagger UI";
    });
}

#region --- ENDPOINTS DA CVM ---

/// <summary>
/// Consulta cotações históricas de uma ação listada na B3 pelo Ticker (ex: PETR4, VALE3, ITUB4).
/// </summary>
/// <summary>
/// Consulta cotações históricas de uma ação listada na B3 por Ticker (ex: PETR4) ou por CNPJ da Empresa.
/// </summary>
app.MapGet("/api/cvm/acao/cotacao/{termo}", async (string termo, CvmSyncService syncService) =>
{
    var (ticker, cotacoes) = await syncService.ObterCotacoesAcaoAsync(termo);

    if (!cotacoes.Any())
    {
        return Results.NotFound(new { mensagem = $"Não foram encontradas cotações para o termo/CNPJ: {termo}" });
    }

    return Results.Ok(new
    {
        termoPesquisado = termo,
        tickerIdentificado = ticker,
        totalRegistros = cotacoes.Count,
        historicoPrecos = cotacoes
    });
})
.WithTags("2. Consultas")
.WithSummary("Busca cotações diárias por Ticker (ex: PETR4) ou por CNPJ")
.WithDescription("Aceita tanto o código de negociação (PETR4, VALE3) quanto o CNPJ da Companhia Aberta.");

/// <summary>
/// Sincroniza a base de Companhias Abertas (Ações/Empresas) registradas na CVM.
/// </summary>
app.MapPost("/api/cvm/acoes/sincronizar", async (CvmSyncService syncService) =>
{
    var processados = await syncService.SincronizarCompanhiasAbertasAsync();
    
    return Results.Ok(new { 
        mensagem = "Cadastro de Companhias Abertas (Ações) atualizado com sucesso!", 
        totalEmpresasCadastradas = processados 
    });
})
.WithTags("1. Sincronização")
.WithSummary("Baixa o cadastro de Companhias Abertas (cad_cia_aberta.csv)");


/// <summary>
/// Consulta os dados cadastrais de uma empresa/companhia aberta pelo CNPJ ou Código CVM.
/// </summary>
app.MapGet("/api/cvm/empresa/{busca}", async (string busca, AppDbContext db) =>
{
    string termo = System.Net.WebUtility.UrlDecode(busca).Trim();
    string termoDigitos = new string(termo.Where(char.IsDigit).ToArray());

    var empresas = await db.CompanhiasAbertas
        .Where(e => (termoDigitos.Length > 0 && e.Cnpj == termoDigitos.PadLeft(14, '0')) 
                 || e.CodigoCvm == termo 
                 || EF.Functions.Like(e.RazaoSocial, $"%{termo}%"))
        .ToListAsync();

    if (!empresas.Any())
    {
        return Results.NotFound(new { mensagem = $"Nenhuma empresa encontrada para o termo: {termo}" });
    }

    return Results.Ok(new
    {
        totalEncontradas = empresas.Count,
        empresas = empresas
    });
})
.WithTags("2. Consultas")
.WithSummary("Busca empresa por CNPJ, Código CVM ou Razão Social");

/// <summary>
/// Sincroniza a base de dados cadastrais de todos os fundos da CVM (Razão Social, Nome, Status e Administrador).
/// </summary>
app.MapPost("/api/cvm/cadastros/sincronizar", async (CvmSyncService syncService) =>
{
    var processados = await syncService.SincronizarCadastroFundosAsync();
    
    return Results.Ok(new { 
        mensagem = "Cadastro de fundos atualizado com sucesso!", 
        totalFundosCadastrados = processados 
    });
})
.WithTags("1. Sincronização")
.WithSummary("Baixa o cadastro geral de fundos (cad_fi.csv)")
.WithDescription("Obtém a Razão Social, Nome Fantasia, Status e Administrador de todos os fundos registrados na CVM.");


/// <summary>
/// Sincroniza os informes diários da CVM para uma data específica.
/// </summary>
/// <param name="dataStr">Data no formato DD-MM-AAAA, DD/MM/AAAA ou AAAA-MM-DD</param>
app.MapPost("/api/cvm/sincronizar/{dataStr}", async (string dataStr, CvmSyncService syncService) =>
{
    string[] formatosPermitidos = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
    
    if (!DateTime.TryParseExact(dataStr, formatosPermitidos, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dataConvertida))
    {
        return Results.BadRequest(new { mensagem = "Formato de data inválido. Use DD-MM-AAAA ou AAAA-MM-DD." });
    }

    var processados = await syncService.SincronizarDataDiariaAsync(dataConvertida);
    
    return Results.Ok(new { 
        mensagem = "Processado com sucesso!", 
        dataSincronizada = dataConvertida.ToString("dd/MM/yyyy"), 
        registrosNovos = processados 
    });
})
.WithTags("1. Sincronização")
.WithSummary("Baixa e processa os dados da CVM em lote")
.WithDescription("Faz o download do arquivo ZIP referente ao mês/ano informado, extrai o CSV e grava no SQLite os registros do dia digitado.");


/// <summary>
/// Lista os registros importados mais recentes no banco de dados.
/// </summary>
/// <param name="quantidade">Quantidade de registros a retornar (padrão: 50)</param>
app.MapGet("/api/cvm/informes/recentes", async (AppDbContext db, int quantidade = 50) =>
{
    var registros = await db.InformesDiarios
        .OrderByDescending(f => f.DataCompetencia)
        .Take(quantidade)
        .Select(f => new
        {
            f.CnpjFundo,
            f.TipoFundo,
            data = f.DataCompetencia.ToString("dd/MM/yyyy"),
            f.ValorCota,
            f.PatrimonioLiquido,
            f.ValorTotal
        })
        .ToListAsync();

    return Results.Ok(new
    {
        totalNoBanco = await db.InformesDiarios.CountAsync(),
        retornados = registros.Count,
        dados = registros
    });
})
.WithTags("2. Consultas")
.WithSummary("Lista os últimos registros importados")
.WithDescription("Retorna a contagem total de linhas no SQLite e os N registros mais recentes.");


/// <summary>
/// Consulta todas as ocorrências cadastrais e o histórico de cotas de um fundo específico.
/// </summary>
/// <param name="cnpj">CNPJ do fundo (com ou sem pontuação)</param>
app.MapGet("/api/cvm/fundo/{*cnpj}", async (string cnpj, AppDbContext db) =>
{
    string cnpjDecodificado = System.Net.WebUtility.UrlDecode(cnpj);
    string cnpjLimpo = new string(cnpjDecodificado.Where(char.IsDigit).ToArray()).PadLeft(14, '0');

    // Traz TODAS as ocorrências cadastrais para o CNPJ pesquisado
    var cadastros = await db.FundosCadastro
        .Where(f => f.CnpjFundo == cnpjLimpo)
        .Select(f => new
        {
            f.Id,
            f.RazaoSocial,
            f.NomeComercial,
            f.Situacao,
            f.DataInicioAtividade,
            f.Administrador,
            f.CnpjAdministrador
        })
        .ToListAsync();

    // Traz o histórico de cotas
    var historico = await db.InformesDiarios
        .Where(f => f.CnpjFundo == cnpjLimpo)
        .OrderByDescending(f => f.DataCompetencia)
        .Select(f => new
        {
            data = f.DataCompetencia.ToString("dd/MM/yyyy"),
            valorCota = f.ValorCota,
            patrimonioLiquido = f.PatrimonioLiquido,
            valorTotal = f.ValorTotal
        })
        .ToListAsync();

    if (!cadastros.Any() && !historico.Any())
    {
        return Results.NotFound(new { mensagem = $"Nenhum dado cadastral ou cota encontrada para o CNPJ: {cnpjLimpo}" });
    }

    return Results.Ok(new
    {
        cnpj = cnpjLimpo,
        totalOcorrenciasCadastrais = cadastros.Count,
        cadastros = cadastros,
        totalCotasRegistradas = historico.Count,
        historicoCotas = historico
    });
})
.WithTags("2. Consultas")
.WithSummary("Busca detalhes, Razão Social e histórico pelo CNPJ")
.WithDescription("Retorna a lista cadastral completa combinada com a série histórica de cotas.");

#endregion

app.Run();