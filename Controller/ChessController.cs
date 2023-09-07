using BlazorStack.Data;
using Microsoft.AspNetCore.Components.Web;
using System.Collections;
using System.Drawing;

namespace BlazorStack.Controller
{
    public class ChessController
    {
        public Square? holdingSquare;
        private Square? targetSquare;
        public Square[] Squares = new Square[64];
        private bool canRochadeW,canRochadeB;
        public BoardDataService Ds { get; set; }
        public bool BlackInCheck;
        public bool WhiteInCheck;
        private int lastDoublePawnMoveTo;

        private List<Piece> wCheckingPieces = new();
        private List<Piece> bCheckingPieces = new();
        private struct Piece
        {
            public Piece(string pName,int pPosition)
            {
                Name = pName;
                Position = pPosition;
            }
            public string Name { get; set; }
            public int Position { get; set; }
        }

        public ChessController(BoardDataService boardDataService)
        {
            Ds = boardDataService;
        }
        public bool IsCheckmate(string Color)
        {
            //If not in Check cannot be checkmate
            if (!IsInCheck(Color))
                return false;

            //List moves around king and find king
            int[] KingMoves = { -9,-8,-7,-1,1,7,8,9};
            int KingIndex = FindKingIndex(Color);
            //Check if King can escape
            foreach (int moves in KingMoves)
            {
                if ((KingIndex + moves) < 64 && (KingIndex + moves) >= 0 && !Squares[KingIndex + moves -1].Piece.StartsWith(Color) && IsViableMove($"{Color}king",KingIndex, KingIndex + moves))
                {
                    return false;
                }
            }
            IsInCheck(Color);
            List<Piece> CheckingPieces = Color == "w" ? wCheckingPieces : bCheckingPieces;
            List<Piece> MovesToCheckingPieces = ViableMovesToPosition(Color, CheckingPieces.First().Position);
            //Check if piece Can Caputre Checking Piece
            if (CheckingPieces.Count == 1 && MovesToCheckingPieces.Count != 0)
            {
                return false;
            }
            //Check if piece can Move in between
            if(CheckingPieces.Count > 0)
            {
                foreach (Piece piece in CheckingPieces)
                {
                    if (piece.Name.Contains("king"))   { Console.WriteLine("King shouldnt be able to be here"); }
                    if (piece.Name.Contains("bpawn"))  { return true; }
                    if (piece.Name.Contains("wpawn"))  { return true; }
                    if (piece.Name.Contains("knight")) { return true; }
                    if (piece.Name.Contains("rook"))   { return !CanMoveBetweenRookAndKing(Color,piece); }
                    if (piece.Name.Contains("bishop")) { return !CanMoveBetweenBishopAndKing(Color,piece); }
                    if (piece.Name.Contains("queen"))  { return !CanMoveBetweenQueenAndKing(Color,piece); }
                    
                }
            }


            //if all posibilites to prevent check are not possible checkmate is true
            return true;
        }
        private bool CanMoveBetweenRookAndKing(string Color,Piece piece)
        {
            int kingIndex = FindKingIndex(Color);
            int position = piece.Position;
            int Rank = IndexToRank(position);
            int File = IndexToFile(position);
            int kingRank = IndexToRank(kingIndex);
            int kingFile = IndexToFile(kingIndex);

            if (File == kingFile)
            {
                for (int i = 1; i < Math.Abs(kingRank - Rank); i++)
                {
                    if (kingRank < Rank)
                    {
                        i = -i;
                    }
                    if (ViableMovesToPosition(Color, File + Rank * 8 + i * 8).Count != 0)
                    {
                        return true;
                    }
                    i = Math.Abs(i);
                }
            }
            if (Rank == kingRank)
            {
                for (int i = 1; i < Math.Abs(kingFile - File); i++)
                {
                    if (kingFile < File)
                    {
                        i = -i;
                    }
                    if (ViableMovesToPosition(Color, File + Rank * 8 + i).Count != 0)
                    {
                        return true;
                    }
                    i = Math.Abs(i);
                }
            }

            return false;
        }
        private bool CanMoveBetweenBishopAndKing(string Color,Piece piece)
        {
            return false;
        }
        private bool CanMoveBetweenQueenAndKing(string Color,Piece piece)
        {
            return CanMoveBetweenBishopAndKing(Color,piece) || CanMoveBetweenRookAndKing(Color,piece);
        }


        public bool IsViableMove(string Piece)
        {
            if (holdingSquare == null || targetSquare == null)
                return false;
            int indexFrom = holdingSquare.Index();
            int indexTo = targetSquare.Index();
            return IsViableMove(Piece,indexFrom,indexTo);
        }
        public bool IsViableMove(string Piece, int indexFrom, int indexTo)
        {
            //Get Color from piece
            string Color = Piece[0].ToString();

            CheckRochade(Squares);

            //Save wether each player could rochade
            bool couldRochadeB = canRochadeB;
            bool couldRochadeW = canRochadeW;
            
            bool viableMove;


            //If Move is viable with check Rules or Player not in check, check wether move is inside piece move rules
            //If Player is in Check Rochade is not viable
            viableMove = IsViableMoveByIndex(Piece, indexFrom, indexTo);
            if (viableMove && (indexTo != lastDoublePawnMoveTo || !(IndexToRank(indexFrom) == 1 || IndexToRank(indexFrom) == 6)))
                lastDoublePawnMoveTo = -1;

            //If move is theoreticly viable check if player in Check and the Move wouldn't move Out or between checking piece reach
            if (viableMove && IsInCheck(Color) && !MovePreventsCheck(Color, indexFrom, indexTo))
            {
                viableMove = false;
            }
            canRochadeB = couldRochadeB;
            canRochadeW = couldRochadeW;
            return viableMove;

        }
        /// <summary> Method <c>MovePreventsCheck</c> Checks if a move prevents Check</summary>
        /// <returns> A bool representing wether Move prevents Check</returns>
        private bool MovePreventsCheck(string Color, int indexFrom, int indexTo)
        {
            //Remember the piece that is on the targeted square so you can put it back after simulating the Move
            string toPiece = Squares[indexTo - 1].Piece;
            //Exclude Rochade as a viable move when in Check
            canRochadeB = false;
            canRochadeW = false;
            //Simulate making the move and check wether the Piece is in check after Move(preventsCheck)
            Squares[indexTo - 1].Piece = Squares[indexFrom - 1].Piece;
            Squares[indexFrom - 1].Piece = "nn";
            bool preventsCheck = !IsInCheck(Color);
            Squares[indexFrom - 1].Piece = Squares[indexTo - 1].Piece;
            Squares[indexTo - 1].Piece = toPiece;

            return preventsCheck;
        }
        private bool IsViableMoveByIndex(string Piece, int indexFrom,int indexTo)
        {
            //Checks which piece the given one is and returns if given Move is viable for it
            if (Piece.Contains("knight")) { return CheckKnightMove(indexFrom, indexTo); }
            if (Piece.Contains("rook"))   { return CheckRookMove(Piece[0].ToString(), indexFrom, indexTo); }
            if (Piece.Contains("bishop")) { return CheckBishopMove(Piece[0].ToString(), indexFrom, indexTo); }
            if (Piece.Contains("queen"))  { return CheckQueenMove(Piece[0].ToString(), indexFrom, indexTo); }
            if (Piece.Contains("king"))   { return CheckKingMove(Piece[0].ToString(),indexFrom, indexTo); }
            if (Piece.Contains("bpawn"))  { return CheckBlackPawnMove(indexFrom, indexTo); }
            if (Piece.Contains("wpawn"))  { return CheckWhitePawnMove(indexFrom, indexTo); }
            return false;
        }
        /// <summary>
        /// King of given <paramref name="Color"/> in Check 
        /// </summary>
        /// <param name="Color">Color for which to search for check</param>
        /// <returns>Is King of <paramref name="Color"/> in Check</returns>
        public bool IsInCheck(string Color)
        {
            if (Color == "n") return false;
            string enemyColor = Color == "b" ? "w" : "b";
            int kingPosition = FindKingIndex(Color);
            if (Color == "w")
            {
                wCheckingPieces = ViableMovesToPosition(enemyColor, kingPosition);
                return wCheckingPieces.Count != 0;

            }
            bCheckingPieces = ViableMovesToPosition(enemyColor, kingPosition);
            return bCheckingPieces.Count != 0;
        }
        /// <summary>
        /// Which Pieces of given <paramref name="Color"/> can move to <paramref name="Position"/>
        /// </summary>
        /// <param name="Color"> Color for moving Pieces </param>
        /// <param name="Position"> Position to move to </param>
        /// <returns>List of all Pieces that can move to position</returns>
        private List<Piece> ViableMovesToPosition(string Color,int Position)
        {
            List<Piece> pieces = new();
            if (Position < 1 || Position > 64)
                return pieces;
            foreach (Square square in Squares)
            {
                if (square.Piece.StartsWith(Color) && IsViableMoveByIndex(square.Piece, square.Index(), Position))
                {
                    pieces.Add(new Piece( square.Piece , square.Index()));
                }
            }
            return pieces;
        }
        private int FindKingIndex(string Color)
        {
            foreach (Square square in Squares)
            {
                if (square.Piece.Contains($"{Color}king"))
                {
                    return square.Index();
                }
            }
            return -1;
        }
        private void CheckRochade(Square[] squares)
        {
            //Check if black Rook king or Rook has moved if so rochde is forever imposible
            if (!(squares[0].Piece.Equals("brook") && squares[7].Piece.Equals("brook") && squares[4].Piece.Equals("bking")))
            {
                canRochadeB = false;
            }
            if (!(squares[56].Piece.Equals("wrook") && squares[63].Piece.Equals("wrook") && squares[60].Piece.Equals("wking")))
            {
                canRochadeW = false;
            }
        }
        private bool CheckKnightMove(int from, int to)
        {
            int distance = Math.Abs(to - from);
            if (distance == 15 || distance == 17 || distance == 6 || distance == 10)
                return true;
            return false;
        }
        private bool CheckRookMove(string Color,int from, int to)
        {
            //moves on same rank
            if (IndexToRank(from) == IndexToRank(to) && !IsInterceptedByPieceOnRank(Color, from, to))
                return true;
            
            //move on same file
            if (Math.Abs(to - from) % 8 == 0 && !IsInterceptedByPieceOnFile(Color, from,to))
                return true;

            return false;
        }
        private int IndexToRank(int index)
        {
            return (index - 1) / 8;

        }
        private int IndexToFile(int index)
        {
            return index - (IndexToRank(index) * 8);
        }
        private bool IsInterceptedByPieceOnRank(string Color,int from,int to)
        {
            int Rank = IndexToRank(from);
            foreach (var square in Squares)
            {
                if(square.Rank() == Rank && !square.Equals(holdingSquare))
                {
                    if (square.Piece.StartsWith(Color) && ((to >= square.Index() && from < square.Index()) || (to <= square.Index() && from > square.Index())))
                    {
                        return true;
                    }
                    else if (!square.Piece.StartsWith("n") && ((to > square.Index() && from < square.Index()) || (to < square.Index() && from > square.Index())))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool IsInterceptedByPieceOnFile(string Color,int from, int to)
        {
            int toRank = IndexToRank(to);
            int fromRank = IndexToRank(from);
            foreach (var square in Squares)
            {
                if (square.File() == IndexToFile(from) && !square.Equals(holdingSquare))
                {
                    if (square.Piece.StartsWith(Color) && ((toRank >= square.Rank() && fromRank < square.Rank()) || (toRank <= square.Rank() && fromRank > square.Rank())))
                    {
                        return true;
                    }
                    else if (!square.Piece.StartsWith("n") && ((toRank > square.Rank() && fromRank < square.Rank()) || (toRank < square.Rank() && fromRank > square.Rank())))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool CheckBishopMove(string Color,int from, int to)
        {
            int distance = Math.Abs(from - to);
            if (distance % 7 == 0 && !InterceptedByPieceOnDiagonall(Color, from,to,7))
                return true;
            if (distance % 9 == 0 && !InterceptedByPieceOnDiagonall(Color, from,to,9))
                return true;
            
            return false;
        }
        private bool InterceptedByPieceOnDiagonall(string Color,int from,int to,int requiredDistance)
        {
            foreach (Square square in Squares)
            {
                if(Math.Abs(square.Index() - from) % requiredDistance == 0 && !square.Equals(holdingSquare))
                {
                    if (square.Piece.StartsWith(Color) && ((to >= square.Index() && from < square.Index()) || (to <= square.Index() && from > square.Index())))
                    {
                        return true;
                    }
                    else if (!square.Piece.StartsWith("n") && ((to > square.Index() && from < square.Index()) || (to < square.Index() && from > square.Index())))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        private bool CheckQueenMove(string Color,int from, int to)
        {
            return (CheckRookMove(Color,from, to) || CheckBishopMove(Color,from, to));
        }
        private bool CheckKingMove(string Color,int from, int to)
        {
            //cannot move into Check
            string toPiece = Squares[to - 1].Piece;
            Squares[to - 1].Piece = Squares[from - 1].Piece;
            Squares[from - 1].Piece = "nn";
            if (IsInCheck(Color))
            {
                Squares[from - 1].Piece = Squares[to - 1].Piece;
                Squares[to - 1].Piece = toPiece;
                return false;
            }
            Squares[from - 1].Piece = Squares[to - 1].Piece;
            Squares[to - 1].Piece = toPiece;

            //Normal moving
            int distance = Math.Abs(to - from);
            if (distance == 1 || (distance > 6 && distance < 10))
                return true;
            //Rochade
            if(canRochadeW && from == 61)
            {
                if (to == 63 && Squares[61].Piece.Equals("nn"))
                {
                    Ds.MovePieceByIndex(64, 62);
                    return true;

                }
                else if(to == 59 && Squares[59].Piece.Equals("nn") && Squares[57].Piece.Equals("nn"))
                {
                    Ds.MovePieceByIndex(57, 60);
                    return true;

                }
            }
            if (canRochadeB && from == 5)
            {
                if (to == 7 && Squares[5].Piece.Equals("nn"))
                {
                    Ds.MovePieceByIndex(8, 6);
                    return true;
                }
                else if(to == 3 && Squares[1].Piece.Equals("nn") && Squares[3].Piece.Equals("nn"))
                {
                    Ds.MovePieceByIndex(1, 4);
                    return true;
                }
                
            }
            return false;
        }
        private bool CheckBlackPawnMove(int from, int to)
        {
            //normal moves
            if (to - from == 8 && IsSquareFree(from + 8))
                return true;
            //capture a piece
            if (Squares[to - 1].Piece.StartsWith("w") && (Math.Abs(IndexToFile(from) - IndexToFile(to)) == 1) && IndexToRank(from) - IndexToRank(to) == -1)
                return true;
            //Double move from starting pos
            if (to - from == 16 && IndexToRank(from) == 1 && IsSquareFree(from + 8) && Squares[to - 1].Piece.StartsWith("n"))
            {
                lastDoublePawnMoveTo = to;
                return true;
            }
            //en passant
            if ((lastDoublePawnMoveTo == from - 1 && to == from + 7 && !Squares[from - 2].Piece.StartsWith("b")))
            {
                Ds.RemovePiece(from - 1);
                return true;
            }
            if(lastDoublePawnMoveTo == from + 1 && to == from + 9 && !Squares[from].Piece.StartsWith("b"))
            {
                Ds.RemovePiece(from + 1);
                return true;
            }


            return false;
        }
        private bool IsSquareFree(int index)
        {
            index--;
            return (Squares[index].Piece == "nn" || Squares[index].Piece.StartsWith("n"));
        }
        private bool CheckWhitePawnMove(int from, int to)
        {
            //normal moves
            if (to - from == -8 && IsSquareFree(from - 8))
                return true;
            //capture a piece
            if (Squares[to - 1].Piece.StartsWith("b")  && (Math.Abs(IndexToFile(from) - IndexToFile(to)) == 1) && IndexToRank(from) - IndexToRank(to) == 1)
                return true;
            //Double move from starting pos
            if(to - from == -16 && IndexToRank(from) == 6 && IsSquareFree(from - 8) && Squares[to - 1].Piece.StartsWith("n")){
                lastDoublePawnMoveTo = to;
                return true;
            }
            //en passant
            if ((lastDoublePawnMoveTo == from - 1 && to == from - 9 && !Squares[from - 2].Piece.StartsWith("w")))
            {
                Ds.RemovePiece(from - 1);
                return true;
            } 
                
            if(lastDoublePawnMoveTo == from + 1 && to == from - 7 && !Squares[from].Piece.StartsWith("w")) {
                Ds.RemovePiece(from + 1);
                return true;
            }

            return false;
        }
        private bool inRectangle(int x, int y, int width, int height, int pX, int pY)
        {
            return pX > x && pY > y && pX < x + width && pY < y + height;
        }
        public void mouseDown(PointerEventArgs e)
        {
            int squareSize = BoardDataService.squareSize; 
            int mouseX = (int)e.OffsetX;
            int mouseY = (int)e.OffsetY;
            foreach (Square square in Squares)
            {
                if (holdingSquare == null)
                {
                    if (inRectangle(square.X, square.Y, squareSize, squareSize, mouseX, mouseY) && square.Piece != "nn" && !square.Piece.StartsWith("n"))
                    {
                        holdingSquare = square;
                    }
                }
                else
                {
                    if (inRectangle(square.X, square.Y, squareSize, squareSize, mouseX, mouseY))
                    {
                        targetSquare = square;
                        int holdingSquareIndex = holdingSquare.Index();
                        int targetSqareIndex = targetSquare.Index();
                        if (holdingSquare != targetSquare && !targetSquare.Piece.StartsWith(holdingSquare.Piece[0]) && IsViableMove(holdingSquare.Piece))
                        {
                            Ds.MovePieceByIndex(holdingSquareIndex, targetSqareIndex);
                        }

                        holdingSquare = null;
                        targetSquare = null;

                    }
                }

            }
        }
        public void Reset()
        {
            canRochadeB = true;
            canRochadeW = true;
        }
    }
}
