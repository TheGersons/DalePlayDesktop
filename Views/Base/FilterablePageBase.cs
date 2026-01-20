using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace StreamManager.Views.Base
{
    /// <summary>
    /// Clase base para páginas con funcionalidad de filtrado avanzado.
    /// Proporciona métodos y propiedades comunes para implementar filtros de manera consistente.
    /// </summary>
    /// <typeparam name="T">El tipo de entidad que se va a filtrar</typeparam>
    public abstract class FilterablePageBase<T> : Page where T : class
    {
        // Colecciones para gestionar los datos
        protected ObservableCollection<T> _items;
        protected List<T> _todosItems;

        // Contador de resultados (si existe en el XAML)
        protected TextBlock? _contadorResultados;

        protected FilterablePageBase()
        {
            _items = new ObservableCollection<T>();
            _todosItems = new List<T>();
        }

        /// <summary>
        /// Método abstracto que cada página debe implementar para definir su lógica de filtrado específica
        /// </summary>
        /// <param name="item">El item a evaluar</param>
        /// <param name="filtros">Diccionario con los valores de los filtros activos</param>
        /// <returns>True si el item cumple con todos los filtros</returns>
        protected abstract bool CumpleFiltros(T item, Dictionary<string, object> filtros);

        /// <summary>
        /// Aplica los filtros a la lista completa de items
        /// </summary>
        /// <param name="filtros">Diccionario con los filtros activos</param>
        protected void AplicarFiltrosBase(Dictionary<string, object> filtros)
        {
            var itemsFiltrados = _todosItems.Where(item => CumpleFiltros(item, filtros)).ToList();

            _items.Clear();
            foreach (var item in itemsFiltrados)
            {
                _items.Add(item);
            }

            ActualizarContadorResultados(itemsFiltrados.Count);
        }

        /// <summary>
        /// Actualiza el contador de resultados si existe
        /// </summary>
        protected void ActualizarContadorResultados(int cantidad)
        {
            if (_contadorResultados != null)
            {
                _contadorResultados.Text = cantidad == 1
                    ? "1 resultado"
                    : $"{cantidad} resultados";
            }
        }

        /// <summary>
        /// Helper para verificar si un texto coincide con un filtro de búsqueda (case-insensitive)
        /// </summary>
        protected bool CoincideBusqueda(string? valorCampo, string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro)) return true;
            if (string.IsNullOrWhiteSpace(valorCampo)) return false;
            return valorCampo.ToLower().Contains(filtro.ToLower().Trim());
        }

        /// <summary>
        /// Helper para verificar si una fecha está dentro de un rango
        /// </summary>
        protected bool CoincideRangoFecha(DateTime fecha, DateTime? desde, DateTime? hasta)
        {
            var coincideDesde = !desde.HasValue || fecha >= desde.Value;
            var coincideHasta = !hasta.HasValue || fecha <= hasta.Value;
            return coincideDesde && coincideHasta;
        }

        /// <summary>
        /// Helper para verificar coincidencia exacta de estado/categoría
        /// </summary>
        protected bool CoincideEstado(string? valorCampo, string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro)) return true;
            if (string.IsNullOrWhiteSpace(valorCampo)) return false;
            return valorCampo.Trim().Equals(filtro.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Helper para crear un diccionario de filtros desde controles de UI
        /// </summary>
        protected Dictionary<string, object> CrearDiccionarioFiltros(params (string key, object? value)[] filtros)
        {
            var diccionario = new Dictionary<string, object>();
            
            foreach (var (key, value) in filtros)
            {
                if (value != null)
                {
                    diccionario[key] = value;
                }
            }
            
            return diccionario;
        }

        /// <summary>
        /// Helper para extraer valor de ComboBoxItem Tag
        /// </summary>
        protected string ObtenerTagComboBox(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
        }
    }
}
