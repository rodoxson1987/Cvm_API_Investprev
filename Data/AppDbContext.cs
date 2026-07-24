using Microsoft.EntityFrameworkCore;
using CvmApi.Models;

namespace CvmApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<InformeDiario> InformesDiarios { get; set; }
}