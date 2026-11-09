using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clube.Domain.Entities;

namespace Clube.Application.Interfaces;

public interface IClubeDbContext
{
    DbSet<Domain.Entities.Clube> Clubes { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Associado> Associados { get; }
    DbSet<Dependente> Dependentes { get; }
    DbSet<Evento> Eventos { get; }
    DbSet<InscricaoEvento> InscricoesEventos { get; }
    DbSet<Espaco> Espacos { get; }
    DbSet<ReservaEspaco> ReservasEspacos { get; }
    DbSet<Mensalidade> Mensalidades { get; }
    DbSet<SolicitacaoReclamacao> SolicitacoesReclamacoes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
