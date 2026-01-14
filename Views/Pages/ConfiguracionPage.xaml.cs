using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Pages
{
    public partial class ConfiguracionPage : Page
    {
        public ConfiguracionPage()
        {
            InitializeComponent();

            CargarConfiguracion();
        }

        private void CargarConfiguracion()
        {
            // Aquí cargarías la configuración desde la base de datos o archivo de configuración
            // Por ahora, usamos valores por defecto

            UltimoRespaldoTextBlock.Text = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy HH:mm");
        }

        private void GuardarConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Aquí guardarías la configuración

                var nombreEmpresa = NombreEmpresaTextBox.Text;
                var moneda = (MonedaComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var idioma = (IdiomaComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var zonaHoraria = (ZonaHorariaComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                var diasAlerta = int.TryParse(DiasAlertaTextBox.Text, out int dias) ? dias : 3;

                var alertasEmail = AlertasEmailToggle.IsChecked ?? false;
                var alertasSistema = AlertasSistemaToggle.IsChecked ?? false;
                var alertasPagos = AlertasPagosToggle.IsChecked ?? false;
                var respaldoAutomatico = RespaldoAutomaticoToggle.IsChecked ?? false;

                // Guardar en Properties.Settings o en la base de datos
                Properties.Settings.Default.NombreEmpresa = nombreEmpresa;
                Properties.Settings.Default.Moneda = moneda ?? "HNL";
                Properties.Settings.Default.DiasAlerta = diasAlerta;
                Properties.Settings.Default.AlertasEmail = alertasEmail;
                Properties.Settings.Default.AlertasSistema = alertasSistema;
                Properties.Settings.Default.AlertasPagos = alertasPagos;
                Properties.Settings.Default.RespaldoAutomatico = respaldoAutomatico;
                Properties.Settings.Default.Save();

                MessageBox.Show(
                    "Configuración guardada exitosamente.\n\nAlgunos cambios pueden requerir reiniciar la aplicación.",
                    "Configuración Guardada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar configuración: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CrearRespaldoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Respaldo_StreamManager_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".bak",
                    Filter = "Archivo de Respaldo (.bak)|*.bak|Todos los archivos (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Aquí implementarías la lógica real de respaldo
                    // Por ahora, solo simulamos

                    MessageBox.Show(
                        $"Respaldo creado exitosamente en:\n{saveFileDialog.FileName}\n\n" +
                        "NOTA: Esta es una funcionalidad simulada.\n" +
                        "En producción, aquí se exportaría la base de datos completa.",
                        "Respaldo Creado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    UltimoRespaldoTextBlock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear respaldo: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RestaurarRespaldoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "⚠️ ADVERTENCIA ⚠️\n\n" +
                    "Restaurar un respaldo reemplazará TODOS los datos actuales.\n" +
                    "Esta acción NO se puede deshacer.\n\n" +
                    "¿Estás seguro de continuar?",
                    "Confirmar Restauración",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        DefaultExt = ".bak",
                        Filter = "Archivo de Respaldo (.bak)|*.bak|Todos los archivos (*.*)|*.*"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        // Aquí implementarías la lógica real de restauración

                        MessageBox.Show(
                            $"Respaldo restaurado desde:\n{openFileDialog.FileName}\n\n" +
                            "NOTA: Esta es una funcionalidad simulada.\n" +
                            "En producción, aquí se importaría la base de datos completa.\n\n" +
                            "Se recomienda reiniciar la aplicación.",
                            "Respaldo Restaurado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al restaurar respaldo: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
