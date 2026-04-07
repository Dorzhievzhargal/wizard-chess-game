using System;
using UnityEngine;
using WizardChess.Core;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Controls piece animations: Idle, Move, Attack, Hit_Reaction, Death.
    /// Uses naming convention: {PieceType}_{ChessAnimationState}.
    /// </summary>
    public interface IAnimationController
    {
        void PlayAnimation(GameObject piece, ChessAnimationState state);
        void PlayDeathEffect(GameObject piece, PieceType type);
        bool IsAnimationPlaying(GameObject piece);

        event Action<GameObject> OnAnimationComplete;
    }
}
