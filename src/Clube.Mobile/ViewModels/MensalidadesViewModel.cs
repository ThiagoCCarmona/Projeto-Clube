using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Clube.Domain.Enums;
using Clube.Mobile.Models;
using Clube.Mobile.Services;

namespace Clube.Mobile.ViewModels;

public class MensalidadesViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _localDb;
    private readonly IMobileSyncService _syncService;

    public ObservableCollection<LocalMensalidade> Mensalidades { get; } = new();

    private LocalUserSession? _sessao;
    public LocalUserSession? Sessao
    {
        get => _sessao;
        set => SetProperty(ref _sessao, value);
    }

    private decimal _totalPago;
    public decimal TotalPago { get => _totalPago; set => SetProperty(ref _totalPago, value); }

    private decimal _totalPendente;
    public decimal TotalPendente { get => _totalPendente; set => SetProperty(ref _totalPendente, value); }

    public ICommand CarregarMensalidadesCommand { get; }
    public ICommand CopiarPixCommand { get; }
    public ICommand SincronizarCommand { get; }

    public MensalidadesViewModel(ILocalDatabaseService localDb, IMobileSyncService syncService)
    {
        _localDb = localDb;
        _syncService = syncService;

        Title = "Minhas Mensalidades";
        CarregarMensalidadesCommand = new AsyncRelayCommand(CarregarMensalidadesAsync);
        CopiarPixCommand = new AsyncRelayCommand<string>(CopiarPixAsync);
        SincronizarCommand = new AsyncRelayCommand(async () =>
        {
            var r = await _syncService.SincronizarAsync();
            StatusMessage = r.Mensagem;
            await CarregarMensalidadesAsync();
        });
    }

    public async Task InicializarAsync()
    {
        Sessao = await _localDb.GetSessionAsync();
        IsOffline = !_syncService.IsOnline;
        await CarregarMensalidadesAsync();
    }

    private async Task CarregarMensalidadesAsync()
    {
        IsBusy = true;
        try
        {
            var associadoId = Sessao?.AssociadoId;
            var lista = await _localDb.GetMensalidadesAsync(associadoId);

            Mensalidades.Clear();
            foreach (var m in lista)
                Mensalidades.Add(m);

            TotalPago = lista.Where(m => m.Status == StatusMensalidade.Paga).Sum(m => m.Valor);
            TotalPendente = lista.Where(m => m.Status != StatusMensalidade.Paga).Sum(m => m.Valor);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CopiarPixAsync(string codigoPix)
    {
        if (string.IsNullOrWhiteSpace(codigoPix))
        {
            await App.Current.MainPage.DisplayAlert("PIX Indisponível", "Código PIX não disponível para esta mensalidade.", "OK");
            return;
        }

        await Clipboard.Default.SetTextAsync(codigoPix);
        await App.Current.MainPage.DisplayAlert("PIX Copiado!", 
            "Código PIX Copia-e-Cola copiado para a área de transferência. Abra seu aplicativo de banco e cole na opção PIX Copia e Cola.", "OK");
    }
}
