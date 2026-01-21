using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Views.Dialogs
{
    public partial class SuscripcionDetalleDialog : Window
    {
        private readonly SupabaseService _supabase;
        private string _correoElectronico = string.Empty;
        private string _contrasena = string.Empty;

        public SuscripcionDetalleDialog(SuscripcionViewModel viewModel)
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            CargarDatos(viewModel);
        }

        private async void CargarDatos(SuscripcionViewModel viewModel)
        {
            // Header
            ClienteTextBlock.Text = viewModel.ClienteNombre;
            PerfilTextBlock.Text = $"{viewModel.PerfilNombre} - {viewModel.PlataformaNombre}";

            // Información de Suscripción
            PlataformaTextBlock.Text = viewModel.PlataformaNombre;
            PerfilDetalleTextBlock.Text = viewModel.PerfilNombre;
            CostoTextBlock.Text = $"L {viewModel.CostoMensual:N2}";
            FechaInicioTextBlock.Text = viewModel.FechaInicio.ToString("dd/MM/yyyy");

            // Estado
            EstadoTextBlock.Text = viewModel.Estado;
            EstadoBorder.Background = viewModel.Estado.ToLower() switch
            {
                "activa" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Verde
                "vencida" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Rojo
                "cancelada" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gris
                _ => new SolidColorBrush(Color.FromRgb(33, 150, 243))  // Azul
            };

            // Información de Pagos
            ProximoPagoTextBlock.Text = viewModel.ProximoPago.ToString("dd/MM/yyyy");

            // Calcular días restantes
            var diasRestantes = (viewModel.ProximoPago.Date - DateTime.Today).Days;
            if (diasRestantes < 0)
            {
                DiasRestantesTextBlock.Text = $"Vencido hace {Math.Abs(diasRestantes)} días";
                DiasRestantesTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rojo
            }
            else if (diasRestantes == 0)
            {
                DiasRestantesTextBlock.Text = "Vence hoy";
                DiasRestantesTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Naranja
            }
            else if (diasRestantes <= 3)
            {
                DiasRestantesTextBlock.Text = $"En {diasRestantes} días";
                DiasRestantesTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amarillo
            }
            else
            {
                DiasRestantesTextBlock.Text = $"En {diasRestantes} días";
                DiasRestantesTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Verde
            }

            // Calcular tiempo de servicio
            var diasServicio = (DateTime.Today - viewModel.FechaInicio.Date).Days;
            if (diasServicio < 30)
            {
                TiempoServicioTextBlock.Text = $"{diasServicio} días";
            }
            else if (diasServicio < 365)
            {
                var meses = diasServicio / 30;
                TiempoServicioTextBlock.Text = $"{meses} {(meses == 1 ? "mes" : "meses")}";
            }
            else
            {
                var años = diasServicio / 365;
                var mesesRestantes = (diasServicio % 365) / 30;
                TiempoServicioTextBlock.Text = mesesRestantes > 0
                    ? $"{años} {(años == 1 ? "año" : "años")} y {mesesRestantes} {(mesesRestantes == 1 ? "mes" : "meses")}"
                    : $"{años} {(años == 1 ? "año" : "años")}";
            }

            // Información del Cliente
            ClienteNombreTextBlock.Text = viewModel.ClienteNombre;

            // Obtener teléfono del cliente
            try
            {
                var clientes = await _supabase.ObtenerClientesAsync();
                var cliente = clientes.FirstOrDefault(c => c.NombreCompleto == viewModel.ClienteNombre);

                if (cliente != null)
                {
                    ClienteTelefonoTextBlock.Text = cliente.Telefono ?? "Sin teléfono";
                }
                else
                {
                    ClienteTelefonoTextBlock.Text = "Sin teléfono";
                }
            }
            catch
            {
                ClienteTelefonoTextBlock.Text = "Error al cargar";
            }

            // Credenciales de Acceso - Obtener de la cuenta
            try
            {
                var perfiles = await _supabase.ObtenerPerfilesAsync();
                var perfil = perfiles.FirstOrDefault(p => p.NombrePerfil == viewModel.PerfilNombre);

                if (perfil != null)
                {
                    var cuentas = await _supabase.ObtenerCuentasAsync();
                    var cuenta = cuentas.FirstOrDefault(c => c.Id == perfil.CuentaId);

                    if (cuenta != null)
                    {
                        _correoElectronico = cuenta.Email;
                        _contrasena = cuenta.Password;

                        CorreoTextBlock.Text = cuenta.Email;
                        ContrasenaTextBlock.Text = "••••••••";
                    }
                    else
                    {
                        CorreoTextBlock.Text = "No disponible";
                        ContrasenaTextBlock.Text = "••••••••";
                    }
                }
                else
                {
                    CorreoTextBlock.Text = "No disponible";
                    ContrasenaTextBlock.Text = "••••••••";
                }
            }
            catch
            {
                CorreoTextBlock.Text = "Error al cargar";
                ContrasenaTextBlock.Text = "••••••••";
            }

            // Notas
            NotasTextBlock.Text = string.IsNullOrWhiteSpace(viewModel.Notas)
                ? "Sin notas"
                : viewModel.Notas;
        }

        private void CopiarCorreoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_correoElectronico))
            {
                try
                {
                    Clipboard.SetText(_correoElectronico);
                    MessageBox.Show(
                        "Correo electrónico copiado al portapapeles",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al copiar: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CopiarContrasenaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_contrasena))
            {
                try
                {
                    Clipboard.SetText(_contrasena);
                    MessageBox.Show(
                        "Contraseña copiada al portapapeles",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al copiar: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}