using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("auth_users")]
    public class Usuario : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("nombre_completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Column("rol")]
        public string Rol { get; set; } = "vendedor";

        [Column("estado")]
        public string Estado { get; set; } = "activo";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("fecha_ultimo_acceso")]
        public DateTime? FechaUltimoAcceso { get; set; }

        // Propiedades no mapeadas
        [JsonIgnore]
        public bool EsAdmin => Rol == "admin";

        [JsonIgnore]
        public bool EstaActivo => Estado == "activo";
    }
}
