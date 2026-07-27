using Microsoft.EntityFrameworkCore;
using CvmApi.Models;

namespace CvmApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FundoCadastro> FundosCadastro { get; set; }
        public DbSet<InformeDiario> InformesDiarios { get; set; }
        public DbSet<CompanhiaAberta> CompanhiasAbertas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FundoCadastro>().ToTable("fundos_cadastro");
            modelBuilder.Entity<InformeDiario>().ToTable("informes_diarios");
            modelBuilder.Entity<CompanhiaAberta>().ToTable("companhias_abertas");

            // Índices para deixar as buscas por CNPJ mais rápidas
            modelBuilder.Entity<FundoCadastro>().HasIndex(f => f.CnpjFundo);
            modelBuilder.Entity<InformeDiario>().HasIndex(i => i.CnpjFundo);
            modelBuilder.Entity<CompanhiaAberta>().HasIndex(c => c.Cnpj);
        }
    }
}