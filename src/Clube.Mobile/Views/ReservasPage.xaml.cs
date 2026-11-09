using Clube.Mobile.ViewModels;

namespace Clube.Mobile.Views;

public partial class ReservasPage : ContentPage
{
    private readonly ReservasViewModel _viewModel;

    public ReservasPage(ReservasViewModel viewModel)
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
