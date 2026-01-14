using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StreamManager.Views.Controls
{
    public partial class AlertasBadgeControl : UserControl
    {
        private readonly AlertaService _alertaService;
        private readonly DispatcherTimer _timer;

        public event EventHandler? AlertasButtonClicked;

        public AlertasBadgeControl()
        {
            InitializeComponent();

            _alertaService = App.ServiceProvider?.GetRequiredService<AlertaService>()
                ?? throw new InvalidOperationException("AlertaService no disponible");

            // Timer para actualizar cada 30 segundos
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += async (s, e) => await ActualizarContadorAsync();
            _timer.Start();

            Loaded += async (s, e) => await ActualizarContadorAsync();
        }

        private async Task ActualizarContadorAsync()
        {
            try
            {
                // Generar nuevas alertas
                await _alertaService.GenerarAlertasAutomaticasAsync();

                // Obtener conteo
                var contador = await _alertaService.ObtenerConteoAlertasPendientesAsync();

                // Actualizar UI
                if (contador > 0)
                {
                    ContadorTextBlock.Text = contador > 99 ? "99+" : contador.ToString();
                    BadgeBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    BadgeBorder.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar contador de alertas: {ex.Message}");
            }
        }

        private void AlertasButton_Click(object sender, RoutedEventArgs e)
        {
            AlertasButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        public async Task RefrescarAsync()
        {
            await ActualizarContadorAsync();
        }
    }
}
