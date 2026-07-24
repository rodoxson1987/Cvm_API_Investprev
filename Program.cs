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
app.MapPost("/api/cvm/sincronizar/{data}", async (string data, CvmSyncService syncService) =>
{
    if (!DateTime.TryParse(data, out var dataAlvo))
        return Results.BadRequest("Data inválida. Use o formato AAAA-MM-DD.");

    int salvos = await syncService.SincronizarDataDiariaAsync(dataAlvo);
    return Results.Ok(new { mensagem = "Processado com sucesso!", registrosNovos = salvos });
});

app.Run();