using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StreamManager.Views.Dialogs
{
    public partial class CuentaDialog : Window
    {
        private readonly SupabaseService _supabase;
        public CuentaCorreo CuentaCorreo { get; private set; }
        public PagoPlataforma? PagoPlataforma { get; private set; }
        public bool DebeCrearPago => RegistrarPagoCheckBox.IsChecked == true;

        private bool _esEdicion;
        private List<Plataforma> _plataformas = new();

        public CuentaDialog(CuentaCorreo? cuenta = null, PagoPlataforma? pago = null)
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _esEdicion = cuenta != null;
            CuentaCorreo = cuenta ?? new CuentaCorreo();
            PagoPlataforma = pago;

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Cuenta de Correo";
            }

            Loaded += CuentaDialog_Loaded;
        }

        private async void CuentaDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Establecer fecha actual por defecto
            FechaProximoPagoDatePicker.SelectedDate = DateTime.Today;

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
            // Cargar datos de cuenta
            var plataforma = _plataformas.FirstOrDefault(p => p.Id == CuentaCorreo.PlataformaId);
            if (plataforma != null)
            {
                PlataformaComboBox.SelectedItem = plataforma;
            }

            CorreoTextBox.Text = CuentaCorreo.Email;
            ContraseñaTextBox.Text = CuentaCorreo.Password;

            foreach (ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == CuentaCorreo.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }

            NotasTextBox.Text = CuentaCorreo.Notas ?? string.Empty;

            // Cargar datos de pago si existe
            if (PagoPlataforma != null)
            {
                RegistrarPagoCheckBox.IsChecked = true;
                MontoMensualTextBox.Text = PagoPlataforma.MontoMensual.ToString("F2");
                FechaProximoPagoDatePicker.SelectedDate = PagoPlataforma.FechaProximoPago.ToDateTime(TimeOnly.MinValue);
            }
        }

        private void RegistrarPagoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DatosPagoPanel != null)
                DatosPagoPanel.Visibility = Visibility.Visible;
        }

        private void RegistrarPagoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DatosPagoPanel != null)
                DatosPagoPanel.Visibility = Visibility.Collapsed;
        }

        private void NumeroTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir números y punto decimal
            e.Handled = !IsTextNumeric(e.Text);
        }

        private bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, @"^[0-9.]+$");
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                // Guardar datos de cuenta
                CuentaCorreo.PlataformaId = (PlataformaComboBox.SelectedItem as Plataforma)!.Id;
                CuentaCorreo.Email = CorreoTextBox.Text.Trim();
                CuentaCorreo.Password = ContraseñaTextBox.Text.Trim();
                CuentaCorreo.Estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "activo";
                CuentaCorreo.Notas = string.IsNullOrWhiteSpace(NotasTextBox.Text) ? null : NotasTextBox.Text.Trim();

                // Guardar datos de pago si está marcado
                if (RegistrarPagoCheckBox.IsChecked == true)
                {
                    var montoMensual = decimal.Parse(MontoMensualTextBox.Text);
                    var fechaProximoPago = FechaProximoPagoDatePicker.SelectedDate!.Value;
                    var diaPagoMes = fechaProximoPago.Day;

                    PagoPlataforma = new PagoPlataforma
                    {
                        PlataformaId = CuentaCorreo.PlataformaId,
                        MontoMensual = montoMensual,
                        DiaPagoMes = diaPagoMes,
                        FechaProximoPago = DateOnly.FromDateTime(fechaProximoPago),
                        FechaLimitePago = DateOnly.FromDateTime(fechaProximoPago.AddDays(5)), // 5 días de gracia por defecto
                        DiasGracia = 5,
                        Estado = "por_pagar",
                        MetodoPagoPreferido = "Tarjeta de crédito"
                    };
                }
                else
                {
                    PagoPlataforma = null;
                }

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
            // Validar datos de cuenta
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

            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(CorreoTextBox.Text.Trim(), emailRegex))
            {
                MessageBox.Show("El formato del correo electrónico no es válido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CorreoTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContraseñaTextBox.Text))
            {
                MessageBox.Show("La contraseña es obligatoria", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContraseñaTextBox.Focus();
                return false;
            }

            if (ContraseñaTextBox.Text.Length < 4)
            {
                MessageBox.Show("La contraseña debe tener al menos 4 caracteres", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContraseñaTextBox.Focus();
                return false;
            }

            // Validar datos de pago si está marcado
            if (RegistrarPagoCheckBox.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(MontoMensualTextBox.Text))
                {
                    MessageBox.Show("El monto mensual es obligatorio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MontoMensualTextBox.Focus();
                    return false;
                }

                if (!decimal.TryParse(MontoMensualTextBox.Text, out decimal monto) || monto <= 0)
                {
                    MessageBox.Show("El monto debe ser un número positivo", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MontoMensualTextBox.Focus();
                    return false;
                }

                if (FechaProximoPagoDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("La fecha de próximo pago es obligatoria", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FechaProximoPagoDatePicker.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}