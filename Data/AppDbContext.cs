using Microsoft.EntityFrameworkCore;
using CvmApi.Models;

namespace CvmApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<InformeDiario> InformesDiarios { get; set; }
    public DbSet<FundoCadastro> FundosCadastro { get; set; }

    public DbSet<CompanhiaAberta> CompanhiasAbertas { get; set; }

    public DbSet<DemonstracaoFinanceira> DemonstracoesFinanceiras { get; set; }
}