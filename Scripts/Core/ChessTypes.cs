namespace WizardChess.Core
{
    /// <summary>
    /// Types of chess pieces.
    /// </summary>
    public enum PieceType
    {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }

    /// <summary>
    /// Colors of chess pieces / sides.
    /// </summary>
    public enum PieceColor
    {
        White,
        Black
    }

    /// <summary>
    /// Possible states of a chess game.
    /// </summary>
    public enum GameState
    {
        Ongoing,
        Check,
        Checkmate,
        Stalemate
    }

    /// <summary>
    /// Represents a position on the chess board.
    /// File: 0-7 (a-h), Rank: 0-7 (1-8).
    /// </summary>
    public struct BoardPosition
    {
        public int File; // 0-7 (a-h)
        public int Rank; // 0-7 (1-8)

        public BoardPosition(int file, int rank)
        {
            File = file;
            Rank = rank;
        }
    }

    /// <summary>
    /// Represents a chess piece with its type, color, position, and move history.
    /// </summary>
    public struct ChessPiece
    {
        public PieceType Type;
        public PieceColor Color;
        public BoardPosition Position;
        public bool HasMoved;

        public ChessPiece(PieceType type, PieceColor color, BoardPosition position, bool hasMoved = false)
        {
            Type = type;
            Color = color;
            Position = position;
            HasMoved = hasMoved;
        }
    }

    /// <summary>
    /// Represents a chess move from one position to another.
    /// </summary>
    public struct Move
    {
        public BoardPosition From;
        public BoardPosition To;
        public bool IsCapture;
        public bool IsCastling;
        public bool IsEnPassant;
        public PieceType? PromotionType;

        public Move(BoardPosition from, BoardPosition to, bool isCapture = false,
                     bool isCastling = false, bool isEnPassant = false,
                     PieceType? promotionType = null)
        {
            From = from;
            To = to;
            IsCapture = isCapture;
            IsCastling = isCastling;
            IsEnPassant = isEnPassant;
            PromotionType = promotionType;
        }
    }

    /// <summary>
    /// Result of executing a chess move.
    /// </summary>
    public struct MoveResult
    {
        public bool Success;
        public bool IsCapture;
        public ChessPiece? CapturedPiece;
        public GameState NewGameState;

        public MoveResult(bool success, bool isCapture, ChessPiece? capturedPiece, GameState newGameState)
        {
            Success = success;
            IsCapture = isCapture;
            CapturedPiece = capturedPiece;
            NewGameState = newGameState;
        }
    }
}
