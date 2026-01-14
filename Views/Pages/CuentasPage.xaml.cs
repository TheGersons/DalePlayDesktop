using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using StreamManager.ViewModels;
using StreamManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Pages
{
    public partial class CuentasPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<CuentaViewModel> _cuentas;
        private List<CuentaViewModel> _todasCuentas;
        private List<Plataforma> _plataformas;

        public CuentasPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _cuentas = new ObservableCollection<CuentaViewModel>();
            _todasCuentas = new List<CuentaViewModel>();
            _plataformas = new List<Plataforma>();

            CuentasDataGrid.ItemsSource = _cuentas;

            Loaded += CuentasPage_Loaded;
        }

        private async void CuentasPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarCuentasAsync();
        }

        private async Task CargarCuentasAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Cargar datos relacionados
                var cuentas = await _supabase.ObtenerCuentasAsync();
                _plataformas = await _supabase.ObtenerPlataformasAsync();
                var perfiles = await _supabase.ObtenerPerfilesAsync();

                // Crear ViewModels
                _todasCuentas = cuentas.Select(c =>
                {
                    var plataforma = _plataformas.FirstOrDefault(p => p.Id == c.PlataformaId);
                    var perfilesDisponibles = perfiles.Count(p => p.CuentaId == c.Id && p.Estado == "disponible");

                    return new CuentaViewModel
                    {
                        Id = c.Id,
                        PlataformaId = c.PlataformaId,
                        PlataformaNombre = plataforma?.Nombre ?? "Desconocida",
                        CorreoElectronico = c.Email,
                        Contraseña = c.Password,
                        PerfilesDisponibles = perfilesDisponibles,
                        FechaCreacion = c.FechaCreacion,
                        Estado = c.Estado,
                        Notas = c.Notas,
                        CuentaCorreo = c
                    };
                }).OrderByDescending(c => c.FechaCreacion).ToList();

                // Cargar filtro de plataformas
                PlataformaFiltroComboBox.Items.Clear();
                PlataformaFiltroComboBox.Items.Add(new { Id = Guid.Empty, Nombre = "Todas las plataformas" });
                foreach (var plataforma in _plataformas.OrderBy(p => p.Nombre))
                {
                    PlataformaFiltroComboBox.Items.Add(plataforma);
                }
                PlataformaFiltroComboBox.SelectedIndex = 0;

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar cuentas: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void AplicarFiltros()
        {
            var busqueda = BuscarTextBox.Text.ToLower();
            
            // Obtener plataforma seleccionada
            Guid plataformaId = Guid.Empty;
            if (PlataformaFiltroComboBox.SelectedItem != null)
            {
                var selectedItem = PlataformaFiltroComboBox.SelectedItem;
                var idProp = selectedItem.GetType().GetProperty("Id");
                if (idProp != null)
                {
                    plataformaId = (Guid)idProp.GetValue(selectedItem)!;
                }
            }

            var filtradas = _todasCuentas.Where(c =>
            {
                // Filtro de búsqueda
                var coincideBusqueda = string.IsNullOrWhiteSpace(busqueda) ||
                    c.CorreoElectronico.ToLower().Contains(busqueda) ||
                    c.PlataformaNombre.ToLower().Contains(busqueda);

                // Filtro de plataforma
                var coincidePlataforma = plataformaId == Guid.Empty || c.PlataformaId == plataformaId;

                return coincideBusqueda && coincidePlataforma;
            }).ToList();

            _cuentas.Clear();
            foreach (var cuenta in filtradas)
            {
                _cuentas.Add(cuenta);
            }
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CuentaDialog { Owner = Window.GetWindow(this) };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevaCuenta = dialog.CuentaCorreo;

                    // 1. Asegurar formato de fecha para Postgres
                    nuevaCuenta.FechaCreacion = DateTime.UtcNow;

                    // 2. Asegurar que el estado coincida con el CHECK del SQL ('activo')
                    nuevaCuenta.Estado = "activo";

                    // 3. Insertar la cuenta
                    await _supabase.CrearCuentaAsync(nuevaCuenta);

                    MessageBox.Show("Cuenta creada exitosamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CargarCuentasAsync();
                }
                catch (Exception ex)
                {
                    // Aquí verás si el error es 'violates foreign key constraint'
                    MessageBox.Show($"Error al crear cuenta: {ex.Message}", "Error de Relación");
                }
                finally { LoadingOverlay.Visibility = Visibility.Collapsed; }
            }
        }

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                var dialog = new CuentaDialog(viewModel.CuentaCorreo)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.ActualizarCuentaAsync(dialog.CuentaCorreo);

                        MessageBox.Show(
                            "Cuenta actualizada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarCuentasAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar cuenta: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void VerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                var dialog = new CuentaDetalleDialog(viewModel)
                {
                    Owner = Window.GetWindow(this)
                };

                dialog.ShowDialog();
            }
        }

        private void CopiarPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                try
                {
                    Clipboard.SetText(viewModel.Contraseña);
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

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuentaViewModel viewModel)
            {
                var confirmDialog = new ConfirmDialog(
                    $"¿Estás seguro de eliminar la cuenta '{viewModel.CorreoElectronico}'?\n\n" +
                    "⚠️ ADVERTENCIA: Esta acción eliminará:\n" +
                    $"• Todos los perfiles asociados ({viewModel.PerfilesDisponibles} disponibles)\n" +
                    "• Todas las suscripciones relacionadas\n\n" +
                    "Esta acción NO se puede deshacer.",
                    "Eliminar Cuenta",
                    "Delete")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarCuentaAsync(viewModel.Id);

                        MessageBox.Show(
                            "Cuenta eliminada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarCuentasAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al eliminar cuenta: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async void RefrescarButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarCuentasAsync();
        }

        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
            }
        }
    }
}
