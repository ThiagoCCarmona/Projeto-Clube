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

public class RegisterViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    public ObservableCollection<LocalClube> ClubesDisponiveis { get; } = new();

    private LocalClube? _clubeSelecionado;
    public LocalClube? ClubeSelecionado
    {
        get => _clubeSelecionado;
        set => SetProperty(ref _clubeSelecionado, value);
    }

    private string _nome = "";
    public string Nome { get => _nome; set => SetProperty(ref _nome, value); }

    private string _email = "";
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    private string _senha = "";
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }

    private string _cpf = "";
    public string CPF { get => _cpf; set => SetProperty(ref _cpf, value); }

    private string _telefone = "";
    public string Telefone { get => _telefone; set => SetProperty(ref _telefone, value); }

    private string _endereco = "";
    public string Endereco { get => _endereco; set => SetProperty(ref _endereco, value); }

    private string _bairro = "";
    public string Bairro { get => _bairro; set => SetProperty(ref _bairro, value); }

    private string _cidade = "Foz do Iguaçu";
    public string Cidade { get => _cidade; set => SetProperty(ref _cidade, value); }

    private string _uf = "PR";
    public string UF { get => _uf; set => SetProperty(ref _uf, value); }

    private DateTime _dataNascimento = new(1990, 1, 1);
    public DateTime DataNascimento { get => _dataNascimento; set => SetProperty(ref _dataNascimento, value); }

    private string _sexo = "Masculino";
    public string Sexo { get => _sexo; set => SetProperty(ref _sexo, value); }

    private string _estadoCivil = "Casado";
    public string EstadoCivil { get => _estadoCivil; set => SetProperty(ref _estadoCivil, value); }

    private string _interesses = "Pesca, Churrasco, Eventos Familiares";
    public string Interesses { get => _interesses; set => SetProperty(ref _interesses, value); }

    private TipoAssociado _tipoAssociado = TipoAssociado.Simpatizante;
    public TipoAssociado TipoAssociado
    {
        get => _tipoAssociado;
        set => SetProperty(ref _tipoAssociado, value);
    }

    public ICommand CadastrarCommand { get; }
    public ICommand VoltarLoginCommand { get; }

    public RegisterViewModel(IApiClient apiClient, ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _apiClient = apiClient;
        _localDb = localDb;
        _syncService = syncService;

        Title = "Filie-se ao Clube";
        CadastrarCommand = new AsyncRelayCommand(ExecutarCadastroAsync);
        VoltarLoginCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task InicializarAsync()
    {
        var clubes = await _localDb.GetClubesAsync();
        ClubesDisponiveis.Clear();
        foreach (var c in clubes)
            ClubesDisponiveis.Add(c);

        if (ClubesDisponiveis.Any())
            ClubeSelecionado = ClubesDisponiveis.First();
    }

    private async Task ExecutarCadastroAsync()
    {
        if (ClubeSelecionado == null)
        {
            StatusMessage = "Selecione o clube ao qual deseja se associar.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Email) || 
            string.IsNullOrWhiteSpace(Senha) || string.IsNullOrWhiteSpace(CPF))
        {
            StatusMessage = "Preencha os campos obrigatórios (Nome, E-mail, Senha e CPF).";
            return;
        }

        IsBusy = true;
        StatusMessage = "Enviando cadastro...";

        var request = new RegistroPublicoRequest
        {
            ClubeId = ClubeSelecionado.Id,
            Nome = Nome.Trim(),
            Email = Email.Trim(),
            Senha = Senha,
            CPF = CPF.Trim(),
            Telefone = Telefone.Trim(),
            Endereco = Endereco.Trim(),
            Bairro = Bairro.Trim(),
            Cidade = Cidade.Trim(),
            UF = UF.Trim(),
            DataNascimento = DataNascimento,
            Sexo = Sexo,
            EstadoCivil = EstadoCivil,
            Interesses = Interesses,
            TipoPretendido = TipoAssociado
        };

        try
        {
            if (_syncService.IsOnline)
            {
                var dto = await _apiClient.AutoCadastrarSocioAsync(request);
                if (dto != null)
                {
                    StatusMessage = "Cadastro realizado! Sua adesão está pendente de aprovação pela diretoria.";
                    await App.Current.MainPage.DisplayAlert("Cadastro Concluído", 
                        "Sua solicitação de adesão foi enviada. Conforme regras do clube, o Presidente irá homologar o cadastro.", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }
            }

            // Offline-First: Enfileira no Outbox
            await _localDb.EnqueueOutboxAsync("ASSOCIADO_CADASTRAR", "Associado", request);
            await App.Current.MainPage.DisplayAlert("Modo Offline", 
                "Cadastro armazenado com sucesso no dispositivo! Ele será transmitido automaticamente ao servidor assim que houver conexão.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Falha ao cadastrar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
