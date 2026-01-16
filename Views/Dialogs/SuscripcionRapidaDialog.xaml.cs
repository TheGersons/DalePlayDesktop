using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamManager.Views.Dialogs
{
    public partial class SuscripcionRapidaDialog : Window
    {
        private readonly SupabaseService _supabase;
        private List<Plataforma> _plataformas = new();
        private List<Cliente> _clientes = new();
        private List<CuentaCorreo> _cuentas = new();
        private List<Perfil> _perfiles = new();

        private bool _clienteNuevo = false;
        private bool _cuentaNueva = false;
        private bool _perfilNuevo = false;

        public SuscripcionRapidaDialog()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            Loaded += SuscripcionRapidaDialog_Loaded;
        }

        private async void SuscripcionRapidaDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Cargar plataformas
                _plataformas = await _supabase.ObtenerPlataformasAsync();
                PlataformaComboBox.ItemsSource = _plataformas;
                PlataformaComboBox.DisplayMemberPath = "Nombre";
                PlataformaComboBox.SelectedValuePath = "Id";

                // Cargar clientes
                _clientes = await _supabase.ObtenerClientesAsync();
                ActualizarListaClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar datos: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ActualizarListaClientes()
        {
            ClienteComboBox.ItemsSource = _clientes;
            ClienteComboBox.DisplayMemberPath = "NombreCompleto";
            ClienteComboBox.SelectedValuePath = "Id";
        }

        // ===== PASO 1: CLIENTE =====
        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem != null)
            {
                _clienteNuevo = false;
                NuevoClientePanel.Visibility = Visibility.Collapsed;

                // Habilitar siguiente paso
                PlataformaComboBox.IsEnabled = true;
            }
        }

        private void NuevoClienteButton_Click(object sender, RoutedEventArgs e)
        {
            _clienteNuevo = true;
            ClienteComboBox.SelectedItem = null;
            NuevoClientePanel.Visibility = Visibility.Visible;

            // Habilitar siguiente paso
            PlataformaComboBox.IsEnabled = true;
        }

        // ===== PASO 2: PLATAFORMA =====
        private async void PlataformaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlataformaComboBox.SelectedItem is Plataforma plataforma)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // ✅ MOSTRAR PRECIO DE LA PLATAFORMA AUTOMÁTICAMENTE
                    PrecioTextBlock.Text = $"L {plataforma.PrecioBase:N2}";
                    PrecioTextBlock.Visibility = Visibility.Visible;

                    // Cargar cuentas de esta plataforma
                    var todasCuentas = await _supabase.ObtenerCuentasAsync();
                    _cuentas = todasCuentas.Where(c => c.PlataformaId == plataforma.Id).ToList();

                    // Cargar perfiles de estas cuentas
                    var todosPerfiles = await _supabase.ObtenerPerfilesAsync();
                    var cuentasIds = _cuentas.Select(c => c.Id).ToList();
                    _perfiles = todosPerfiles.Where(p => cuentasIds.Contains(p.CuentaId)).ToList();

                    ActualizarListaCuentas();

                    // Habilitar siguiente paso
                    CuentaComboBox.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ActualizarListaCuentas()
        {
            CuentaComboBox.ItemsSource = _cuentas;
            CuentaComboBox.DisplayMemberPath = "Email";
            CuentaComboBox.SelectedValuePath = "Id";
        }

        // ===== PASO 3: CUENTA =====
        private void CuentaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CuentaComboBox.SelectedItem is CuentaCorreo cuenta)
            {
                _cuentaNueva = false;
                NuevaCuentaPanel.Visibility = Visibility.Collapsed;

                // Cargar perfiles de esta cuenta
                var perfilesCuenta = _perfiles.Where(p => p.CuentaId == cuenta.Id).ToList();
                ActualizarListaPerfiles(perfilesCuenta);

                // Habilitar siguiente paso
                PerfilComboBox.IsEnabled = true;
            }
        }

        private void NuevaCuentaButton_Click(object sender, RoutedEventArgs e)
        {
            _cuentaNueva = true;
            CuentaComboBox.SelectedItem = null;
            NuevaCuentaPanel.Visibility = Visibility.Visible;

            // No hay perfiles aún, crear uno nuevo automáticamente
            _perfilNuevo = true;
            PerfilComboBox.IsEnabled = false;
            NuevoPerfilPanel.Visibility = Visibility.Visible;
        }

        private void ActualizarListaPerfiles(List<Perfil> perfiles)
        {
            // Filtrar solo perfiles disponibles
            var perfilesDisponibles = perfiles.Where(p => p.Estado == "disponible").ToList();

            PerfilComboBox.ItemsSource = perfilesDisponibles;
            PerfilComboBox.DisplayMemberPath = "NombrePerfil";
            PerfilComboBox.SelectedValuePath = "Id";

            // Mostrar mensaje si no hay perfiles disponibles
            if (!perfilesDisponibles.Any())
            {
                PerfilesAgotadosPanel.Visibility = Visibility.Visible;
                PerfilComboBox.IsEnabled = false;
            }
            else
            {
                PerfilesAgotadosPanel.Visibility = Visibility.Collapsed;
            }
        }

        // ===== PASO 4: PERFIL =====
        private void PerfilComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PerfilComboBox.SelectedItem != null)
            {
                _perfilNuevo = false;
                NuevoPerfilPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void NuevoPerfilButton_Click(object sender, RoutedEventArgs e)
        {
            _perfilNuevo = true;
            PerfilComboBox.SelectedItem = null;
            NuevoPerfilPanel.Visibility = Visibility.Visible;
        }

        private void CrearPerfilEnCuentaNuevaButton_Click(object sender, RoutedEventArgs e)
        {
            _perfilNuevo = true;
            NuevoPerfilPanel.Visibility = Visibility.Visible;
            PerfilesAgotadosPanel.Visibility = Visibility.Collapsed;
        }

        // ===== GUARDAR =====
        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos
                if (!ValidarDatos())
                    return;

                LoadingOverlay.Visibility = Visibility.Visible;

                Guid clienteId;
                Guid cuentaId;
                Guid perfilId;
                var plataforma = (Plataforma)PlataformaComboBox.SelectedItem;
                Guid plataformaId = plataforma.Id;

                // ============================================
                // PASO 1: Crear o usar cliente
                // ============================================
                if (_clienteNuevo)
                {
                    var cliente = new Cliente
                    {
                        NombreCompleto = NuevoClienteNombreTextBox.Text.Trim(),
                        Telefono = NuevoClienteTelefonoTextBox.Text.Trim(),
                        Estado = "activo"
                    };
                    var clienteCreado = await _supabase.CrearClienteAsync(cliente);
                    clienteId = clienteCreado.Id;
                }
                else
                {
                    clienteId = ((Cliente)ClienteComboBox.SelectedItem).Id;
                }

                // ============================================
                // PASO 2: Crear o usar cuenta
                // ============================================
                if (_cuentaNueva)
                {
                    var cuenta = new CuentaCorreo
                    {
                        PlataformaId = plataformaId,
                        Email = NuevaCuentaEmailTextBox.Text.Trim(),
                        Password = NuevaCuentaPasswordTextBox.Password.Trim(),
                        Estado = "activo"
                    };
                    var cuentaCreada = await _supabase.CrearCuentaAsync(cuenta);
                    cuentaId = cuentaCreada.Id;
                }
                else
                {
                    cuentaId = ((CuentaCorreo)CuentaComboBox.SelectedItem).Id;
                }

                // ============================================
                // PASO 3: Crear o usar perfil
                // ============================================
                if (_perfilNuevo)
                {
                    var perfil = new Perfil
                    {
                        CuentaId = cuentaId,
                        NombrePerfil = NuevoPerfilNombreTextBox.Text.Trim(),
                        Pin = NuevoPerfilPinTextBox.Text.Trim(),
                        Estado = "disponible" // ✅ CORRECCIÓN: Crear como disponible
                    };
                    var perfilCreado = await _supabase.CrearPerfilAsync(perfil);
                    perfilId = perfilCreado.Id;
                }
                else
                {
                    perfilId = ((Perfil)PerfilComboBox.SelectedItem).Id;
                    // ✅ NO marcar como ocupado manualmente - el trigger lo hará
                }

                // ============================================
                // PASO 4: Crear suscripción
                // ============================================
                // ✅ CORRECCIÓN: Usar precio de la plataforma
                var precio = plataforma.PrecioBase;
                var fechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.Today;
                var fechaProximoPago = fechaInicio.AddMonths(1);

                var suscripcion = new Suscripcion
                {
                    ClienteId = clienteId,
                    PerfilId = perfilId,
                    PlataformaId = plataformaId,
                    TipoSuscripcion = "perfil",
                    Precio = precio,
                    FechaInicio = DateOnly.FromDateTime(fechaInicio),
                    FechaProximoPago = DateOnly.FromDateTime(fechaProximoPago),
                    FechaLimitePago = DateOnly.FromDateTime(fechaProximoPago.AddDays(5)),
                    Estado = "activa"
                };

                await _supabase.CrearSuscripcionAsync(suscripcion);

                MessageBox.Show(
                    "✓ Suscripción creada exitosamente\n\n" +
                    $"Cliente: {(_clienteNuevo ? NuevoClienteNombreTextBox.Text : ((Cliente)ClienteComboBox.SelectedItem).NombreCompleto)}\n" +
                    $"Plataforma: {plataforma.Nombre}\n" +
                    $"Precio: L {precio:N2}\n" +
                    $"Próximo pago: {fechaProximoPago:dd/MM/yyyy}",
                    "Suscripción Creada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear suscripción: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private bool ValidarDatos()
        {
            // Validar cliente
            if (!_clienteNuevo && ClienteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un cliente o crear uno nuevo", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_clienteNuevo)
            {
                if (string.IsNullOrWhiteSpace(NuevoClienteNombreTextBox.Text))
                {
                    MessageBox.Show("El nombre del cliente es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Validar plataforma
            if (PlataformaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar una plataforma", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validar cuenta
            if (!_cuentaNueva && CuentaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar una cuenta o crear una nueva", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_cuentaNueva)
            {
                if (string.IsNullOrWhiteSpace(NuevaCuentaEmailTextBox.Text))
                {
                    MessageBox.Show("El email de la cuenta es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Validar perfil
            if (!_perfilNuevo && !_cuentaNueva && PerfilComboBox.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un perfil o crear uno nuevo", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_perfilNuevo)
            {
                if (string.IsNullOrWhiteSpace(NuevoPerfilNombreTextBox.Text))
                {
                    MessageBox.Show("El nombre del perfil es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}