using BlazorStack.Data;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorStack.Controller
{
    public class ChessController
    {
        public Square? holdingSquare;
        private Square? targetSquare;
        public Square[] Squares = new Square[64];
        private bool canRochadeW,canRochadeB;
        public BoardDataService Ds { get; set; }
        public ChessController(BoardDataService boardDataService)
        {
            Ds = boardDataService;
        }
        public bool IsViableMove(string Piece)
        {
            CheckRochade(Squares);
            if (holdingSquare != null && targetSquare != null)
            {
                int indexFrom = holdingSquare.X / BoardDataService.squareSize + (holdingSquare.Y / BoardDataService.squareSize) * 8 + 1;
                int indexTo = targetSquare.X / BoardDataService.squareSize + (targetSquare.Y / BoardDataService.squareSize) * 8 + 1;
                if (Piece.Contains("knight")) { return CheckKnightMove(indexFrom, indexTo); }
                if (Piece.Contains("rook")) { return CheckRookMove(indexFrom, indexTo); }
                if (Piece.Contains("bishop")) { return CheckBishopMove(indexFrom, indexTo); }
                if (Piece.Contains("queen")) { return CheckQueenMove(indexFrom, indexTo); }
                if (Piece.Contains("king")) { return CheckKingMove(indexFrom, indexTo); }
                if (Piece.Contains("bpawn")) { return CheckBlackPawnMove(indexFrom, indexTo); }
                if (Piece.Contains("wpawn")) { return CheckWhitePawnMove(indexFrom, indexTo); }
            }

            return false;
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
            if (distance % 15 == 0 || distance % 17 == 0 || distance % 6 == 0 || distance % 10 == 0)
                return true;
            return false;
        }
        private bool CheckRookMove(int from, int to)
        {
            //moves on same rank
            if (IndexToRank(from) == IndexToRank(to) && !IsInterceptedByPieceOnRank(from, to))
                return true;
            
            //move on same file
            if (Math.Abs(to - from) % 8 == 0 && !IsInterceptedByPieceOnFile(from,to))
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
        private bool IsInterceptedByPieceOnRank(int from,int to)
        {
            int Rank = IndexToRank(from);
            foreach (var square in Squares)
            {
                if(square.Rank() == Rank && !square.Equals(holdingSquare))
                {
                    if (square.Piece.StartsWith(holdingSquare.Piece[0]) && ((to >= square.Index() && from < square.Index()) || (to <= square.Index() && from > square.Index())))
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
        private bool IsInterceptedByPieceOnFile(int from, int to)
        {
            int toRank = IndexToRank(to);
            int fromRank = IndexToRank(from);
            foreach (var square in Squares)
            {
                if (square.File() == IndexToFile(from) && !square.Equals(holdingSquare))
                {
                    if (square.Piece.StartsWith(holdingSquare.Piece[0]) && ((toRank >= square.Rank() && fromRank < square.Rank()) || (toRank <= square.Rank() && fromRank > square.Rank())))
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
        private bool CheckBishopMove(int from, int to)
        {
            int distance = Math.Abs(from - to);
            if (distance % 7 == 0 && !InterceptedByPieceOnDiagonall(from,to,7))
                return true;
            if (distance % 9 == 0 && !InterceptedByPieceOnDiagonall(from,to,9))
                return true;
            
            return false;
        }
        private bool InterceptedByPieceOnDiagonall(int from,int to,int requiredDistance)
        {
            foreach (Square square in Squares)
            {
                if(Math.Abs(square.Index() - from) % requiredDistance == 0 && !square.Equals(holdingSquare))
                {
                    if (square.Piece.StartsWith(holdingSquare.Piece[0]) && ((to >= square.Index() && from < square.Index()) || (to <= square.Index() && from > square.Index())))
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
        private bool CheckQueenMove(int from, int to)
        {
            return (CheckRookMove(from, to) || CheckBishopMove(from, to));
        }
        private bool CheckKingMove(int from, int to)
        {
            int distance = Math.Abs(to - from);
            if (distance == 1 || (distance > 6 && distance < 10))
                return true;
            //Rochade
            if(canRochadeW && from == 61)
            {
                if (to == 63 && Squares[61].Piece.Equals("nn"))
                {
                    Ds.MoveDataByIndex(64, 62);
                    return true;

                }
                else if(to == 59 && Squares[59].Piece.Equals("nn") && Squares[57].Piece.Equals("nn"))
                {
                    Ds.MoveDataByIndex(57, 60);
                    return true;

                }
            }
            if (canRochadeB && from == 5)
            {
                if (to == 7 && Squares[5].Piece.Equals("nn"))
                {
                    Ds.MoveDataByIndex(8, 6);
                    return true;
                }
                else if(to == 3 && Squares[1].Piece.Equals("nn") && Squares[3].Piece.Equals("nn"))
                {
                    Ds.MoveDataByIndex(1, 4);
                    return true;
                }
                
            }
            return false;
        }
        private bool CheckBlackPawnMove(int from, int to)
        {
            //normal moves
            if (to - from == 8 || (to - from == 16 && IndexToRank(from) == 1) && IsSquareFree(from + 8))
                return true;
            //capture a piece
            if (targetSquare.Piece.StartsWith("w") && (to - from == 7 || to - from == 9))
                return true;
            //en passant
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
            if (to - from == -8 || (to - from == -16 && IndexToRank(from) == 6 && IsSquareFree(from - 8)))
                return true;
            //capture a piece
            if (targetSquare.Piece.StartsWith("b") && (to - from == -7 || to - from == -9))
                return true;
            //en passant
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
                            Ds.MoveDataByIndex(holdingSquareIndex, targetSqareIndex);
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
