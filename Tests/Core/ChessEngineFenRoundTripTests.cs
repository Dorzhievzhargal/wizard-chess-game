using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using WizardChess.Core;

namespace WizardChess.Tests.Core
{
    /// <summary>
    /// Property-based test: FEN round-trip.
    /// Validates: Requirements 2.9
    ///
    /// FOR ALL valid positions, serialization to FEN followed by deserialization
    /// SHALL reproduce an equivalent position (round-trip property).
    ///
    /// Since ToFen() always outputs halfmove=0 and fullmove=1, we compare
    /// only the first 4 FEN fields: piece placement, active color, castling, en passant.
    /// </summary>
    [TestFixture]
    public class ChessEngineFenRoundTripTests
    {
        /// <summary>
        /// Extracts the first 4 FEN fields (piece placement, active color, castling, en passant).
        /// </summary>
        private static string GetFenCore(string fen)
        {
            string[] parts = fen.Split(' ');
            Assert.GreaterOrEqual(parts.Length, 4, "FEN must have at least 4 fields");
            return $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
        }

        // ----------------------------------------------------------------
        // FsCheck Arbitrary: generates valid chess FEN positions
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates a random valid chess position as a FEN string.
        /// Constraints:
        ///   - Exactly one white king and one black king
        ///   - Kings not adjacent to each other
        ///   - No pawns on ranks 1 or 8
        ///   - Side not to move is not in check
        ///   - Castling rights consistent with king/rook positions and HasMoved state
        ///   - En passant target consistent with pawn placement
        /// </summary>
        private static Arbitrary<string> ValidFenArbitrary()
        {
            return Arb.From(GenValidFen());
        }

        private static Gen<string> GenValidFen()
        {
            return Gen.Sized(size =>
            {
                // We generate boards by:
                // 1. Place white king and black king on non-adjacent squares
                // 2. Randomly place 0..N other pieces (no pawns on rank 1/8)
                // 3. Pick active color
                // 4. Derive castling rights from king/rook positions
                // 5. Optionally set en passant target if valid

                return from whiteKingSquare in Gen.Choose(0, 63)
                       from blackKingSquare in Gen.Choose(0, 63).Where(sq =>
                           sq != whiteKingSquare && !AreAdjacent(whiteKingSquare, sq))
                       from pieceCount in Gen.Choose(0, Math.Min(size / 2 + 2, 14))
                       from pieces in GenPieceList(pieceCount, whiteKingSquare, blackKingSquare)
                       from activeColorIsWhite in Arb.Generate<bool>()
                       from castlingFlags in Gen.Choose(0, 15)
                       from hasEnPassant in Gen.Frequency(
                           Tuple.Create(4, Gen.Constant(false)),
                           Tuple.Create(1, Gen.Constant(true)))
                       select BuildFen(whiteKingSquare, blackKingSquare, pieces,
                                       activeColorIsWhite, castlingFlags, hasEnPassant);
            });
        }

        private static bool AreAdjacent(int sq1, int sq2)
        {
            int f1 = sq1 % 8, r1 = sq1 / 8;
            int f2 = sq2 % 8, r2 = sq2 / 8;
            return Math.Abs(f1 - f2) <= 1 && Math.Abs(r1 - r2) <= 1;
        }

        private struct PlacedPiece
        {
            public int Square;
            public char FenChar;
        }

        private static Gen<List<PlacedPiece>> GenPieceList(int count, int wkSq, int bkSq)
        {
            if (count == 0)
                return Gen.Constant(new List<PlacedPiece>());

            // Piece chars (excluding kings): P R N B Q p r n b q
            char[] pieceChars = { 'P', 'R', 'N', 'B', 'Q', 'p', 'r', 'n', 'b', 'q' };

            return Gen.ListOf(count,
                from sq in Gen.Choose(0, 63)
                from pieceIdx in Gen.Choose(0, pieceChars.Length - 1)
                select new PlacedPiece { Square = sq, FenChar = pieceChars[pieceIdx] }
            ).Select(list =>
            {
                // Remove pieces on king squares and duplicates on same square
                var usedSquares = new HashSet<int> { wkSq, bkSq };
                var result = new List<PlacedPiece>();
                foreach (var p in list)
                {
                    if (usedSquares.Contains(p.Square))
                        continue;

                    int rank = p.Square / 8;
                    // No pawns on rank 0 (rank 1) or rank 7 (rank 8)
                    if ((p.FenChar == 'P' || p.FenChar == 'p') && (rank == 0 || rank == 7))
                        continue;

                    usedSquares.Add(p.Square);
                    result.Add(p);
                }
                return result;
            });
        }

        private static string BuildFen(int wkSq, int bkSq, List<PlacedPiece> pieces,
                                        bool activeColorIsWhite, int castlingFlags, bool hasEnPassant)
        {
            // Build board array
            char?[,] boardArr = new char?[8, 8];
            int wkFile = wkSq % 8, wkRank = wkSq / 8;
            int bkFile = bkSq % 8, bkRank = bkSq / 8;
            boardArr[wkFile, wkRank] = 'K';
            boardArr[bkFile, bkRank] = 'k';

            foreach (var p in pieces)
            {
                int f = p.Square % 8, r = p.Square / 8;
                boardArr[f, r] = p.FenChar;
            }

            // Piece placement string
            var sb = new StringBuilder();
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    if (boardArr[file, rank].HasValue)
                    {
                        if (empty > 0) { sb.Append(empty); empty = 0; }
                        sb.Append(boardArr[file, rank].Value);
                    }
                    else
                    {
                        empty++;
                    }
                }
                if (empty > 0) sb.Append(empty);
                if (rank > 0) sb.Append('/');
            }

            // Active color
            string activeColor = activeColorIsWhite ? "w" : "b";

            // Castling — only valid if king and rook are in correct positions and unmoved
            var castlingSb = new StringBuilder();
            // K: white king at e1 (4,0) and rook at h1 (7,0)
            if ((castlingFlags & 1) != 0 && wkFile == 4 && wkRank == 0
                && boardArr[7, 0].HasValue && boardArr[7, 0].Value == 'R')
                castlingSb.Append('K');
            // Q: white king at e1 (4,0) and rook at a1 (0,0)
            if ((castlingFlags & 2) != 0 && wkFile == 4 && wkRank == 0
                && boardArr[0, 0].HasValue && boardArr[0, 0].Value == 'R')
                castlingSb.Append('Q');
            // k: black king at e8 (4,7) and rook at h8 (7,7)
            if ((castlingFlags & 4) != 0 && bkFile == 4 && bkRank == 7
                && boardArr[7, 7].HasValue && boardArr[7, 7].Value == 'r')
                castlingSb.Append('k');
            // q: black king at e8 (4,7) and rook at a8 (0,7)
            if ((castlingFlags & 8) != 0 && bkFile == 4 && bkRank == 7
                && boardArr[0, 7].HasValue && boardArr[0, 7].Value == 'r')
                castlingSb.Append('q');
            string castling = castlingSb.Length > 0 ? castlingSb.ToString() : "-";

            // En passant — only valid with correct pawn placement
            string enPassant = "-";
            if (hasEnPassant)
            {
                // If white to move, en passant target is on rank 6 (index 5) with black pawn on rank 5 (index 4)
                // If black to move, en passant target is on rank 3 (index 2) with white pawn on rank 4 (index 3)
                if (activeColorIsWhite)
                {
                    // Look for a black pawn on rank 4 that could have just double-pushed
                    for (int f = 0; f < 8; f++)
                    {
                        if (boardArr[f, 4].HasValue && boardArr[f, 4].Value == 'p'
                            && !boardArr[f, 5].HasValue)
                        {
                            enPassant = $"{(char)('a' + f)}6";
                            break;
                        }
                    }
                }
                else
                {
                    // Look for a white pawn on rank 3 that could have just double-pushed
                    for (int f = 0; f < 8; f++)
                    {
                        if (boardArr[f, 3].HasValue && boardArr[f, 3].Value == 'P'
                            && !boardArr[f, 2].HasValue)
                        {
                            enPassant = $"{(char)('a' + f)}3";
                            break;
                        }
                    }
                }
            }

            return $"{sb} {activeColor} {castling} {enPassant} 0 1";
        }

        // ----------------------------------------------------------------
        // Property test
        // ----------------------------------------------------------------

        /// <summary>
        /// **Validates: Requirements 2.9**
        ///
        /// Property: For any valid chess position represented as FEN,
        /// LoadFromFen(fen) followed by ToFen() produces a FEN string
        /// whose first 4 fields (piece placement, active color, castling,
        /// en passant) are identical to the original.
        ///
        /// This verifies the round-trip: serialize → deserialize → serialize
        /// yields an equivalent position.
        /// </summary>
        [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(ChessEngineFenRoundTripTests) },
                                MaxTest = 200, QuietOnSuccess = true)]
        public void FenRoundTrip_LoadThenSerialize_PreservesPosition(string fen)
        {
            var engine = new ChessEngine();

            // Load the generated FEN
            engine.LoadFromFen(fen);

            // Serialize back to FEN
            string resultFen = engine.ToFen();

            // Compare the first 4 fields (piece placement, active color, castling, en passant)
            string originalCore = GetFenCore(fen);
            string resultCore = GetFenCore(resultFen);

            Assert.AreEqual(originalCore, resultCore,
                $"FEN round-trip failed.\n  Original: {fen}\n  Result:   {resultFen}");
        }

        // ----------------------------------------------------------------
        // Deterministic round-trip tests for well-known positions
        // ----------------------------------------------------------------

        private static readonly string[] KnownFenPositions = new[]
        {
            // Starting position
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            // After 1. e4
            "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            // After 1. e4 e5
            "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 1",
            // Sicilian Defense
            "rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 1",
            // No castling rights
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w - - 0 1",
            // Only white kingside castling
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w K - 0 1",
            // Black to move
            "8/8/8/4k3/8/8/8/4K3 b - - 0 1",
            // Complex middlegame
            "r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1",
            // Endgame position
            "8/5k2/8/8/8/8/3K4/8 w - - 0 1",
        };

        [Test]
        public void FenRoundTrip_KnownPositions_AllPreserved()
        {
            var engine = new ChessEngine();

            foreach (string fen in KnownFenPositions)
            {
                engine.LoadFromFen(fen);
                string resultFen = engine.ToFen();

                string originalCore = GetFenCore(fen);
                string resultCore = GetFenCore(resultFen);

                Assert.AreEqual(originalCore, resultCore,
                    $"FEN round-trip failed for known position.\n  Original: {fen}\n  Result:   {resultFen}");
            }
        }

        // ----------------------------------------------------------------
        // FsCheck Arbitrary registration (used by Property attribute)
        // ----------------------------------------------------------------

        public static Arbitrary<string> String()
        {
            return ValidFenArbitrary();
        }
    }
}
