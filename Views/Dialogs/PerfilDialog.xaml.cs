using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StreamManager.Views.Dialogs
{
    public partial class PerfilDialog : Window
    {
        private readonly SupabaseService _supabase;
        public Perfil Perfil { get; private set; }
        private bool _esEdicion;

        private List<Plataforma> _plataformas = new();
        private List<CuentaCorreo> _cuentas = new();

        public PerfilDialog(Perfil? perfil = null)
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _esEdicion = perfil != null;
            Perfil = perfil ?? new Perfil();

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Perfil";
            }

            Loaded += PerfilDialog_Loaded;
        }

        private async void PerfilDialog_Loaded(object sender, RoutedEventArgs e)
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

                _cuentas = await _supabase.ObtenerCuentasAsync();

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
            // Obtener cuenta para determinar plataforma
            var cuenta = _cuentas.FirstOrDefault(c => c.Id == Perfil.CuentaId);
            if (cuenta != null)
            {
                var plataforma = _plataformas.FirstOrDefault(p => p.Id == cuenta.PlataformaId);
                if (plataforma != null)
                {
                    PlataformaComboBox.SelectedItem = plataforma;

                    // Esto disparará el evento que cargará las cuentas
                    Dispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(100);
                        CuentaComboBox.SelectedItem = cuenta;
                    });
                }
            }

            NombreTextBox.Text = Perfil.NombrePerfil;
            PinTextBox.Text = Perfil.Pin ?? string.Empty;

            // Seleccionar estado
            foreach (ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == Perfil.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void PlataformaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlataformaComboBox.SelectedItem is Plataforma plataforma)
            {
                // Usamos ToLower() y Trim() para que sea una comparación segura
                var cuentasPlataforma = _cuentas
                    .Where(c => c.PlataformaId == plataforma.Id &&
                           c.Estado.Trim().ToLower() == "activo")
                    .ToList();

                CuentaComboBox.ItemsSource = cuentasPlataforma;
                CuentaComboBox.IsEnabled = cuentasPlataforma.Any();
                CuentaComboBox.SelectedIndex = -1;

                if (!cuentasPlataforma.Any())
                {
                    MessageBox.Show(
                        "Esta plataforma no tiene cuentas activas disponibles.\n\nCrea una cuenta primero en la sección 'Cuentas de Correo'.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    CuentaComboBox.SelectedIndex = 0;
                }
            }
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                Perfil.CuentaId = (CuentaComboBox.SelectedItem as CuentaCorreo)!.Id;
                Perfil.NombrePerfil = NombreTextBox.Text.Trim();
                Perfil.Pin = string.IsNullOrWhiteSpace(PinTextBox.Text) ? null : PinTextBox.Text.Trim();
                Perfil.Estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "disponible";

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

            if (CuentaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una cuenta de correo", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CuentaComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                MessageBox.Show("El nombre del perfil es obligatorio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                NombreTextBox.Focus();
                return false;
            }

            return true;
        }

        private void PinTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir solo números
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }
    }
}
