using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StreamManager.Views.Dialogs; // Para reconocerse a sí mismo y otros diálogos
using System.Collections.Generic; // Para que reconozca List<>
using System.Threading.Tasks;    // Para que reconozca Task
using System.Linq;               // Para que reconozca .FirstOrDefault() y .Any()

namespace StreamManager.Views.Dialogs
{
    public partial class SuscripcionDialog : Window
    {
        private readonly SupabaseService _supabase;
        public Suscripcion Suscripcion { get; private set; }
        private bool _esEdicion;

        private List<Cliente> _clientes = new();
        private List<Plataforma> _plataformas = new();
        private List<CuentaCorreo> _cuentas = new();
        private List<Perfil> _perfiles = new();


        public SuscripcionDialog(Suscripcion? suscripcion = null)
        {
            InitializeComponent();


            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _esEdicion = suscripcion != null;
            Suscripcion = suscripcion ?? new Suscripcion();

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Suscripción";
            }
            else
            {
                // Valores por defecto
                FechaInicioDatePicker.SelectedDate = DateTime.Today;
                ProximoPagoDatePicker.SelectedDate = DateTime.Today.AddMonths(1);
            }

            Loaded += SuscripcionDialog_Loaded;
        }

        private async void SuscripcionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                // Cargar datos desde la base de datos
                _clientes = (await _supabase.ObtenerClientesAsync()).Where(c => c.Estado == "activo").ToList();
                _plataformas = (await _supabase.ObtenerPlataformasAsync()).Where(p => p.Estado == "activa").ToList();
                _cuentas = await _supabase.ObtenerCuentasAsync();
                _perfiles = await _supabase.ObtenerPerfilesAsync();

                // Cargar ComboBoxes
                ClienteComboBox.ItemsSource = _clientes;
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
            // Seleccionar cliente
            var cliente = _clientes.FirstOrDefault(c => c.Id == Suscripcion.ClienteId);
            if (cliente != null)
            {
                ClienteComboBox.SelectedItem = cliente;
            }

            // Obtener perfil para determinar cuenta y plataforma
            var perfil = _perfiles.FirstOrDefault(p => p.Id == Suscripcion.PerfilId);
            if (perfil != null)
            {
                var cuenta = _cuentas.FirstOrDefault(c => c.Id == perfil.CuentaId);
                if (cuenta != null)
                {
                    var plataforma = _plataformas.FirstOrDefault(p => p.Id == cuenta.PlataformaId);
                    if (plataforma != null)
                    {
                        PlataformaComboBox.SelectedItem = plataforma;
                        // Esto disparará el evento que cargará las cuentas

                        // Esperar a que se carguen las cuentas y luego seleccionar
                        Dispatcher.InvokeAsync(async () =>
                        {
                            await Task.Delay(100);

                            // FIX: Buscar el objeto anónimo que contiene la cuenta
                            if (CuentaComboBox.ItemsSource != null)
                            {
                                var cuentaItem = ((IEnumerable<dynamic>)CuentaComboBox.ItemsSource)
                                    .FirstOrDefault(item => item.Cuenta.Id == cuenta.Id);

                                if (cuentaItem != null)
                                {
                                    CuentaComboBox.SelectedItem = cuentaItem;
                                }
                            }

                            await Task.Delay(100);
                            PerfilComboBox.SelectedItem = perfil;
                        });
                    }
                }
            }

            CostoTextBox.Text = Suscripcion.Precio.ToString("F2");
            FechaInicioDatePicker.SelectedDate = Suscripcion.FechaInicio.ToDateTime(TimeOnly.MinValue);
            ProximoPagoDatePicker.SelectedDate = Suscripcion.FechaProximoPago.ToDateTime(TimeOnly.MinValue);

            // Seleccionar estado
            foreach (ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == Suscripcion.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }

            NotasTextBox.Text = Suscripcion.Notas ?? string.Empty;
        }

        private void PlataformaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlataformaComboBox.SelectedItem is Plataforma plataforma)
            {
                // Filtramos cuentas activas (usando el "activo" que corregimos antes)
                var cuentasPlataforma = _cuentas
                    .Where(c => c.PlataformaId == plataforma.Id && c.Estado.ToLower().Contains("activo"))
                    .Select(c => new {
                        Cuenta = c,
                        Texto = $"{c.Email} ({c.Estado})"
                    }).ToList();

                CuentaComboBox.ItemsSource = cuentasPlataforma;
                CuentaComboBox.DisplayMemberPath = "Texto";
                CuentaComboBox.IsEnabled = cuentasPlataforma.Any();

                // Limpiar perfiles al cambiar plataforma (solo si no estamos en modo edición inicial)
                if (!_esEdicion || CuentaComboBox.SelectedItem != null)
                {
                    PerfilComboBox.ItemsSource = null;
                    PerfilComboBox.IsEnabled = false;
                }
            }
        }

        private void CuentaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CuentaComboBox.SelectedItem != null)
            {
                // Extraer la cuenta del objeto anónimo de forma segura
                dynamic selectedItem = CuentaComboBox.SelectedItem;
                CuentaCorreo cuenta = selectedItem.Cuenta;

                if (cuenta != null)
                {
                    // BUSCAMOS PERFILES: 
                    // - Perfiles con estado 'disponible'
                    // - En modo edición, también incluir el perfil actual aunque esté 'ocupado'
                    var perfilesDisponibles = _perfiles
                        .Where(p => p.CuentaId == cuenta.Id &&
                               (p.Estado.Trim().ToLower() == "disponible" ||
                                (_esEdicion && p.Id == Suscripcion.PerfilId)))
                        .ToList();

                    PerfilComboBox.ItemsSource = perfilesDisponibles;
                    PerfilComboBox.DisplayMemberPath = "NombrePerfil";
                    PerfilComboBox.IsEnabled = perfilesDisponibles.Any();

                    if (!perfilesDisponibles.Any())
                    {
                        // Mensaje de ayuda para el usuario
                        Console.WriteLine("No se encontraron perfiles disponibles para esta cuenta.");
                    }
                }
            }
        }

        private void FechaInicioDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FechaInicioDatePicker.SelectedDate.HasValue && !_esEdicion)
            {
                // Auto-calcular próximo pago (30 días después)
                ProximoPagoDatePicker.SelectedDate = FechaInicioDatePicker.SelectedDate.Value.AddMonths(1);
            }
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            try
            {
                // 1. ASIGNACIÓN DE IDs (Arreglo del punto 1)
                // Obtenemos el cliente
                Suscripcion.ClienteId = (ClienteComboBox.SelectedItem as Cliente)!.Id;

                // Obtenemos el perfil
                var perfilSeleccionado = (PerfilComboBox.SelectedItem as Perfil)!;
                Suscripcion.PerfilId = perfilSeleccionado.Id;

                // ¡IMPORTANTE! Obtenemos la plataforma (Campo NOT NULL en tu SQL)
                var plataformaSeleccionada = (PlataformaComboBox.SelectedItem as Plataforma)!;
                Suscripcion.PlataformaId = plataformaSeleccionada.Id;

                // 2. TIPO DE SUSCRIPCIÓN (Punto 3: Manejo del caso)
                // Tu SQL tiene: CONSTRAINT chk_tipo_suscripcion CHECK (tipo_suscripcion IN ('perfil', 'cuenta_completa'))
                Suscripcion.TipoSuscripcion = "perfil";

                // 3. DATOS ECONÓMICOS Y ESTADO
                Suscripcion.Precio = decimal.Parse(CostoTextBox.Text);
                Suscripcion.Estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "activa";
                Suscripcion.Notas = string.IsNullOrWhiteSpace(NotasTextBox.Text) ? null : NotasTextBox.Text.Trim();

                // 4. MANEJO DE FECHAS (Punto 2: Fecha Límite)
                var inicio = FechaInicioDatePicker.SelectedDate!.Value;
                var proximo = ProximoPagoDatePicker.SelectedDate!.Value;

                Suscripcion.FechaInicio = DateOnly.FromDateTime(inicio);
                Suscripcion.FechaProximoPago = DateOnly.FromDateTime(proximo);

                // Asignamos la fecha límite igual a la de próximo pago como pediste
                Suscripcion.FechaLimitePago = DateOnly.FromDateTime(proximo);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al preparar los datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidarFormulario()
        {
            if (ClienteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un cliente", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClienteComboBox.Focus();
                return false;
            }

            if (PlataformaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una plataforma", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                PlataformaComboBox.Focus();
                return false;
            }

            if (CuentaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una cuenta", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CuentaComboBox.Focus();
                return false;
            }

            if (PerfilComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un perfil disponible", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                PerfilComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CostoTextBox.Text) || !decimal.TryParse(CostoTextBox.Text, out decimal costo) || costo < 0)
            {
                MessageBox.Show("Ingresa un costo válido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CostoTextBox.Focus();
                return false;
            }

            if (!FechaInicioDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Selecciona la fecha de inicio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                FechaInicioDatePicker.Focus();
                return false;
            }

            if (!ProximoPagoDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Selecciona la fecha del próximo pago", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProximoPagoDatePicker.Focus();
                return false;
            }

            if (ProximoPagoDatePicker.SelectedDate.Value < FechaInicioDatePicker.SelectedDate.Value)
            {
                MessageBox.Show("La fecha del próximo pago no puede ser anterior a la fecha de inicio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProximoPagoDatePicker.Focus();
                return false;
            }

            return true;
        }

        private void NumeroTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.]+$");
        }
    }
}