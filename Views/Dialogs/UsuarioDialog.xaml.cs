using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;

namespace StreamManager.Views.Dialogs
{
    public partial class UsuarioDialog : Window
    {
        private readonly SupabaseService _supabase;
        private readonly AuthUser? _usuarioExistente;
        private readonly bool _esEdicion;

        public UsuarioDialog(AuthUser? usuario = null)
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _usuarioExistente = usuario;
            _esEdicion = usuario != null;

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Usuario";
                CargarDatosUsuario();
                
                // Ocultar campo de contraseña en edición
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordInfoText.Text = "La contraseña no se puede cambiar desde aquí";
            }

            NombreTextBox.Focus();
        }

        private void CargarDatosUsuario()
        {
            if (_usuarioExistente == null) return;

            NombreTextBox.Text = _usuarioExistente.NombreCompleto;
            EmailTextBox.Text = _usuarioExistente.Email;

            // Seleccionar rol
            foreach (System.Windows.Controls.ComboBoxItem item in RolComboBox.Items)
            {
                if (item.Tag?.ToString() == _usuarioExistente.Rol)
                {
                    RolComboBox.SelectedItem = item;
                    break;
                }
            }

            // Seleccionar estado
            foreach (System.Windows.Controls.ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == _usuarioExistente.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                GuardarButton.IsEnabled = false;

                var nombre = NombreTextBox.Text.Trim();
                var email = EmailTextBox.Text.Trim().ToLower();
                var rol = (RolComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "vendedor";
                var estado = (EstadoComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "activo";

                if (_esEdicion)
                {
                    // Editar usuario existente
                    _usuarioExistente!.NombreCompleto = nombre;
                    _usuarioExistente.Email = email;
                    _usuarioExistente.Rol = rol;
                    _usuarioExistente.Estado = estado;

                    await _supabase.ActualizarUsuarioAsync(_usuarioExistente);

                    MessageBox.Show(
                        "Usuario actualizado exitosamente",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Crear nuevo usuario
                    var password = PasswordBox.Password;
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 11);

                    var nuevoUsuario = new AuthUser
                    {
                        Email = email,
                        PasswordHash = passwordHash,
                        NombreCompleto = nombre,
                        Rol = rol,
                        Estado = estado,
                        FechaCreacion = DateTime.Now
                    };

                    await _supabase.CrearUsuarioAsync(nuevoUsuario);

                    MessageBox.Show(
                        "Usuario creado exitosamente",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar usuario:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                GuardarButton.IsEnabled = true;
            }
        }

        private bool ValidarFormulario()
        {
            // Validar nombre
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                MessageBox.Show(
                    "Por favor ingresa el nombre completo",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                NombreTextBox.Focus();
                return false;
            }

            // Validar email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show(
                    "Por favor ingresa el correo electrónico",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                MessageBox.Show(
                    "Por favor ingresa un correo electrónico válido",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            // Validar contraseña (solo al crear)
            if (!_esEdicion)
            {
                if (string.IsNullOrEmpty(PasswordBox.Password))
                {
                    MessageBox.Show(
                        "Por favor ingresa una contraseña",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return false;
                }

                if (PasswordBox.Password.Length < 8)
                {
                    MessageBox.Show(
                        "La contraseña debe tener al menos 8 caracteres",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
