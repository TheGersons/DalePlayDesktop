using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("suscripciones")]
    public class Suscripcion : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("cliente_id")]
        public Guid ClienteId { get; set; }

        [Column("perfil_id")]
        public Guid? PerfilId { get; set; }

        [Column("plataforma_id")]
        public Guid PlataformaId { get; set; }

        [Column("tipo_suscripcion")]
        public string TipoSuscripcion { get; set; } = "perfil";

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("fecha_inicio")]
        public DateOnly FechaInicio { get; set; }

        [Column("fecha_proximo_pago")]
        public DateOnly FechaProximoPago { get; set; }

        [Column("fecha_limite_pago")]
        public DateOnly FechaLimitePago { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "activa";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }
    }
}