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
        private Dictionary<string, GameObject> pieceObjects;
        private Dictionary<string, ChessPiece> pieceData;
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
            pieceData = new Dictionary<string, ChessPiece>();

            if (boardManagerComponent != null)
            {
                boardManager = boardManagerComponent as IBoardManager;
            }
        }

        // ── IPieceController implementation ────────────────────────────

        /// <summary>
        /// Creates all piece GameObjects from the given list using PlaceholderModelFactory
        /// and positions them on the board via IBoardManager.BoardToWorldPosition.
        /// </summary>
        public void SpawnPieces(List<ChessPiece> pieces)
        {
            ClearExistingPieces();
            EnsureContainer();

            if (pieces == null) return;

            foreach (var piece in pieces)
            {
                string key = PositionKey(piece.Position);

                GameObject pieceObj = PlaceholderModelFactory.CreatePieceModel(
                    piece.Type, piece.Color, tileSize);

                Vector3 worldPos = GetWorldPosition(piece.Position);
                pieceObj.transform.position = worldPos;
                pieceObj.transform.SetParent(piecesContainer, true);

                pieceObjects[key] = pieceObj;
                pieceData[key] = piece;
            }
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
        /// Removes and destroys the piece at the given board position.
        /// </summary>
        public void RemovePiece(BoardPosition position)
        {
            string key = PositionKey(position);

            if (pieceObjects.TryGetValue(key, out GameObject obj))
            {
                Destroy(obj);
                pieceObjects.Remove(key);
            }

            pieceData.Remove(key);
        }

        /// <summary>
        /// Removes a piece from tracking dictionaries WITHOUT destroying the GameObject.
        /// Used for cinematic captures where the defender needs to stay alive during battle animation.
        /// </summary>
        public void UntrackPiece(BoardPosition position)
        {
            string key = PositionKey(position);
            pieceObjects.Remove(key);
            pieceData.Remove(key);
        }

        /// <summary>
        /// Re-registers a piece GameObject at a new position after a cinematic capture.
        /// </summary>
        public void RetrackPiece(GameObject pieceObj, BoardPosition oldPos, BoardPosition newPos)
        {
            string oldKey = PositionKey(oldPos);
            string newKey = PositionKey(newPos);

            // Remove from old position if still tracked
            if (pieceObjects.ContainsKey(oldKey) && pieceObjects[oldKey] == pieceObj)
            {
                pieceObjects.Remove(oldKey);
            }
            if (pieceData.TryGetValue(oldKey, out var data))
            {
                pieceData.Remove(oldKey);
                pieceData[newKey] = data;
            }

            pieceObjects[newKey] = pieceObj;
        }

        /// <summary>
        /// Static version of PositionKey for external use.
        /// </summary>
        public static string PositionKeyStatic(BoardPosition pos)
        {
            return $"{pos.File}_{pos.Rank}";
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
        /// Animates the currently selected piece smoothly from its current position
        /// to the target board position. Triggers Walking animation on Animator models
        /// during movement. After animation, updates internal dictionaries.
        /// Does NOT clear selection state or restore selection visuals — that is
        /// GameManager's responsibility via DeselectPiece().
        /// Does NOT manage inputEnabled — that is GameManager's responsibility.
        /// </summary>
        public IEnumerator MovePieceTo(BoardPosition target)
        {
            if (!selectedPosition.HasValue) yield break;

            BoardPosition from = selectedPosition.Value;
            string fromKey = PositionKey(from);

            if (!pieceObjects.TryGetValue(fromKey, out GameObject pieceObj)) yield break;
            if (!pieceData.TryGetValue(fromKey, out ChessPiece piece)) yield break;

            // Calculate target world position
            Vector3 startPos = pieceObj.transform.position;
            Vector3 endPos = GetWorldPosition(target);

            // Face movement direction
            Vector3 moveDir = endPos - startPos;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.001f)
                pieceObj.transform.rotation = Quaternion.LookRotation(moveDir);

            // Start walk animation
            SetWalkAnimation(pieceObj, true);

            // Smooth lerp from current position to target (speed-based duration)
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

            // Stop walk animation and snap to exact target
            SetWalkAnimation(pieceObj, false);
            pieceObj.transform.position = endPos;

            // Update dictionaries: remove old key, add new key
            string toKey = PositionKey(target);
            pieceObjects.Remove(fromKey);
            pieceObjects[toKey] = pieceObj;

            pieceData.Remove(fromKey);
            piece.Position = target;
            pieceData[toKey] = piece;
        }

        // ── Movement helpers ──────────────────────────────────────────

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
            pieceData = new Dictionary<string, ChessPiece>();

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
