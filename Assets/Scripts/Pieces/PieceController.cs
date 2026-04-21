using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Core;
using WizardChess.Interfaces;

namespace WizardChess.Pieces
{
    /// <summary>
    /// Controls chess piece spawning, selection, movement, and input.
    /// Implements IPieceController to manage piece GameObjects on the board.
    /// </summary>
    public class PieceController : MonoBehaviour, IPieceController
    {
        // ── Inspector-configurable fields ──────────────────────────────

        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour boardManagerComponent;

        [Header("Piece Settings")]
        [SerializeField] private float tileSize = 1.0f;
        [SerializeField] private float pieceYOffset = 0.0f;

        [Header("Movement Animation")]
        [SerializeField] private float moveSpeed = 3.0f;

        // ── Internal state ─────────────────────────────────────────────

        private IBoardManager boardManager;
        private Dictionary<string, GameObject> pieceObjects; // ONLY visual mapping: BoardPosition -> GameObject
        private Transform piecesContainer;
        private bool inputEnabled = true;
        private BoardPosition? selectedPosition;

        // ── Selection visual feedback state ────────────────────────────

        private const float SelectionScaleMultiplier = 1.15f;
        private static readonly Color SelectionTint = new Color(0.4f, 0.4f, 0.1f, 0f);

        private Vector3 originalSelectedScale;
        private Color[] originalColors;
        private Renderer[] selectedRenderers;

        // ── Events ─────────────────────────────────────────────────────

        public event Action<BoardPosition> OnPieceSelected;

        // ── Unity lifecycle ────────────────────────────────────────────

        private void Awake()
        {
            pieceObjects = new Dictionary<string, GameObject>();

            if (boardManagerComponent != null)
            {
                boardManager = boardManagerComponent as IBoardManager;
            }
        }

        // ── IPieceController implementation ────────────────────────────

        /// <summary>
        /// Creates all piece GameObjects from the given list using PlaceholderModelFactory
        /// and positions them on the board via IBoardManager.BoardToWorldPosition.
        /// VISUAL ONLY: Does not store chess logic data.
        /// </summary>
        public void SpawnPieces(List<ChessPiece> pieces)
        {
            ClearExistingPieces();
            EnsureContainer();

            if (pieces == null) return;

            Debug.Log($"[PieceController] SpawnPieces: Spawning {pieces.Count} pieces");

            foreach (var piece in pieces)
            {
                string key = PositionKey(piece.Position);

                GameObject pieceObj = PlaceholderModelFactory.CreatePieceModel(
                    piece.Type, piece.Color, tileSize);

                Vector3 worldPos = GetWorldPosition(piece.Position);
                pieceObj.transform.position = worldPos;
                pieceObj.transform.SetParent(piecesContainer, true);

                pieceObjects[key] = pieceObj;

                // Debug: Log detailed spawn information
                var renderers = pieceObj.GetComponentsInChildren<Renderer>();
                string boundsInfo = "no renderers";
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);
                    boundsInfo = $"bounds center={bounds.center}, size={bounds.size}";
                }

                Debug.Log($"[PieceController] Spawned {piece.Color} {piece.Type} at board({piece.Position.File},{piece.Position.Rank}) " +
                          $"world={worldPos} | GameObject={pieceObj.name} active={pieceObj.activeSelf} " +
                          $"pos={pieceObj.transform.position} scale={pieceObj.transform.localScale} | {boundsInfo}");
                
                // Check if model child exists
                Transform modelChild = pieceObj.transform.Find("Model");
                if (modelChild != null)
                {
                    Debug.Log($"[PieceController]   Model child: active={modelChild.gameObject.activeSelf} " +
                              $"localPos={modelChild.localPosition} localScale={modelChild.localScale} " +
                              $"renderers={modelChild.GetComponentsInChildren<Renderer>().Length}");
                }
                else
                {
                    Debug.LogWarning($"[PieceController]   No 'Model' child found in {pieceObj.name}!");
                }
            }

            Debug.Log($"[PieceController] SpawnPieces complete. Total pieces in scene: {pieceObjects.Count}");
        }

        /// <summary>
        /// Returns the GameObject at the given board position, or null if none exists.
        /// </summary>
        public GameObject GetPieceObject(BoardPosition position)
        {
            string key = PositionKey(position);
            pieceObjects.TryGetValue(key, out GameObject obj);
            return obj;
        }

        /// <summary>
        /// Removes and destroys the piece GameObject at the given board position.
        /// VISUAL ONLY: Does not modify chess logic state.
        /// </summary>
        public void RemovePiece(BoardPosition position)
        {
            string key = PositionKey(position);

            if (pieceObjects.TryGetValue(key, out GameObject obj))
            {
                Destroy(obj);
                pieceObjects.Remove(key);
            }
        }

        /// <summary>
        /// Removes a piece from tracking dictionary WITHOUT destroying the GameObject.
        /// Used for cinematic captures where the defender needs to stay alive during battle animation.
        /// VISUAL ONLY: Does not modify chess logic state.
        /// </summary>
        public void UntrackPiece(BoardPosition position)
        {
            string key = PositionKey(position);
            pieceObjects.Remove(key);
        }

        /// <summary>
        /// Re-registers a piece GameObject at a new position after a cinematic capture.
        /// VISUAL ONLY: Updates visual mapping, does not modify chess logic state.
        /// </summary>
        public void RetrackPiece(GameObject pieceObj, BoardPosition oldPos, BoardPosition newPos)
        {
            if (pieceObj == null)
            {
                Debug.LogWarning("[PieceController] RetrackPiece: pieceObj is null");
                return;
            }

            string oldKey = PositionKey(oldPos);
            string newKey = PositionKey(newPos);

            // Remove from old position if it's the same object
            if (pieceObjects.ContainsKey(oldKey) && pieceObjects[oldKey] == pieceObj)
                pieceObjects.Remove(oldKey);

            // Register at new position
            pieceObjects[newKey] = pieceObj;

            // Update selection tracking if the moved piece was selected
            if (selectedPosition.HasValue &&
                selectedPosition.Value.File == oldPos.File &&
                selectedPosition.Value.Rank == oldPos.Rank)
            {
                selectedPosition = newPos;
            }

            Debug.Log($"[PieceController] RetrackPiece (visual only): {oldKey} -> {newKey}");
        }

        /// <summary>
        /// Replaces the visual model of a piece at the given position with a new piece type.
        /// Used for pawn promotion: destroys the old pawn model and spawns the promoted piece model.
        /// Preserves position, color, and board registration.
        /// VISUAL ONLY: Does not modify chess logic state (caller must update engine separately).
        /// </summary>
        public void ReplaceVisualModel(BoardPosition position, PieceType newType, PieceColor color)
        {
            string key = PositionKey(position);

            // Get the old piece GameObject
            if (!pieceObjects.TryGetValue(key, out GameObject oldPieceObj))
            {
                Debug.LogWarning($"[PieceController] ReplaceVisualModel: no piece at {key}");
                return;
            }

            // Store the world position before destroying
            Vector3 worldPos = oldPieceObj.transform.position;

            // Destroy the old piece
            Destroy(oldPieceObj);
            pieceObjects.Remove(key);

            // Create the new piece model
            GameObject newPieceObj = PlaceholderModelFactory.CreatePieceModel(newType, color, tileSize);
            newPieceObj.transform.position = worldPos;
            newPieceObj.transform.SetParent(piecesContainer, true);

            // Register the new piece at the same position
            pieceObjects[key] = newPieceObj;

            Debug.Log($"[PieceController] ReplaceVisualModel: replaced piece at ({position.File},{position.Rank}) " +
                      $"with {color} {newType}");
        }

        /// <summary>
        /// Static version of PositionKey for external use.
        /// </summary>
        public static string PositionKeyStatic(BoardPosition pos)
        {
            return $"{pos.File}_{pos.Rank}";
        }

        /// <summary>
        /// Returns the world position for a board position, including Y offset.
        /// This is the position where pieces should actually be placed.
        /// Public accessor for GameManager to use the same coordinate system.
        /// </summary>
        public Vector3 GetWorldPositionForTile(BoardPosition position)
        {
            return GetWorldPosition(position);
        }

        // ── Selection & movement ──────────────────────────────────────

        /// <summary>
        /// Selects the piece at the given position with visual feedback (scale + emission glow).
        /// If input is disabled or no piece exists at the position, does nothing.
        /// If another piece is already selected, deselects it first.
        /// </summary>
        public void SelectPiece(BoardPosition position)
        {
            if (!inputEnabled) return;

            if (selectedPosition.HasValue)
            {
                DeselectPiece();
            }

            string key = PositionKey(position);
            if (!pieceObjects.TryGetValue(key, out GameObject pieceObj)) return;

            selectedPosition = position;

            // Store original scale and apply selection scale
            originalSelectedScale = pieceObj.transform.localScale;
            pieceObj.transform.localScale = originalSelectedScale * SelectionScaleMultiplier;

            // Store original colors and apply selection tint
            selectedRenderers = pieceObj.GetComponentsInChildren<Renderer>();
            originalColors = new Color[selectedRenderers.Length];

            for (int i = 0; i < selectedRenderers.Length; i++)
            {
                Material mat = selectedRenderers[i].material;
                if (mat.HasProperty("_BaseColor"))
                {
                    originalColors[i] = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", originalColors[i] + SelectionTint);
                }
                else
                {
                    originalColors[i] = mat.color;
                    mat.color = originalColors[i] + SelectionTint;
                }
            }

            OnPieceSelected?.Invoke(position);
        }

        /// <summary>
        /// Deselects the currently selected piece, restoring its original scale and emission.
        /// </summary>
        public void DeselectPiece()
        {
            if (!selectedPosition.HasValue) return;

            string key = PositionKey(selectedPosition.Value);
            if (pieceObjects.TryGetValue(key, out GameObject pieceObj))
            {
                // Restore original scale
                pieceObj.transform.localScale = originalSelectedScale;

                // Restore original colors
                if (selectedRenderers != null)
                {
                    for (int i = 0; i < selectedRenderers.Length; i++)
                    {
                        if (selectedRenderers[i] != null)
                        {
                            Material mat = selectedRenderers[i].material;
                            if (mat.HasProperty("_BaseColor"))
                                mat.SetColor("_BaseColor", originalColors[i]);
                            else
                                mat.color = originalColors[i];
                        }
                    }
                }
            }
            else if (selectedRenderers != null)
            {
                // Piece moved to new position — restore colors on cached renderers directly
                for (int i = 0; i < selectedRenderers.Length; i++)
                {
                    if (selectedRenderers[i] != null)
                    {
                        Material mat = selectedRenderers[i].material;
                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", originalColors[i]);
                        else
                            mat.color = originalColors[i];
                    }
                }
            }

            selectedPosition = null;
            selectedRenderers = null;
            originalColors = null;
        }

        /// <summary>
        /// Animates a piece GameObject smoothly from one board position to another.
        /// VISUAL ONLY: Updates visual mapping, does not modify chess logic state.
        /// This is for NORMAL MOVES ONLY - no combat offset, no strike distance.
        /// Piece walks directly from start tile center to destination tile center.
        /// Triggers Walking animation on Animator models during movement.
        /// Does NOT clear selection state or restore selection visuals — that is
        /// GameManager's responsibility via DeselectPiece().
        /// Does NOT manage inputEnabled — that is GameManager's responsibility.
        /// CRITICAL: Always snaps to exact tile center at the end to prevent drift.
        /// </summary>
        public IEnumerator MovePieceTo(BoardPosition from, BoardPosition target)
        {
            string fromKey = PositionKey(from);

            if (!pieceObjects.TryGetValue(fromKey, out GameObject pieceObj))
            {
                Debug.LogWarning($"[PieceController] MovePieceTo: no piece object at {fromKey}");
                yield break;
            }

            Vector3 startPos = pieceObj.transform.position;
            Vector3 endPos = GetWorldPosition(target);

            // Calculate movement direction for rotation
            Vector3 moveDir = endPos - startPos;
            moveDir.y = 0f;
            
            // Rotate piece to face movement direction
            // CRITICAL: Use actual movement direction, not model's initial facing
            // Black pieces need 180° correction because their models face backward
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion baseRot = Quaternion.LookRotation(moveDir);
                bool isBlack = IsBlackPiece(pieceObj);
                pieceObj.transform.rotation = isBlack 
                    ? baseRot * Quaternion.Euler(0f, 180f, 0f) 
                    : baseRot;
            }

            SetWalkAnimation(pieceObj, true);

            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / Mathf.Max(moveSpeed, 0.1f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                pieceObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            SetWalkAnimation(pieceObj, false);

            // CRITICAL: Force snap to EXACT tile center - board logic is source of truth
            SnapPieceToTileCenter(pieceObj, target);

            // Update visual mapping: move GameObject from old position to new position
            string toKey = PositionKey(target);
            pieceObjects.Remove(fromKey);
            pieceObjects[toKey] = pieceObj;

            // If the moved piece was selected visually, update selection to new tile
            if (selectedPosition.HasValue &&
                selectedPosition.Value.File == from.File &&
                selectedPosition.Value.Rank == from.Rank)
            {
                selectedPosition = target;
            }

            Debug.Log($"[PieceController] NormalMove complete: from ({from.File},{from.Rank}) to ({target.File},{target.Rank}), " +
                      $"piece at {pieceObj.transform.position}, expected {endPos}");
        }

        /// <summary>
        /// Executes a cinematic capture move with battle animation.
        /// CAPTURE FLOW:
        /// 1. Attacker moves to strike position near defender
        /// 2. Both pieces face each other (handled by caller via callback)
        /// 3. Battle animation plays (handled by caller)
        /// 4. Defender is destroyed (handled by caller)
        /// 5. Attacker moves to EXACT CENTER of defender's tile
        /// 6. Attacker is re-registered at new position
        /// This method handles steps 1, 5, and 6. Caller handles 2, 3, 4 via callback.
        /// </summary>
        public IEnumerator ExecuteCaptureMove(GameObject attackerObj, BoardPosition from, BoardPosition to, 
            float strikeDistance, System.Func<IEnumerator> onBattlePosition)
        {
            if (attackerObj == null)
            {
                Debug.LogWarning("[PieceController] ExecuteCaptureMove: attackerObj is null");
                yield break;
            }

            Vector3 fromWorld = GetWorldPosition(from);
            Vector3 toWorld = GetWorldPosition(to);

            // Calculate strike position — stop strikeDistance tiles away from defender
            Vector3 dir = (toWorld - fromWorld);
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            Vector3 strikePos = toWorld - dir * strikeDistance;

            // Step 1: Move attacker to strike position
            SetWalkAnimation(attackerObj, true);
            
            // Rotate to face movement direction
            // Black pieces need 180° correction because their models face backward
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion baseRot = Quaternion.LookRotation(dir);
                bool isBlack = IsBlackPiece(attackerObj);
                attackerObj.transform.rotation = isBlack 
                    ? baseRot * Quaternion.Euler(0f, 180f, 0f) 
                    : baseRot;
            }
            
            yield return StartCoroutine(MoveToWorldPositionInternal(attackerObj, strikePos, 0.6f));
            SetWalkAnimation(attackerObj, false);

            // Steps 2-4: Caller handles battle (face each other, animate, destroy defender)
            if (onBattlePosition != null)
            {
                yield return StartCoroutine(onBattlePosition());
            }

            // Step 5: Move attacker to EXACT CENTER of defender's tile
            if (attackerObj != null && attackerObj.activeSelf)
            {
                SetWalkAnimation(attackerObj, true);
                
                // Rotate to face final movement direction
                // Black pieces need 180° correction because their models face backward
                Vector3 finalDir = (toWorld - attackerObj.transform.position);
                finalDir.y = 0f;
                if (finalDir.sqrMagnitude > 0.001f)
                {
                    Quaternion baseRot = Quaternion.LookRotation(finalDir);
                    bool isBlack = IsBlackPiece(attackerObj);
                    attackerObj.transform.rotation = isBlack 
                        ? baseRot * Quaternion.Euler(0f, 180f, 0f) 
                        : baseRot;
                }
                
                yield return StartCoroutine(MoveToWorldPositionInternal(attackerObj, toWorld, 0.3f));
                SetWalkAnimation(attackerObj, false);

                // CRITICAL: Force snap to EXACT tile center
                SnapPieceToTileCenter(attackerObj, to);
            }

            // Step 6: Re-register attacker at new position
            RetrackPiece(attackerObj, from, to);

            Debug.Log($"[PieceController] CaptureMove complete: from ({from.File},{from.Rank}) to ({to.File},{to.Rank}), " +
                      $"piece at {attackerObj.transform.position}, expected {toWorld}");
        }

        /// <summary>
        /// Internal helper: Smoothly moves a GameObject to a world position over duration.
        /// Snaps to exact target at the end.
        /// </summary>
        private IEnumerator MoveToWorldPositionInternal(GameObject obj, Vector3 target, float duration)
        {
            if (obj == null) yield break;
            
            Vector3 start = obj.transform.position;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                obj.transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            
            if (obj != null)
                obj.transform.position = target;
        }

        /// <summary>
        /// Force-snaps a piece GameObject to the exact center of a tile.
        /// This is the FINAL positioning step that ensures board logic is the source of truth.
        /// Resets rotation to upright if needed.
        /// </summary>
        public void SnapPieceToTileCenter(GameObject pieceObj, BoardPosition position)
        {
            if (pieceObj == null) return;

            Vector3 exactCenter = GetWorldPosition(position);
            pieceObj.transform.position = exactCenter;

            // Reset any rotation drift (keep only Y rotation for facing direction)
            Vector3 euler = pieceObj.transform.rotation.eulerAngles;
            pieceObj.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

            // Ensure piece is active
            if (!pieceObj.activeSelf)
                pieceObj.SetActive(true);

            Debug.Log($"[PieceController] SnapPieceToTileCenter: piece snapped to {exactCenter} " +
                      $"at tile ({position.File},{position.Rank})");
        }

        // ── Movement helpers ──────────────────────────────────────────

        /// <summary>
        /// Determines if a piece GameObject is black based on its name.
        /// Black pieces have "Black" in their GameObject name from PlaceholderModelFactory.
        /// </summary>
        private static bool IsBlackPiece(GameObject pieceObj)
        {
            if (pieceObj == null) return false;
            return pieceObj.name.Contains("Black");
        }

        /// <summary>
        /// Sets the Walking bool on the piece's Animator (if present).
        /// Used during smooth movement to trigger walk animation on 3D models.
        /// </summary>
        private static void SetWalkAnimation(GameObject pieceObj, bool walking)
        {
            if (pieceObj == null) return;
            var animator = pieceObj.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("Walking", walking);
            }
        }

        /// <summary>
        /// Enables or disables player input for turn-based control.
        /// When disabled, SelectPiece calls are ignored.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        /// <summary>
        /// Returns whether player input is currently enabled.
        /// Used by GameManager to query input state.
        /// </summary>
        public bool IsInputEnabled => inputEnabled;

        // ── Private helpers ────────────────────────────────────────────

        private Vector3 GetWorldPosition(BoardPosition pos)
        {
            Vector3 worldPos;

            if (boardManager != null)
            {
                worldPos = boardManager.BoardToWorldPosition(pos);
            }
            else
            {
                // Fallback when no board manager is assigned
                worldPos = new Vector3(
                    pos.File * tileSize + tileSize * 0.5f,
                    0f,
                    pos.Rank * tileSize + tileSize * 0.5f);
            }

            worldPos.y += pieceYOffset;
            return worldPos;
        }

        private void EnsureContainer()
        {
            if (piecesContainer == null)
            {
                var containerObj = new GameObject("Pieces");
                containerObj.transform.SetParent(transform, false);
                piecesContainer = containerObj.transform;
            }
        }

        private void ClearExistingPieces()
        {
            if (pieceObjects != null)
            {
                foreach (var kvp in pieceObjects)
                {
                    if (kvp.Value != null)
                    {
                        Destroy(kvp.Value);
                    }
                }
            }

            pieceObjects = new Dictionary<string, GameObject>();

            if (piecesContainer != null)
            {
                Destroy(piecesContainer.gameObject);
                piecesContainer = null;
            }
        }

        private static string PositionKey(BoardPosition pos)
        {
            return $"{pos.File}_{pos.Rank}";
        }
    }
}
