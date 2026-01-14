using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("plataformas")]
    public class Plataforma : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("icono")]
        public string Icono { get; set; } = "Television";

        [Column("precio_base")]
        public decimal PrecioBase { get; set; }

        [Column("max_perfiles")]
        public int MaxPerfiles { get; set; } = 4;

        [Column("color")]
        public string Color { get; set; } = "#2196F3";

        [Column("estado")]
        public string Estado { get; set; } = "activa";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }
    }
}
