using Microsoft.EntityFrameworkCore;
using CvmApi.Data;
using CvmApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuração dos Serviços
// Substitua o UseNpgsql por UseSqlite:
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<CvmSyncService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // <-- Esta linha cria o banco .db e aplica as tabelas automaticamente se ele não existir!
}

// Endpoint para acionar a importação diária
app.MapPost("/api/cvm/sincronizar/{dataStr}", async (string dataStr, CvmSyncService syncService) =>
{
    // Tenta converter o texto recebido (ex: "12-05-2026" ou "12/05/2026") para DateTime
    string[] formatosPermitidos = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
    
    if (!DateTime.TryParseExact(dataStr, formatosPermitidos, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dataConvertida))
    {
        return Results.BadRequest(new { mensagem = "Formato de data inválido. Use DD-MM-AAAA ou AAAA-MM-DD." });
    }

    var processados = await syncService.SincronizarDataDiariaAsync(dataConvertida);
    
    return Results.Ok(new { 
        mensagem = "Processado com sucesso!", 
        dataSincronizada = dataConvertida.ToString("dd/MM/yyyy"), // Retorna formatado no padrão brasileiro!
        registrosNovos = processados 
    });
});

app.Run();