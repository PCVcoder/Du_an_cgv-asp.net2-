namespace Du_an_CGV.Models.ViewModels
{
    public class SaveSeatMatrixDto
    {
        public int HallId { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public List<List<string>> Grid { get; set; } = new();
    }
}

