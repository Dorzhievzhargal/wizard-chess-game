using System.Collections;
using UnityEngine;

namespace WizardChess.Interfaces
{
    /// <summary>
    /// Manages gameplay and battle camera views with smooth transitions.
    /// </summary>
    public interface ICameraSystem
    {
        void SetGameplayView();
        IEnumerator TransitionToBattleView(Vector3 attackerPos, Vector3 defenderPos);
        IEnumerator TransitionToBattleView(Vector3 attackerPos, Vector3 defenderPos, float customDuration);
        IEnumerator ReturnToGameplayView();
        void ApplyCameraShake(float intensity, float duration);
    }
}
