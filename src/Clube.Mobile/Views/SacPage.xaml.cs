using Clube.Mobile.ViewModels;

namespace Clube.Mobile.Views;

public partial class SacPage : ContentPage
{
    private readonly SacViewModel _viewModel;

    public SacPage(SacViewModel viewModel)
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
