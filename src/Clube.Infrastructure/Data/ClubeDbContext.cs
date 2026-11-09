using System;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Domain.Entities;
using Clube.Domain.Enums;

namespace Clube.Infrastructure.Data;

public class ClubeDbContext : DbContext, IClubeDbContext
{
    private readonly ITenantContext _tenantContext;

    public ClubeDbContext(DbContextOptions<ClubeDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public DbSet<Domain.Entities.Clube> Clubes => Set<Domain.Entities.Clube>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Associado> Associados => Set<Associado>();
    public DbSet<Dependente> Dependentes => Set<Dependente>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<InscricaoEvento> InscricoesEventos => Set<InscricaoEvento>();
    public DbSet<Espaco> Espacos => Set<Espaco>();
    public DbSet<ReservaEspaco> ReservasEspacos => Set<ReservaEspaco>();
    public DbSet<Mensalidade> Mensalidades => Set<Mensalidade>();
    public DbSet<SolicitacaoReclamacao> SolicitacoesReclamacoes => Set<SolicitacaoReclamacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Multi-Tenant Global Query Filters
        modelBuilder.Entity<Associado>()
            .HasQueryFilter(a => _tenantContext.IsSuperAdmin || a.ClubeId == _tenantContext.CurrentClubeId);

        modelBuilder.Entity<Evento>()
            .HasQueryFilter(e => _tenantContext.IsSuperAdmin || e.ClubeId == _tenantContext.CurrentClubeId);

        modelBuilder.Entity<Espaco>()
            .HasQueryFilter(esp => _tenantContext.IsSuperAdmin || esp.ClubeId == _tenantContext.CurrentClubeId);

        modelBuilder.Entity<ReservaEspaco>()
            .HasQueryFilter(r => _tenantContext.IsSuperAdmin || r.ClubeId == _tenantContext.CurrentClubeId);

        modelBuilder.Entity<Mensalidade>()
            .HasQueryFilter(m => _tenantContext.IsSuperAdmin || m.ClubeId == _tenantContext.CurrentClubeId);

        modelBuilder.Entity<SolicitacaoReclamacao>()
            .HasQueryFilter(s => _tenantContext.IsSuperAdmin || s.ClubeId == _tenantContext.CurrentClubeId);

        modelBuilder.Entity<Usuario>()
            .HasQueryFilter(u => _tenantContext.IsSuperAdmin || u.ClubeId == _tenantContext.CurrentClubeId || u.Perfil == PerfilUsuario.SuperAdmin);

        // Mapeamentos de Chaves e Relacionamentos
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.CPF)
            .IsUnique();

        modelBuilder.Entity<Associado>()
            .HasOne(a => a.Usuario)
            .WithOne(u => u.Associado)
            .HasForeignKey<Associado>(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dependente>()
            .HasOne(d => d.Usuario)
            .WithOne(u => u.Dependente)
            .HasForeignKey<Dependente>(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dependente>()
            .HasOne(d => d.AssociadoTitular)
            .WithMany(a => a.Dependentes)
            .HasForeignKey(d => d.AssociadoTitularId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReservaEspaco>()
            .HasOne(r => r.Espaco)
            .WithMany(e => e.Reservas)
            .HasForeignKey(r => r.EspacoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReservaEspaco>()
            .HasOne(r => r.Associado)
            .WithMany(a => a.Reservas)
            .HasForeignKey(r => r.AssociadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mensalidade>()
            .HasOne(m => m.Associado)
            .WithMany(a => a.Mensalidades)
            .HasForeignKey(m => m.AssociadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InscricaoEvento>()
            .HasOne(i => i.Evento)
            .WithMany(e => e.Inscricoes)
            .HasForeignKey(i => i.EventoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
