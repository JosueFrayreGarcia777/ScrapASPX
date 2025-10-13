namespace ScrapSystem.Models
{
    public class ComponenteSeleccionado
    {
        public bool Seleccionado { get; set; }
        public int MaterialID { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public int Cantidad { get; set; } = 1;
    }
}
