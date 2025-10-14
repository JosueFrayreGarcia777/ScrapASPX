namespace ScrapSystem.Models
{
    public class ComponenteRechazadoDetalle
    {
        public int MaterialID { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}