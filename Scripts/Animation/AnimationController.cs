using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Core;
using WizardChess.Interfaces;

namespace WizardChess.Animation
{
    /// <summary>
    /// Controls procedural animations for chess pieces using coroutines.
    /// Implements IAnimationController with 5 animation states: Idle, Move, Attack, Hit_Reaction, Death.
    /// Uses placeholder procedural animations until real Animator controllers are connected.
    ///
    /// Animator State Machine Structure (for future real models):
    /// ┌──────┐   ┌──────┐   ┌────────┐   ┌──────────────┐   ┌───────┐
    /// │ Idle │──▶│ Move │──▶│ Attack │──▶│ Hit_Reaction │──▶│ Death │
    /// └──┬───┘   └──┬───┘   └──┬─────┘   └──────────────┘   └───────┘
    ///    │◀──────────┘          │                ▲
    ///    │◀──────────────────────┘                │
    ///    │          Any State ────────────────────┘
    ///
    /// Valid transitions:
    ///   Idle  → Move, Attack, Hit_Reaction, Death
    ///   Move  → Idle, Attack
    ///   Attack → Idle
    ///   Hit_Reaction → Idle, Death
    ///   Death → (terminal state)
    ///
    /// Naming convention: {PieceType}_{ChessAnimationState}
    ///   e.g. Pawn_Idle, Queen_Attack, King_Death
    /// </summary>
    public class AnimationController : MonoBehaviour, IAnimationController
    {
        // --- Configuration ---
        [Header("Idle Animation")]
        [SerializeField] private float idleBobAmplitude = 0.03f;
        [SerializeField] private float idleBobFrequency = 1.2f;
        [SerializeField] private float idleScalePulse = 0.01f;

        [Header("Attack Animation")]
        [SerializeField] private float attackLungeDistance = 0.4f;
        [SerializeField] private float attackDuration = 0.4f;

        [Header("Hit Reaction Animation")]
        [SerializeField] private float hitShakeIntensity = 0.1f;
        [SerializeField] private float hitDuration = 0.3f;

        [Header("Death Animation")]
        [SerializeField] private float deathDuration = 0.8f;

        // --- State tracking ---
        private readonly HashSet<GameObject> _animatingPieces = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, Coroutine> _activeCoroutines = new Dictionary<GameObject, Coroutine>();
        private readonly HashSet<GameObject> _idleRegistered = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, Coroutine> _idleCoroutines = new Dictionary<GameObject, Coroutine>();
        private readonly Dictionary<GameObject, Vector3> _basePositions = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<GameObject, Vector3> _baseScales = new Dictionary<GameObject, Vector3>();

        // --- IAnimationController event ---
        public event Action<GameObject> OnAnimationComplete;

        #region Valid Transitions (6.2 - Animator State Machine structure)

        /// <summary>
        /// Defines valid transitions between animation states.
        /// When real Animator controllers are connected, these map to Animator transitions.
        /// </summary>
        private static readonly Dictionary<ChessAnimationState, ChessAnimationState[]> ValidTransitions =
            new Dictionary<ChessAnimationState, ChessAnimationState[]>
            {
                { ChessAnimationState.Idle,         new[] { ChessAnimationState.Move, ChessAnimationState.Attack, ChessAnimationState.Hit_Reaction, ChessAnimationState.Death } },
                { ChessAnimationState.Move,         new[] { ChessAnimationState.Idle, ChessAnimationState.Attack } },
                { ChessAnimationState.Attack,       new[] { ChessAnimationState.Idle } },
                { ChessAnimationState.Hit_Reaction, new[] { ChessAnimationState.Idle, ChessAnimationState.Death } },
                { ChessAnimationState.Death,        new ChessAnimationState[0] } // terminal state
            };

        /// <summary>
        /// Checks whether a transition from one state to another is valid.
        /// </summary>
        public static bool IsValidTransition(ChessAnimationState from, ChessAnimationState to)
        {
            if (ValidTransitions.TryGetValue(from, out var targets))
            {
                foreach (var t in targets)
                    if (t == to) return true;
            }
            return false;
        }

        #endregion

        #region Naming Convention (6.3)

        /// <summary>
        /// Returns the animation clip name following the convention {PieceType}_{ChessAnimationState}.
        /// Used when real Animator controllers are connected.
        /// Examples: "Pawn_Idle", "Queen_Attack", "King_Death"
        /// </summary>
        public static string GetAnimationClipName(PieceType pieceType, ChessAnimationState state)
        {
            return $"{pieceType}_{state}";
        }

        #endregion

        #region PlayAnimation (6.1)

        /// <summary>
        /// Plays a procedural animation on the given piece for the specified state.
        /// Fires OnAnimationComplete when the animation finishes.
        /// </summary>
        public void PlayAnimation(GameObject piece, ChessAnimationState state)
        {
            if (piece == null) return;

            // Stop any currently running non-idle animation on this piece
            StopActiveCoroutine(piece);

            switch (state)
            {
                case ChessAnimationState.Idle:
                    // Idle is handled by the continuous idle system (RegisterForIdle)
                    // If not registered yet, register automatically
                    RegisterForIdle(piece);
                    CompleteAnimation(piece);
                    break;

                case ChessAnimationState.Move:
                    // Move animation is handled by PieceController; we just track state
                    _animatingPieces.Add(piece);
                    StartTrackedCoroutine(piece, MoveAnimationCoroutine(piece));
                    break;

                case ChessAnimationState.Attack:
                    PauseIdle(piece);
                    _animatingPieces.Add(piece);
                    StartTrackedCoroutine(piece, AttackAnimationCoroutine(piece));
                    break;

                case ChessAnimationState.Hit_Reaction:
                    PauseIdle(piece);
                    _animatingPieces.Add(piece);
                    StartTrackedCoroutine(piece, HitReactionCoroutine(piece));
                    break;

                case ChessAnimationState.Death:
                    PauseIdle(piece);
                    UnregisterFromIdle(piece);
                    _animatingPieces.Add(piece);
                    StartTrackedCoroutine(piece, DeathAnimationCoroutine(piece));
                    break;
            }
        }

        /// <summary>
        /// Returns true if the piece currently has a non-idle animation playing.
        /// </summary>
        public bool IsAnimationPlaying(GameObject piece)
        {
            return piece != null && _animatingPieces.Contains(piece);
        }

        #endregion

        #region Death Effects (6.4)

        /// <summary>
        /// Death effect types assigned per piece type.
        /// </summary>
        public enum DeathEffectType
        {
            StoneBreak,
            MagicDissolve,
            HeavyImpactFall,
            MagicDissolvePlusStoneBreak,
            KingDramatic
        }

        /// <summary>
        /// Returns the death effect type for a given piece type per design spec.
        /// </summary>
        public static DeathEffectType GetDeathEffectForPiece(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:   return DeathEffectType.HeavyImpactFall; // or StoneBreak
                case PieceType.Rook:   return DeathEffectType.StoneBreak;
                case PieceType.Knight: return DeathEffectType.HeavyImpactFall;
                case PieceType.Bishop: return DeathEffectType.MagicDissolve;
                case PieceType.Queen:  return DeathEffectType.MagicDissolvePlusStoneBreak;
                case PieceType.King:   return DeathEffectType.KingDramatic;
                default:               return DeathEffectType.StoneBreak;
            }
        }

        /// <summary>
        /// Plays the death effect for a piece based on its type.
        /// Pawn → Heavy Impact Fall / Stone Break
        /// Rook → Stone Break
        /// Knight → Heavy Impact Fall
        /// Bishop → Magic Dissolve
        /// Queen → Magic Dissolve + Stone Break
        /// King → Unique dramatic effect
        /// </summary>
        public void PlayDeathEffect(GameObject piece, PieceType type)
        {
            if (piece == null) return;

            StopActiveCoroutine(piece);
            PauseIdle(piece);
            UnregisterFromIdle(piece);
            _animatingPieces.Add(piece);

            DeathEffectType effect = GetDeathEffectForPiece(type);

            switch (effect)
            {
                case DeathEffectType.StoneBreak:
                    StartTrackedCoroutine(piece, StoneBreakCoroutine(piece));
                    break;
                case DeathEffectType.MagicDissolve:
                    StartTrackedCoroutine(piece, MagicDissolveCoroutine(piece));
                    break;
                case DeathEffectType.HeavyImpactFall:
                    StartTrackedCoroutine(piece, HeavyImpactFallCoroutine(piece));
                    break;
                case DeathEffectType.MagicDissolvePlusStoneBreak:
                    StartTrackedCoroutine(piece, MagicDissolvePlusStoneBreakCoroutine(piece));
                    break;
                case DeathEffectType.KingDramatic:
                    StartTrackedCoroutine(piece, KingDramaticDeathCoroutine(piece));
                    break;
            }
        }

        #endregion

        #region Idle System (6.5)

        /// <summary>
        /// Registers a piece for continuous idle breathing animation.
        /// </summary>
        public void RegisterForIdle(GameObject piece)
        {
            if (piece == null || _idleRegistered.Contains(piece)) return;

            _idleRegistered.Add(piece);
            CacheBaseTransform(piece);
            var co = StartCoroutine(IdleBreathingCoroutine(piece));
            _idleCoroutines[piece] = co;
        }

        /// <summary>
        /// Unregisters a piece from idle animation (e.g., when removed from board).
        /// </summary>
        public void UnregisterFromIdle(GameObject piece)
        {
            if (piece == null) return;

            _idleRegistered.Remove(piece);
            if (_idleCoroutines.TryGetValue(piece, out var co))
            {
                if (co != null) StopCoroutine(co);
                _idleCoroutines.Remove(piece);
            }
            // Restore base transform
            RestoreBaseTransform(piece);
        }

        /// <summary>
        /// Registers multiple pieces for idle animation at once.
        /// </summary>
        public void RegisterAllForIdle(IEnumerable<GameObject> pieces)
        {
            foreach (var piece in pieces)
                RegisterForIdle(piece);
        }

        /// <summary>
        /// Unregisters all pieces from idle animation.
        /// </summary>
        public void UnregisterAllFromIdle()
        {
            foreach (var piece in new List<GameObject>(_idleRegistered))
                UnregisterFromIdle(piece);
        }

        #endregion

        #region Procedural Animation Coroutines

        /// <summary>
        /// Idle: subtle sine-wave bobbing and gentle scale pulsing. Runs continuously.
        /// </summary>
        private IEnumerator IdleBreathingCoroutine(GameObject piece)
        {
            CacheBaseTransform(piece);
            float phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f); // randomize phase per piece

            while (piece != null && _idleRegistered.Contains(piece))
            {
                float t = Time.time * idleBobFrequency + phase;

                // Gentle up/down bobbing
                Vector3 basePos = _basePositions.ContainsKey(piece) ? _basePositions[piece] : piece.transform.position;
                float bobOffset = Mathf.Sin(t) * idleBobAmplitude;
                piece.transform.position = new Vector3(basePos.x, basePos.y + bobOffset, basePos.z);

                // Very slight scale pulsing
                Vector3 baseScale = _baseScales.ContainsKey(piece) ? _baseScales[piece] : Vector3.one;
                float scaleFactor = 1f + Mathf.Sin(t * 0.8f) * idleScalePulse;
                piece.transform.localScale = baseScale * scaleFactor;

                yield return null;
            }
        }

        /// <summary>
        /// Move: brief tracking state. Actual movement is handled by PieceController.
        /// </summary>
        private IEnumerator MoveAnimationCoroutine(GameObject piece)
        {
            // Movement is driven by PieceController; we just track state briefly
            yield return new WaitForSeconds(0.1f);
            CompleteAnimation(piece);
        }

        /// <summary>
        /// Attack: quick forward lunge and return.
        /// </summary>
        private IEnumerator AttackAnimationCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startPos = piece.transform.position;
            Vector3 forward = piece.transform.forward;
            Vector3 lungeTarget = startPos + forward * attackLungeDistance;

            float half = attackDuration * 0.5f;

            // Lunge forward
            float elapsed = 0f;
            while (elapsed < half && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
                piece.transform.position = Vector3.Lerp(startPos, lungeTarget, t);
                yield return null;
            }

            // Return
            elapsed = 0f;
            while (elapsed < half && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
                piece.transform.position = Vector3.Lerp(lungeTarget, startPos, t);
                yield return null;
            }

            if (piece != null)
                piece.transform.position = startPos;

            ResumeIdle(piece);
            CompleteAnimation(piece);
        }

        /// <summary>
        /// Hit Reaction: knockback shake effect.
        /// </summary>
        private IEnumerator HitReactionCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startPos = piece.transform.position;
            float elapsed = 0f;

            while (elapsed < hitDuration && piece != null)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / hitDuration);
                float offsetX = UnityEngine.Random.Range(-1f, 1f) * hitShakeIntensity * decay;
                float offsetZ = UnityEngine.Random.Range(-1f, 1f) * hitShakeIntensity * decay;
                piece.transform.position = startPos + new Vector3(offsetX, 0f, offsetZ);
                yield return null;
            }

            if (piece != null)
                piece.transform.position = startPos;

            CompleteAnimation(piece);
        }

        /// <summary>
        /// Death: generic fall + shrink effect.
        /// </summary>
        private IEnumerator DeathAnimationCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startScale = piece.transform.localScale;
            Quaternion startRot = piece.transform.rotation;
            float elapsed = 0f;

            while (elapsed < deathDuration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / deathDuration;

                // Shrink
                piece.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                // Tilt over
                piece.transform.rotation = startRot * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 90f, t));

                yield return null;
            }

            if (piece != null)
                piece.SetActive(false);

            CompleteAnimation(piece);
        }

        #endregion

        #region Death Effect Coroutines (6.4)

        /// <summary>
        /// Stone Break: piece cracks and crumbles (simulated with scale jitter + shrink).
        /// </summary>
        private IEnumerator StoneBreakCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startScale = piece.transform.localScale;
            float elapsed = 0f;
            float duration = deathDuration;

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Jitter scale to simulate cracking
                float jitter = UnityEngine.Random.Range(0.95f, 1.05f);
                float shrink = Mathf.Lerp(1f, 0f, t * t);
                piece.transform.localScale = startScale * shrink * jitter;

                // Slight random rotation for crumble feel
                piece.transform.rotation *= Quaternion.Euler(
                    UnityEngine.Random.Range(-2f, 2f) * (1f - t),
                    UnityEngine.Random.Range(-2f, 2f) * (1f - t),
                    0f);

                yield return null;
            }

            if (piece != null)
                piece.SetActive(false);

            CompleteAnimation(piece);
        }

        /// <summary>
        /// Magic Dissolve: piece fades out with color shift (simulated with scale + material alpha).
        /// </summary>
        private IEnumerator MagicDissolveCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startScale = piece.transform.localScale;
            Renderer renderer = piece.GetComponentInChildren<Renderer>();
            Color startColor = Color.white;
            if (renderer != null && renderer.material != null)
                startColor = renderer.material.color;

            float elapsed = 0f;
            float duration = deathDuration * 1.2f; // slightly longer for dissolve

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale up slightly then shrink (magical expansion before vanishing)
                float scaleCurve = t < 0.2f
                    ? Mathf.Lerp(1f, 1.1f, t / 0.2f)
                    : Mathf.Lerp(1.1f, 0f, (t - 0.2f) / 0.8f);
                piece.transform.localScale = startScale * scaleCurve;

                // Color shift to purple/transparent
                if (renderer != null && renderer.material != null)
                {
                    Color dissolveColor = Color.Lerp(startColor, new Color(0.6f, 0.2f, 0.8f, 0f), t);
                    renderer.material.color = dissolveColor;
                }

                yield return null;
            }

            if (piece != null)
                piece.SetActive(false);

            CompleteAnimation(piece);
        }

        /// <summary>
        /// Heavy Impact Fall: piece topples over with impact.
        /// </summary>
        private IEnumerator HeavyImpactFallCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startPos = piece.transform.position;
            Quaternion startRot = piece.transform.rotation;
            Vector3 startScale = piece.transform.localScale;
            float elapsed = 0f;
            float duration = deathDuration;

            // Random fall direction
            float fallAngle = UnityEngine.Random.Range(0f, 360f);
            Vector3 fallDir = Quaternion.Euler(0f, fallAngle, 0f) * Vector3.forward;

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Accelerating fall (gravity-like)
                float fallT = t * t;

                // Topple rotation
                piece.transform.rotation = startRot * Quaternion.Euler(
                    Mathf.Lerp(0f, 90f, fallT) * fallDir.z,
                    0f,
                    Mathf.Lerp(0f, -90f, fallT) * fallDir.x);

                // Slight downward movement
                piece.transform.position = startPos + Vector3.down * (fallT * 0.2f);

                // Shrink at the end
                if (t > 0.7f)
                {
                    float shrinkT = (t - 0.7f) / 0.3f;
                    piece.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, shrinkT);
                }

                yield return null;
            }

            if (piece != null)
                piece.SetActive(false);

            CompleteAnimation(piece);
        }

        /// <summary>
        /// Magic Dissolve + Stone Break combo (Queen): dissolve with crumble particles.
        /// </summary>
        private IEnumerator MagicDissolvePlusStoneBreakCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startScale = piece.transform.localScale;
            Renderer renderer = piece.GetComponentInChildren<Renderer>();
            Color startColor = Color.white;
            if (renderer != null && renderer.material != null)
                startColor = renderer.material.color;

            float elapsed = 0f;
            float duration = deathDuration * 1.4f; // longer for combo effect

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Phase 1 (0-50%): Magic dissolve with color shift
                // Phase 2 (50-100%): Stone break crumble
                if (t < 0.5f)
                {
                    float phase1T = t / 0.5f;
                    float scaleCurve = Mathf.Lerp(1f, 0.6f, phase1T);
                    piece.transform.localScale = startScale * scaleCurve;

                    if (renderer != null && renderer.material != null)
                    {
                        Color dissolveColor = Color.Lerp(startColor, new Color(0.6f, 0.2f, 0.8f, 0.5f), phase1T);
                        renderer.material.color = dissolveColor;
                    }
                }
                else
                {
                    float phase2T = (t - 0.5f) / 0.5f;
                    float jitter = UnityEngine.Random.Range(0.9f, 1.1f);
                    float shrink = Mathf.Lerp(0.6f, 0f, phase2T);
                    piece.transform.localScale = startScale * shrink * jitter;

                    piece.transform.rotation *= Quaternion.Euler(
                        UnityEngine.Random.Range(-3f, 3f) * (1f - phase2T),
                        UnityEngine.Random.Range(-3f, 3f) * (1f - phase2T),
                        0f);
                }

                yield return null;
            }

            if (piece != null)
                piece.SetActive(false);

            CompleteAnimation(piece);
        }

        /// <summary>
        /// King Dramatic Death: unique dramatic effect — glow, shake, slow fall, flash.
        /// </summary>
        private IEnumerator KingDramaticDeathCoroutine(GameObject piece)
        {
            if (piece == null) yield break;

            Vector3 startPos = piece.transform.position;
            Vector3 startScale = piece.transform.localScale;
            Quaternion startRot = piece.transform.rotation;
            Renderer renderer = piece.GetComponentInChildren<Renderer>();
            Color startColor = Color.white;
            if (renderer != null && renderer.material != null)
                startColor = renderer.material.color;

            float duration = deathDuration * 2f; // extra dramatic
            float elapsed = 0f;

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Phase 1 (0-30%): dramatic shake + glow
                if (t < 0.3f)
                {
                    float shakeT = t / 0.3f;
                    float shakeIntensity = Mathf.Sin(shakeT * Mathf.PI * 8f) * 0.08f;
                    piece.transform.position = startPos + new Vector3(shakeIntensity, 0f, shakeIntensity * 0.5f);

                    if (renderer != null && renderer.material != null)
                    {
                        // Glow bright
                        Color glow = Color.Lerp(startColor, Color.white, Mathf.PingPong(shakeT * 4f, 1f) * 0.5f);
                        renderer.material.color = glow;
                    }
                }
                // Phase 2 (30-60%): rise up slightly
                else if (t < 0.6f)
                {
                    float riseT = (t - 0.3f) / 0.3f;
                    piece.transform.position = startPos + Vector3.up * Mathf.Sin(riseT * Mathf.PI) * 0.3f;
                }
                // Phase 3 (60-100%): slow dramatic fall + dissolve
                else
                {
                    float fallT = (t - 0.6f) / 0.4f;
                    piece.transform.position = startPos + Vector3.down * (fallT * fallT * 0.3f);
                    piece.transform.rotation = startRot * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 90f, fallT));
                    piece.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, fallT);

                    if (renderer != null && renderer.material != null)
                    {
                        Color fadeColor = Color.Lerp(startColor, new Color(1f, 0.8f, 0f, 0f), fallT);
                        renderer.material.color = fadeColor;
                    }
                }

                yield return null;
            }

            if (piece != null)
                piece.SetActive(false);

            CompleteAnimation(piece);
        }

        #endregion

        #region Internal Helpers

        private void CacheBaseTransform(GameObject piece)
        {
            if (piece == null) return;
            if (!_basePositions.ContainsKey(piece))
                _basePositions[piece] = piece.transform.position;
            if (!_baseScales.ContainsKey(piece))
                _baseScales[piece] = piece.transform.localScale;
        }

        private void RestoreBaseTransform(GameObject piece)
        {
            if (piece == null) return;
            if (_basePositions.TryGetValue(piece, out var pos))
                piece.transform.position = pos;
            if (_baseScales.TryGetValue(piece, out var scale))
                piece.transform.localScale = scale;
        }

        /// <summary>
        /// Updates the cached base position for a piece (call after a move completes).
        /// </summary>
        public void UpdateBasePosition(GameObject piece, Vector3 newPosition)
        {
            if (piece == null) return;
            _basePositions[piece] = newPosition;
        }

        private void StartTrackedCoroutine(GameObject piece, IEnumerator routine)
        {
            var co = StartCoroutine(routine);
            _activeCoroutines[piece] = co;
        }

        private void StopActiveCoroutine(GameObject piece)
        {
            if (piece != null && _activeCoroutines.TryGetValue(piece, out var co))
            {
                if (co != null) StopCoroutine(co);
                _activeCoroutines.Remove(piece);
            }
        }

        private void PauseIdle(GameObject piece)
        {
            if (piece == null) return;
            if (_idleCoroutines.TryGetValue(piece, out var co))
            {
                if (co != null) StopCoroutine(co);
                _idleCoroutines.Remove(piece);
            }
            RestoreBaseTransform(piece);
        }

        private void ResumeIdle(GameObject piece)
        {
            if (piece == null || !_idleRegistered.Contains(piece)) return;
            if (_idleCoroutines.ContainsKey(piece)) return; // already running

            CacheBaseTransform(piece);
            var co = StartCoroutine(IdleBreathingCoroutine(piece));
            _idleCoroutines[piece] = co;
        }

        private void CompleteAnimation(GameObject piece)
        {
            if (piece != null)
            {
                _animatingPieces.Remove(piece);
                _activeCoroutines.Remove(piece);
            }
            OnAnimationComplete?.Invoke(piece);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            _animatingPieces.Clear();
            _activeCoroutines.Clear();
            _idleRegistered.Clear();
            _idleCoroutines.Clear();
            _basePositions.Clear();
            _baseScales.Clear();
        }

        #endregion
    }
}
