namespace ScrapSystem.Models
{
    public class ComponenteRechazado
    {
        public int ComponenteRechazadoID { get; set; }
        public int RechazadoID { get; set; }
        public int MaterialID { get; set; }
        public int Cantidad { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }


        
    }
}
