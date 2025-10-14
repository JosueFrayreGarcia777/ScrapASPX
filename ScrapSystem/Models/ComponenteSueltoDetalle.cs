namespace ScrapSystem.Models
{
    public class ComponenteSueltoDetalle
    {
        public int MaterialID { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}