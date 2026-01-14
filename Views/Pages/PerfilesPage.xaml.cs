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
    public partial class PerfilesPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<PerfilViewModel> _perfiles;
        private List<PerfilViewModel> _todosPerfiles;

        public PerfilesPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _perfiles = new ObservableCollection<PerfilViewModel>();
            _todosPerfiles = new List<PerfilViewModel>();

            PerfilesDataGrid.ItemsSource = _perfiles;

            Loaded += PerfilesPage_Loaded;
        }

        private async void PerfilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPerfilesAsync();
        }

        private async Task CargarPerfilesAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Cargar datos relacionados
                var perfiles = await _supabase.ObtenerPerfilesAsync();
                var cuentas = await _supabase.ObtenerCuentasAsync();
                var plataformas = await _supabase.ObtenerPlataformasAsync();

                // Crear ViewModels
                _todosPerfiles = perfiles.Select(p =>
                {
                    var cuenta = cuentas.FirstOrDefault(c => c.Id == p.CuentaId);
                    var plataforma = cuenta != null ? plataformas.FirstOrDefault(pl => pl.Id == cuenta.PlataformaId) : null;

                    return new PerfilViewModel
                    {
                        Id = p.Id,
                        CuentaCorreoId = p.CuentaId,
                        CuentaNombre = cuenta?.Email ?? "Cuenta desconocida",
                        PlataformaNombre = plataforma?.Nombre ?? "Plataforma desconocida",
                        Nombre = p.NombrePerfil,
                        Pin = p.Pin ?? "Sin PIN",
                        Estado = p.Estado,
                        Perfil = p
                    };
                }).OrderBy(p => p.PlataformaNombre).ThenBy(p => p.Nombre).ToList();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar perfiles: {ex.Message}",
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
            var estadoFiltro = (EstadoFiltroComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            var filtrados = _todosPerfiles.Where(p =>
            {
                // Filtro de búsqueda
                var coincideBusqueda = string.IsNullOrWhiteSpace(busqueda) ||
                    p.Nombre.ToLower().Contains(busqueda) ||
                    p.PlataformaNombre.ToLower().Contains(busqueda) ||
                    p.CuentaNombre.ToLower().Contains(busqueda);

                // Filtro de estado
                var coincideEstado = string.IsNullOrWhiteSpace(estadoFiltro) ||
                    p.Estado.ToLower() == estadoFiltro;

                return coincideBusqueda && coincideEstado;
            }).ToList();

            _perfiles.Clear();
            foreach (var perfil in filtrados)
            {
                _perfiles.Add(perfil);
            }
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PerfilDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevoPerfil = dialog.Perfil;
                    await _supabase.CrearPerfilAsync(nuevoPerfil);

                    MessageBox.Show(
                        "Perfil creado exitosamente",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await CargarPerfilesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al crear perfil: {ex.Message}",
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

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PerfilViewModel viewModel)
            {
                var dialog = new PerfilDialog(viewModel.Perfil)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.ActualizarPerfilAsync(dialog.Perfil);

                        MessageBox.Show(
                            "Perfil actualizado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPerfilesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar perfil: {ex.Message}",
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

        private void CopiarPinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PerfilViewModel viewModel)
            {
                if (viewModel.Pin == "Sin PIN")
                {
                    MessageBox.Show(
                        "Este perfil no tiene PIN",
                        "Información",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                try
                {
                    Clipboard.SetText(viewModel.Pin);
                    MessageBox.Show(
                        "PIN copiado al portapapeles",
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
            if (sender is Button button && button.Tag is PerfilViewModel viewModel)
            {
                var confirmDialog = new ConfirmDialog(
                    $"¿Estás seguro de eliminar el perfil '{viewModel.Nombre}'?\n\n" +
                    $"• Plataforma: {viewModel.PlataformaNombre}\n" +
                    $"• Estado: {viewModel.Estado}\n\n" +
                    "Esta acción NO se puede deshacer.",
                    "Eliminar Perfil",
                    "Delete")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarPerfilAsync(viewModel.Id);

                        MessageBox.Show(
                            "Perfil eliminado exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPerfilesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al eliminar perfil: {ex.Message}",
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
            await CargarPerfilesAsync();
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
