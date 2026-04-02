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
        [SerializeField] private float moveStepDuration = 0.2f;
        [SerializeField] private float liftHeight = 0.5f;

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
        /// Animates the currently selected piece to the target position using step-by-step
        /// hopping motion (lift → move horizontally → set down) for each intermediate tile.
        /// Knights perform a single hop directly to the target.
        /// After animation, updates internal dictionaries and clears selection.
        /// </summary>
        public IEnumerator MovePieceTo(BoardPosition target)
        {
            if (!selectedPosition.HasValue) yield break;

            BoardPosition from = selectedPosition.Value;
            string fromKey = PositionKey(from);

            if (!pieceObjects.TryGetValue(fromKey, out GameObject pieceObj)) yield break;
            if (!pieceData.TryGetValue(fromKey, out ChessPiece piece)) yield break;

            // Disable input during movement animation to prevent concurrent interactions
            bool wasInputEnabled = inputEnabled;
            inputEnabled = false;

            // Build path: knights hop directly, others step tile-by-tile
            List<BoardPosition> path = piece.Type == PieceType.Knight
                ? new List<BoardPosition> { target }
                : BuildStepPath(from, target);

            // Animate each step along the path
            foreach (BoardPosition step in path)
            {
                yield return AnimateSingleStep(pieceObj, step);
            }

            // Update dictionaries: remove old key, add new key
            string toKey = PositionKey(target);
            pieceObjects.Remove(fromKey);
            pieceObjects[toKey] = pieceObj;

            pieceData.Remove(fromKey);
            piece.Position = target;
            pieceData[toKey] = piece;

            // Restore selection visuals before clearing state (piece has moved to new position)
            if (selectedRenderers != null && originalColors != null)
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
            // Restore original scale
            if (originalSelectedScale != Vector3.zero)
                pieceObj.transform.localScale = originalSelectedScale;

            // Clear selection state
            selectedPosition = null;
            selectedRenderers = null;
            originalColors = null;

            // Restore input state after animation completes
            inputEnabled = wasInputEnabled;
        }

        // ── Movement helpers ──────────────────────────────────────────

        /// <summary>
        /// Builds a tile-by-tile path from <paramref name="from"/> to <paramref name="to"/>.
        /// Moves along the file axis first, then the rank axis.
        /// Does not include the starting position.
        /// </summary>
        private static List<BoardPosition> BuildStepPath(BoardPosition from, BoardPosition to)
        {
            var path = new List<BoardPosition>();

            int currentFile = from.File;
            int currentRank = from.Rank;

            int fileDir = to.File > currentFile ? 1 : (to.File < currentFile ? -1 : 0);
            int rankDir = to.Rank > currentRank ? 1 : (to.Rank < currentRank ? -1 : 0);

            // Move diagonally as far as possible (both axes simultaneously)
            while (currentFile != to.File && currentRank != to.Rank)
            {
                currentFile += fileDir;
                currentRank += rankDir;
                path.Add(new BoardPosition(currentFile, currentRank));
            }

            // Then finish any remaining straight-line steps
            while (currentFile != to.File)
            {
                currentFile += fileDir;
                path.Add(new BoardPosition(currentFile, currentRank));
            }

            while (currentRank != to.Rank)
            {
                currentRank += rankDir;
                path.Add(new BoardPosition(currentFile, currentRank));
            }

            return path;
        }

        /// <summary>
        /// Animates a single hop: lift up → move horizontally → set down.
        /// Each phase takes moveStepDuration / 3 seconds.
        /// </summary>
        private IEnumerator AnimateSingleStep(GameObject pieceObj, BoardPosition target)
        {
            Vector3 startPos = pieceObj.transform.position;
            Vector3 endPos = GetWorldPosition(target);

            float phaseDuration = moveStepDuration / 3f;

            // Phase 1: Lift up
            Vector3 liftedStart = startPos + Vector3.up * liftHeight;
            yield return LerpPosition(pieceObj, startPos, liftedStart, phaseDuration);

            // Phase 2: Move horizontally (stay lifted)
            Vector3 liftedEnd = endPos + Vector3.up * liftHeight;
            yield return LerpPosition(pieceObj, liftedStart, liftedEnd, phaseDuration);

            // Phase 3: Set down
            yield return LerpPosition(pieceObj, liftedEnd, endPos, phaseDuration);
        }

        /// <summary>
        /// Smoothly interpolates a GameObject's position from start to end over the given duration.
        /// </summary>
        private static IEnumerator LerpPosition(GameObject obj, Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                obj.transform.position = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                obj.transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }

            obj.transform.position = to;
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
