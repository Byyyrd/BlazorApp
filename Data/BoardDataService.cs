using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.Text;

namespace BlazorStack.Data
{

    public class BoardDataService
    {
        private int squareSize = 100;
        public int SquareSize { get { return squareSize; } }
        private Square[] squares = new Square[64];
        private SqlConnection cnn;
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
        public void MovePiece(string Piece, int ID)
        {
            UpdateData(Piece, ID);
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
                Console.WriteLine(squares[i].ToString());
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
