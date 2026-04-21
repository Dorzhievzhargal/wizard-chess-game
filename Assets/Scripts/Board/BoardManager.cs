using System;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Core;
using WizardChess.Interfaces;
using WizardChess.Visual;

namespace WizardChess.Board
{
    /// <summary>
    /// Manages the 8×8 chess board: tile generation, coordinate conversion,
    /// move highlighting, touch input, and piece placement/removal.
    /// </summary>
    public class BoardManager : MonoBehaviour, IBoardManager
    {
        // ── Inspector-configurable fields ──────────────────────────────

        [Header("Board Settings")]
        [SerializeField] private float tileSize = 1.0f;
        [SerializeField] private Vector3 boardOrigin = Vector3.zero;

        [Header("Materials")]
        [SerializeField] private Material lightTileMaterial;
        [SerializeField] private Material darkTileMaterial;
        [SerializeField] private Material highlightMaterial;

        [Header("Input")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private LayerMask boardLayerMask = ~0;

        // ── Constants ──────────────────────────────────────────────────

        public const int BoardSize = 8;
        private const string TileTag = "Tile";

        // ── Internal state ─────────────────────────────────────────────

        private GameObject[,] tiles;
        private Material[,] originalMaterials;
        private List<BoardPosition> highlightedPositions;
        private Transform boardParent;

        // ── Events ─────────────────────────────────────────────────────

        public event Action<BoardPosition> OnTileClicked;

        // ── Unity lifecycle ────────────────────────────────────────────

        private void Awake()
        {
            tiles = new GameObject[BoardSize, BoardSize];
            originalMaterials = new Material[BoardSize, BoardSize];
            highlightedPositions = new List<BoardPosition>();

            if (gameCamera == null)
                gameCamera = Camera.main;

            // Auto-create marble materials when none are assigned in Inspector
            if (lightTileMaterial == null)
                lightTileMaterial = MaterialFactory.CreateLightMarbleMaterial();
            if (darkTileMaterial == null)
                darkTileMaterial = MaterialFactory.CreateDarkMarbleMaterial();
            if (highlightMaterial == null)
                highlightMaterial = MaterialFactory.CreateHighlightMaterial();
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDrawGizmos()
        {
            // Draw tile centers for debugging positioning issues
            if (tiles == null) return;

            Gizmos.color = Color.yellow;
            for (int file = 0; file < BoardSize; file++)
            {
                for (int rank = 0; rank < BoardSize; rank++)
                {
                    Vector3 center = BoardToWorldPosition(new BoardPosition(file, rank));
                    // Draw a small sphere at each tile center
                    Gizmos.DrawWireSphere(center, 0.05f);
                    // Draw a vertical line to make it visible
                    Gizmos.DrawLine(center, center + Vector3.up * 0.1f);
                }
            }
        }

        // ── 3.1  Board generation & coordinate system ──────────────────

        /// <summary>
        /// Generates the 8×8 board with alternating light/dark tiles.
        /// Each tile is a flat quad with a BoxCollider for raycasting.
        /// Coordinate system: File 0-7 (a-h), Rank 0-7 (1-8).
        /// </summary>
        public void InitializeBoard()
        {
            ClearExistingBoard();

            boardParent = new GameObject("ChessBoard").transform;
            boardParent.SetParent(transform);
            boardParent.localPosition = Vector3.zero;

            for (int file = 0; file < BoardSize; file++)
            {
                for (int rank = 0; rank < BoardSize; rank++)
                {
                    GameObject tile = CreateTile(file, rank);
                    tiles[file, rank] = tile;

                    bool isLight = (file + rank) % 2 == 0;
                    Material mat = isLight ? lightTileMaterial : darkTileMaterial;

                    var renderer = tile.GetComponent<MeshRenderer>();
                    if (mat != null)
                        renderer.material = mat;

                    originalMaterials[file, rank] = renderer.material;
                }
            }
        }

        private GameObject CreateTile(int file, int rank)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Tile_{(char)('a' + file)}{rank + 1}";
            // Tag assignment removed — not required for gameplay
            tile.transform.SetParent(boardParent);

            // Quads face +Z by default; rotate to face up (+Y)
            tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            BoardPosition pos = new BoardPosition(file, rank);
            Vector3 worldPos = BoardToWorldPosition(pos);
            tile.transform.position = worldPos;
            tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            // Ensure collider exists for raycasting
            var collider = tile.GetComponent<Collider>();
            if (collider == null)
                tile.AddComponent<BoxCollider>();

            return tile;
        }

        private void ClearExistingBoard()
        {
            if (boardParent != null)
            {
                Destroy(boardParent.gameObject);
                boardParent = null;
            }

            tiles = new GameObject[BoardSize, BoardSize];
            originalMaterials = new Material[BoardSize, BoardSize];
            highlightedPositions.Clear();
        }

        /// <summary>
        /// Returns the file letter (a-h) for a given file index (0-7).
        /// </summary>
        public static char FileToChar(int file)
        {
            return (char)('a' + Mathf.Clamp(file, 0, BoardSize - 1));
        }

        /// <summary>
        /// Returns the rank number string (1-8) for a given rank index (0-7).
        /// </summary>
        public static int RankToNumber(int rank)
        {
            return Mathf.Clamp(rank, 0, BoardSize - 1) + 1;
        }

        // ── 3.2  Coordinate conversion ─────────────────────────────────

        /// <summary>
        /// Converts a board position (file 0-7, rank 0-7) to a world-space position.
        /// The board origin is at file=0, rank=0 (a1). Tiles are centered.
        /// This is the SINGLE SOURCE OF TRUTH for tile center positions.
        /// </summary>
        public Vector3 BoardToWorldPosition(BoardPosition pos)
        {
            float x = boardOrigin.x + pos.File * tileSize + tileSize * 0.5f;
            float y = boardOrigin.y;
            float z = boardOrigin.z + pos.Rank * tileSize + tileSize * 0.5f;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns the exact world position for the center of a tile.
        /// Alias for BoardToWorldPosition for clarity in positioning code.
        /// </summary>
        public Vector3 GetWorldPositionFromTile(int file, int rank)
        {
            return BoardToWorldPosition(new BoardPosition(file, rank));
        }

        /// <summary>
        /// Converts a world-space position to a board position.
        /// Returns null if the position is outside the board bounds.
        /// </summary>
        public BoardPosition? WorldToBoardPosition(Vector3 worldPos)
        {
            float relX = worldPos.x - boardOrigin.x;
            float relZ = worldPos.z - boardOrigin.z;

            int file = Mathf.FloorToInt(relX / tileSize);
            int rank = Mathf.FloorToInt(relZ / tileSize);

            if (file < 0 || file >= BoardSize || rank < 0 || rank >= BoardSize)
                return null;

            return new BoardPosition(file, rank);
        }

        // ── 3.3  Highlight system ──────────────────────────────────────

        /// <summary>
        /// Highlights tiles corresponding to valid move destinations.
        /// </summary>
        public void HighlightValidMoves(List<Move> moves)
        {
            ClearHighlights();

            if (moves == null) return;

            foreach (Move move in moves)
            {
                int file = move.To.File;
                int rank = move.To.Rank;

                if (!IsValidPosition(file, rank)) continue;

                GameObject tile = tiles[file, rank];
                if (tile == null) continue;

                var renderer = tile.GetComponent<MeshRenderer>();
                if (renderer != null && highlightMaterial != null)
                    renderer.material = highlightMaterial;

                highlightedPositions.Add(new BoardPosition(file, rank));
            }
        }

        /// <summary>
        /// Clears all tile highlights, restoring original materials.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (BoardPosition pos in highlightedPositions)
            {
                if (!IsValidPosition(pos.File, pos.Rank)) continue;

                GameObject tile = tiles[pos.File, pos.Rank];
                if (tile == null) continue;

                var renderer = tile.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.material = originalMaterials[pos.File, pos.Rank];
            }

            highlightedPositions.Clear();
        }

        // ── 3.4  Touch / click input handling ──────────────────────────

        /// <summary>
        /// Handles touch (mobile) and mouse click (editor) input.
        /// Performs a raycast against the board and fires OnTileClicked
        /// when a tile is hit.
        /// </summary>
        private void HandleInput()
        {
            if (!TryGetInputPosition(out Vector3 screenPos))
                return;

            if (gameCamera == null) return;

            Ray ray = gameCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, boardLayerMask))
            {
                BoardPosition? boardPos = WorldToBoardPosition(hit.point);
                if (boardPos.HasValue)
                {
                    OnTileClicked?.Invoke(boardPos.Value);
                }
            }
        }

        /// <summary>
        /// Returns the screen position of the current input (touch or mouse click).
        /// Returns false if there is no input this frame.
        /// </summary>
        private bool TryGetInputPosition(out Vector3 screenPos)
        {
            screenPos = Vector3.zero;

            // Touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPos = touch.position;
                    return true;
                }
                return false;
            }

            // Mouse input (editor / desktop fallback)
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }

            return false;
        }

        // ── 3.5  Piece placement / removal (DEPRECATED - Use PieceController) ─────────────────────────────

        /// <summary>
        /// DEPRECATED: Piece tracking moved to PieceController.
        /// This method is kept for interface compatibility but does nothing.
        /// </summary>
        public void PlacePiece(ChessPiece piece, BoardPosition position)
        {
            // No-op: PieceController handles all piece tracking
        }

        /// <summary>
        /// DEPRECATED: Piece tracking moved to PieceController.
        /// This method is kept for interface compatibility but does nothing.
        /// </summary>
        public void RemovePiece(BoardPosition position)
        {
            // No-op: PieceController handles all piece tracking
        }

        /// <summary>
        /// DEPRECATED: Piece tracking moved to PieceController.
        /// Returns null. Use PieceController.GetPieceObject instead.
        /// </summary>
        public GameObject GetPieceObjectAt(BoardPosition position)
        {
            return null;
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static bool IsValidPosition(int file, int rank)
        {
            return file >= 0 && file < BoardSize && rank >= 0 && rank < BoardSize;
        }
    }
}
