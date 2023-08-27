using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.IO.Pipelines;
using System.Text;

namespace BlazorStack.Data
{

    public class BoardDataService
    {
        private int squareSize = 80;
        public int SquareSize { get { return squareSize; } }
        private Square[] squares = new Square[64];
        private SqlConnection cnn;
        private Square holdingSquare;
        public Square HoldingSquare { get { return holdingSquare; } }
        private Square targetSquare;
        private string connectionString = "Data Source = DESKTOP-UKI1896\\SQLEXPRESS; Integrated Security = True; Connect Timeout = 30; Encrypt = False; Trust Server Certificate = False; Application Intent = ReadWrite; Multi Subnet Failover = False";

        public Task<Square[]> GetBoardAsync()
        {
            ReadData();
            return Task.FromResult(squares);
        }
        public Task<Square[]> GetInitialBoardAsync()
        {
            Setup();
            ReadData();
            return Task.FromResult(squares);
        }
        public void ResetBoard()
        {
            Setup();
        }
        public void MovePiece(string From, string To)
        {
            MoveDataByString(From, To);
        }
        public void mouseDown(PointerEventArgs e)
        {
            int mouseX = (int)e.OffsetX;
            int mouseY = (int)e.OffsetY;
            foreach(Square square in squares)
            {
                if (holdingSquare == null)
                {
                    if (inRectangle(square.X, square.Y, squareSize, squareSize, mouseX, mouseY) && square.Piece != "nn" && !square.Piece.StartsWith("n") && !square.Piece.EndsWith("n"))
                    {
                        holdingSquare = square;
                    }
                }
                else
                {
                    if (inRectangle(square.X, square.Y, squareSize, squareSize, mouseX, mouseY))
                    {
                        targetSquare = square;
                        int holdingSquareIndex = holdingSquare.X / squareSize + (holdingSquare.Y / squareSize) * 8 + 1;
                        int targetSqareIndex = targetSquare.X / squareSize + (targetSquare.Y / squareSize) * 8 + 1;
                        if (IsViableMove(holdingSquare.Piece, holdingSquareIndex, targetSqareIndex) && holdingSquare != targetSquare && !targetSquare.Piece.StartsWith(holdingSquare.Piece[0]))
                        {
                            MoveDataByIndex(holdingSquareIndex, targetSqareIndex);
                        }
                        
                        holdingSquare = null;
                        targetSquare = null;
                        
                    }
                }
                
            }
        }
        private bool IsViableMove(string Piece,int indexFrom,int indexTo)
        {
            if (Piece.Contains("knight")) { return checkKnightMove(indexFrom,indexTo); }
            if (Piece.Contains("rook"))   { return checkRookMove(indexFrom, indexTo); }
            if (Piece.Contains("bishop")) { return checkBishopMove(indexFrom, indexTo); }
            if (Piece.Contains("queen"))  { return checkQueenMove(indexFrom, indexTo); }
            if (Piece.Contains("king"))   { return checkKingMove(indexFrom, indexTo); }
            if (Piece.Contains("bpawn"))  { return checkBlackPawnMove(indexFrom, indexTo); }
            if (Piece.Contains("wpawn"))  { return checkWhitePawnMove(indexFrom, indexTo); }

            return false;
        }
        private bool checkKnightMove(int from,int to)
        {
            int distance = Math.Abs(to - from);
            if (distance % 15 == 0 || distance % 17 == 0 || distance % 6 == 0 || distance % 10 == 0)
                return true;
            return false;
        }
        private bool checkRookMove(int from, int to)
        {
            //moves on same rank
            if (((from - 1) / 8) == ((to - 1) / 8))
                return true;
            //move on same file
            if (Math.Abs(to - from) % 8 == 0)
                return true;

            return false;
        }
        private bool checkBishopMove(int from,int to)
        {
            if (Math.Abs(from - to) % 7 == 0 || Math.Abs(from - to) % 9 == 0)
                return true;

            return false;
        }
        private bool checkQueenMove(int from, int to)
        {
            return (checkRookMove(from,to) || checkBishopMove(from,to));
        }
        private bool checkKingMove(int from, int to)
        {
            int distance = Math.Abs(to - from);
            if (distance == 1 || (distance > 6 && distance < 10))
                return true;
            return false;
        }
        private bool checkBlackPawnMove(int from,int to)
        {
            //normal moves
            if (to - from == 8 || (to - from == 16 && (from-1) / 8 == 1))
                return true;
            //capture a piece
            if(targetSquare.Piece.StartsWith("w") && (to - from == 7 || to - from == 9))
                return true;
            //en passant
            return false;
        }
        private bool checkWhitePawnMove(int from, int to)
        {
            //normal moves
            if (to - from == -8 || (to - from == -16 && (from - 1) / 8 == 6))
                return true;
            //capture a piece
            if (targetSquare.Piece.StartsWith("b") && (to - from == -7 || to - from == -9))
                return true;
            //en passant
            return false;
        }
        
        
        public void mouseMoved(PointerEventArgs e)
        {
            
        }
        public bool inRectangle(int x,int y,int width, int height,int pX, int pY)
        {
            return pX > x && pY > y && pX < x + width && pY < y + height;
        }
        public void mouseUp(PointerEventArgs e)
        {

        }
        private void Setup()
        {
            //Generate Squares(x,y,color) without Pieces
            int j = 0;
            for (int i = 0; i < 64; i++)
            {
                squares[i] = new Square();
                if ((i + j % 2) % 2 == 0)
                {
                    squares[i].SquareColor = "w";
                }
                else
                {
                    squares[i].SquareColor = "b";
                }
                squares[i].X = (i - j * 8) * squareSize;
                squares[i].Y = j * squareSize;
                if ((i + 1) % 8 == 0)
                {
                    j++;
                }
            }
            cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            //Reset Database to default Board
            StringBuilder sql = BuildBoardResetString();
            command = new SqlCommand(sql.ToString(), cnn);
            SqlDataReader reader = command.ExecuteReader();
            cnn.Close();
        }
        private void Fill()
        {
            cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            //Fill Database with DefaultValues
            StringBuilder sql = FillDatabaseStringBuilder();
            command = new SqlCommand(sql.ToString(), cnn);
            SqlDataReader reader = command.ExecuteReader();
            cnn.Close();
        }
        private void ReadData()
        {
            if (squares[0] == null)
            {
                Setup();
            }
            cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            string sql = "USE [julibank] SELECT * FROM Board";
            command = new SqlCommand(sql, cnn);
            SqlDataReader reader = command.ExecuteReader();
            
            int i = 0;
            while (reader.Read())
            {
                squares[i].Piece = reader.GetString(1) + reader.GetString(2);
                i++;
            }
            cnn.Close();
        }
        private int ChessPositionToIndex(string chessPoition)
        {
            int index = 0;
            char[] chars = chessPoition.ToCharArray();
            switch (chars[0])
            {
                case 'a': index = 1;break;
                case 'b': index = 2;break;
                case 'c': index = 3;break;
                case 'd': index = 4;break;
                case 'e': index = 5;break;
                case 'f': index = 6;break;
                case 'g': index = 7;break;
                case 'h': index = 8;break;
            }
            //make a8 a index of 1
            return 64 - ((int)Char.GetNumericValue(chars[1]) * 8) + index;
        }
        private void MoveDataByString(string from,string to)
        {
            int indexFrom = ChessPositionToIndex(from);
            int indexTo   = ChessPositionToIndex(to);

            MoveDataByIndex(indexFrom, indexTo);
            
        }
        public void MoveDataByIndex(int indexFrom, int indexTo)
        {
            string piece = "n";
            string color = "n";

            cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            String sql = $"USE [julibank] SELECT * FROM Board";
            command = new SqlCommand(sql, cnn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt32(0) == indexFrom)
                {
                    color = reader.GetString(1);
                    piece = reader.GetString(2);
                }
            }
            cnn.Close();
            if (!color.Equals("n") && !piece.Equals("n"))
            {
                cnn = new SqlConnection(connectionString);
                cnn.Open();
                sql = $"USE [julibank] UPDATE Board SET Color = '{color}',Piece = '{piece}' WHERE ID = {indexTo} UPDATE Board SET Color = 'n',Piece = 'n' WHERE ID = {indexFrom}";
                command = new SqlCommand(sql, cnn);
                reader = command.ExecuteReader();
                cnn.Close();
            }
        }
        private void UpdateData(string Piece, int ID)
        {
            cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            String sql = $"USE [julibank] UPDATE Board SET Piece = '{Piece}' WHERE ID = {ID}";
            command = new SqlCommand(sql, cnn);
            SqlDataReader reader = command.ExecuteReader();

            cnn.Close();
        }
        private StringBuilder BuildBoardResetString()
        {
            StringBuilder sqlCommandString = new StringBuilder();
            //set using database to julibank
            sqlCommandString.AppendLine("USE [julibank]");
            //Set Rooks to default poition
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'b' , Piece = 'rook' WHERE ID = 1 OR ID = 8");
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'w' , Piece = 'rook' WHERE ID = 57 OR ID = 64");
            //Set Knights to default position
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'b' , Piece = 'knight' WHERE ID = 2 OR ID = 7");
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'w' , Piece = 'knight' WHERE ID = 58 OR ID = 63");
            //Set Bishops to default position
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'b' , Piece = 'bishop' WHERE ID = 3 OR ID = 6");
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'w' , Piece = 'bishop' WHERE ID = 59 OR ID = 62");
            //Set Queens to default position
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'b' , Piece = 'queen' WHERE ID = 4");
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'w' , Piece = 'queen' WHERE ID = 60");
            //Set Kings to default position
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'b' , Piece = 'king' WHERE ID = 5");
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'w' , Piece = 'king' WHERE ID = 61");
            //Set Pawns to default position
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'b' , Piece = 'pawn' WHERE ID > 8 AND ID < 17");
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'w' , Piece = 'pawn' WHERE ID > 48 AND ID < 57");
            //Set remaining position to null
            sqlCommandString.AppendLine("UPDATE BOARD SET Color = 'n' , Piece = 'n' WHERE ID > 16 AND ID < 49");
            return sqlCommandString;
        }
        private StringBuilder FillDatabaseStringBuilder()
        {
            StringBuilder sql = new StringBuilder();
            //OLd BAd CODe
            sql.AppendLine("USE [julibank]");
            sql.AppendLine("DELETE FROM Board");
            sql.AppendLine("INSERT INTO Board ([color],[Piece]) VALUES   (N'b',N'rook'),   (N'b',N'knight'),   (N'b',N'bishop'),   (N'b',N'queen'),   (N'b',N'king'),(N'b',N'bishop'),(N'b',N'knight'),(N'b',N'rook')");
            for (int i = 9; i < 17; i++)
            {
                sql.AppendLine("INSERT INTO Board ([color],[Piece]) VALUES   (N'b',N'pawn')");
            }
            for (int i = 17; i < 49; i++)
            {
                sql.AppendLine("INSERT INTO Board ([color],[Piece]) VALUES   (N'n',N'n')");
            }
            for (int i = 49; i < 57; i++)
            {
                sql.AppendLine("INSERT INTO Board ([color],[Piece]) VALUES   (N'w',N'pawn')");
            }
            sql.AppendLine("INSERT INTO Board ([color],[Piece]) VALUES   (N'w',N'rook'),   (N'w',N'knight'),   (N'w',N'bishop'),   (N'w',N'queen'),   (N'w',N'king'),(N'w',N'bishop'),(N'w',N'knight'),(N'w',N'rook')");
            return sql;
        }
    }
}
