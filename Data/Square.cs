namespace BlazorStack.Data
{
    public class Square
    {
        public Square(int x, int y, string squareColor)
        {
            X = x;
            Y = y;
            SquareColor = squareColor;
            Piece = "nn";
        }
        public Square()
        {
            SquareColor = "n";
            Piece = "nn";
        }
        public int X { get; set; }
        public int Y { get; set; }
        public string Piece { get; set; }
        public string SquareColor { get; set; }


        public override string ToString() => $"({X},{Y},{SquareColor},{Piece})";
    }
}
