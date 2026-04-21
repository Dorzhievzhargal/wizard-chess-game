using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardChess.Battle;
using WizardChess.Board;
using WizardChess.CameraModule;
using WizardChess.Interfaces;
using WizardChess.Pieces;

namespace WizardChess.Core
{
    /// <summary>
    /// Main game coordinator. Manages the game loop: initialization, piece selection,
    /// move execution, captures, game state transitions, and pawn promotion.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Inspector dependencies ─────────────────────────────────────

        [Header("Dependencies")]
        [SerializeField] private BoardManager boardManagerComponent;
        [SerializeField] private PieceController pieceControllerComponent;
        [SerializeField] private BattleSystem battleSystemComponent;
        [SerializeField] private CameraSystem cameraSystemComponent;

        // ── Interfaces (resolved from serialized components) ───────────

        private IChessEngine chessEngine;
        private IBoardManager boardManager;
        private IPieceController pieceController;
        private IBattleSystem battleSystem;
        private ICameraSystem cameraSystem;

        // ── Game state ─────────────────────────────────────────────────

        private BoardPosition? selectedPiecePosition;
        private List<Move> currentValidMoves = new List<Move>();
        private bool isProcessingMove;
        private bool isGameOver;

        // ── Pawn promotion state ───────────────────────────────────────

        private bool isWaitingForPromotion;
        private Move pendingPromotionMove;
        private PieceType? selectedPromotionType;

        // ── Promotion UI layout ────────────────────────────────────────

        private readonly PieceType[] promotionOptions = {
            PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight
        };

        // ── Unity lifecycle ────────────────────────────────────────────

        /// <summary>
        /// Task 5.1: Initialize all modules and subscribe to events.
        /// </summary>
        private void Start()
        {
            // Create pure C# chess engine
            chessEngine = new ChessEngine();

            // Auto-find components if not assigned in Inspector
            if (boardManagerComponent == null)
                boardManagerComponent = FindAnyObjectByType<BoardManager>();
            if (pieceControllerComponent == null)
                pieceControllerComponent = FindAnyObjectByType<PieceController>();
            if (battleSystemComponent == null)
                battleSystemComponent = FindAnyObjectByType<BattleSystem>();
            if (cameraSystemComponent == null)
                cameraSystemComponent = FindAnyObjectByType<CameraSystem>();

            // Resolve interfaces from serialized MonoBehaviour components
            boardManager = boardManagerComponent;
            pieceController = pieceControllerComponent;
            battleSystem = battleSystemComponent;
            cameraSystem = cameraSystemComponent;

            // Wire camera system into battle system
            if (battleSystemComponent != null && cameraSystemComponent != null)
            {
                battleSystemComponent.SetCameraSystem(cameraSystem);
            }

            // Initialize board state and visuals
            chessEngine.InitializeBoard();
            boardManager.InitializeBoard();

            // Spawn all pieces from both sides
            var allPieces = new List<ChessPiece>();
            allPieces.AddRange(chessEngine.GetAllPieces(PieceColor.White));
            allPieces.AddRange(chessEngine.GetAllPieces(PieceColor.Black));
            pieceController.SpawnPieces(allPieces);

            // Subscribe to input events
            boardManager.OnTileClicked += HandleTileClicked;
            pieceController.OnPieceSelected += HandlePieceSelected;
        }

        private void OnDestroy()
        {
            if (boardManager != null)
                boardManager.OnTileClicked -= HandleTileClicked;
            if (pieceController != null)
                pieceController.OnPieceSelected -= HandlePieceSelected;
        }

        // ── Event handlers ─────────────────────────────────────────────

        /// <summary>
        /// Called when a tile on the board is clicked.
        /// Routes to selection or move execution logic.
        /// </summary>
        private void HandleTileClicked(BoardPosition position)
        {
            if (isProcessingMove || isGameOver || isWaitingForPromotion)
                return;

            // If a piece is selected, check if this is a valid move target
            if (selectedPiecePosition.HasValue)
            {
                Move? targetMove = FindMoveToPosition(position);
                if (targetMove.HasValue)
                {
                    // Task 5.6: Check if this is a promotion move (no type yet)
                    if (IsPromotionMove(targetMove.Value))
                    {
                        StartPromotionFlow(targetMove.Value);
                        return;
                    }

                    StartCoroutine(ExecuteMove(targetMove.Value));
                    return;
                }

                // Task 5.2: Clicking another own piece switches selection
                ChessPiece? pieceAtPos = chessEngine.GetPieceAt(position);
                if (pieceAtPos.HasValue && pieceAtPos.Value.Color == chessEngine.GetCurrentTurn())
                {
                    SelectPiece(position);
                    return;
                }

                // Clicking outside valid moves — deselect
                DeselectPiece();
                return;
            }

            // No piece selected — try to select one
            TrySelectPieceAt(position);
        }

        /// <summary>
        /// Called when a piece GameObject is clicked directly.
        /// </summary>
        private void HandlePieceSelected(BoardPosition position)
        {
            // No-op: piece selection is already handled by HandleTileClicked.
            // This prevents infinite recursion: HandleTileClicked → SelectPiece →
            // PieceController.OnPieceSelected → HandlePieceSelected → HandleTileClicked...
        }

        // ── Task 5.2: Piece selection ──────────────────────────────────

        /// <summary>
        /// Attempts to select a piece at the given position if it belongs
        /// to the current turn player.
        /// </summary>
        private void TrySelectPieceAt(BoardPosition position)
        {
            ChessPiece? piece = chessEngine.GetPieceAt(position);
            if (!piece.HasValue || piece.Value.Color != chessEngine.GetCurrentTurn())
                return;

            SelectPiece(position);
        }

        /// <summary>
        /// Selects a piece: highlights valid moves and updates visual feedback.
        /// </summary>
        private void SelectPiece(BoardPosition position)
        {
            // Clear previous selection if any
            if (selectedPiecePosition.HasValue)
            {
                pieceController.DeselectPiece();
                boardManager.ClearHighlights();
            }

            selectedPiecePosition = position;
            pieceController.SelectPiece(position);

            currentValidMoves = chessEngine.GetValidMoves(position);
            boardManager.HighlightValidMoves(currentValidMoves);
        }

        /// <summary>
        /// Deselects the current piece and clears all highlights.
        /// </summary>
        private void DeselectPiece()
        {
            if (!selectedPiecePosition.HasValue)
                return;

            pieceController.DeselectPiece();
            boardManager.ClearHighlights();
            selectedPiecePosition = null;
            currentValidMoves.Clear();
        }

        // ── Task 5.3 & 5.4: Move execution ────────────────────────────

        /// <summary>
        /// Finds a valid move targeting the given position from currentValidMoves.
        /// For promotion moves, returns the first match (Queen default) since
        /// the promotion UI will handle the actual type selection.
        /// </summary>
        private Move? FindMoveToPosition(BoardPosition target)
        {
            foreach (var move in currentValidMoves)
            {
                if (move.To.File == target.File && move.To.Rank == target.Rank)
                    return move;
            }
            return null;
        }

        /// <summary>
        /// Executes a chess move: updates engine, animates pieces, handles
        /// special moves (castling, en passant, captures), and checks game state.
        /// Normal captures use cinematic battle animations via BattleSystem.
        /// </summary>
        private IEnumerator ExecuteMove(Move move)
        {
            isProcessingMove = true;
            pieceController.SetInputEnabled(false);
            boardManager.ClearHighlights();

            // Task 5.4: Handle capture — determine capture positions and save piece info
            BoardPosition? enPassantCapturePos = null;
            if (move.IsEnPassant)
            {
                // En passant: captured pawn is at (target file, source rank)
                enPassantCapturePos = new BoardPosition(move.To.File, move.From.Rank);
            }

            // Save attacker info BEFORE MakeMove (piece data changes after the move)
            PieceType? attackerType = null;
            GameObject attackerObj = null;
            GameObject defenderObj = null;
            ChessPiece? attackerPiece = chessEngine.GetPieceAt(move.From);
            if (attackerPiece.HasValue)
            {
                attackerType = attackerPiece.Value.Type;
            }

            // Save GameObjects BEFORE any piece removal or movement
            bool useCinematicCapture = false;
            if (move.IsCapture && !move.IsEnPassant && battleSystem != null && attackerType.HasValue)
            {
                attackerObj = pieceController.GetPieceObject(move.From);
                defenderObj = pieceController.GetPieceObject(move.To);
                useCinematicCapture = attackerObj != null && defenderObj != null;
            }

            // Debug: log move details
            Vector3 fromWorld = boardManager.BoardToWorldPosition(move.From);
            Vector3 toWorld = boardManager.BoardToWorldPosition(move.To);
            Debug.Log($"[WizardChess] ExecuteMove: {(attackerType.HasValue ? attackerType.Value.ToString() : "?")} " +
                      $"from ({move.From.File},{move.From.Rank}) to ({move.To.File},{move.To.Rank}) " +
                      $"isCapture={move.IsCapture} | fromWorld={fromWorld} toWorld={toWorld}");

            // Execute the move in the chess engine
            MoveResult result = chessEngine.MakeMove(move);
            if (!result.Success)
            {
                Debug.LogWarning($"[WizardChess] ExecuteMove: ChessEngine.MakeMove FAILED for " +
                                 $"({move.From.File},{move.From.Rank})->({move.To.File},{move.To.Rank})");
                isProcessingMove = false;
                pieceController.SetInputEnabled(true);
                yield break;
            }

            // Clear selection state but keep piece selected in PieceController for MovePieceTo
            selectedPiecePosition = null;
            currentValidMoves.Clear();

            if (result.IsCapture)
            {
                if (enPassantCapturePos.HasValue)
                {
                    // En passant: basic removal (no cinematic battle)
                    pieceController.RemovePiece(enPassantCapturePos.Value);
                    boardManager.RemovePiece(enPassantCapturePos.Value);

                    // Animate the attacker movement, then deselect
                    yield return StartCoroutine(pieceController.MovePieceTo(move.To));
                    pieceController.DeselectPiece();
                }
                else if (useCinematicCapture)
                {
                    // Cinematic capture:
                    // 1. Untrack defender (keep GameObject alive for battle)
                    // 2. Deselect attacker visuals (restore scale/colors before battle)
                    // 3. Move attacker near defender (strike distance)
                    // 4. Face each other, play battle
                    // 5. Destroy defender, move attacker to target tile center
                    // 6. Re-register attacker at new position
                    pieceController.UntrackPiece(move.To);

                    // Deselect attacker visuals BEFORE cinematic movement
                    // (MovePieceTo is NOT called for captures, so we must deselect here)
                    pieceController.DeselectPiece();

                    // Calculate strike position — stop 0.7 tiles away from defender
                    Vector3 dir = (toWorld - fromWorld).normalized;
                    float strikeDistance = 0.7f;
                    Vector3 strikePos = toWorld - dir * strikeDistance;

                    // Trigger walk animation during movement
                    TriggerWalkAnimation(attackerObj, true);

                    // Move attacker to strike position (not on top of defender)
                    yield return StartCoroutine(MoveToWorldPosition(attackerObj, strikePos, 0.6f));

                    // Stop walk animation
                    TriggerWalkAnimation(attackerObj, false);

                    // Face each other
                    if (attackerObj != null && defenderObj != null)
                    {
                        Vector3 toDefender = (defenderObj.transform.position - attackerObj.transform.position);
                        toDefender.y = 0f;
                        if (toDefender.sqrMagnitude > 0.001f)
                        {
                            attackerObj.transform.rotation = Quaternion.LookRotation(toDefender);
                            defenderObj.transform.rotation = Quaternion.LookRotation(-toDefender);
                        }
                    }

                    // Play cinematic battle
                    PieceType defenderType = result.CapturedPiece.HasValue
                        ? result.CapturedPiece.Value.Type
                        : PieceType.Pawn;
                    yield return StartCoroutine(battleSystem.ExecuteCapture(
                        attackerObj, defenderObj, attackerType.Value, defenderType));

                    // Destroy defender after battle
                    if (defenderObj != null)
                        Object.Destroy(defenderObj);

                    // Move attacker to target tile center and ensure active
                    if (attackerObj != null)
                    {
                        if (!attackerObj.activeSelf)
                            attackerObj.SetActive(true);
                        TriggerWalkAnimation(attackerObj, true);
                        yield return StartCoroutine(MoveToWorldPosition(attackerObj, toWorld, 0.3f));
                        TriggerWalkAnimation(attackerObj, false);

                        // Force-snap to exact tile center and ensure active
                        attackerObj.transform.position = toWorld;
                        attackerObj.SetActive(true);
                    }

                    // Re-register attacker at new position in PieceController
                    pieceController.RetrackPiece(attackerObj, move.From, move.To);

                    boardManager.RemovePiece(move.To);

                    // Debug: verify final position
                    if (attackerObj != null)
                    {
                        Debug.Log($"[WizardChess] Cinematic capture complete: piece at " +
                                  $"{attackerObj.transform.position}, expected {toWorld}");
                    }
                }
                else
                {
                    // Fallback: basic capture (battleSystem is null)
                    pieceController.RemovePiece(move.To);
                    boardManager.RemovePiece(move.To);

                    // Animate the attacker movement, then deselect
                    yield return StartCoroutine(pieceController.MovePieceTo(move.To));
                    pieceController.DeselectPiece();
                }
            }
            else
            {
                // Non-capture move: animate movement, then deselect
                yield return StartCoroutine(pieceController.MovePieceTo(move.To));
                pieceController.DeselectPiece();
            }

            // Task 5.3: Handle castling — also move the rook visually
            if (move.IsCastling)
            {
                yield return StartCoroutine(HandleCastlingRook(move));
            }

            // Debug: verify piece is at correct world position after move
            {
                GameObject movedPiece = pieceController.GetPieceObject(move.To);
                if (movedPiece != null)
                {
                    Vector3 expectedPos = boardManager.BoardToWorldPosition(move.To);
                    float drift = Vector3.Distance(
                        new Vector3(movedPiece.transform.position.x, 0f, movedPiece.transform.position.z),
                        new Vector3(expectedPos.x, 0f, expectedPos.z));
                    if (drift > 0.01f)
                    {
                        Debug.LogWarning($"[WizardChess] Position drift detected! Piece at " +
                                         $"{movedPiece.transform.position}, expected {expectedPos}, drift={drift:F3}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[WizardChess] No piece found at ({move.To.File},{move.To.Rank}) " +
                                     $"after move — dictionary may be out of sync!");
                }
            }

            // Task 5.5: Handle game state after move
            HandleGameState(result.NewGameState);

            isProcessingMove = false;
            if (!isGameOver)
            {
                pieceController.SetInputEnabled(true);
            }
        }

        /// <summary>
        /// Moves the rook visually for a castling move.
        /// Kingside: rook from h-file to f-file.
        /// Queenside: rook from a-file to d-file.
        /// </summary>
        private IEnumerator HandleCastlingRook(Move kingMove)
        {
            int rank = kingMove.To.Rank;
            BoardPosition rookFrom;
            BoardPosition rookTo;

            if (kingMove.To.File == 6) // Kingside
            {
                rookFrom = new BoardPosition(7, rank);
                rookTo = new BoardPosition(5, rank);
            }
            else // Queenside (king to file 2)
            {
                rookFrom = new BoardPosition(0, rank);
                rookTo = new BoardPosition(3, rank);
            }

            // Select the rook temporarily to set it as the moving piece
            pieceController.SelectPiece(rookFrom);
            yield return StartCoroutine(pieceController.MovePieceTo(rookTo));
            pieceController.DeselectPiece();
        }

        // ── Task 5.5: Game state handling ──────────────────────────────

        /// <summary>
        /// Processes the game state after a move: check notification,
        /// checkmate/stalemate end game.
        /// </summary>
        private void HandleGameState(GameState state)
        {
            switch (state)
            {
                case GameState.Check:
                    PieceColor checkedPlayer = chessEngine.GetCurrentTurn();
                    Debug.Log($"[WizardChess] Check! {checkedPlayer} king is in check.");
                    break;

                case GameState.Checkmate:
                    isGameOver = true;
                    pieceController.SetInputEnabled(false);
                    // The winner is the player who just moved (opponent of current turn)
                    PieceColor winner = chessEngine.GetCurrentTurn() == PieceColor.White
                        ? PieceColor.Black
                        : PieceColor.White;
                    Debug.Log($"[WizardChess] Checkmate! {winner} wins!");
                    break;

                case GameState.Stalemate:
                    isGameOver = true;
                    pieceController.SetInputEnabled(false);
                    Debug.Log("[WizardChess] Stalemate! The game is a draw.");
                    break;

                case GameState.Ongoing:
                default:
                    break;
            }
        }

        // ── Task 5.6: Pawn promotion ───────────────────────────────────

        /// <summary>
        /// Checks if a move is a pawn promotion (pawn reaching last rank)
        /// that still needs a promotion type selection.
        /// </summary>
        private bool IsPromotionMove(Move move)
        {
            ChessPiece? piece = chessEngine.GetPieceAt(move.From);
            if (!piece.HasValue || piece.Value.Type != PieceType.Pawn)
                return false;

            int lastRank = piece.Value.Color == PieceColor.White ? 7 : 0;
            return move.To.Rank == lastRank;
        }

        /// <summary>
        /// Begins the promotion flow: blocks input and shows the promotion UI.
        /// </summary>
        private void StartPromotionFlow(Move move)
        {
            isWaitingForPromotion = true;
            pieceController.SetInputEnabled(false);

            // Store the base move (without promotion type) for later execution
            pendingPromotionMove = move;
            selectedPromotionType = null;
        }

        /// <summary>
        /// Called when the player selects a promotion piece type from the UI.
        /// Executes the move with the chosen promotion type.
        /// </summary>
        private void OnPromotionSelected(PieceType type)
        {
            selectedPromotionType = type;
            isWaitingForPromotion = false;

            // Build the final move with the selected promotion type
            Move promotionMove = new Move(
                pendingPromotionMove.From,
                pendingPromotionMove.To,
                pendingPromotionMove.IsCapture,
                pendingPromotionMove.IsCastling,
                pendingPromotionMove.IsEnPassant,
                type
            );

            StartCoroutine(ExecuteMove(promotionMove));
        }

        // ── Helper: smooth world position movement ──────────────────

        /// <summary>
        /// Smoothly moves a GameObject to a world position over duration.
        /// Faces the movement direction at the start, then lerps smoothly.
        /// Snaps to exact target at the end.
        /// </summary>
        private IEnumerator MoveToWorldPosition(GameObject obj, Vector3 target, float duration)
        {
            if (obj == null) yield break;
            Vector3 start = obj.transform.position;
            Vector3 moveDir = target - start;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.01f)
                obj.transform.rotation = Quaternion.LookRotation(moveDir);

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
        /// Triggers or stops walk animation on a piece's Animator.
        /// </summary>
        private static void TriggerWalkAnimation(GameObject piece, bool walking)
        {
            if (piece == null) return;
            var animator = piece.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("Walking", walking);
            }
        }

        // ── Promotion UI (OnGUI) ──────────────────────────────────────

        private void OnGUI()
        {
            if (!isWaitingForPromotion)
                return;

            // Semi-transparent overlay
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Panel dimensions
            float panelWidth = 320f;
            float panelHeight = 220f;
            float panelX = (Screen.width - panelWidth) / 2f;
            float panelY = (Screen.height - panelHeight) / 2f;

            // Panel background
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "");

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(panelX, panelY + 10f, panelWidth, 30f), "Pawn Promotion", titleStyle);

            // Buttons for each promotion option
            float buttonWidth = 140f;
            float buttonHeight = 36f;
            float spacing = 6f;
            float startY = panelY + 50f;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16
            };

            for (int i = 0; i < promotionOptions.Length; i++)
            {
                float btnX = panelX + (panelWidth - buttonWidth) / 2f;
                float btnY = startY + i * (buttonHeight + spacing);

                if (GUI.Button(new Rect(btnX, btnY, buttonWidth, buttonHeight),
                    promotionOptions[i].ToString(), buttonStyle))
                {
                    OnPromotionSelected(promotionOptions[i]);
                }
            }
        }
    }
}
