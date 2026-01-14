using Microsoft.Extensions.DependencyInjection;
using StreamManager.Data.Models;
using StreamManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StreamManager.Views.Dialogs; // Esto corrige los errores de PlataformaDialog y ConfirmDialog

namespace StreamManager.Views.Pages
{
    public partial class PlataformasPage : Page
    {
        private readonly SupabaseService _supabase;
        private ObservableCollection<Plataforma> _plataformas;
        private List<Plataforma> _todasPlataformas;

        public PlataformasPage()
        {
            InitializeComponent();

            _supabase = App.ServiceProvider?.GetRequiredService<SupabaseService>()
                ?? throw new InvalidOperationException("SupabaseService no disponible");

            _plataformas = new ObservableCollection<Plataforma>();
            _todasPlataformas = new List<Plataforma>();

            PlataformasDataGrid.ItemsSource = _plataformas;

            Loaded += PlataformasPage_Loaded;
        }

        private async void PlataformasPage_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPlataformasAsync();
        }

        private async Task CargarPlataformasAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                var plataformas = await _supabase.ObtenerPlataformasAsync();
                _todasPlataformas = plataformas.OrderBy(p => p.Nombre).ToList();

                _plataformas.Clear();
                foreach (var plataforma in _todasPlataformas)
                {
                    _plataformas.Add(plataforma);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar plataformas: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PlataformaDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    var nuevaPlataforma = dialog.Plataforma;
                    nuevaPlataforma.FechaCreacion = DateTime.Now;

                    await _supabase.CrearPlataformaAsync(nuevaPlataforma);

                    MessageBox.Show(
                        "Plataforma creada exitosamente",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await CargarPlataformasAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al crear plataforma: {ex.Message}",
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
            if (sender is Button button && button.Tag is Plataforma plataforma)
            {
                var dialog = new PlataformaDialog(plataforma)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.ActualizarPlataformaAsync(dialog.Plataforma);

                        MessageBox.Show(
                            "Plataforma actualizada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPlataformasAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al actualizar plataforma: {ex.Message}",
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

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Plataforma plataforma)
            {
                var confirmDialog = new ConfirmDialog(
                    $"¿Estás seguro de eliminar la plataforma '{plataforma.Nombre}'?\n\n" +
                    "?? ADVERTENCIA: Esta acción eliminará:\n" +
                    "• Todas las cuentas asociadas\n" +
                    "• Todos los perfiles de esas cuentas\n" +
                    "• Todas las suscripciones relacionadas\n\n" +
                    "Esta acción NO se puede deshacer.",
                    "Eliminar Plataforma",
                    "Delete")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    try
                    {
                        LoadingOverlay.Visibility = Visibility.Visible;

                        await _supabase.EliminarPlataformaAsync(plataforma.Id);

                        MessageBox.Show(
                            "Plataforma eliminada exitosamente",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await CargarPlataformasAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al eliminar plataforma: {ex.Message}",
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
            await CargarPlataformasAsync();
        }

        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var busqueda = BuscarTextBox.Text.ToLower();

            var filtradas = string.IsNullOrWhiteSpace(busqueda)
                ? _todasPlataformas
                : _todasPlataformas.Where(p =>
                    p.Nombre.ToLower().Contains(busqueda) ||
                    p.Estado.ToLower().Contains(busqueda)).ToList();

            _plataformas.Clear();
            foreach (var plataforma in filtradas)
            {
                _plataformas.Add(plataforma);
            }
        }
    }
}
