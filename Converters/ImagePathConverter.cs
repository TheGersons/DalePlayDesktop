using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace StreamManager.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    Debug.WriteLine($"[ImagePathConverter] Intentando cargar: {path}");

                    // Si empieza con /Assets/, es una imagen PNG
                    if (path.StartsWith("/Assets/"))
                    {
                        var uri = new Uri(path, UriKind.Relative);
                        var bitmap = new BitmapImage(uri);
                        Debug.WriteLine($"[ImagePathConverter] ✅ Imagen cargada: {path}");
                        return bitmap;
                    }

                    // Si no, retornar null (para que no se muestre nada)
                    Debug.WriteLine($"[ImagePathConverter] ⚠️ Ruta no válida: {path}");
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ImagePathConverter] ❌ Error cargando {path}: {ex.Message}");
                    return null;
                }
            }

            Debug.WriteLine("[ImagePathConverter] ⚠️ Valor null o vacío");
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}