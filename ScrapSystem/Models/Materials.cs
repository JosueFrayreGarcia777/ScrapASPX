namespace ScrapSystem.Models
{
    public class Materials
    {
        public int MaterialID { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Per { get; set; }
    }
}
