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

public class ReservasViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    public ObservableCollection<LocalReserva> MinhasReservas { get; } = new();
    public ObservableCollection<LocalEspaco> EspacosDisponiveis { get; } = new();

    private LocalUserSession? _sessao;
    public LocalUserSession? Sessao
    {
        get => _sessao;
        set => SetProperty(ref _sessao, value);
    }

    private LocalEspaco? _espacoSelecionado;
    public LocalEspaco? EspacoSelecionado
    {
        get => _espacoSelecionado;
        set => SetProperty(ref _espacoSelecionado, value);
    }

    // Intervalo contínuo solicitado no vídeo (ex: 31/10 18:00 a 01/11 02:00 para feijoada)
    private DateTime _dataInicio = DateTime.Today.AddDays(2);
    public DateTime DataInicio { get => _dataInicio; set => SetProperty(ref _dataInicio, value); }

    private TimeSpan _horaInicio = new(18, 0, 0);
    public TimeSpan HoraInicio { get => _horaInicio; set => SetProperty(ref _horaInicio, value); }

    private DateTime _dataFim = DateTime.Today.AddDays(3);
    public DateTime DataFim { get => _dataFim; set => SetProperty(ref _dataFim, value); }

    private TimeSpan _horaFim = new(2, 0, 0);
    public TimeSpan HoraFim { get => _horaFim; set => SetProperty(ref _horaFim, value); }

    private FinalidadeUso _finalidade = FinalidadeUso.Feijoada;
    public FinalidadeUso Finalidade { get => _finalidade; set => SetProperty(ref _finalidade, value); }

    private string _observacoes = "Feijoada Beneficente comunitária - Salão Social";
    public string Observacoes { get => _observacoes; set => SetProperty(ref _observacoes, value); }

    public ICommand CarregarDadosCommand { get; }
    public ICommand SolicitarReservaCommand { get; }
    public ICommand SincronizarCommand { get; }

    public ReservasViewModel(ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _localDb = localDb;
        _syncService = syncService;

        Title = "Reserva de Espaços";
        CarregarDadosCommand = new AsyncRelayCommand(CarregarDadosAsync);
        SolicitarReservaCommand = new AsyncRelayCommand(SolicitarReservaAsync);
        SincronizarCommand = new AsyncRelayCommand(async () =>
        {
            var r = await _syncService.SincronizarAsync();
            StatusMessage = r.Mensagem;
            await CarregarDadosAsync();
        });
    }

    public async Task InicializarAsync()
    {
        Sessao = await _localDb.GetSessionAsync();
        IsOffline = !_syncService.IsOnline;
        await CarregarDadosAsync();
    }

    private async Task CarregarDadosAsync()
    {
        IsBusy = true;
        try
        {
            var clubeId = Sessao?.ClubeId;
            var espacos = await _localDb.GetEspacosAsync(clubeId);
            EspacosDisponiveis.Clear();
            foreach (var e in espacos)
                EspacosDisponiveis.Add(e);

            if (EspacosDisponiveis.Any() && EspacoSelecionado == null)
                EspacoSelecionado = EspacosDisponiveis.First();

            var reservas = await _localDb.GetReservasAsync(clubeId);
            MinhasReservas.Clear();
            foreach (var r in reservas)
                MinhasReservas.Add(r);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SolicitarReservaAsync()
    {
        if (EspacoSelecionado == null)
        {
            StatusMessage = "Selecione o espaço que deseja reservar.";
            return;
        }

        var inicioCompleto = DataInicio.Date + HoraInicio;
        var fimCompleto = DataFim.Date + HoraFim;

        if (fimCompleto <= inicioCompleto)
        {
            await App.Current.MainPage.DisplayAlert("Horário Inválido", 
                "A data/hora de término deve ser posterior à data/hora de início.", "OK");
            return;
        }

        // Validação local de sobreposição prévia
        var todasReservas = await _localDb.GetReservasAsync(EspacoSelecionado.ClubeId);
        var conflito = todasReservas.Any(r => 
            r.EspacoId == EspacoSelecionado.Id &&
            r.Status != StatusReserva.Cancelada &&
            r.Status != StatusReserva.Rejeitada &&
            inicioCompleto < r.DataHoraFim && fimCompleto > r.DataHoraInicio);

        if (conflito)
        {
            var continuar = await App.Current.MainPage.DisplayAlert("Aviso de Conflito de Horário", 
                "Já existe uma reserva registrada para este espaço no mesmo período. A diretoria terá que arbitrar a preferência. Deseja registrar a solicitação mesmo assim?", 
                "Sim, Registrar", "Cancelar");

            if (!continuar) return;
        }

        var reservaId = Guid.NewGuid();
        var reservaLocal = new LocalReserva
        {
            Id = reservaId,
            ClubeId = EspacoSelecionado.ClubeId,
            EspacoId = EspacoSelecionado.Id,
            NomeEspaco = EspacoSelecionado.Nome,
            AssociadoId = Sessao?.AssociadoId ?? Guid.Empty,
            NomeAssociado = Sessao?.Nome ?? "Associado",
            DataHoraInicio = inicioCompleto,
            DataHoraFim = fimCompleto,
            Status = StatusReserva.PendenteAprovacao,
            FinalidadeUso = Finalidade,
            Observacoes = Observacoes?.Trim(),
            DataSolicitacao = DateTime.UtcNow
        };

        // Salva imediatamente no banco local (Optimistic UI)
        await _localDb.SaveReservaAsync(reservaLocal);
        MinhasReservas.Insert(0, reservaLocal);

        var request = new CriarReservaRequest
        {
            EspacoId = EspacoSelecionado.Id,
            FinalidadeUso = Finalidade,
            DataHoraInicio = inicioCompleto,
            DataHoraFim = fimCompleto,
            Observacoes = reservaLocal.Observacoes ?? "",
            CriadoOffline = !_syncService.IsOnline
        };

        // Enfileira no Outbox
        await _localDb.EnqueueOutboxAsync("RESERVA_CRIAR", "Reserva", request);

        if (_syncService.IsOnline)
        {
            _ = Task.Run(async () => await _syncService.SincronizarAsync());
        }

        await App.Current.MainPage.DisplayAlert("Reserva Solicitada", 
            $"Sua reserva para '{EspacoSelecionado.Nome}' ({inicioCompleto:dd/MM HH:mm} até {fimCompleto:dd/MM HH:mm}) foi enviada para homologação do Presidente.", "OK");
    }
}
