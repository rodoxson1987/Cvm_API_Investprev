using Microsoft.EntityFrameworkCore;
using CvmApi.Data;
using CvmApi.Services;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=cvm_data.db"));

// 2. Registro de Services HTTP
builder.Services.AddHttpClient<CvmSyncService>();

// Descomente a linha abaixo se você tiver a classe YahooFinanceService.cs criada
// builder.Services.AddHttpClient<YahooFinanceService>();

// 3. Suporte a Swagger / OpenAPI
// 3. Suporte a Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "API CVM & B3 - Cotações e Fundos",
        Version = "v1",
        Description = "API para sincronização e consulta de Companhias Abertas (B3) e Fundos de Investimento (CVM)."
    });
});
// 4. Suporte a CORS para consumo pelo Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Garantir criação do banco SQLite na inicialização
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API CVM & B3 v1");
    });
}

// ============================================================================
// 📊 GRUPO 1: AÇÕES / COMPANHIAS ABERTAS (B3)
// ============================================================================

app.MapPost("/api/cvm/acoes/sincronizar", async (CvmSyncService syncService) =>
{
    var total = await syncService.SincronizarCompanhiasAbertasAsync();
    return Results.Ok(new 
    { 
        mensagem = "Companhias abertas sincronizadas com sucesso!", 
        totalEmpresasCadastradas = total 
    });
})
.WithTags("1. Ações (B3)")
.WithSummary("Sincroniza o cadastro de companhias abertas da CVM (cad_cia_aberta.csv)");


app.MapGet("/api/cvm/acoes/listar", async (AppDbContext db, string? busca, string? situacao, int quantidade = 50) =>
{
    var query = db.CompanhiasAbertas.AsQueryable();

    // Filtro por termo (Razão Social, Nome Comercial ou CNPJ)
    if (!string.IsNullOrWhiteSpace(busca))
    {
        string buscaLimpa = busca.Trim().ToLower();
        query = query.Where(c => c.RazaoSocial.ToLower().Contains(buscaLimpa) 
                              || (c.NomeComercial != null && c.NomeComercial.ToLower().Contains(buscaLimpa))
                              || c.Cnpj.Contains(buscaLimpa));
    }

    // Filtro por Situação CVM (ex: ATIVO, CANCELADA, etc.)
    if (!string.IsNullOrWhiteSpace(situacao) && !situacao.Equals("TODAS", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(c => c.Situacao.ToUpper() == situacao.ToUpper());
    }

    var totalNoBanco = await db.CompanhiasAbertas.CountAsync();

    var empresas = await query
        .OrderBy(c => c.RazaoSocial)
        .Take(quantidade)
        .Select(c => new
        {
            c.Id,
            c.Cnpj,
            c.RazaoSocial,
            c.NomeComercial,
            c.CodigoCvm,
            c.Situacao
        })
        .ToListAsync();

    return Results.Ok(new
    {
        totalNoBanco = totalNoBanco,
        retornados = empresas.Count,
        empresas = empresas
    });
})
.WithTags("1. Ações (B3)")
.WithSummary("Lista companhias abertas com todas as informações e filtros de busca");
// Descomente este endpoint quando o YahooFinanceService estiver implementado
/*
app.MapGet("/api/cvm/acao/cotacao/{termo}", async (YahooFinanceService yahooService, string termo) =>
{
    var resultado = await yahooService.ObterCotacoesAsync(termo);
    return resultado != null ? Results.Ok(resultado) : Results.NotFound("Nenhum dado encontrado para o Ticker/CNPJ informado.");
})
.WithTags("1. Ações (B3)")
.WithSummary("Busca cotações e histórico de preços de uma ação na B3 (ex: PETR4)");
*/


// ============================================================================
// 🏦 GRUPO 2: FUNDOS DE INVESTIMENTO (CVM)
// ============================================================================

app.MapPost("/api/cvm/cadastros/sincronizar", async (CvmSyncService syncService) =>
{
    var total = await syncService.SincronizarCadastroFundosAsync();
    return Results.Ok(new 
    { 
        mensagem = "Cadastro geral de fundos sincronizado com sucesso!", 
        totalFundosCadastrados = total 
    });
})
.WithTags("2. Fundos de Investimento (CVM)")
.WithSummary("Sincroniza a base geral cadastral de fundos da CVM (cad_fi.csv)");

app.MapPost("/api/cvm/sincronizar/{dataStr}", async (CvmSyncService syncService, string dataStr) =>
{
    // OBS: Se o seu método no CvmSyncService tiver outro nome (ex: SincronizarInformeDiarioPorDataAsync), 
    // ajuste a chamada abaixo:
    var total = await syncService.SincronizarInformeDiarioAsync(dataStr);
    
    return Results.Ok(new 
    { 
        mensagem = "Informe diário de cotas importado com sucesso!", 
        dataSincronizada = dataStr, 
        registrosNovos = total 
    });
})
.WithTags("2. Fundos de Investimento (CVM)")
.WithSummary("Importa o informe diário de cotas para uma data específica (Formato: DD-MM-YYYY)");

app.MapGet("/api/cvm/fundos/listar", async (AppDbContext db, string? busca, string? situacao, int quantidade = 50) =>
{
    var query = db.FundosCadastro.AsQueryable();

    // Filtro por texto (Razão Social ou CNPJ)
    if (!string.IsNullOrWhiteSpace(busca))
    {
        string buscaLimpa = busca.Trim().ToLower();
        query = query.Where(f => f.RazaoSocial.ToLower().Contains(buscaLimpa) || f.CnpjFundo.Contains(buscaLimpa));
    }

    // Filtro por Situação CVM
    if (!string.IsNullOrWhiteSpace(situacao) && !situacao.Equals("TODAS", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(f => f.Situacao.ToUpper() == situacao.ToUpper());
    }

    var fundos = await query
        .OrderBy(f => f.RazaoSocial)
        .Take(quantidade)
        .Select(f => new
        {
            f.CnpjFundo,
            f.RazaoSocial,
            f.NomeComercial,
            f.Situacao,
            f.Administrador
        })
        .ToListAsync();

    return Results.Ok(new
    {
        totalNoBanco = await db.FundosCadastro.CountAsync(),
        retornados = fundos.Count,
        fundos = fundos
    });
})
.WithTags("2. Fundos de Investimento (CVM)")
.WithSummary("Lista fundos cadastrados no banco local com filtros por busca e situação");

app.MapGet("/api/cvm/fundo/{cnpj}", async (AppDbContext db, string cnpj) =>
{
    // Trata e limpa o CNPJ mantendo apenas os números
    string cnpjLimpo = new string(cnpj.Where(char.IsDigit).ToArray()).PadLeft(14, '0');

    var cadastros = await db.FundosCadastro
        .Where(f => f.CnpjFundo == cnpjLimpo)
        .ToListAsync();

    // OBS: Se na sua classe InformeDiario o campo de data se chamar 'Data' em vez de 'DataInforme',
    // altere abaixo 'i.DataInforme' para 'i.Data':
    var cotas = await db.InformesDiarios
        .Where(i => i.CnpjFundo == cnpjLimpo)
        .OrderByDescending(i => i.DataInforme) 
        .Select(i => new
        {
            Data = i.DataInforme.ToString("dd/MM/yyyy"),
            ValorCota = i.ValorCota,
            PatrimonioLiquido = i.PatrimonioLiquido
        })
        .ToListAsync();

    return Results.Ok(new
    {
        cnpj = cnpjLimpo,
        cadastros = cadastros,
        historicoCotas = cotas
    });
})
.WithTags("2. Fundos de Investimento (CVM)")
.WithSummary("Retorna os detalhes cadastrais e o histórico de cotas de um fundo pelo CNPJ");

app.Run();