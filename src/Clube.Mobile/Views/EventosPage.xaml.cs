using Clube.Mobile.ViewModels;

namespace Clube.Mobile.Views;

public partial class EventosPage : ContentPage
{
    private readonly EventosViewModel _viewModel;

    public EventosPage(EventosViewModel viewModel)
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
