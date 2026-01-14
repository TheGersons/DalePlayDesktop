using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("pagos")]
    public class Pago : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("suscripcion_id")]
        public Guid SuscripcionId { get; set; }

        [Column("cliente_id")]
        public Guid ClienteId { get; set; }

        [Column("monto")]
        public decimal Monto { get; set; }

        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [Column("metodo_pago")]
        public string MetodoPago { get; set; } = "efectivo";

        [Column("referencia")]
        public string? Referencia { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }

        [Column("registrado_por")]
        public Guid? RegistradoPor { get; set; }
    }
}