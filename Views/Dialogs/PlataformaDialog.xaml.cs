using StreamManager.Data.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StreamManager.Views.Dialogs
{
    public partial class PlataformaDialog : Window
    {
        public Plataforma Plataforma { get; private set; }
        private bool _esEdicion;

        public PlataformaDialog(Plataforma? plataforma = null)
        {
            InitializeComponent();

            _esEdicion = plataforma != null;
            Plataforma = plataforma ?? new Plataforma();

            if (_esEdicion)
            {
                TituloTextBlock.Text = "Editar Plataforma";
                CargarDatos();
            }
            else
            {
                // Valores por defecto
                IconoComboBox.SelectedIndex = 0;
                EstadoComboBox.SelectedIndex = 0;
            }
        }

        private void CargarDatos()
        {
            NombreTextBox.Text = Plataforma.Nombre;
            
            // Seleccionar icono
            foreach (ComboBoxItem item in IconoComboBox.Items)
            {
                if (item.Tag?.ToString() == Plataforma.Icono)
                {
                    IconoComboBox.SelectedItem = item;
                    break;
                }
            }

            PrecioTextBox.Text = Plataforma.PrecioBase.ToString("F2");
            MaxPerfilesTextBox.Text = Plataforma.MaxPerfiles.ToString();
            ColorTextBox.Text = Plataforma.Color;
            
            // Seleccionar estado
            foreach (ComboBoxItem item in EstadoComboBox.Items)
            {
                if (item.Tag?.ToString() == Plataforma.Estado)
                {
                    EstadoComboBox.SelectedItem = item;
                    break;
                }
            }

            NotasTextBox.Text = Plataforma.Notas ?? string.Empty;
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                Plataforma.Nombre = NombreTextBox.Text.Trim();
                Plataforma.Icono = (IconoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Television";
                Plataforma.PrecioBase = decimal.Parse(PrecioTextBox.Text);
                Plataforma.MaxPerfiles = int.Parse(MaxPerfilesTextBox.Text);
                Plataforma.Color = ColorTextBox.Text.Trim();
                Plataforma.Estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "activa";
                Plataforma.Notas = string.IsNullOrWhiteSpace(NotasTextBox.Text) ? null : NotasTextBox.Text.Trim();

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
                MessageBox.Show("El nombre es obligatorio", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                NombreTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PrecioTextBox.Text) || !decimal.TryParse(PrecioTextBox.Text, out decimal precio) || precio < 0)
            {
                MessageBox.Show("Ingresa un precio válido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                PrecioTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(MaxPerfilesTextBox.Text) || !int.TryParse(MaxPerfilesTextBox.Text, out int maxPerfiles) || maxPerfiles < 1)
            {
                MessageBox.Show("Ingresa un número de perfiles válido (mínimo 1)", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxPerfilesTextBox.Focus();
                return false;
            }

            // Validar color hex
            var colorTexto = ColorTextBox.Text.Trim();
            if (!Regex.IsMatch(colorTexto, @"^#[0-9A-Fa-f]{6}$"))
            {
                MessageBox.Show("El color debe estar en formato #RRGGBB (ej: #2196F3)", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                ColorTextBox.Focus();
                return false;
            }

            return true;
        }

        private void NumeroTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir solo números y un punto decimal
            e.Handled = !IsTextAllowed(e.Text, allowDecimal: true);
        }

        private void NumeroEnteroTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir solo números enteros
            e.Handled = !IsTextAllowed(e.Text, allowDecimal: false);
        }

        private bool IsTextAllowed(string text, bool allowDecimal)
        {
            if (allowDecimal)
            {
                return Regex.IsMatch(text, @"^[0-9.]+$");
            }
            else
            {
                return Regex.IsMatch(text, @"^[0-9]+$");
            }
        }
    }
}
