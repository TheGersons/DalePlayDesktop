using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("historial_pagos_plataforma")]
    public class HistorialPagoPlataforma : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("pago_plataforma_id")]
        public Guid PagoPlataformaId { get; set; }

        [Column("monto_pagado")]
        public decimal MontoPagado { get; set; }

        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [Column("metodo_pago")]
        public string? MetodoPago { get; set; }

        [Column("referencia")]
        public string? Referencia { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }

        [Column("registrado_por")]
        public Guid? RegistradoPor { get; set; }
    }
}