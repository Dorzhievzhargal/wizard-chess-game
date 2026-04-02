using System;
using System.Collections;
using UnityEngine;
using WizardChess.Core;
using WizardChess.Interfaces;

namespace WizardChess.Battle
{
    /// <summary>
    /// Orchestrates cinematic battle scenes during piece captures.
    /// Coordinates attack animations, hit reactions, death effects,
    /// and ensures battles complete within 1.5–3 seconds.
    /// </summary>
    public class BattleSystem : MonoBehaviour, IBattleSystem
    {
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour animationControllerComponent;
        [SerializeField] private MonoBehaviour pieceControllerComponent;
        [SerializeField] private MonoBehaviour cameraSystemComponent;

        [Header("Battle Timing")]
        [SerializeField] private float minBattleDuration = 1.5f;
        [SerializeField] private float maxBattleDuration = 3.0f;

        [Header("Phase Timing (fractions of total duration)")]
        [SerializeField] private float attackPhaseFraction = 0.35f;
        [SerializeField] private float hitReactionPhaseFraction = 0.25f;

        private IAnimationController _animationController;
        private IPieceController _pieceController;
        private ICameraSystem _cameraSystem;
        private BattleConfig _battleConfig;

        public bool IsBattleInProgress { get; private set; }
        public event Action OnBattleComplete;

        private void Awake()
        {
            if (animationControllerComponent != null)
            {
                _animationController = animationControllerComponent as IAnimationController;
            }

            if (pieceControllerComponent != null)
            {
                _pieceController = pieceControllerComponent as IPieceController;
            }

            if (cameraSystemComponent != null)
            {
                _cameraSystem = cameraSystemComponent as ICameraSystem;
            }

            _battleConfig = new BattleConfig(minBattleDuration, maxBattleDuration);
        }

        /// <summary>
        /// Allows setting the animation controller dependency at runtime (e.g. from GameManager).
        /// </summary>
        public void SetAnimationController(IAnimationController controller)
        {
            _animationController = controller;
        }

        /// <summary>
        /// Allows setting the piece controller dependency at runtime (e.g. from GameManager).
        /// </summary>
        public void SetPieceController(IPieceController controller)
        {
            _pieceController = controller;
        }

        /// <summary>
        /// Allows setting the camera system dependency at runtime (e.g. from GameManager).
        /// </summary>
        public void SetCameraSystem(ICameraSystem camera)
        {
            _cameraSystem = camera;
        }

        /// <summary>
        /// Orchestrates the full battle sequence for a capture:
        /// 1. Block player input
        /// 2. Mark battle in progress
        /// 3. Play Attack animation on attacker
        /// 4. Play Hit_Reaction on defender
        /// 5. Play Death effect on defender
        /// 6. Wait for total duration within BattleConfig bounds
        /// 7. Mark battle complete, re-enable input, and fire event
        /// </summary>
        public IEnumerator ExecuteCapture(GameObject attacker, GameObject defender,
                                           PieceType attackerType, PieceType defenderType)
        {
            // Block input before anything else
            if (_pieceController != null)
            {
                _pieceController.SetInputEnabled(false);
            }

            try
            {
                IsBattleInProgress = true;

                float totalDuration = CalculateBattleDuration(attackerType);
                float attackDuration = totalDuration * attackPhaseFraction;
                float hitReactionDuration = totalDuration * hitReactionPhaseFraction;
                float deathDuration = totalDuration - attackDuration - hitReactionDuration;
                float elapsed = 0f;

                // Transition camera to battle close-up
                if (_cameraSystem != null && attacker != null && defender != null)
                {
                    yield return _cameraSystem.TransitionToBattleView(
                        attacker.transform.position, defender.transform.position);
                }

                // Phase 1: Piece-specific procedural attack animation on attacker
                if (_animationController != null && attacker != null)
                {
                    _animationController.PlayAnimation(attacker, ChessAnimationState.Attack);
                }
                if (attacker != null)
                {
                    yield return PlayAttackAnimation(attacker, attackerType, attackDuration);
                }
                else
                {
                    yield return WaitForDuration(attackDuration);
                }
                elapsed += attackDuration;

                // Phase 2: Hit reaction on defender — apply camera shake on impact
                if (_animationController != null && defender != null)
                {
                    _animationController.PlayAnimation(defender, ChessAnimationState.Hit_Reaction);
                }
                if (_cameraSystem != null)
                {
                    _cameraSystem.ApplyCameraShake(0.15f, 0.25f);
                }
                yield return WaitForDuration(hitReactionDuration);
                elapsed += hitReactionDuration;

                // Phase 3: Death effect on defender
                if (_animationController != null && defender != null)
                {
                    _animationController.PlayDeathEffect(defender, defenderType);
                }
                yield return WaitForDuration(deathDuration);
                elapsed += deathDuration;

                // Safety: ensure we've waited at least minDuration
                float remaining = _battleConfig.MinDuration - elapsed;
                if (remaining > 0f)
                {
                    yield return WaitForDuration(remaining);
                }

                // Return attacker to Idle
                if (_animationController != null && attacker != null)
                {
                    _animationController.PlayAnimation(attacker, ChessAnimationState.Idle);
                }

                IsBattleInProgress = false;

                // Return camera to gameplay view
                if (_cameraSystem != null)
                {
                    yield return _cameraSystem.ReturnToGameplayView();
                }

                // Re-enable input after battle completes, before firing event
                if (_pieceController != null)
                {
                    _pieceController.SetInputEnabled(true);
                }

                OnBattleComplete?.Invoke();
            }
            finally
            {
                // Ensure input is always re-enabled even if something goes wrong
                if (_pieceController != null && !_pieceController.IsInputEnabled)
                {
                    _pieceController.SetInputEnabled(true);
                }
            }
        }

        /// <summary>
        /// Returns the attack style for the given piece type.
        /// Descriptions match the Russian spec exactly.
        /// </summary>
        public AttackStyle GetAttackStyle(PieceType attackerType)
        {
            switch (attackerType)
            {
                case PieceType.Pawn:
                    return new AttackStyle(PieceType.Pawn, "Pawn_Attack", "Быстрый удар копьём");
                case PieceType.Rook:
                    return new AttackStyle(PieceType.Rook, "Rook_Attack", "Тяжёлый удар");
                case PieceType.Knight:
                    return new AttackStyle(PieceType.Knight, "Knight_Attack", "Атака с разбега");
                case PieceType.Bishop:
                    return new AttackStyle(PieceType.Bishop, "Bishop_Attack", "Магический луч");
                case PieceType.Queen:
                    return new AttackStyle(PieceType.Queen, "Queen_Attack", "Комбо: ближний бой + магия");
                case PieceType.King:
                    return new AttackStyle(PieceType.King, "King_Attack", "Мощный удар мечом");
                default:
                    return new AttackStyle(attackerType, $"{attackerType}_Attack", "Стандартная атака");
            }
        }

        /// <summary>
        /// Plays a piece-specific procedural attack animation using Transform manipulation.
        /// Each piece type has a unique visual style. The IAnimationController.PlayAnimation(Attack)
        /// is still called for state tracking, but this provides the procedural visual.
        /// </summary>
        private IEnumerator PlayAttackAnimation(GameObject attacker, PieceType attackerType, float phaseDuration)
        {
            switch (attackerType)
            {
                case PieceType.Pawn:
                    yield return PawnAttack(attacker, phaseDuration);
                    break;
                case PieceType.Rook:
                    yield return RookAttack(attacker, phaseDuration);
                    break;
                case PieceType.Knight:
                    yield return KnightAttack(attacker, phaseDuration);
                    break;
                case PieceType.Bishop:
                    yield return BishopAttack(attacker, phaseDuration);
                    break;
                case PieceType.Queen:
                    yield return QueenAttack(attacker, phaseDuration);
                    break;
                case PieceType.King:
                    yield return KingAttack(attacker, phaseDuration);
                    break;
                default:
                    yield return WaitForDuration(phaseDuration);
                    break;
            }
        }

        /// <summary>
        /// Pawn: Quick forward lunge and return (быстрый удар копьём, ~0.3s feel).
        /// </summary>
        private IEnumerator PawnAttack(GameObject attacker, float duration)
        {
            Transform t = attacker.transform;
            Vector3 originalPos = t.position;
            Vector3 forward = t.forward;
            float lungeDistance = 0.5f;
            float lungeTime = duration * 0.4f;
            float returnTime = duration * 0.6f;

            // Quick lunge forward
            float elapsed = 0f;
            while (elapsed < lungeTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lungeTime);
                float curve = Mathf.Sin(progress * Mathf.PI * 0.5f); // ease-out
                t.position = originalPos + forward * lungeDistance * curve;
                yield return null;
            }

            // Return to original position
            Vector3 lungedPos = t.position;
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / returnTime);
                t.position = Vector3.Lerp(lungedPos, originalPos, progress);
                yield return null;
            }

            t.position = originalPos;
        }

        /// <summary>
        /// Rook: Slow wind-up, heavy slam forward (тяжёлый удар, ~0.6s feel).
        /// </summary>
        private IEnumerator RookAttack(GameObject attacker, float duration)
        {
            Transform t = attacker.transform;
            Vector3 originalPos = t.position;
            Vector3 originalScale = t.localScale;
            Vector3 forward = t.forward;
            float windUpTime = duration * 0.4f;
            float slamTime = duration * 0.3f;
            float recoverTime = duration * 0.3f;
            float pullBackDistance = 0.3f;
            float slamDistance = 0.8f;

            // Wind-up: pull back slightly and scale up
            float elapsed = 0f;
            while (elapsed < windUpTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / windUpTime);
                t.position = originalPos - forward * pullBackDistance * progress;
                t.localScale = originalScale * (1f + 0.15f * progress);
                yield return null;
            }

            // Heavy slam forward
            Vector3 windUpPos = t.position;
            Vector3 slamTarget = originalPos + forward * slamDistance;
            elapsed = 0f;
            while (elapsed < slamTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / slamTime);
                float curve = progress * progress; // ease-in for heavy feel
                t.position = Vector3.Lerp(windUpPos, slamTarget, curve);
                yield return null;
            }

            // Recover to original
            Vector3 slamPos = t.position;
            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / recoverTime);
                t.position = Vector3.Lerp(slamPos, originalPos, progress);
                t.localScale = Vector3.Lerp(originalScale * 1.15f, originalScale, progress);
                yield return null;
            }

            t.position = originalPos;
            t.localScale = originalScale;
        }

        /// <summary>
        /// Knight: Charge from distance — move back first, then rush forward (атака с разбега, ~0.5s feel).
        /// </summary>
        private IEnumerator KnightAttack(GameObject attacker, float duration)
        {
            Transform t = attacker.transform;
            Vector3 originalPos = t.position;
            Vector3 forward = t.forward;
            float retreatTime = duration * 0.3f;
            float chargeTime = duration * 0.4f;
            float returnTime = duration * 0.3f;
            float retreatDistance = 0.8f;
            float chargeDistance = 1.2f;

            // Retreat: move back to build distance
            float elapsed = 0f;
            while (elapsed < retreatTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / retreatTime);
                t.position = originalPos - forward * retreatDistance * progress;
                yield return null;
            }

            // Charge forward (gallop feel — slight vertical bounce)
            Vector3 retreatPos = t.position;
            Vector3 chargeTarget = originalPos + forward * chargeDistance;
            elapsed = 0f;
            while (elapsed < chargeTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / chargeTime);
                float curve = Mathf.Sin(progress * Mathf.PI * 0.5f); // ease-out rush
                Vector3 pos = Vector3.Lerp(retreatPos, chargeTarget, curve);
                // Add gallop bounce
                float bounce = Mathf.Sin(progress * Mathf.PI * 3f) * 0.15f * (1f - progress);
                pos.y += bounce;
                t.position = pos;
                yield return null;
            }

            // Return to original
            Vector3 chargePos = t.position;
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / returnTime);
                t.position = Vector3.Lerp(chargePos, originalPos, progress);
                yield return null;
            }

            t.position = originalPos;
        }

        /// <summary>
        /// Bishop: Stay in place, scale up with a "casting" feel, then pulse (магический луч, ~0.5s feel).
        /// </summary>
        private IEnumerator BishopAttack(GameObject attacker, float duration)
        {
            Transform t = attacker.transform;
            Vector3 originalScale = t.localScale;
            float castTime = duration * 0.5f;
            float pulseTime = duration * 0.3f;
            float recoverTime = duration * 0.2f;
            float castScaleMultiplier = 1.25f;
            float pulseScaleMultiplier = 1.4f;

            // Casting phase: gradual scale up
            float elapsed = 0f;
            while (elapsed < castTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / castTime);
                float scale = Mathf.Lerp(1f, castScaleMultiplier, progress);
                t.localScale = originalScale * scale;
                yield return null;
            }

            // Pulse: quick scale burst (magic release)
            elapsed = 0f;
            while (elapsed < pulseTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / pulseTime);
                float scale = Mathf.Lerp(castScaleMultiplier, pulseScaleMultiplier, Mathf.Sin(progress * Mathf.PI));
                t.localScale = originalScale * scale;
                yield return null;
            }

            // Recover to original scale
            Vector3 currentScale = t.localScale;
            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / recoverTime);
                t.localScale = Vector3.Lerp(currentScale, originalScale, progress);
                yield return null;
            }

            t.localScale = originalScale;
        }

        /// <summary>
        /// Queen: Two-phase — quick melee lunge, then scale pulse for magic blast
        /// (комбо: ближний бой + магия, ~0.7s feel).
        /// </summary>
        private IEnumerator QueenAttack(GameObject attacker, float duration)
        {
            Transform t = attacker.transform;
            Vector3 originalPos = t.position;
            Vector3 originalScale = t.localScale;
            Vector3 forward = t.forward;
            float meleeLungeTime = duration * 0.3f;
            float meleeReturnTime = duration * 0.15f;
            float magicCastTime = duration * 0.3f;
            float recoverTime = duration * 0.25f;
            float lungeDistance = 0.6f;

            // Phase 1: Quick melee lunge
            float elapsed = 0f;
            while (elapsed < meleeLungeTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / meleeLungeTime);
                float curve = Mathf.Sin(progress * Mathf.PI * 0.5f);
                t.position = originalPos + forward * lungeDistance * curve;
                yield return null;
            }

            // Return from melee
            Vector3 lungedPos = t.position;
            elapsed = 0f;
            while (elapsed < meleeReturnTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / meleeReturnTime);
                t.position = Vector3.Lerp(lungedPos, originalPos, progress);
                yield return null;
            }

            t.position = originalPos;

            // Phase 2: Magic blast — scale pulse
            elapsed = 0f;
            while (elapsed < magicCastTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / magicCastTime);
                float scale = 1f + 0.35f * Mathf.Sin(progress * Mathf.PI);
                t.localScale = originalScale * scale;
                yield return null;
            }

            // Recover
            Vector3 currentScale = t.localScale;
            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / recoverTime);
                t.localScale = Vector3.Lerp(currentScale, originalScale, progress);
                yield return null;
            }

            t.position = originalPos;
            t.localScale = originalScale;
        }

        /// <summary>
        /// King: Dramatic wind-up (rise up), then powerful downward strike
        /// (мощный удар мечом, ~0.6s feel).
        /// </summary>
        private IEnumerator KingAttack(GameObject attacker, float duration)
        {
            Transform t = attacker.transform;
            Vector3 originalPos = t.position;
            Vector3 originalScale = t.localScale;
            Vector3 forward = t.forward;
            float riseTime = duration * 0.4f;
            float strikeTime = duration * 0.3f;
            float recoverTime = duration * 0.3f;
            float riseHeight = 0.5f;
            float strikeDistance = 0.7f;

            // Wind-up: rise up dramatically with slight scale increase
            float elapsed = 0f;
            while (elapsed < riseTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / riseTime);
                float curve = Mathf.Sin(progress * Mathf.PI * 0.5f); // ease-out
                Vector3 pos = originalPos;
                pos.y += riseHeight * curve;
                t.position = pos;
                t.localScale = originalScale * (1f + 0.1f * curve);
                yield return null;
            }

            // Powerful downward strike forward
            Vector3 raisedPos = t.position;
            Vector3 strikeTarget = originalPos + forward * strikeDistance;
            Vector3 raisedScale = t.localScale;
            elapsed = 0f;
            while (elapsed < strikeTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / strikeTime);
                float curve = progress * progress; // ease-in for power
                t.position = Vector3.Lerp(raisedPos, strikeTarget, curve);
                yield return null;
            }

            // Recover to original
            Vector3 strikePos = t.position;
            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / recoverTime);
                t.position = Vector3.Lerp(strikePos, originalPos, progress);
                t.localScale = Vector3.Lerp(raisedScale, originalScale, progress);
                yield return null;
            }

            t.position = originalPos;
            t.localScale = originalScale;
        }

        /// <summary>
        /// Calculates battle duration based on attacker type, clamped to BattleConfig bounds.
        /// Heavier/more dramatic pieces get longer battles.
        /// </summary>
        private float CalculateBattleDuration(PieceType attackerType)
        {
            float duration;
            switch (attackerType)
            {
                case PieceType.Pawn:
                    duration = _battleConfig.MinDuration; // fastest
                    break;
                case PieceType.Knight:
                    duration = Mathf.Lerp(_battleConfig.MinDuration, _battleConfig.MaxDuration, 0.4f);
                    break;
                case PieceType.Bishop:
                    duration = Mathf.Lerp(_battleConfig.MinDuration, _battleConfig.MaxDuration, 0.5f);
                    break;
                case PieceType.Rook:
                    duration = Mathf.Lerp(_battleConfig.MinDuration, _battleConfig.MaxDuration, 0.6f);
                    break;
                case PieceType.Queen:
                    duration = Mathf.Lerp(_battleConfig.MinDuration, _battleConfig.MaxDuration, 0.85f);
                    break;
                case PieceType.King:
                    duration = _battleConfig.MaxDuration; // most dramatic
                    break;
                default:
                    duration = Mathf.Lerp(_battleConfig.MinDuration, _battleConfig.MaxDuration, 0.5f);
                    break;
            }

            return Mathf.Clamp(duration, _battleConfig.MinDuration, _battleConfig.MaxDuration);
        }

        private IEnumerator WaitForDuration(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }
    }
}
