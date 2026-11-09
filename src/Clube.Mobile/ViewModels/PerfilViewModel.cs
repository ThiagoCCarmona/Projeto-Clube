using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Clube.Domain.Enums;
using Clube.Mobile.Models;
using Clube.Mobile.Services;
using Clube.Shared.DTOs;

namespace Clube.Mobile.ViewModels;

public class PerfilViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    private LocalUserSession? _sessao;
    public LocalUserSession? Sessao
    {
        get => _sessao;
        set => SetProperty(ref _sessao, value);
    }

    private LocalAssociado? _associado;
    public LocalAssociado? Associado
    {
        get => _associado;
        set => SetProperty(ref _associado, value);
    }

    public ObservableCollection<LocalDependente> Dependentes { get; } = new();

    // Novo dependente modal / form
    private string _novoDepNome = "";
    public string NovoDepNome { get => _novoDepNome; set => SetProperty(ref _novoDepNome, value); }

    private string _novoDepCpf = "";
    public string NovoDepCpf { get => _novoDepCpf; set => SetProperty(ref _novoDepCpf, value); }

    private string _novoDepParentesco = "Filho(a)";
    public string NovoDepParentesco { get => _novoDepParentesco; set => SetProperty(ref _novoDepParentesco, value); }

    public ICommand SincronizarCommand { get; }
    public ICommand AdicionarDependenteCommand { get; }
    public ICommand LogoutCommand { get; }

    public PerfilViewModel(ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _localDb = localDb;
        _syncService = syncService;

        Title = "Meu Perfil & Dependentes";
        SincronizarCommand = new AsyncRelayCommand(SincronizarAsync);
        AdicionarDependenteCommand = new AsyncRelayCommand(AdicionarDependenteAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
    }

    public async Task InicializarAsync()
    {
        Sessao = await _localDb.GetSessionAsync();
        IsOffline = !_syncService.IsOnline;

        if (Sessao?.AssociadoId.HasValue ?? false)
        {
            Associado = await _localDb.GetAssociadoAsync(Sessao.AssociadoId.Value);
            var deps = await _localDb.GetDependentesAsync(Sessao.AssociadoId.Value);
            Dependentes.Clear();
            foreach (var d in deps)
                Dependentes.Add(d);
        }
    }

    public string DescricaoDireitoVoto
    {
        get
        {
            if (Sessao?.TipoAssociado == TipoAssociado.SocioFundadorComVoto)
                return "DIREITO A VOTO: SIM (Sócio Fundador com plena prerrogativa estatutária de voto em assembleias)";
            if (Sessao?.TipoAssociado == TipoAssociado.SocioConvidadoSemVoto)
                return "DIREITO A VOTO: NÃO (Sócio Convidado sob período avaliativo, sem prerrogativa de voto)";
            return "DIREITO A VOTO: NÃO (Simpatizante comunitário, sem prerrogativa de voto)";
        }
    }

    private async Task SincronizarAsync()
    {
        IsBusy = true;
        StatusMessage = "Sincronizando com o servidor...";
        try
        {
            var r = await _syncService.SincronizarAsync();
            StatusMessage = r.Mensagem;
            await InicializarAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AdicionarDependenteAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoDepNome) || string.IsNullOrWhiteSpace(NovoDepCpf))
        {
            StatusMessage = "Preencha o nome e CPF do dependente.";
            return;
        }

        if (!Sessao?.AssociadoId.HasValue ?? true) return;

        var dep = new LocalDependente
        {
            Id = Guid.NewGuid(),
            AssociadoId = Sessao.AssociadoId.Value,
            Nome = NovoDepNome.Trim(),
            CPF = NovoDepCpf.Trim(),
            Parentesco = NovoDepParentesco,
            DataNascimento = DateTime.Today.AddYears(-10)
        };

        await _localDb.SaveDependentesAsync(new[] { dep });
        Dependentes.Add(dep);

        var dto = new NovoDependenteDto
        {
            Nome = dep.Nome,
            CPF = dep.CPF,
            GrauParentesco = dep.Parentesco,
            DataNascimento = dep.DataNascimento
        };

        await _localDb.EnqueueOutboxAsync("DEPENDENTE_CRIAR", "Dependente", dto);

        NovoDepNome = "";
        NovoDepCpf = "";

        if (_syncService.IsOnline)
        {
            _ = Task.Run(async () => await _syncService.SincronizarAsync());
        }

        await App.Current.MainPage.DisplayAlert("Dependente Cadastrado", 
            $"Dependente '{dep.Nome}' vinculado com sucesso ao seu cadastro familiar.", "OK");
    }

    private async Task LogoutAsync()
    {
        var confirmar = await App.Current.MainPage.DisplayAlert("Encerrar Sessão", "Deseja realmente sair da sua conta?", "Sair", "Cancelar");
        if (confirmar)
        {
            await _localDb.ClearSessionAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
