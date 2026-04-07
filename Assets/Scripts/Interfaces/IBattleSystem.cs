using System;
using System.Collections;
using UnityEngine;
using WizardChess.Core;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Orchestrates cinematic battle scenes during piece captures.
    /// Battle animations last 1.5–3 seconds.
    /// </summary>
    public interface IBattleSystem
    {
        IEnumerator ExecuteCapture(GameObject attacker, GameObject defender,
                                    PieceType attackerType, PieceType defenderType);
        AttackStyle GetAttackStyle(PieceType attackerType);
        bool IsBattleInProgress { get; }

        event Action OnBattleComplete;
    }
}
