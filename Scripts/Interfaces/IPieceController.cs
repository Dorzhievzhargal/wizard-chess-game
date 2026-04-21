using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Core;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Controls chess piece spawning, selection, movement, and input.
    /// </summary>
    public interface IPieceController
    {
        void SpawnPieces(List<ChessPiece> pieces);
        void SelectPiece(BoardPosition position);
        void DeselectPiece();
        IEnumerator MovePieceTo(BoardPosition target);
        void RemovePiece(BoardPosition position);
        void UntrackPiece(BoardPosition position);
        void RetrackPiece(GameObject pieceObj, BoardPosition oldPos, BoardPosition newPos);
        void SetInputEnabled(bool enabled);
        bool IsInputEnabled { get; }

        GameObject GetPieceObject(BoardPosition position);

        event Action<BoardPosition> OnPieceSelected;
    }
}
