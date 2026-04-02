using System;
using System.Collections.Generic;
using System.Text;
using WizardChess.Interfaces;

namespace WizardChess.Core
{
    /// <summary>
    /// Pure C# chess engine — no Unity dependencies.
    /// Implements standard chess rules, move validation, game state, and FEN support.
    /// </summary>
    public class ChessEngine : IChessEngine
    {
        private ChessPiece?[,] board = new ChessPiece?[8, 8];
        private PieceColor currentTurn;
        private BoardPosition? enPassantTarget;

        /// <summary>
        /// Initializes the board with the standard chess starting position (32 pieces).
        /// White pieces on ranks 0-1, black pieces on ranks 6-7.
        /// </summary>
        public void InitializeBoard()
        {
            board = new ChessPiece?[8, 8];
            currentTurn = PieceColor.White;
            enPassantTarget = null;

            // Back rank piece order: Rook, Knight, Bishop, Queen, King, Bishop, Knight, Rook
            PieceType[] backRank = {
                PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
                PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
            };

            for (int file = 0; file < 8; file++)
            {
                // White back rank (rank 0)
                board[file, 0] = new ChessPiece(backRank[file], PieceColor.White, new BoardPosition(file, 0));
                // White pawns (rank 1)
                board[file, 1] = new ChessPiece(PieceType.Pawn, PieceColor.White, new BoardPosition(file, 1));
                // Black pawns (rank 6)
                board[file, 6] = new ChessPiece(PieceType.Pawn, PieceColor.Black, new BoardPosition(file, 6));
                // Black back rank (rank 7)
                board[file, 7] = new ChessPiece(backRank[file], PieceColor.Black, new BoardPosition(file, 7));
            }
        }

        /// <summary>
        /// Returns the piece at the given board position, or null if the square is empty.
        /// </summary>
        public ChessPiece? GetPieceAt(BoardPosition position)
        {
            if (position.File < 0 || position.File > 7 || position.Rank < 0 || position.Rank > 7)
                return null;

            return board[position.File, position.Rank];
        }

        /// <summary>
        /// Returns all pieces of the given color currently on the board.
        /// </summary>
        public List<ChessPiece> GetAllPieces(PieceColor color)
        {
            var pieces = new List<ChessPiece>();
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    ChessPiece? piece = board[file, rank];
                    if (piece.HasValue && piece.Value.Color == color)
                    {
                        pieces.Add(piece.Value);
                    }
                }
            }
            return pieces;
        }

        /// <summary>
        /// Returns whose turn it is (White starts).
        /// </summary>
        public PieceColor GetCurrentTurn()
        {
            return currentTurn;
        }

        /// <summary>
        /// Loads a board position from a FEN (Forsyth–Edwards Notation) string.
        /// Sets piece placement, active color, castling availability, and en passant target.
        /// Halfmove clock and fullmove number are parsed but not tracked.
        /// </summary>
        /// <param name="fen">A valid FEN string with 6 space-separated fields.</param>
        /// <exception cref="ArgumentException">Thrown when fen is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when fen is malformed.</exception>
        public void LoadFromFen(string fen)
        {
            if (string.IsNullOrEmpty(fen))
                throw new ArgumentException("FEN string cannot be null or empty.", nameof(fen));

            string[] parts = fen.Split(' ');
            if (parts.Length != 6)
                throw new FormatException($"FEN must have 6 space-separated fields, got {parts.Length}.");

            // 1. Clear board and parse piece placement
            board = new ChessPiece?[8, 8];

            string[] ranks = parts[0].Split('/');
            if (ranks.Length != 8)
                throw new FormatException($"FEN piece placement must have 8 ranks separated by '/', got {ranks.Length}.");

            for (int i = 0; i < 8; i++)
            {
                int rank = 7 - i; // FEN starts from rank 8 (index 7) down to rank 1 (index 0)
                int file = 0;
                foreach (char c in ranks[i])
                {
                    if (char.IsDigit(c))
                    {
                        file += c - '0';
                    }
                    else
                    {
                        if (file > 7)
                            throw new FormatException($"FEN rank {8 - i} has too many squares.");

                        PieceColor color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                        PieceType type = CharToPieceType(char.ToUpper(c));
                        // Default HasMoved = true; castling parsing will set false where needed
                        board[file, rank] = new ChessPiece(type, color, new BoardPosition(file, rank), hasMoved: true);
                        file++;
                    }
                }
                if (file != 8)
                    throw new FormatException($"FEN rank {8 - i} does not describe exactly 8 squares.");
            }

            // 2. Active color
            if (parts[1] == "w")
                currentTurn = PieceColor.White;
            else if (parts[1] == "b")
                currentTurn = PieceColor.Black;
            else
                throw new FormatException($"FEN active color must be 'w' or 'b', got '{parts[1]}'.");

            // 3. Castling availability — set HasMoved = false for kings/rooks that can castle
            string castling = parts[2];
            if (castling != "-")
            {
                foreach (char c in castling)
                {
                    switch (c)
                    {
                        case 'K': // White kingside
                            SetHasMovedFalse(4, 0, PieceType.King, PieceColor.White);
                            SetHasMovedFalse(7, 0, PieceType.Rook, PieceColor.White);
                            break;
                        case 'Q': // White queenside
                            SetHasMovedFalse(4, 0, PieceType.King, PieceColor.White);
                            SetHasMovedFalse(0, 0, PieceType.Rook, PieceColor.White);
                            break;
                        case 'k': // Black kingside
                            SetHasMovedFalse(4, 7, PieceType.King, PieceColor.Black);
                            SetHasMovedFalse(7, 7, PieceType.Rook, PieceColor.Black);
                            break;
                        case 'q': // Black queenside
                            SetHasMovedFalse(4, 7, PieceType.King, PieceColor.Black);
                            SetHasMovedFalse(0, 7, PieceType.Rook, PieceColor.Black);
                            break;
                        default:
                            throw new FormatException($"Invalid castling character '{c}' in FEN.");
                    }
                }
            }

            // 4. En passant target
            if (parts[3] == "-")
            {
                enPassantTarget = null;
            }
            else
            {
                if (parts[3].Length != 2)
                    throw new FormatException($"FEN en passant target must be a square like 'e3', got '{parts[3]}'.");
                int epFile = parts[3][0] - 'a';
                int epRank = parts[3][1] - '1';
                if (epFile < 0 || epFile > 7 || epRank < 0 || epRank > 7)
                    throw new FormatException($"FEN en passant target '{parts[3]}' is out of range.");
                enPassantTarget = new BoardPosition(epFile, epRank);
            }

            // 5 & 6. Halfmove clock and fullmove number — ignored (not tracked)
        }

        /// <summary>
        /// Serializes the current board position to a FEN (Forsyth–Edwards Notation) string.
        /// </summary>
        /// <returns>A FEN string representing the current position.</returns>
        public string ToFen()
        {
            var sb = new StringBuilder();

            // 1. Piece placement (rank 8 down to rank 1)
            for (int rank = 7; rank >= 0; rank--)
            {
                int emptyCount = 0;
                for (int file = 0; file < 8; file++)
                {
                    ChessPiece? piece = board[file, rank];
                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        sb.Append(PieceToFenChar(piece.Value));
                    }
                }
                if (emptyCount > 0)
                    sb.Append(emptyCount);
                if (rank > 0)
                    sb.Append('/');
            }

            // 2. Active color
            sb.Append(' ');
            sb.Append(currentTurn == PieceColor.White ? 'w' : 'b');

            // 3. Castling availability
            sb.Append(' ');
            string castling = BuildCastlingString();
            sb.Append(castling.Length > 0 ? castling : "-");

            // 4. En passant target
            sb.Append(' ');
            if (enPassantTarget.HasValue)
            {
                sb.Append((char)('a' + enPassantTarget.Value.File));
                sb.Append((char)('1' + enPassantTarget.Value.Rank));
            }
            else
            {
                sb.Append('-');
            }

            // 5. Halfmove clock (not tracked, default 0)
            sb.Append(" 0");

            // 6. Fullmove number (not tracked, default 1)
            sb.Append(" 1");

            return sb.ToString();
        }

        /// <summary>
        /// Maps a FEN piece character (uppercase) to a PieceType.
        /// </summary>
        private static PieceType CharToPieceType(char c)
        {
            switch (c)
            {
                case 'P': return PieceType.Pawn;
                case 'R': return PieceType.Rook;
                case 'N': return PieceType.Knight;
                case 'B': return PieceType.Bishop;
                case 'Q': return PieceType.Queen;
                case 'K': return PieceType.King;
                default: throw new FormatException($"Invalid FEN piece character '{c}'.");
            }
        }

        /// <summary>
        /// Returns the FEN character for a given chess piece.
        /// White pieces are uppercase, black pieces are lowercase.
        /// </summary>
        private static char PieceToFenChar(ChessPiece piece)
        {
            char c;
            switch (piece.Type)
            {
                case PieceType.Pawn:   c = 'P'; break;
                case PieceType.Rook:   c = 'R'; break;
                case PieceType.Knight: c = 'N'; break;
                case PieceType.Bishop: c = 'B'; break;
                case PieceType.Queen:  c = 'Q'; break;
                case PieceType.King:   c = 'K'; break;
                default: throw new ArgumentException($"Unknown piece type: {piece.Type}");
            }
            return piece.Color == PieceColor.Black ? char.ToLower(c) : c;
        }

        /// <summary>
        /// Builds the castling availability string by checking if kings and rooks
        /// are on their starting squares and have not moved.
        /// </summary>
        private string BuildCastlingString()
        {
            var sb = new StringBuilder(4);

            // White kingside: king at (4,0) not moved AND rook at (7,0) not moved
            if (CanCastle(4, 0, PieceType.King, PieceColor.White) &&
                CanCastle(7, 0, PieceType.Rook, PieceColor.White))
                sb.Append('K');

            // White queenside: king at (4,0) not moved AND rook at (0,0) not moved
            if (CanCastle(4, 0, PieceType.King, PieceColor.White) &&
                CanCastle(0, 0, PieceType.Rook, PieceColor.White))
                sb.Append('Q');

            // Black kingside: king at (4,7) not moved AND rook at (7,7) not moved
            if (CanCastle(4, 7, PieceType.King, PieceColor.Black) &&
                CanCastle(7, 7, PieceType.Rook, PieceColor.Black))
                sb.Append('k');

            // Black queenside: king at (4,7) not moved AND rook at (0,7) not moved
            if (CanCastle(4, 7, PieceType.King, PieceColor.Black) &&
                CanCastle(0, 7, PieceType.Rook, PieceColor.Black))
                sb.Append('q');

            return sb.ToString();
        }

        /// <summary>
        /// Checks if a piece at the given position matches the expected type, color,
        /// and has not moved (eligible for castling).
        /// </summary>
        private bool CanCastle(int file, int rank, PieceType expectedType, PieceColor expectedColor)
        {
            ChessPiece? piece = board[file, rank];
            return piece.HasValue &&
                   piece.Value.Type == expectedType &&
                   piece.Value.Color == expectedColor &&
                   !piece.Value.HasMoved;
        }

        /// <summary>
        /// Sets HasMoved to false for a piece at the given position if it matches
        /// the expected type and color. Used during FEN parsing for castling rights.
        /// </summary>
        private void SetHasMovedFalse(int file, int rank, PieceType expectedType, PieceColor expectedColor)
        {
            ChessPiece? piece = board[file, rank];
            if (piece.HasValue && piece.Value.Type == expectedType && piece.Value.Color == expectedColor)
            {
                board[file, rank] = new ChessPiece(
                    piece.Value.Type,
                    piece.Value.Color,
                    piece.Value.Position,
                    hasMoved: false
                );
            }
        }

        /// <summary>
        /// Executes a move on the board. Validates legality, handles captures,
        /// castling, en passant, pawn promotion, and updates game state.
        /// </summary>
        public MoveResult MakeMove(Move move)
        {
            // 1. Validate: piece must exist and belong to current player
            var piece = board[move.From.File, move.From.Rank];
            if (!piece.HasValue || piece.Value.Color != currentTurn)
                return new MoveResult(false, false, null, GameState.Ongoing);

            // 2. Validate: move must be in the legal moves list
            var validMoves = GetValidMoves(move.From);
            bool isLegal = false;
            foreach (var vm in validMoves)
            {
                if (vm.To.File == move.To.File && vm.To.Rank == move.To.Rank &&
                    vm.IsCastling == move.IsCastling &&
                    vm.IsEnPassant == move.IsEnPassant &&
                    vm.PromotionType == move.PromotionType)
                {
                    isLegal = true;
                    break;
                }
            }
            if (!isLegal)
                return new MoveResult(false, false, null, GameState.Ongoing);

            // 3. Determine captured piece (before modifying the board)
            ChessPiece? capturedPiece = null;
            bool isCapture = false;

            if (move.IsEnPassant)
            {
                // En passant: captured pawn is at (move.To.File, move.From.Rank)
                capturedPiece = board[move.To.File, move.From.Rank];
                isCapture = true;
            }
            else if (board[move.To.File, move.To.Rank].HasValue)
            {
                // Normal capture
                capturedPiece = board[move.To.File, move.To.Rank];
                isCapture = true;
            }

            // 4. Execute the move on the board
            var movingPiece = piece.Value;
            board[move.From.File, move.From.Rank] = null;

            // Handle en passant: remove the captured pawn
            if (move.IsEnPassant)
            {
                board[move.To.File, move.From.Rank] = null;
            }

            // Determine the piece type at destination (may change with promotion)
            PieceType destType = move.PromotionType ?? movingPiece.Type;

            // Place piece at destination with HasMoved = true
            board[move.To.File, move.To.Rank] = new ChessPiece(
                destType, movingPiece.Color, move.To, true);

            // Handle castling: also move the rook
            if (move.IsCastling)
            {
                int rank = move.To.Rank;
                if (move.To.File == 6) // Kingside
                {
                    var rook = board[7, rank];
                    board[7, rank] = null;
                    if (rook.HasValue)
                        board[5, rank] = new ChessPiece(PieceType.Rook, rook.Value.Color, new BoardPosition(5, rank), true);
                }
                else if (move.To.File == 2) // Queenside
                {
                    var rook = board[0, rank];
                    board[0, rank] = null;
                    if (rook.HasValue)
                        board[3, rank] = new ChessPiece(PieceType.Rook, rook.Value.Color, new BoardPosition(3, rank), true);
                }
            }

            // 5. Update en passant target
            if (movingPiece.Type == PieceType.Pawn &&
                Math.Abs(move.To.Rank - move.From.Rank) == 2)
            {
                // Pawn double push: en passant target is the square the pawn passed through
                int passedRank = (move.From.Rank + move.To.Rank) / 2;
                enPassantTarget = new BoardPosition(move.From.File, passedRank);
            }
            else
            {
                enPassantTarget = null;
            }

            // 6. Switch turn
            PieceColor opponent = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            currentTurn = opponent;

            // 7. Determine new game state
            GameState newState = GetGameState();

            return new MoveResult(true, isCapture, capturedPiece, newState);
        }

        public GameState GetGameState()
        {
            if (IsCheckmate())
                return GameState.Checkmate;
            if (IsStalemate())
                return GameState.Stalemate;
            if (IsInCheck(currentTurn))
                return GameState.Check;
            return GameState.Ongoing;
        }

        public bool IsCheckmate()
        {
            return IsInCheck(currentTurn) && !HasAnyLegalMoves(currentTurn);
        }

        public bool IsStalemate()
        {
            return !IsInCheck(currentTurn) && !HasAnyLegalMoves(currentTurn);
        }

        /// <summary>
        /// Returns true if any piece of the given color has at least one legal move.
        /// Temporarily sets currentTurn to the given color so GetValidMoves works correctly.
        /// </summary>
        private bool HasAnyLegalMoves(PieceColor color)
        {
            PieceColor savedTurn = currentTurn;
            currentTurn = color;
            try
            {
                for (int file = 0; file < 8; file++)
                {
                    for (int rank = 0; rank < 8; rank++)
                    {
                        var piece = board[file, rank];
                        if (piece.HasValue && piece.Value.Color == color)
                        {
                            var moves = GetValidMoves(new BoardPosition(file, rank));
                            if (moves.Count > 0)
                                return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                currentTurn = savedTurn;
            }
        }

        // --- Implemented: GetValidMoves, IsInCheck, helpers ---

        /// <summary>
        /// Returns all legal moves for the piece at the given position.
        /// Returns empty list if position is empty or piece doesn't belong to current turn.
        /// Filters out moves that would leave own king in check.
        /// </summary>
        public List<Move> GetValidMoves(BoardPosition piecePosition)
        {
            var piece = GetPieceAt(piecePosition);
            if (!piece.HasValue || piece.Value.Color != currentTurn)
                return new List<Move>();

            var pseudoLegal = GetPseudoLegalMoves(piece.Value);
            var legal = new List<Move>();

            foreach (var move in pseudoLegal)
            {
                if (IsMoveLegal(move, piece.Value.Color))
                    legal.Add(move);
            }

            return legal;
        }

        /// <summary>
        /// Returns true if the king of the given color is currently in check.
        /// </summary>
        public bool IsInCheck(PieceColor color)
        {
            var kingPos = FindKing(color);
            if (!kingPos.HasValue)
                return false;

            PieceColor opponent = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            return IsSquareAttackedBy(kingPos.Value, opponent);
        }

        // --- Private helpers ---

        private bool IsOnBoard(int file, int rank)
        {
            return file >= 0 && file <= 7 && rank >= 0 && rank <= 7;
        }

        private BoardPosition? FindKing(PieceColor color)
        {
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    var p = board[f, r];
                    if (p.HasValue && p.Value.Type == PieceType.King && p.Value.Color == color)
                        return new BoardPosition(f, r);
                }
            return null;
        }

        /// <summary>
        /// Checks if a square is attacked by any piece of the given attacker color.
        /// </summary>
        private bool IsSquareAttackedBy(BoardPosition pos, PieceColor attackerColor)
        {
            // Pawn attacks
            int pawnDir = attackerColor == PieceColor.White ? 1 : -1;
            // A pawn on (pos.File±1, pos.Rank-pawnDir) attacks pos
            foreach (int df in new[] { -1, 1 })
            {
                int pf = pos.File + df;
                int pr = pos.Rank - pawnDir;
                if (IsOnBoard(pf, pr))
                {
                    var p = board[pf, pr];
                    if (p.HasValue && p.Value.Color == attackerColor && p.Value.Type == PieceType.Pawn)
                        return true;
                }
            }

            // Knight attacks
            int[][] knightOffsets = {
                new[] {-2, -1}, new[] {-2, 1}, new[] {-1, -2}, new[] {-1, 2},
                new[] {1, -2}, new[] {1, 2}, new[] {2, -1}, new[] {2, 1}
            };
            foreach (var off in knightOffsets)
            {
                int nf = pos.File + off[0], nr = pos.Rank + off[1];
                if (IsOnBoard(nf, nr))
                {
                    var p = board[nf, nr];
                    if (p.HasValue && p.Value.Color == attackerColor && p.Value.Type == PieceType.Knight)
                        return true;
                }
            }

            // King attacks (one square in any direction)
            for (int df = -1; df <= 1; df++)
                for (int dr = -1; dr <= 1; dr++)
                {
                    if (df == 0 && dr == 0) continue;
                    int kf = pos.File + df, kr = pos.Rank + dr;
                    if (IsOnBoard(kf, kr))
                    {
                        var p = board[kf, kr];
                        if (p.HasValue && p.Value.Color == attackerColor && p.Value.Type == PieceType.King)
                            return true;
                    }
                }

            // Sliding attacks: Rook/Queen (straight), Bishop/Queen (diagonal)
            // Straight directions (rook + queen)
            int[][] straightDirs = { new[] {0, 1}, new[] {0, -1}, new[] {1, 0}, new[] {-1, 0} };
            foreach (var dir in straightDirs)
            {
                for (int i = 1; i < 8; i++)
                {
                    int sf = pos.File + dir[0] * i, sr = pos.Rank + dir[1] * i;
                    if (!IsOnBoard(sf, sr)) break;
                    var p = board[sf, sr];
                    if (p.HasValue)
                    {
                        if (p.Value.Color == attackerColor &&
                            (p.Value.Type == PieceType.Rook || p.Value.Type == PieceType.Queen))
                            return true;
                        break; // blocked
                    }
                }
            }

            // Diagonal directions (bishop + queen)
            int[][] diagDirs = { new[] {1, 1}, new[] {1, -1}, new[] {-1, 1}, new[] {-1, -1} };
            foreach (var dir in diagDirs)
            {
                for (int i = 1; i < 8; i++)
                {
                    int sf = pos.File + dir[0] * i, sr = pos.Rank + dir[1] * i;
                    if (!IsOnBoard(sf, sr)) break;
                    var p = board[sf, sr];
                    if (p.HasValue)
                    {
                        if (p.Value.Color == attackerColor &&
                            (p.Value.Type == PieceType.Bishop || p.Value.Type == PieceType.Queen))
                            return true;
                        break; // blocked
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Generates all pseudo-legal moves for a piece (without check filtering).
        /// </summary>
        private List<Move> GetPseudoLegalMoves(ChessPiece piece)
        {
            switch (piece.Type)
            {
                case PieceType.Pawn:   return GetPawnMoves(piece);
                case PieceType.Rook:   return GetSlidingMoves(piece, straight: true, diagonal: false);
                case PieceType.Knight: return GetKnightMoves(piece);
                case PieceType.Bishop: return GetSlidingMoves(piece, straight: false, diagonal: true);
                case PieceType.Queen:  return GetSlidingMoves(piece, straight: true, diagonal: true);
                case PieceType.King:   return GetKingMoves(piece);
                default: return new List<Move>();
            }
        }

        private List<Move> GetPawnMoves(ChessPiece pawn)
        {
            var moves = new List<Move>();
            int dir = pawn.Color == PieceColor.White ? 1 : -1;
            int startRank = pawn.Color == PieceColor.White ? 1 : 6;
            int promoRank = pawn.Color == PieceColor.White ? 7 : 0;
            int f = pawn.Position.File;
            int r = pawn.Position.Rank;

            // Forward one
            int newR = r + dir;
            if (IsOnBoard(f, newR) && !board[f, newR].HasValue)
            {
                if (newR == promoRank)
                    AddPromotionMoves(moves, pawn.Position, new BoardPosition(f, newR), false);
                else
                    moves.Add(new Move(pawn.Position, new BoardPosition(f, newR)));

                // Forward two from starting rank
                int twoR = r + 2 * dir;
                if (r == startRank && IsOnBoard(f, twoR) && !board[f, twoR].HasValue)
                    moves.Add(new Move(pawn.Position, new BoardPosition(f, twoR)));
            }

            // Diagonal captures
            foreach (int df in new[] { -1, 1 })
            {
                int cf = f + df;
                if (!IsOnBoard(cf, newR)) continue;

                var target = board[cf, newR];
                if (target.HasValue && target.Value.Color != pawn.Color)
                {
                    if (newR == promoRank)
                        AddPromotionMoves(moves, pawn.Position, new BoardPosition(cf, newR), true);
                    else
                        moves.Add(new Move(pawn.Position, new BoardPosition(cf, newR), isCapture: true));
                }

                // En passant
                if (enPassantTarget.HasValue &&
                    enPassantTarget.Value.File == cf && enPassantTarget.Value.Rank == newR)
                {
                    moves.Add(new Move(pawn.Position, new BoardPosition(cf, newR),
                        isCapture: true, isEnPassant: true));
                }
            }

            return moves;
        }

        private void AddPromotionMoves(List<Move> moves, BoardPosition from, BoardPosition to, bool isCapture)
        {
            moves.Add(new Move(from, to, isCapture, promotionType: PieceType.Queen));
            moves.Add(new Move(from, to, isCapture, promotionType: PieceType.Rook));
            moves.Add(new Move(from, to, isCapture, promotionType: PieceType.Bishop));
            moves.Add(new Move(from, to, isCapture, promotionType: PieceType.Knight));
        }

        private List<Move> GetKnightMoves(ChessPiece knight)
        {
            var moves = new List<Move>();
            int[][] offsets = {
                new[] {-2, -1}, new[] {-2, 1}, new[] {-1, -2}, new[] {-1, 2},
                new[] {1, -2}, new[] {1, 2}, new[] {2, -1}, new[] {2, 1}
            };
            foreach (var off in offsets)
            {
                int nf = knight.Position.File + off[0], nr = knight.Position.Rank + off[1];
                if (!IsOnBoard(nf, nr)) continue;
                var target = board[nf, nr];
                if (target.HasValue && target.Value.Color == knight.Color) continue;
                bool capture = target.HasValue;
                moves.Add(new Move(knight.Position, new BoardPosition(nf, nr), isCapture: capture));
            }
            return moves;
        }

        private List<Move> GetSlidingMoves(ChessPiece piece, bool straight, bool diagonal)
        {
            var moves = new List<Move>();
            var dirs = new List<int[]>();

            if (straight)
            {
                dirs.Add(new[] {0, 1});
                dirs.Add(new[] {0, -1});
                dirs.Add(new[] {1, 0});
                dirs.Add(new[] {-1, 0});
            }
            if (diagonal)
            {
                dirs.Add(new[] {1, 1});
                dirs.Add(new[] {1, -1});
                dirs.Add(new[] {-1, 1});
                dirs.Add(new[] {-1, -1});
            }

            foreach (var dir in dirs)
            {
                for (int i = 1; i < 8; i++)
                {
                    int nf = piece.Position.File + dir[0] * i;
                    int nr = piece.Position.Rank + dir[1] * i;
                    if (!IsOnBoard(nf, nr)) break;

                    var target = board[nf, nr];
                    if (target.HasValue)
                    {
                        if (target.Value.Color != piece.Color)
                            moves.Add(new Move(piece.Position, new BoardPosition(nf, nr), isCapture: true));
                        break; // blocked
                    }
                    moves.Add(new Move(piece.Position, new BoardPosition(nf, nr)));
                }
            }

            return moves;
        }

        private List<Move> GetKingMoves(ChessPiece king)
        {
            var moves = new List<Move>();

            // Normal king moves (one square in any direction)
            for (int df = -1; df <= 1; df++)
                for (int dr = -1; dr <= 1; dr++)
                {
                    if (df == 0 && dr == 0) continue;
                    int nf = king.Position.File + df, nr = king.Position.Rank + dr;
                    if (!IsOnBoard(nf, nr)) continue;
                    var target = board[nf, nr];
                    if (target.HasValue && target.Value.Color == king.Color) continue;
                    bool capture = target.HasValue;
                    moves.Add(new Move(king.Position, new BoardPosition(nf, nr), isCapture: capture));
                }

            // Castling
            if (!king.HasMoved)
            {
                PieceColor opponent = king.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
                int rank = king.Color == PieceColor.White ? 0 : 7;

                // Don't castle out of check
                if (!IsSquareAttackedBy(king.Position, opponent))
                {
                    // Kingside castling (king moves to file 6, rook on file 7)
                    var kRook = board[7, rank];
                    if (kRook.HasValue && kRook.Value.Type == PieceType.Rook &&
                        kRook.Value.Color == king.Color && !kRook.Value.HasMoved)
                    {
                        if (!board[5, rank].HasValue && !board[6, rank].HasValue &&
                            !IsSquareAttackedBy(new BoardPosition(5, rank), opponent) &&
                            !IsSquareAttackedBy(new BoardPosition(6, rank), opponent))
                        {
                            moves.Add(new Move(king.Position, new BoardPosition(6, rank), isCastling: true));
                        }
                    }

                    // Queenside castling (king moves to file 2, rook on file 0)
                    var qRook = board[0, rank];
                    if (qRook.HasValue && qRook.Value.Type == PieceType.Rook &&
                        qRook.Value.Color == king.Color && !qRook.Value.HasMoved)
                    {
                        if (!board[1, rank].HasValue && !board[2, rank].HasValue && !board[3, rank].HasValue &&
                            !IsSquareAttackedBy(new BoardPosition(2, rank), opponent) &&
                            !IsSquareAttackedBy(new BoardPosition(3, rank), opponent))
                        {
                            moves.Add(new Move(king.Position, new BoardPosition(2, rank), isCastling: true));
                        }
                    }
                }
            }

            return moves;
        }

        /// <summary>
        /// Simulates a move on a copy of the board and checks if the moving side's king is safe.
        /// </summary>
        private bool IsMoveLegal(Move move, PieceColor color)
        {
            // Save board state
            var savedBoard = new ChessPiece?[8, 8];
            Array.Copy(board, savedBoard, board.Length);

            // Apply move on the real board temporarily
            var piece = board[move.From.File, move.From.Rank];
            board[move.From.File, move.From.Rank] = null;

            // Handle en passant capture: remove the captured pawn
            if (move.IsEnPassant)
            {
                int capturedPawnRank = move.From.Rank; // the captured pawn is on the same rank as the moving pawn
                board[move.To.File, capturedPawnRank] = null;
            }

            // Place piece at destination
            if (piece.HasValue)
            {
                board[move.To.File, move.To.Rank] = new ChessPiece(
                    piece.Value.Type, piece.Value.Color, move.To, true);
            }

            // Handle castling: also move the rook
            if (move.IsCastling && piece.HasValue)
            {
                int rank = move.To.Rank;
                if (move.To.File == 6) // kingside
                {
                    var rook = board[7, rank];
                    board[7, rank] = null;
                    if (rook.HasValue)
                        board[5, rank] = new ChessPiece(PieceType.Rook, rook.Value.Color, new BoardPosition(5, rank), true);
                }
                else if (move.To.File == 2) // queenside
                {
                    var rook = board[0, rank];
                    board[0, rank] = null;
                    if (rook.HasValue)
                        board[3, rank] = new ChessPiece(PieceType.Rook, rook.Value.Color, new BoardPosition(3, rank), true);
                }
            }

            bool inCheck = IsInCheck(color);

            // Restore board state
            Array.Copy(savedBoard, board, board.Length);

            return !inCheck;
        }
    }
}
