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

                bool loginExitoso = await _supabase.LoginAsync(email, password);


                if (loginExitoso)
                {

                    // Obtener datos del usuario
                    var usuarios = await _supabase.ObtenerUsuariosAsync();
                    var usuario = usuarios.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

                    if (usuario != null)
                    {
                        // Guardar en sesión
                        UserSession.CurrentUser = usuario;

                        // Actualizar último acceso
                        usuario.FechaUltimoAcceso = DateTime.Now;
                        await _supabase.ActualizarUsuarioAsync(usuario);

                    }

                    // Abrir ventana principal
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
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