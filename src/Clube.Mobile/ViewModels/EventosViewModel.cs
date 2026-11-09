using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Clube.Domain.Enums;
using Clube.Mobile.Models;
using Clube.Mobile.Services;
using Clube.Shared.DTOs;

namespace Clube.Mobile.ViewModels;

public class EventosViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    public ObservableCollection<LocalEvento> Eventos { get; } = new();

    private LocalUserSession? _sessao;
    public LocalUserSession? Sessao
    {
        get => _sessao;
        set => SetProperty(ref _sessao, value);
    }

    private string _filtro = "Todos";
    public string Filtro
    {
        get => _filtro;
        set
        {
            if (SetProperty(ref _filtro, value))
                _ = CarregarEventosAsync();
        }
    }

    public string StatusConexaoTexto => IsOffline ? "MODO OFFLINE (Dados do SQLite local)" : "ONLINE (Sincronizado)";
    public string StatusConexaoCor => IsOffline ? "#991b1b" : "#1e293b";

    public ICommand CarregarEventosCommand { get; }
    public ICommand SincronizarCommand { get; }
    public ICommand InscreverEventoCommand { get; }

    public EventosViewModel(ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _localDb = localDb;
        _syncService = syncService;

        Title = "Agenda de Eventos";
        CarregarEventosCommand = new AsyncRelayCommand(CarregarEventosAsync);
        SincronizarCommand = new AsyncRelayCommand(SincronizarAsync);
        InscreverEventoCommand = new AsyncRelayCommand<Guid>(InscreverEventoAsync);

        _syncService.ConnectivityChanged += (s, online) =>
        {
            IsOffline = !online;
            OnPropertyChanged(nameof(StatusConexaoTexto));
            OnPropertyChanged(nameof(StatusConexaoCor));
        };
    }

    public async Task InicializarAsync()
    {
        IsOffline = !_syncService.IsOnline;
        Sessao = await _localDb.GetSessionAsync();
        await CarregarEventosAsync();
    }

    private async Task CarregarEventosAsync()
    {
        IsBusy = true;
        try
        {
            var clubeId = Sessao?.ClubeId;
            var lista = await _localDb.GetEventosAsync(clubeId);

            if (Filtro == "Socios")
                lista = lista.Where(e => e.PublicoAlvo == PublicoAlvo.SomenteAssociados).ToList();
            else if (Filtro == "Aberto")
                lista = lista.Where(e => e.PublicoAlvo == PublicoAlvo.AbertoAoPublico).ToList();

            Eventos.Clear();
            foreach (var ev in lista)
                Eventos.Add(ev);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SincronizarAsync()
    {
        IsBusy = true;
        StatusMessage = "Sincronizando com o servidor...";
        try
        {
            var res = await _syncService.SincronizarAsync();
            StatusMessage = res.Mensagem;
            await CarregarEventosAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task InscreverEventoAsync(Guid eventoId)
    {
        var ev = Eventos.FirstOrDefault(e => e.Id == eventoId);
        if (ev == null) return;

        // Regra de público destino do vídeo
        if (ev.PublicoAlvo == PublicoAlvo.SomenteAssociados && Sessao?.TipoAssociado == TipoAssociado.Simpatizante)
        {
            await App.Current.MainPage.DisplayAlert("Evento Exclusivo", 
                "Este evento é restrito a Sócios Fundadores e Convidados do Clube. Como Simpatizante, você pode participar dos eventos abertos ao público.", "Compreendi");
            return;
        }

        if (ev.Inscrito)
        {
            await App.Current.MainPage.DisplayAlert("Inscrição", "Você já está confirmado neste evento!", "OK");
            return;
        }

        // Atualiza estado local imediatamente (Optimistic UI)
        await _localDb.UpdateInscricaoEventoAsync(eventoId, true);
        ev.Inscrito = true;

        var request = new InscricaoEventoRequest
        {
            QuantidadeAcompanhantes = 0
        };

        // Enfileira no Outbox
        await _localDb.EnqueueOutboxAsync("EVENTO_RSVP", "InscricaoEvento", request);

        // Se online, sincroniza imediatamente
        if (_syncService.IsOnline)
        {
            _ = Task.Run(async () => await _syncService.SincronizarAsync());
        }

        await App.Current.MainPage.DisplayAlert("Presença Confirmada!", 
            $"Sua inscrição para o evento '{ev.Titulo}' foi salva localmente e confirmada.", "OK");
    }
}
