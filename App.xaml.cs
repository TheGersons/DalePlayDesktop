using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using StreamManager.Views;
using System.Globalization;
using System.IO;
using System.Windows;

namespace StreamManager
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static IConfiguration? Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configurar cultura (para Lempiras)
            var culture = new CultureInfo("es-HN");
            culture.NumberFormat.CurrencySymbol = "L";
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Cargar configuración
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            // Configurar servicios
            var services = new ServiceCollection();

            // Configuración
            services.AddSingleton<IConfiguration>(Configuration);

            // Servicios principales
            services.AddSingleton<SupabaseService>();
            services.AddSingleton<EmailService>();
            services.AddSingleton<AlertaService>();

            ServiceProvider = services.BuildServiceProvider();

            // Inicializar Supabase
            var supabase = ServiceProvider.GetRequiredService<SupabaseService>();

            Task.Run(async () =>
            {
                try
                {
                    await supabase.InicializarAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al inicializar: {ex.Message}\n\n" +
                        "Verifica tu configuración en appsettings.json",
                        "Error de Inicialización",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }).Wait();

            // Mostrar login
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}