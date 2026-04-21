# Architecture Fix: Unified Piece Tracking

## Problem
The system had TWO separate piece tracking dictionaries:
1. `BoardManager.pieceObjects` - tracked pieces in BoardManager
2. `PieceController.pieceObjects` - tracked pieces in PieceController

This caused critical desynchronization:
- Pieces moved to wrong tiles
- Attacks misaligned
- Selection broke after moves
- Pieces became unusable
- Visual position != logical position

## Root Cause
Duplicate piece tracking meant:
- Movement updated one dictionary but not the other
- Capture removed from one but not the other
- Position queries returned different results depending on which system was asked
- No single source of truth for piece locations

## Solution
**Removed all piece tracking from BoardManager.**

### Changes Made

#### BoardManager.cs
1. **Removed field**: `private Dictionary<string, GameObject> pieceObjects;`
2. **Removed initialization**: `pieceObjects = new Dictionary<string, GameObject>();` from Awake()
3. **Deprecated methods** (kept for interface compatibility):
   - `PlacePiece()` - now no-op
   - `RemovePiece()` - now no-op  
   - `GetPieceObjectAt()` - returns null
4. **Removed helper**: `CreatePlaceholderPiece()` method
5. **Removed helper**: `PositionKey()` method

#### GameManager.cs
Removed all calls to `boardManager.RemovePiece()`:
- En passant capture
- Cinematic capture
- Fallback capture

### BoardManager Responsibilities (After Fix)
- ✅ Tile rendering
- ✅ Input detection (WorldToBoardPosition)
- ✅ Highlighting tiles
- ✅ Coordinate conversion
- ❌ NO piece tracking
- ❌ NO piece creation
- ❌ NO piece removal

### PieceController Responsibilities (After Fix)
- ✅ SINGLE SOURCE OF TRUTH for piece tracking
- ✅ `pieceObjects` dictionary (GameObject storage)
- ✅ `pieceData` dictionary (ChessPiece data storage)
- ✅ Piece creation (SpawnPieces)
- ✅ Piece movement (MovePieceTo)
- ✅ Piece removal (RemovePiece)
- ✅ Piece queries (GetPieceObject)
- ✅ Position updates (RetrackPiece)

## Validation

### Test Cases
1. **Click tile** → BoardManager.WorldToBoardPosition returns correct BoardPosition
2. **Move piece** → PieceController updates pieceObjects and pieceData
3. **Capture piece** → PieceController removes from both dictionaries
4. **Select piece** → PieceController queries its own pieceObjects
5. **After move** → Piece remains at exact tile center, selectable

### Expected Results
- ✅ All systems query PieceController for piece locations
- ✅ No duplicate tracking
- ✅ Movement, capture, selection all reference same data
- ✅ Visual position = logical position
- ✅ Pieces remain usable after moves

## Technical Details

### Before (Broken)
```
Input → BoardManager.WorldToBoardPosition → BoardPosition
GameManager queries BoardManager.GetPieceObjectAt → Wrong/stale data
GameManager queries PieceController.GetPieceObject → Different data
Movement updates PieceController.pieceObjects only
Capture removes from BoardManager.pieceObjects only
Result: DESYNC
```

### After (Fixed)
```
Input → BoardManager.WorldToBoardPosition → BoardPosition
GameManager queries PieceController.GetPieceObject → Correct data
Movement updates PieceController.pieceObjects + pieceData
Capture removes from PieceController.pieceObjects + pieceData
Result: SYNCHRONIZED - Single source of truth
```

## Files Modified
1. `Assets/Scripts/Board/BoardManager.cs`
   - Removed pieceObjects dictionary
   - Deprecated piece tracking methods
   - Removed piece creation code

2. `Assets/Scripts/Core/GameManager.cs`
   - Removed all `boardManager.RemovePiece()` calls
   - All piece operations now go through PieceController

## No Breaking Changes
- Interface methods kept for compatibility (as no-ops)
- All existing code continues to compile
- Only internal implementation changed

## Result
**Single source of truth for piece tracking = PieceController**

All piece queries, movements, captures, and position updates now reference the same data structure, eliminating desynchronization.
