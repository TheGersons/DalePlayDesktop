using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using System.Windows;
using System.Windows.Input;

namespace StreamManager.Views.Dialogs
{
    public partial class LoginDialog : Window
    {
        private readonly SupabaseService _supabase;

        public LoginDialog()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            EmailTextBox.Focus();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await IniciarSesionAsync();
        }

        private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await IniciarSesionAsync();
            }
        }

        private async Task IniciarSesionAsync()
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MostrarError("Por favor ingresa tu correo electrónico");
                EmailTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                MostrarError("Por favor ingresa tu contraseña");
                PasswordBox.Focus();
                return;
            }

            try
            {
                // Mostrar loading
                LoadingProgressBar.Visibility = Visibility.Visible;
                LoginButton.IsEnabled = false;
                ErrorTextBlock.Visibility = Visibility.Collapsed;

                // Obtener usuarios
                var usuarios = await _supabase.ObtenerUsuariosAsync();
                var usuario = usuarios.FirstOrDefault(u =>
                    u.Email.ToLower() == EmailTextBox.Text.ToLower().Trim() &&
                    u.Estado == "activo");

                if (usuario == null)
                {
                    MostrarError("Usuario no encontrado o inactivo");
                    return;
                }

                // Verificar contraseña (en producción usar hash)
                // Por ahora comparación simple
                if (usuario.PasswordHash != PasswordBox.Password)
                {
                    MostrarError("Contraseña incorrecta");
                    return;
                }

                // Login exitoso
                UserSession.CurrentUser = usuario;

                // Actualizar último acceso
                usuario.FechaUltimoAcceso = DateTime.Now;
                await _supabase.ActualizarUsuarioAsync(usuario);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al iniciar sesión: {ex.Message}");
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                LoginButton.IsEnabled = true;
            }
        }

        private void MostrarError(string mensaje)
        {
            ErrorTextBlock.Text = mensaje;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
