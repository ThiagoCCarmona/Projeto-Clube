using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SQLite;
using Clube.Mobile.Models;

namespace Clube.Mobile.Services;

public interface ILocalDatabaseService
{
    Task InitializeAsync();

    // Sessão
    Task<LocalUserSession?> GetSessionAsync();
    Task SaveSessionAsync(LocalUserSession session);
    Task ClearSessionAsync();

    // Clubes
    Task<List<LocalClube>> GetClubesAsync();
    Task SaveClubesAsync(IEnumerable<LocalClube> clubes);

    // Associados & Dependentes
    Task<LocalAssociado?> GetAssociadoAsync(Guid associadoId);
    Task SaveAssociadoAsync(LocalAssociado associado);
    Task<List<LocalDependente>> GetDependentesAsync(Guid associadoId);
    Task SaveDependentesAsync(IEnumerable<LocalDependente> dependentes);

    // Espaços & Reservas
    Task<List<LocalEspaco>> GetEspacosAsync(Guid? clubeId = null);
    Task SaveEspacosAsync(IEnumerable<LocalEspaco> espacos);
    Task<List<LocalReserva>> GetReservasAsync(Guid? clubeId = null);
    Task SaveReservaAsync(LocalReserva reserva);
    Task SaveReservasAsync(IEnumerable<LocalReserva> reservas);

    // Eventos
    Task<List<LocalEvento>> GetEventosAsync(Guid? clubeId = null);
    Task SaveEventosAsync(IEnumerable<LocalEvento> eventos);
    Task UpdateInscricaoEventoAsync(Guid eventoId, bool inscrito);

    // Mensalidades
    Task<List<LocalMensalidade>> GetMensalidadesAsync(Guid? associadoId = null);
    Task SaveMensalidadesAsync(IEnumerable<LocalMensalidade> mensalidades);

    // Solicitações / Reclamações (SAC)
    Task<List<LocalSolicitacao>> GetSolicitacoesAsync(Guid? associadoId = null);
    Task SaveSolicitacaoAsync(LocalSolicitacao solicitacao);
    Task SaveSolicitacoesAsync(IEnumerable<LocalSolicitacao> solicitacoes);

    // Outbox (Offline-First)
    Task<Guid> EnqueueOutboxAsync(string action, string entityType, object payload);
    Task<List<LocalOutboxItem>> GetPendingOutboxAsync();
    Task MarkOutboxSyncedAsync(Guid id, string? errorMessage = null);
    Task ClearOutboxAsync();
}

public class LocalDatabaseService : ILocalDatabaseService
{
    private SQLiteAsyncConnection? _db;
    private readonly string _databasePath;
    private bool _initialized;

    public LocalDatabaseService()
    {
        var appData = FileSystem.AppDataDirectory;
        _databasePath = Path.Combine(appData, "clube_offline_first.db");
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        _db = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<LocalClube>();
        await _db.CreateTableAsync<LocalUserSession>();
        await _db.CreateTableAsync<LocalAssociado>();
        await _db.CreateTableAsync<LocalDependente>();
        await _db.CreateTableAsync<LocalEspaco>();
        await _db.CreateTableAsync<LocalReserva>();
        await _db.CreateTableAsync<LocalEvento>();
        await _db.CreateTableAsync<LocalMensalidade>();
        await _db.CreateTableAsync<LocalSolicitacao>();
        await _db.CreateTableAsync<LocalOutboxItem>();

        _initialized = true;
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
            await InitializeAsync();
    }

    // --- Sessão ---
    public async Task<LocalUserSession?> GetSessionAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalUserSession>().FirstOrDefaultAsync(s => s.Key == "CurrentSession");
    }

    public async Task SaveSessionAsync(LocalUserSession session)
    {
        await EnsureInitializedAsync();
        session.Key = "CurrentSession";
        await _db!.InsertOrReplaceAsync(session);
    }

    public async Task ClearSessionAsync()
    {
        await EnsureInitializedAsync();
        await _db!.DeleteAllAsync<LocalUserSession>();
    }

    // --- Clubes ---
    public async Task<List<LocalClube>> GetClubesAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalClube>().OrderBy(c => c.Nome).ToListAsync();
    }

    public async Task SaveClubesAsync(IEnumerable<LocalClube> clubes)
    {
        await EnsureInitializedAsync();
        foreach (var clube in clubes)
            await _db!.InsertOrReplaceAsync(clube);
    }

    // --- Associados & Dependentes ---
    public async Task<LocalAssociado?> GetAssociadoAsync(Guid associadoId)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalAssociado>().FirstOrDefaultAsync(a => a.Id == associadoId);
    }

    public async Task SaveAssociadoAsync(LocalAssociado associado)
    {
        await EnsureInitializedAsync();
        await _db!.InsertOrReplaceAsync(associado);
    }

    public async Task<List<LocalDependente>> GetDependentesAsync(Guid associadoId)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalDependente>().Where(d => d.AssociadoId == associadoId).ToListAsync();
    }

    public async Task SaveDependentesAsync(IEnumerable<LocalDependente> dependentes)
    {
        await EnsureInitializedAsync();
        foreach (var dep in dependentes)
            await _db!.InsertOrReplaceAsync(dep);
    }

    // --- Espaços & Reservas ---
    public async Task<List<LocalEspaco>> GetEspacosAsync(Guid? clubeId = null)
    {
        await EnsureInitializedAsync();
        var query = _db!.Table<LocalEspaco>().Where(e => e.Ativo);
        if (clubeId.HasValue)
            query = query.Where(e => e.ClubeId == clubeId.Value);

        return await query.OrderBy(e => e.Nome).ToListAsync();
    }

    public async Task SaveEspacosAsync(IEnumerable<LocalEspaco> espacos)
    {
        await EnsureInitializedAsync();
        foreach (var espaco in espacos)
            await _db!.InsertOrReplaceAsync(espaco);
    }

    public async Task<List<LocalReserva>> GetReservasAsync(Guid? clubeId = null)
    {
        await EnsureInitializedAsync();
        var query = _db!.Table<LocalReserva>();
        if (clubeId.HasValue)
            query = query.Where(r => r.ClubeId == clubeId.Value);

        return await query.OrderByDescending(r => r.DataHoraInicio).ToListAsync();
    }

    public async Task SaveReservaAsync(LocalReserva reserva)
    {
        await EnsureInitializedAsync();
        await _db!.InsertOrReplaceAsync(reserva);
    }

    public async Task SaveReservasAsync(IEnumerable<LocalReserva> reservas)
    {
        await EnsureInitializedAsync();
        foreach (var reserva in reservas)
            await _db!.InsertOrReplaceAsync(reserva);
    }

    // --- Eventos ---
    public async Task<List<LocalEvento>> GetEventosAsync(Guid? clubeId = null)
    {
        await EnsureInitializedAsync();
        var query = _db!.Table<LocalEvento>();
        if (clubeId.HasValue)
            query = query.Where(e => e.ClubeId == clubeId.Value);

        return await query.OrderBy(e => e.DataHoraInicio).ToListAsync();
    }

    public async Task SaveEventosAsync(IEnumerable<LocalEvento> eventos)
    {
        await EnsureInitializedAsync();
        foreach (var evento in eventos)
            await _db!.InsertOrReplaceAsync(evento);
    }

    public async Task UpdateInscricaoEventoAsync(Guid eventoId, bool inscrito)
    {
        await EnsureInitializedAsync();
        var evento = await _db!.Table<LocalEvento>().FirstOrDefaultAsync(e => e.Id == eventoId);
        if (evento != null)
        {
            evento.Inscrito = inscrito;
            await _db.UpdateAsync(evento);
        }
    }

    // --- Mensalidades ---
    public async Task<List<LocalMensalidade>> GetMensalidadesAsync(Guid? associadoId = null)
    {
        await EnsureInitializedAsync();
        var query = _db!.Table<LocalMensalidade>();
        if (associadoId.HasValue)
            query = query.Where(m => m.AssociadoId == associadoId.Value);

        return await query.OrderByDescending(m => m.DataVencimento).ToListAsync();
    }

    public async Task SaveMensalidadesAsync(IEnumerable<LocalMensalidade> mensalidades)
    {
        await EnsureInitializedAsync();
        foreach (var m in mensalidades)
            await _db!.InsertOrReplaceAsync(m);
    }

    // --- Solicitações / Reclamações ---
    public async Task<List<LocalSolicitacao>> GetSolicitacoesAsync(Guid? associadoId = null)
    {
        await EnsureInitializedAsync();
        var query = _db!.Table<LocalSolicitacao>();
        if (associadoId.HasValue)
            query = query.Where(s => s.AssociadoId == associadoId.Value);

        return await query.OrderByDescending(s => s.DataAbertura).ToListAsync();
    }

    public async Task SaveSolicitacaoAsync(LocalSolicitacao solicitacao)
    {
        await EnsureInitializedAsync();
        await _db!.InsertOrReplaceAsync(solicitacao);
    }

    public async Task SaveSolicitacoesAsync(IEnumerable<LocalSolicitacao> solicitacoes)
    {
        await EnsureInitializedAsync();
        foreach (var s in solicitacoes)
            await _db!.InsertOrReplaceAsync(s);
    }

    // --- Outbox (Offline-First) ---
    public async Task<Guid> EnqueueOutboxAsync(string action, string entityType, object payload)
    {
        await EnsureInitializedAsync();
        var item = new LocalOutboxItem
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
            Synced = false
        };

        await _db!.InsertAsync(item);
        return item.Id;
    }

    public async Task<List<LocalOutboxItem>> GetPendingOutboxAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalOutboxItem>()
            .Where(o => !o.Synced)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkOutboxSyncedAsync(Guid id, string? errorMessage = null)
    {
        await EnsureInitializedAsync();
        var item = await _db!.Table<LocalOutboxItem>().FirstOrDefaultAsync(o => o.Id == id);
        if (item != null)
        {
            item.Synced = string.IsNullOrEmpty(errorMessage);
            item.ErrorMessage = errorMessage;
            await _db.UpdateAsync(item);
        }
    }

    public async Task ClearOutboxAsync()
    {
        await EnsureInitializedAsync();
        await _db!.Table<LocalOutboxItem>().Where(o => o.Synced).DeleteAsync();
    }
}
