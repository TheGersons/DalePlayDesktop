using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("alertas")]
    public class Alerta : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("tipo_alerta")]
        public string TipoAlerta { get; set; } = string.Empty;

        [Column("tipo_entidad")]
        public string TipoEntidad { get; set; } = string.Empty;

        [Column("entidad_id")]
        public Guid EntidadId { get; set; }

        [Column("cliente_id")]
        public Guid? ClienteId { get; set; }

        [Column("plataforma_id")]
        public Guid? PlataformaId { get; set; }

        [Column("nivel")]
        public string Nivel { get; set; } = "normal";

        [Column("dias_restantes")]
        public int? DiasRestantes { get; set; }

        [Column("monto")]
        public decimal? Monto { get; set; }

        [Column("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Column("estado")]
        public string Estado { get; set; } = "pendiente";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("fecha_envio_email")]
        public DateTime? FechaEnvioEmail { get; set; }

        [Column("email_enviado_a")]
        public string? EmailEnviadoA { get; set; }
    }
}