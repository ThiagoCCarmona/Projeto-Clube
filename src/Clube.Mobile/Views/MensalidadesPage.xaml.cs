using Clube.Mobile.ViewModels;

namespace Clube.Mobile.Views;

public partial class MensalidadesPage : ContentPage
{
    private readonly MensalidadesViewModel _viewModel;

    public MensalidadesPage(MensalidadesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InicializarAsync();
    }
}
