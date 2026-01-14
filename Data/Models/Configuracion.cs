using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("configuracion")]
    public class Configuracion : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("clave")]
        public string Clave { get; set; } = string.Empty;

        [Column("valor")]
        public string Valor { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("tipo_dato")]
        public string TipoDato { get; set; } = "string";

        [Column("categoria")]
        public string Categoria { get; set; } = "general";

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; }
    }
}
