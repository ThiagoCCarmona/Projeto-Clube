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

public class SacViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    public ObservableCollection<LocalSolicitacao> Solicitacoes { get; } = new();

    private LocalUserSession? _sessao;
    public LocalUserSession? Sessao
    {
        get => _sessao;
        set => SetProperty(ref _sessao, value);
    }

    private TipoSolicitacao _tipo = TipoSolicitacao.Solicitacao;
    public TipoSolicitacao Tipo
    {
        get => _tipo;
        set => SetProperty(ref _tipo, value);
    }

    private string _assunto = "";
    public string Assunto { get => _assunto; set => SetProperty(ref _assunto, value); }

    private string _mensagem = "";
    public string Mensagem { get => _mensagem; set => SetProperty(ref _mensagem, value); }

    public ICommand CarregarSolicitacoesCommand { get; }
    public ICommand EnviarSolicitacaoCommand { get; }
    public ICommand SincronizarCommand { get; }

    public SacViewModel(ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _localDb = localDb;
        _syncService = syncService;

        Title = "Atendimento & Ouvidoria";
        CarregarSolicitacoesCommand = new AsyncRelayCommand(CarregarSolicitacoesAsync);
        EnviarSolicitacaoCommand = new AsyncRelayCommand(EnviarSolicitacaoAsync);
        SincronizarCommand = new AsyncRelayCommand(async () =>
        {
            var r = await _syncService.SincronizarAsync();
            StatusMessage = r.Mensagem;
            await CarregarSolicitacoesAsync();
        });
    }

    public async Task InicializarAsync()
    {
        Sessao = await _localDb.GetSessionAsync();
        IsOffline = !_syncService.IsOnline;
        await CarregarSolicitacoesAsync();
    }

    private async Task CarregarSolicitacoesAsync()
    {
        IsBusy = true;
        try
        {
            var associadoId = Sessao?.AssociadoId;
            var lista = await _localDb.GetSolicitacoesAsync(associadoId);

            Solicitacoes.Clear();
            foreach (var s in lista)
                Solicitacoes.Add(s);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EnviarSolicitacaoAsync()
    {
        if (string.IsNullOrWhiteSpace(Assunto) || string.IsNullOrWhiteSpace(Mensagem))
        {
            StatusMessage = "Preencha o assunto e a mensagem detalhada.";
            return;
        }

        var solicitacaoId = Guid.NewGuid();
        var solLocal = new LocalSolicitacao
        {
            Id = solicitacaoId,
            ClubeId = Sessao?.ClubeId ?? Guid.Empty,
            AssociadoId = Sessao?.AssociadoId ?? Guid.Empty,
            Tipo = Tipo,
            Assunto = Assunto.Trim(),
            Mensagem = Mensagem.Trim(),
            Status = StatusSolicitacao.Aberta,
            DataAbertura = DateTime.UtcNow
        };

        await _localDb.SaveSolicitacaoAsync(solLocal);
        Solicitacoes.Insert(0, solLocal);

        var request = new CriarSolicitacaoRequest
        {
            Tipo = Tipo,
            Assunto = solLocal.Assunto,
            Mensagem = solLocal.Mensagem
        };

        await _localDb.EnqueueOutboxAsync("SOLICITACAO_CRIAR", "Solicitacao", request);

        Assunto = "";
        Mensagem = "";

        if (_syncService.IsOnline)
        {
            _ = Task.Run(async () => await _syncService.SincronizarAsync());
        }

        await App.Current.MainPage.DisplayAlert("Manifestação Registrada", 
            "Sua solicitação foi salva e direcionada para a diretoria do clube.", "OK");
    }
}
