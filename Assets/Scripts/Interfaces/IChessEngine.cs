using System.Collections.Generic;
using WizardChess.Core;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Core chess engine interface. Pure C# — no Unity dependencies.
    /// Handles rules, validation, game state, and FEN serialization.
    /// </summary>
    public interface IChessEngine
    {
        void InitializeBoard();
        void LoadFromFen(string fen);
        string ToFen();

        List<Move> GetValidMoves(BoardPosition piecePosition);
        MoveResult MakeMove(Move move);

        GameState GetGameState();
        PieceColor GetCurrentTurn();
        ChessPiece? GetPieceAt(BoardPosition position);
        List<ChessPiece> GetAllPieces(PieceColor color);

        bool IsInCheck(PieceColor color);
        bool IsCheckmate();
        bool IsStalemate();
    }
}
