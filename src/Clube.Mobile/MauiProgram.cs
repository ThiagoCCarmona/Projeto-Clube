using Microsoft.Extensions.Logging;
using Clube.Mobile.Services;
using Clube.Mobile.ViewModels;
using Clube.Mobile.Views;

namespace Clube.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// 1. Serviços de Armazenamento Local e Sincronização
		builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
		builder.Services.AddSingleton<IApiClient, ApiClient>();
		builder.Services.AddSingleton<IMobileSyncService, MobileSyncService>();

		// 2. ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<EventosViewModel>();
		builder.Services.AddTransient<ReservasViewModel>();
		builder.Services.AddTransient<MensalidadesViewModel>();
		builder.Services.AddTransient<SacViewModel>();
		builder.Services.AddTransient<PerfilViewModel>();

		// 3. Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<EventosPage>();
		builder.Services.AddTransient<ReservasPage>();
		builder.Services.AddTransient<MensalidadesPage>();
		builder.Services.AddTransient<SacPage>();
		builder.Services.AddTransient<PerfilPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
