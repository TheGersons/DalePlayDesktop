using StreamManager.Data.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace StreamManager.Views.Dialogs
{
    public partial class ClienteDialog : Window
    {
        public Cliente Cliente { get; private set; }
        private bool _esEdicion;

        public ClienteDialog(Cliente? cliente = null)
        {
            InitializeComponent();

            _esEdicion = cliente != null;
            Cliente = cliente ?? new Cliente();

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Cliente";
                CargarDatos();
            }
            else
            {
                EstadoComboBox.SelectedIndex = 0;
            }
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = Cliente.NombreCompleto;
            TelefonoTextBox.Text = Cliente.Telefono ?? string.Empty;

            // Seleccionar estado
            foreach (ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == Cliente.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }

            NotasTextBox.Text = Cliente.Notas ?? string.Empty;
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                Cliente.NombreCompleto = NombreTextBox.Text.Trim();
                Cliente.Telefono = string.IsNullOrWhiteSpace(TelefonoTextBox.Text) ? "" : TelefonoTextBox.Text.Trim();
                Cliente.Estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "activo";
                Cliente.Notas = string.IsNullOrWhiteSpace(NotasTextBox.Text) ? null : NotasTextBox.Text.Trim();

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
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                MessageBox.Show("El nombre completo es obligatorio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                NombreTextBox.Focus();
                return false;
            }

            // Validar teléfono si está presente
            if (!string.IsNullOrWhiteSpace(TelefonoTextBox.Text))
            {
                var telefonoRegex = @"^[\d\s\-\+\(\)]+$";
                if (!Regex.IsMatch(TelefonoTextBox.Text.Trim(), telefonoRegex))
                {
                    MessageBox.Show("El teléfono solo puede contener números, espacios, guiones y paréntesis", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TelefonoTextBox.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}
