using Microsoft.Extensions.DependencyInjection;
using StreamManager.Services;
using System.Windows;
using System.Windows.Input;

namespace StreamManager.Views
{
    public partial class LoginWindow : Window
    {
        private readonly SupabaseService _supabase;

        public LoginWindow()
        {
            InitializeComponent();
            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await IniciarSesionAsync();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
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
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            var email = EmailTextBox.Text.Trim();
            var password = PasswordBox.Password;

            var hashGenerado = BCrypt.Net.BCrypt.HashPassword(password, 11);
            System.Diagnostics.Debug.WriteLine($"[HASH] Hash para '{password}': {hashGenerado}");

            if (string.IsNullOrEmpty(email))
            {
                MostrarError("Por favor ingresa tu correo electrónico");
                EmailTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MostrarError("Por favor ingresa tu contraseña");
                PasswordBox.Focus();
                return;
            }

            // Mostrar loading
            LoginButton.IsEnabled = false;
            LoadingProgressBar.Visibility = Visibility.Visible;

            try
            {
                // DEBUG: Mostrar intento de login
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Intentando login con email: {email}");

                bool loginExitoso = await _supabase.LoginAsync(email, password);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Resultado login: {loginExitoso}");

                if (loginExitoso)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Login exitoso, abriendo MainWindow");

                    // Abrir ventana principal
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Login falló - credenciales incorrectas");
                    MostrarError("Credenciales incorrectas o usuario inactivo.\n\n" +
                                "Verifica:\n" +
                                "1. Email: admin@streammanager.com\n" +
                                "2. Contraseña: Admin123!\n" +
                                "3. Que el usuario existe en Supabase");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error en login: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] StackTrace: {ex.StackTrace}");

                MostrarError($"Error al iniciar sesión:\n{ex.Message}\n\n" +
                           "Verifica tu conexión a Supabase en appsettings.json");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoadingProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void MostrarError(string mensaje)
        {
            ErrorTextBlock.Text = mensaje;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}