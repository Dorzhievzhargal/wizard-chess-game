using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Core;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Controls visual chess piece GameObjects: spawning, selection feedback, movement animation.
    /// VISUAL ONLY: Does not contain chess logic or game state.
    /// ChessEngine is the single source of truth for chess logic.
    /// </summary>
    public interface IPieceController
    {
        /// <summary>Spawns piece GameObjects from chess piece data (visual only)</summary>
        void SpawnPieces(List<ChessPiece> pieces);
        
        /// <summary>Applies visual selection feedback to a piece (scale, color)</summary>
        void SelectPiece(BoardPosition position);
        
        /// <summary>Removes visual selection feedback</summary>
        void DeselectPiece();
        
        /// <summary>Animates piece GameObject movement (visual only, updates visual mapping)</summary>
        IEnumerator MovePieceTo(BoardPosition from, BoardPosition target);
        
        /// <summary>Executes cinematic capture move with battle animation callback</summary>
        IEnumerator ExecuteCaptureMove(GameObject attackerObj, BoardPosition from, BoardPosition to, 
            float strikeDistance, System.Func<IEnumerator> onBattlePosition);
        
        /// <summary>Destroys piece GameObject and removes from visual mapping</summary>
        void RemovePiece(BoardPosition position);
        
        /// <summary>Removes piece from visual mapping without destroying (for cinematic captures)</summary>
        void UntrackPiece(BoardPosition position);
        
        /// <summary>Re-registers piece GameObject at new position (visual mapping only)</summary>
        void RetrackPiece(GameObject pieceObj, BoardPosition oldPos, BoardPosition newPos);
        
        /// <summary>Replaces the visual model of a piece with a new type (for pawn promotion)</summary>
        void ReplaceVisualModel(BoardPosition position, PieceType newType, PieceColor color);
        
        /// <summary>Enables/disables input handling</summary>
        void SetInputEnabled(bool enabled);
        
        /// <summary>Returns whether input is enabled</summary>
        bool IsInputEnabled { get; }

        /// <summary>Returns the GameObject at a board position (visual mapping only)</summary>
        GameObject GetPieceObject(BoardPosition position);

        /// <summary>
        /// Force-snaps a piece to the exact center of a tile.
        /// Ensures board logic is the single source of truth for positioning.
        /// </summary>
        void SnapPieceToTileCenter(GameObject pieceObj, BoardPosition position);

        /// <summary>
        /// Returns the world position for a board position in PieceController's coordinate system.
        /// This includes the Y offset and is where pieces should actually be placed.
        /// </summary>
        Vector3 GetWorldPositionForTile(BoardPosition position);

        event Action<BoardPosition> OnPieceSelected;
    }
}
