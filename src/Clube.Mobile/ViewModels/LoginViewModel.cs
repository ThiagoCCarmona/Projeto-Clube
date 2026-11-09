using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Clube.Mobile.Models;
using Clube.Mobile.Services;
using Clube.Shared.DTOs;

namespace Clube.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    private string _cpf = "111.111.111-11";
    private string _senha = "User@123";

    public string CPF
    {
        get => _cpf;
        set => SetProperty(ref _cpf, value);
    }

    public string Senha
    {
        get => _senha;
        set => SetProperty(ref _senha, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand IrParaCadastroCommand { get; }
    public ICommand UsarCredenciaisDemoCommand { get; }

    public LoginViewModel(IApiClient apiClient, ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _apiClient = apiClient;
        _localDb = localDb;
        _syncService = syncService;

        Title = "Entrar no Clube";
        LoginCommand = new AsyncRelayCommand(ExecutarLoginAsync);
        IrParaCadastroCommand = new AsyncRelayCommand(IrParaCadastroAsync);
        UsarCredenciaisDemoCommand = new RelayCommand(PreencherCredenciaisDemo);
    }

    public async Task InicializarAsync()
    {
        await _localDb.InitializeAsync();
        IsOffline = !_syncService.IsOnline;

        // Auto-login se já possuir sessão válida salva
        var session = await _localDb.GetSessionAsync();
        if (session != null && !string.IsNullOrEmpty(session.Token))
        {
            _apiClient.SetAuthToken(session.Token);
            await Shell.Current.GoToAsync("//AppTabs/EventosPage");
        }
    }

    private void PreencherCredenciaisDemo()
    {
        CPF = "111.111.111-11";
        Senha = "User@123";
    }

    private async Task ExecutarLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(CPF) || string.IsNullOrWhiteSpace(Senha))
        {
            StatusMessage = "Informe seu CPF e senha.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Autenticando...";

        try
        {
            var auth = await _apiClient.LoginAsync(new LoginRequest
            {
                CPF = CPF.Trim(),
                Senha = Senha
            });

            if (auth != null && auth.Sucesso)
            {
                var session = new LocalUserSession
                {
                    Key = "CurrentSession",
                    UsuarioId = auth.UsuarioId,
                    Nome = auth.Nome,
                    Email = auth.Email,
                    CPF = CPF.Trim(),
                    Perfil = auth.Perfil,
                    ClubeId = auth.ClubeId,
                    AssociadoId = auth.UsuarioId, // Mapeado no contexto
                    TipoAssociado = auth.TipoAssociado,
                    StatusAprovacao = auth.StatusAprovacao,
                    TemDireitoAVoto = auth.TemDireitoAVoto,
                    Token = auth.Token,
                    LastSyncTime = DateTime.UtcNow
                };

                await _localDb.SaveSessionAsync(session);

                // Executa sincronização inicial em background
                _ = Task.Run(async () => await _syncService.SincronizarAsync());

                await Shell.Current.GoToAsync("//AppTabs/EventosPage");
                return;
            }

            // Fallback Offline: se o backend estiver inacessível, verifica se o usuário já fez login previamente
            var savedSession = await _localDb.GetSessionAsync();
            if (savedSession != null && (savedSession.CPF == CPF || savedSession.Email.Equals(CPF, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "Autenticado em modo Offline usando dados salvos no dispositivo.";
                await Shell.Current.GoToAsync("//AppTabs/EventosPage");
                return;
            }

            StatusMessage = auth?.Mensagem ?? "Credenciais inválidas ou clube não localizado.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro de conexão: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task IrParaCadastroAsync()
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }
}
