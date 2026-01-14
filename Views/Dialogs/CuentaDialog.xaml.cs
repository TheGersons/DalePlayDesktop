using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Dialogs
{
    public partial class CuentaDialog : Window
    {
        private readonly SupabaseService _supabase;
        public CuentaCorreo CuentaCorreo { get; private set; }
        private bool _esEdicion;
        private List<Plataforma> _plataformas = new();

        public CuentaDialog(CuentaCorreo? cuenta = null)
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _esEdicion = cuenta != null;
            CuentaCorreo = cuenta ?? new CuentaCorreo();

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Cuenta de Correo";
            }

            Loaded += CuentaDialog_Loaded;
        }

        private async void CuentaDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                _plataformas = (await _supabase.ObtenerPlataformasAsync())
                    .Where(p => p.Estado == "activa")
                    .OrderBy(p => p.Nombre)
                    .ToList();

                PlataformaComboBox.ItemsSource = _plataformas;

                if (_esEdicion)
                {
                    CargarDatos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar datos: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CargarDatos()
        {
            var plataforma = _plataformas.FirstOrDefault(p => p.Id == CuentaCorreo.PlataformaId);
            if (plataforma != null)
            {
                PlataformaComboBox.SelectedItem = plataforma;
            }

            CorreoTextBox.Text = CuentaCorreo.Email;
            ContraseñaTextBox.Text = CuentaCorreo.Password;

            // Seleccionar estado
            foreach (ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == CuentaCorreo.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }

            NotasTextBox.Text = CuentaCorreo.Notas ?? string.Empty;
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                CuentaCorreo.PlataformaId = (PlataformaComboBox.SelectedItem as Plataforma)!.Id;
                CuentaCorreo.Email = CorreoTextBox.Text.Trim();
                CuentaCorreo.Password = ContraseñaTextBox.Text.Trim();
                CuentaCorreo.Estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "activa";
                CuentaCorreo.Notas = string.IsNullOrWhiteSpace(NotasTextBox.Text) ? null : NotasTextBox.Text.Trim();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidarFormulario()
        {
            if (PlataformaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una plataforma", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                PlataformaComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CorreoTextBox.Text))
            {
                MessageBox.Show("El correo electrónico es obligatorio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CorreoTextBox.Focus();
                return false;
            }

            // Validar formato de email
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(CorreoTextBox.Text.Trim(), emailRegex))
            {
                MessageBox.Show("El formato del correo electrónico no es válido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CorreoTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContraseñaTextBox.Text))
            {
                MessageBox.Show("La contraseña es obligatoria!!!", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContraseñaTextBox.Focus();
                return false;
            }

            if (ContraseñaTextBox.Text.Length < 4)
            {
                MessageBox.Show("La contraseña debe tener al menos 4 caracteres", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContraseñaTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}
