using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("clientes")]
    public class Cliente : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("nombre_completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Column("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [Column("estado")]
        public string Estado { get; set; } = "activo";

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }
    }
}
