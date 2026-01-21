using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace StreamManager.Views.Dialogs
{
    public partial class PerfilDetalleDialog : Window
    {
        private readonly SupabaseService _supabase;
        private string _pin = string.Empty;
        private string _correoElectronico = string.Empty;
        private string _contrasena = string.Empty;

        public PerfilDetalleDialog(PerfilViewModel viewModel)
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            CargarDatos(viewModel);
        }

        private async void CargarDatos(PerfilViewModel viewModel)
        {
            // Header
            PerfilNombreTextBlock.Text = viewModel.Nombre;
            PlataformaTextBlock.Text = viewModel.PlataformaNombre;

            // Información del Perfil
            NombrePerfilTextBlock.Text = viewModel.Nombre;
            CuentaAsociadaTextBlock.Text = viewModel.CuentaNombre;

            // Estado
            EstadoTextBlock.Text = viewModel.Estado;
            EstadoBorder.Background = viewModel.Estado.ToLower() switch
            {
                "disponible" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Verde
                "ocupado" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Naranja
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Gris
            };

            // Cliente Asignado - Buscar en suscripciones
            try
            {
                var suscripciones = await _supabase.ObtenerSuscripcionesAsync();
                var suscripcionActiva = suscripciones.FirstOrDefault(s =>
                    s.PerfilId == viewModel.Id && s.Estado == "activa");

                if (suscripcionActiva != null)
                {
                    var clientes = await _supabase.ObtenerClientesAsync();
                    var cliente = clientes.FirstOrDefault(c => c.Id == suscripcionActiva.ClienteId);

                    if (cliente != null)
                    {
                        ClienteNombreTextBlock.Text = cliente.NombreCompleto;
                        ClienteTelefonoTextBlock.Text = cliente.Telefono ?? "Sin teléfono";
                    }
                    else
                    {
                        ClienteNombreTextBlock.Text = "Cliente no encontrado";
                        ClienteTelefonoTextBlock.Text = "-";
                    }
                }
                else
                {
                    ClienteNombreTextBlock.Text = "Sin asignar";
                    ClienteTelefonoTextBlock.Text = "-";
                }
            }
            catch
            {
                ClienteNombreTextBlock.Text = "Error al cargar";
                ClienteTelefonoTextBlock.Text = "-";
            }

            // PIN del perfil
            _pin = viewModel.Pin;
            if (!string.IsNullOrEmpty(_pin) && _pin != "Sin PIN")
            {
                PinTextBlock.Text = _pin;
            }
            else
            {
                PinTextBlock.Text = "Sin PIN";
            }

            // Credenciales de Acceso - Obtener de la cuenta
            try
            {
                var cuentas = await _supabase.ObtenerCuentasAsync();
                var cuenta = cuentas.FirstOrDefault(c => c.Id == viewModel.CuentaCorreoId);

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
            catch
            {
                CorreoTextBlock.Text = "Error al cargar";
                ContrasenaTextBlock.Text = "••••••••";
            }
        }

        private void CopiarPinButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_pin) && _pin != "Sin PIN")
            {
                try
                {
                    Clipboard.SetText(_pin);
                    MessageBox.Show(
                        "PIN copiado al portapapeles",
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
            else
            {
                MessageBox.Show(
                    "Este perfil no tiene PIN",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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