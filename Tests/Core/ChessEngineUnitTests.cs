using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WizardChess.Core;

namespace WizardChess.Tests.Core
{
    /// <summary>
    /// Comprehensive unit tests for ChessEngine covering move validation,
    /// check, checkmate, stalemate, castling, en passant, and pawn promotion.
    /// </summary>
    [TestFixture]
    public class ChessEngineUnitTests
    {
        private ChessEngine engine;

        [SetUp]
        public void SetUp()
        {
            engine = new ChessEngine();
        }

        // ----------------------------------------------------------------
        // Helper methods
        // ----------------------------------------------------------------

        private static BoardPosition Pos(int file, int rank) => new BoardPosition(file, rank);

        /// <summary>
        /// Converts algebraic notation (e.g. "e2") to BoardPosition.
        /// </summary>
        private static BoardPosition Sq(string algebraic)
        {
            int file = algebraic[0] - 'a';
            int rank = algebraic[1] - '1';
            return new BoardPosition(file, rank);
        }

        private bool HasMoveTo(List<Move> moves, BoardPosition to)
        {
            return moves.Any(m => m.To.File == to.File && m.To.Rank == to.Rank);
        }

        private bool HasMoveTo(List<Move> moves, string algebraic)
        {
            var pos = Sq(algebraic);
            return HasMoveTo(moves, pos);
        }

        private MoveResult MakeMove(string from, string to,
            bool isCastling = false, bool isEnPassant = false,
            PieceType? promotionType = null)
        {
            var fromPos = Sq(from);
            var toPos = Sq(to);
            var moves = engine.GetValidMoves(fromPos);
            bool isCapture = moves.Any(m =>
                m.To.File == toPos.File && m.To.Rank == toPos.Rank && m.IsCapture);
            var move = new Move(fromPos, toPos, isCapture, isCastling, isEnPassant, promotionType);
            return engine.MakeMove(move);
        }

        // ================================================================
        // 1. Initial board setup
        // ================================================================

        [Test]
        public void InitializeBoard_WhitePawnsOnRank2()
        {
            engine.InitializeBoard();
            for (int file = 0; file < 8; file++)
            {
                var piece = engine.GetPieceAt(Pos(file, 1));
                Assert.IsNotNull(piece);
                Assert.AreEqual(PieceType.Pawn, piece.Value.Type);
                Assert.AreEqual(PieceColor.White, piece.Value.Color);
            }
        }

        [Test]
        public void InitializeBoard_BlackPawnsOnRank7()
        {
            engine.InitializeBoard();
            for (int file = 0; file < 8; file++)
            {
                var piece = engine.GetPieceAt(Pos(file, 6));
                Assert.IsNotNull(piece);
                Assert.AreEqual(PieceType.Pawn, piece.Value.Type);
                Assert.AreEqual(PieceColor.Black, piece.Value.Color);
            }
        }

        [Test]
        public void InitializeBoard_WhiteStartsFirst()
        {
            engine.InitializeBoard();
            Assert.AreEqual(PieceColor.White, engine.GetCurrentTurn());
        }

        [Test]
        public void InitializeBoard_16PiecesPerSide()
        {
            engine.InitializeBoard();
            Assert.AreEqual(16, engine.GetAllPieces(PieceColor.White).Count);
            Assert.AreEqual(16, engine.GetAllPieces(PieceColor.Black).Count);
        }

        // ================================================================
        // 2. Pawn move validation
        // ================================================================

        [Test]
        public void Pawn_CanMoveOneSquareForward()
        {
            engine.InitializeBoard();
            var moves = engine.GetValidMoves(Sq("e2"));
            Assert.IsTrue(HasMoveTo(moves, "e3"));
        }

        [Test]
        public void Pawn_CanMoveTwoSquaresFromStartingRank()
        {
            engine.InitializeBoard();
            var moves = engine.GetValidMoves(Sq("e2"));
            Assert.IsTrue(HasMoveTo(moves, "e4"));
        }

        [Test]
        public void Pawn_CannotMoveTwoSquaresAfterMoving()
        {
            engine.InitializeBoard();
            MakeMove("e2", "e3"); // white
            MakeMove("a7", "a6"); // black
            var moves = engine.GetValidMoves(Sq("e3"));
            Assert.IsFalse(HasMoveTo(moves, "e5"));
        }

        [Test]
        public void Pawn_CanCapturesDiagonally()
        {
            // Set up position where white pawn on e4 can capture black pawn on d5
            engine.LoadFromFen("rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("e4"));
            Assert.IsTrue(HasMoveTo(moves, "d5"));
        }

        [Test]
        public void Pawn_CannotMoveForwardIntoOccupiedSquare()
        {
            // White pawn on e4, black pawn on e5 — blocked
            engine.LoadFromFen("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("e4"));
            Assert.IsFalse(HasMoveTo(moves, "e5"));
        }

        // ================================================================
        // 3. Knight move validation
        // ================================================================

        [Test]
        public void Knight_HasCorrectMovesFromStartingPosition()
        {
            engine.InitializeBoard();
            var moves = engine.GetValidMoves(Sq("b1"));
            Assert.AreEqual(2, moves.Count);
            Assert.IsTrue(HasMoveTo(moves, "a3"));
            Assert.IsTrue(HasMoveTo(moves, "c3"));
        }

        [Test]
        public void Knight_CanJumpOverPieces()
        {
            engine.InitializeBoard();
            // Knight on b1 can move even though surrounded by pawns
            var moves = engine.GetValidMoves(Sq("g1"));
            Assert.IsTrue(moves.Count > 0);
        }

        // ================================================================
        // 4. Bishop, Rook, Queen move validation
        // ================================================================

        [Test]
        public void Bishop_MovesDiagonally()
        {
            // Bishop on c4, open diagonals
            engine.LoadFromFen("rnbqkbnr/pppp1ppp/8/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("c4"));
            Assert.IsTrue(HasMoveTo(moves, "d5"));
            Assert.IsTrue(HasMoveTo(moves, "b3"));
            // Cannot move straight
            Assert.IsFalse(HasMoveTo(moves, "c5"));
        }

        [Test]
        public void Rook_MovesStraight()
        {
            // Rook on e4, white king on a1, black king on h8
            engine.LoadFromFen("7k/8/8/8/4R3/8/8/K7 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e4"));
            Assert.IsTrue(HasMoveTo(moves, "e8")); // up
            Assert.IsTrue(HasMoveTo(moves, "e1")); // down
            Assert.IsTrue(HasMoveTo(moves, "a4")); // left
            Assert.IsTrue(HasMoveTo(moves, "h4")); // right
            // Cannot move diagonally
            Assert.IsFalse(HasMoveTo(moves, "f5"));
        }

        [Test]
        public void Queen_MovesStraightAndDiagonally()
        {
            engine.LoadFromFen("4k3/8/8/8/4Q3/8/8/4K3 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e4"));
            Assert.IsTrue(HasMoveTo(moves, "e8")); // straight
            Assert.IsTrue(HasMoveTo(moves, "a4")); // straight
            Assert.IsTrue(HasMoveTo(moves, "h7")); // diagonal
            Assert.IsTrue(HasMoveTo(moves, "b1")); // diagonal
        }

        // ================================================================
        // 5. King move validation
        // ================================================================

        [Test]
        public void King_MovesOneSquareInAnyDirection()
        {
            engine.LoadFromFen("4k3/8/8/8/4K3/8/8/8 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e4"));
            // King should have 8 moves (all adjacent squares, minus those near enemy king)
            Assert.IsTrue(HasMoveTo(moves, "e5"));
            Assert.IsTrue(HasMoveTo(moves, "d4"));
            Assert.IsTrue(HasMoveTo(moves, "f4"));
            Assert.IsTrue(HasMoveTo(moves, "d3"));
            Assert.IsTrue(HasMoveTo(moves, "f3"));
            Assert.IsTrue(HasMoveTo(moves, "e3"));
        }

        [Test]
        public void King_CannotMoveIntoCheck()
        {
            // Black rook on d8 controls the d-file — king cannot move to d-file squares
            engine.LoadFromFen("3rk3/8/8/8/4K3/8/8/8 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e4"));
            Assert.IsFalse(HasMoveTo(moves, "d4")); // rook controls d-file
            Assert.IsFalse(HasMoveTo(moves, "d3")); // rook controls d-file
            Assert.IsFalse(HasMoveTo(moves, "d5")); // rook controls d-file
        }

        // ================================================================
        // 6. Check detection
        // ================================================================

        [Test]
        public void IsInCheck_ReturnsTrueWhenKingAttacked()
        {
            // White king on e1, black rook on e8 — white is in check
            engine.LoadFromFen("4r2k/8/8/8/8/8/8/4K3 w - - 0 1");
            Assert.IsTrue(engine.IsInCheck(PieceColor.White));
        }

        [Test]
        public void IsInCheck_ReturnsFalseWhenKingSafe()
        {
            engine.InitializeBoard();
            Assert.IsFalse(engine.IsInCheck(PieceColor.White));
            Assert.IsFalse(engine.IsInCheck(PieceColor.Black));
        }

        [Test]
        public void MoveIntoCheck_IsNotAllowed()
        {
            // White king on e1, black rook on d8 — king cannot move to d1 or d2
            engine.LoadFromFen("3r3k/8/8/8/8/8/8/4K3 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsFalse(HasMoveTo(moves, "d1"));
            Assert.IsFalse(HasMoveTo(moves, "d2"));
        }

        [Test]
        public void PinnedPiece_CannotMoveAwayFromPin()
        {
            // White bishop on d2 is pinned by black rook on d8 (king on d1)
            engine.LoadFromFen("3r3k/8/8/8/8/8/3B4/3K4 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("d2"));
            // Bishop is pinned along d-file, can only move along d-file (but bishop moves diagonally)
            // So bishop has no legal moves
            Assert.AreEqual(0, moves.Count);
        }

        [Test]
        public void GameState_ReturnsCheck_WhenInCheck()
        {
            // After a move that gives check
            engine.LoadFromFen("4r2k/8/8/8/8/8/8/4K3 w - - 0 1");
            Assert.AreEqual(GameState.Check, engine.GetGameState());
        }

        // ================================================================
        // 7. Checkmate
        // ================================================================

        [Test]
        public void IsCheckmate_ScholarsMate()
        {
            // Scholar's mate final position: black is checkmated
            engine.LoadFromFen("r1bqkb1r/pppp1Qpp/2n2n2/4p3/2B1P3/8/PPPP1PPP/RNB1K1NR b KQkq - 0 1");
            Assert.IsTrue(engine.IsCheckmate());
            Assert.AreEqual(GameState.Checkmate, engine.GetGameState());
        }

        [Test]
        public void IsCheckmate_BackRankMate()
        {
            // White rook delivers back rank mate
            engine.LoadFromFen("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");
            // Make the mating move: Ra1-a8
            var result = MakeMove("a1", "a8");
            Assert.IsTrue(result.Success);
            Assert.AreEqual(GameState.Checkmate, result.NewGameState);
        }

        [Test]
        public void IsCheckmate_ReturnsFalseWhenCanBlock()
        {
            // King in check but can block
            engine.LoadFromFen("4k3/8/8/8/8/8/3q4/R3K3 w - - 0 1");
            // White is in check from d2 queen, but rook can block or king can move
            Assert.IsFalse(engine.IsCheckmate());
        }

        // ================================================================
        // 8. Stalemate
        // ================================================================

        [Test]
        public void IsStalemate_KingTrapped()
        {
            // Black king on a8, white queen on b6, white king on c8 — black to move, stalemate
            engine.LoadFromFen("k7/8/1Q6/8/8/8/8/2K5 b - - 0 1");
            Assert.IsTrue(engine.IsStalemate());
            Assert.AreEqual(GameState.Stalemate, engine.GetGameState());
        }

        [Test]
        public void IsStalemate_ReturnsFalseWhenHasLegalMoves()
        {
            engine.InitializeBoard();
            Assert.IsFalse(engine.IsStalemate());
        }

        // ================================================================
        // 9. Castling
        // ================================================================

        [Test]
        public void Castling_KingsideWhite()
        {
            // White can castle kingside: king e1, rook h1, squares f1 g1 empty
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsTrue(moves.Any(m => m.To.File == 6 && m.To.Rank == 0 && m.IsCastling),
                "White should be able to castle kingside");
        }

        [Test]
        public void Castling_QueensideWhite()
        {
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsTrue(moves.Any(m => m.To.File == 2 && m.To.Rank == 0 && m.IsCastling),
                "White should be able to castle queenside");
        }

        [Test]
        public void Castling_KingsideBlack()
        {
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R b KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("e8"));
            Assert.IsTrue(moves.Any(m => m.To.File == 6 && m.To.Rank == 7 && m.IsCastling),
                "Black should be able to castle kingside");
        }

        [Test]
        public void Castling_ExecuteKingside_MovesRook()
        {
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");
            var result = MakeMove("e1", "g1", isCastling: true);
            Assert.IsTrue(result.Success);
            // King should be on g1
            var king = engine.GetPieceAt(Sq("g1"));
            Assert.IsNotNull(king);
            Assert.AreEqual(PieceType.King, king.Value.Type);
            // Rook should have moved from h1 to f1
            var rook = engine.GetPieceAt(Sq("f1"));
            Assert.IsNotNull(rook);
            Assert.AreEqual(PieceType.Rook, rook.Value.Type);
            // h1 should be empty
            Assert.IsNull(engine.GetPieceAt(Sq("h1")));
        }

        [Test]
        public void Castling_ExecuteQueenside_MovesRook()
        {
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");
            var result = MakeMove("e1", "c1", isCastling: true);
            Assert.IsTrue(result.Success);
            // King on c1
            var king = engine.GetPieceAt(Sq("c1"));
            Assert.IsNotNull(king);
            Assert.AreEqual(PieceType.King, king.Value.Type);
            // Rook moved from a1 to d1
            var rook = engine.GetPieceAt(Sq("d1"));
            Assert.IsNotNull(rook);
            Assert.AreEqual(PieceType.Rook, rook.Value.Type);
            Assert.IsNull(engine.GetPieceAt(Sq("a1")));
        }

        [Test]
        public void Castling_NotAllowedWhenKingHasMoved()
        {
            // No castling rights (dash in FEN)
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsFalse(moves.Any(m => m.IsCastling),
                "Castling should not be allowed when rights are absent");
        }

        [Test]
        public void Castling_NotAllowedThroughCheck()
        {
            // Black rook on f8, f-file open — attacks f1, white cannot castle kingside
            engine.LoadFromFen("5r1k/8/8/8/8/8/PPPPP1PP/R3K2R w KQ - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsFalse(moves.Any(m => m.To.File == 6 && m.IsCastling),
                "Cannot castle through attacked square");
        }

        [Test]
        public void Castling_NotAllowedWhileInCheck()
        {
            // White king in check from black rook on e8 (f-file open)
            engine.LoadFromFen("4r1k1/8/8/8/8/8/PPPP1PPP/R3K2R w KQ - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsFalse(moves.Any(m => m.IsCastling),
                "Cannot castle while in check");
        }

        [Test]
        public void Castling_NotAllowedWhenPathBlocked()
        {
            // Bishop on f1 blocks kingside castling
            engine.LoadFromFen("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3KB1R w KQkq - 0 1");
            var moves = engine.GetValidMoves(Sq("e1"));
            Assert.IsFalse(moves.Any(m => m.To.File == 6 && m.IsCastling),
                "Cannot castle when path is blocked");
        }

        // ================================================================
        // 10. En passant
        // ================================================================

        [Test]
        public void EnPassant_WhiteCapturesBlackPawn()
        {
            // White pawn on e5, black just played d7-d5 — en passant target d6
            engine.LoadFromFen("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1");
            var moves = engine.GetValidMoves(Sq("e5"));
            Assert.IsTrue(moves.Any(m => m.To.File == 3 && m.To.Rank == 5 && m.IsEnPassant),
                "White pawn should be able to capture en passant on d6");
        }

        [Test]
        public void EnPassant_Execute_RemovesCapturedPawn()
        {
            engine.LoadFromFen("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1");
            var result = MakeMove("e5", "d6", isEnPassant: true);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsCapture);
            // White pawn should be on d6
            var piece = engine.GetPieceAt(Sq("d6"));
            Assert.IsNotNull(piece);
            Assert.AreEqual(PieceType.Pawn, piece.Value.Type);
            Assert.AreEqual(PieceColor.White, piece.Value.Color);
            // Black pawn on d5 should be removed
            Assert.IsNull(engine.GetPieceAt(Sq("d5")));
        }

        [Test]
        public void EnPassant_BlackCapturesWhitePawn()
        {
            // Black pawn on d4, white just played e2-e4 — en passant target e3
            engine.LoadFromFen("rnbqkbnr/ppp1pppp/8/8/3pP3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");
            var moves = engine.GetValidMoves(Sq("d4"));
            Assert.IsTrue(moves.Any(m => m.To.File == 4 && m.To.Rank == 2 && m.IsEnPassant),
                "Black pawn should be able to capture en passant on e3");
        }

        [Test]
        public void EnPassant_OnlyAvailableImmediatelyAfterDoublePush()
        {
            engine.InitializeBoard();
            MakeMove("e2", "e4"); // white double push
            MakeMove("a7", "a6"); // black plays something else
            MakeMove("e4", "e5"); // white advances
            MakeMove("d7", "d5"); // black double push — en passant available
            // White can capture en passant
            var moves = engine.GetValidMoves(Sq("e5"));
            Assert.IsTrue(moves.Any(m => m.IsEnPassant), "En passant should be available right after double push");
        }

        [Test]
        public void EnPassant_DisappearsAfterOneMove()
        {
            engine.InitializeBoard();
            MakeMove("e2", "e4"); // white double push
            MakeMove("d7", "d5"); // black double push — en passant available for white
            MakeMove("a2", "a3"); // white plays something else instead of en passant
            MakeMove("a6", "a5"); // need valid black move — let's use a different setup

            // Simpler: use FEN to test that en passant disappears
            engine.LoadFromFen("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1");
            MakeMove("a2", "a3"); // white doesn't take en passant
            // Now it's black's turn, then white — en passant should be gone
            MakeMove("a7", "a6"); // black move
            var moves = engine.GetValidMoves(Sq("e5"));
            Assert.IsFalse(moves.Any(m => m.IsEnPassant),
                "En passant should no longer be available after the opportunity was missed");
        }

        // ================================================================
        // 11. Pawn promotion
        // ================================================================

        [Test]
        public void Promotion_PawnOnSeventhRank_HasPromotionMoves()
        {
            // White pawn on e7, can promote on e8 (black king on h8, out of the way)
            engine.LoadFromFen("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e7"));
            // Should have 4 promotion options: Queen, Rook, Bishop, Knight
            var promoMoves = moves.Where(m => m.PromotionType.HasValue).ToList();
            Assert.AreEqual(4, promoMoves.Count, "Should have 4 promotion choices");
            Assert.IsTrue(promoMoves.Any(m => m.PromotionType == PieceType.Queen));
            Assert.IsTrue(promoMoves.Any(m => m.PromotionType == PieceType.Rook));
            Assert.IsTrue(promoMoves.Any(m => m.PromotionType == PieceType.Bishop));
            Assert.IsTrue(promoMoves.Any(m => m.PromotionType == PieceType.Knight));
        }

        [Test]
        public void Promotion_ExecuteQueenPromotion()
        {
            engine.LoadFromFen("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");
            var result = MakeMove("e7", "e8", promotionType: PieceType.Queen);
            Assert.IsTrue(result.Success);
            var piece = engine.GetPieceAt(Sq("e8"));
            Assert.IsNotNull(piece);
            Assert.AreEqual(PieceType.Queen, piece.Value.Type);
            Assert.AreEqual(PieceColor.White, piece.Value.Color);
        }

        [Test]
        public void Promotion_ExecuteKnightPromotion()
        {
            engine.LoadFromFen("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");
            var result = MakeMove("e7", "e8", promotionType: PieceType.Knight);
            Assert.IsTrue(result.Success);
            var piece = engine.GetPieceAt(Sq("e8"));
            Assert.IsNotNull(piece);
            Assert.AreEqual(PieceType.Knight, piece.Value.Type);
        }

        [Test]
        public void Promotion_WithCapture()
        {
            // White pawn on e7, black rook on d8 — can promote with capture
            engine.LoadFromFen("3rk3/4P3/8/8/8/8/8/4K3 w - - 0 1");
            var moves = engine.GetValidMoves(Sq("e7"));
            var capturePromos = moves.Where(m => m.PromotionType.HasValue && m.IsCapture).ToList();
            Assert.IsTrue(capturePromos.Count > 0, "Should have promotion moves with capture");
            Assert.IsTrue(capturePromos.Any(m => m.To.File == 3 && m.PromotionType == PieceType.Queen));
        }

        [Test]
        public void Promotion_BlackPawnPromotes()
        {
            // Black pawn on e2, can promote on e1 (white king on a1, out of the way)
            engine.LoadFromFen("4k3/8/8/8/8/8/4p3/K7 b - - 0 1");
            var moves = engine.GetValidMoves(Sq("e2"));
            var promoMoves = moves.Where(m => m.PromotionType.HasValue).ToList();
            Assert.IsTrue(promoMoves.Count > 0, "Black pawn should have promotion moves");
        }

        // ================================================================
        // 12. MakeMove validation
        // ================================================================

        [Test]
        public void MakeMove_IllegalMove_ReturnsFalse()
        {
            engine.InitializeBoard();
            // Try to move pawn to an illegal square
            var result = engine.MakeMove(new Move(Sq("e2"), Sq("e5")));
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void MakeMove_WrongTurn_ReturnsFalse()
        {
            engine.InitializeBoard();
            // Try to move black piece on white's turn
            var result = engine.MakeMove(new Move(Sq("e7"), Sq("e6")));
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void MakeMove_EmptySquare_ReturnsFalse()
        {
            engine.InitializeBoard();
            var result = engine.MakeMove(new Move(Sq("e4"), Sq("e5")));
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void MakeMove_SwitchesTurn()
        {
            engine.InitializeBoard();
            Assert.AreEqual(PieceColor.White, engine.GetCurrentTurn());
            MakeMove("e2", "e4");
            Assert.AreEqual(PieceColor.Black, engine.GetCurrentTurn());
            MakeMove("e7", "e5");
            Assert.AreEqual(PieceColor.White, engine.GetCurrentTurn());
        }

        [Test]
        public void MakeMove_Capture_ReturnsCorrectResult()
        {
            engine.LoadFromFen("rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
            var result = MakeMove("e4", "d5");
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsCapture);
            Assert.IsNotNull(result.CapturedPiece);
            Assert.AreEqual(PieceType.Pawn, result.CapturedPiece.Value.Type);
        }

        // ================================================================
        // 13. GetValidMoves edge cases
        // ================================================================

        [Test]
        public void GetValidMoves_EmptySquare_ReturnsEmpty()
        {
            engine.InitializeBoard();
            var moves = engine.GetValidMoves(Sq("e4"));
            Assert.AreEqual(0, moves.Count);
        }

        [Test]
        public void GetValidMoves_OpponentPiece_ReturnsEmpty()
        {
            engine.InitializeBoard();
            // White's turn, try to get moves for black pawn
            var moves = engine.GetValidMoves(Sq("e7"));
            Assert.AreEqual(0, moves.Count);
        }

        // ================================================================
        // 14. Game flow integration
        // ================================================================

        [Test]
        public void GameFlow_FoolsMate()
        {
            // Fool's mate: fastest checkmate (2 moves)
            engine.InitializeBoard();
            MakeMove("f2", "f3"); // white weakens kingside
            MakeMove("e7", "e5"); // black opens center
            MakeMove("g2", "g4"); // white blunders
            var result = MakeMove("d8", "h4"); // black queen delivers mate
            Assert.IsTrue(result.Success);
            Assert.AreEqual(GameState.Checkmate, result.NewGameState);
        }

        [Test]
        public void GameState_Ongoing_AtStart()
        {
            engine.InitializeBoard();
            Assert.AreEqual(GameState.Ongoing, engine.GetGameState());
        }
    }
}
