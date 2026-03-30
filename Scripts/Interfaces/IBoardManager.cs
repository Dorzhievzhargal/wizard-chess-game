using System;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Core;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Manages the 8x8 chess board: tile generation, highlighting,
    /// coordinate conversion, and piece placement.
    /// </summary>
    public interface IBoardManager
    {
        void InitializeBoard();
        void HighlightValidMoves(List<Move> moves);
        void ClearHighlights();
        void PlacePiece(ChessPiece piece, BoardPosition position);
        void RemovePiece(BoardPosition position);

        Vector3 BoardToWorldPosition(BoardPosition pos);
        BoardPosition? WorldToBoardPosition(Vector3 worldPos);

        event Action<BoardPosition> OnTileClicked;
    }
}
