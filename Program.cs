using Microsoft.EntityFrameworkCore;
using CvmApi.Data;
using CvmApi.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuração dos Serviços (EF Core + SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<CvmSyncService>();
builder.Services.AddScoped<CvmSyncService>();

builder.Services.AddEndpointsApiExplorer();

// Configuração visual do Swagger ajustada (sem dependência explícita de OpenApi.Models)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "📊 CVM - API de Fundos de Investimento", 
        Version = "v1",
        Description = "API para sincronização diária e consulta de cotas e patrimônio de Fundos de Investimento da CVM."
    });

    // Inclui os comentários XML na documentação do Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Executa as migrations automaticamente ao iniciar
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

app.UseHttpsRedirection();

#region --- ENDPOINTS DA CVM ---

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


/// Sincroniza os informes diários da CVM para uma data específica.

/// <param name="dataStr">Data no formato DD-MM-AAAA ou AAAA-MM-DD</param>
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



/// Consulta o histórico de cotas de um fundo específico pelo CNPJ.

/// <param name="cnpj">CNPJ do fundo (com ou sem pontuação)</param>
app.MapGet("/api/cvm/fundo/{*cnpj}", async (string cnpj, AppDbContext db) =>
{
    // Decodifica %2F se houver e remove todos os caracteres não numéricos
    string cnpjDecodificado = System.Net.WebUtility.UrlDecode(cnpj);
    string cnpjLimpo = new string(cnpjDecodificado.Where(char.IsDigit).ToArray());

    var historico = await db.InformesDiarios
        .Where(f => f.CnpjFundo == cnpjLimpo)
        .OrderByDescending(f => f.DataCompetencia)
        .Select(f => new
        {
            cnpj = f.CnpjFundo,
            tipoFundo = f.TipoFundo,
            data = f.DataCompetencia.ToString("dd/MM/yyyy"),
            valorCota = f.ValorCota,
            patrimonioLiquido = f.PatrimonioLiquido,
            valorTotal = f.ValorTotal
        })
        .ToListAsync();

    if (!historico.Any())
    {
        return Results.NotFound(new { mensagem = $"Nenhum dado encontrado para o CNPJ: {cnpjLimpo}" });
    }

    return Results.Ok(historico);
})
.WithTags("2. Consultas")
.WithSummary("Busca o histórico de cota/patrimônio de um fundo pelo CNPJ")
.WithDescription("Retorna uma lista ordenada com todas as datas salvas para o fundo solicitado.");

#endregion

app.Run();