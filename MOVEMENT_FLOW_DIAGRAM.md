# Movement Flow Diagram

## Normal Move Flow (NO Strike Distance)

```
┌─────────────────────────────────────────────────────────────┐
│ User clicks destination tile                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ GameManager.ExecuteMove()                                    │
│ • ChessEngine.MakeMove() [logic update]                     │
│ • if (!result.IsCapture) ← NORMAL MOVE BRANCH               │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ PieceController.MovePieceTo(from, to)                       │
│                                                              │
│   startPos = piece.transform.position                       │
│   endPos = GetWorldPosition(target) ← EXACT TILE CENTER     │
│                                                              │
│   moveDir = endPos - startPos                               │
│   piece.rotation = LookRotation(moveDir) ← FACE DIRECTION   │
│                                                              │
│   Walk animation ON                                          │
│                                                              │
│   while (elapsed < duration)                                 │
│   {                                                          │
│       piece.position = Lerp(startPos, endPos, t)            │
│       ↑                                                      │
│       └─ DIRECT PATH, NO INTERMEDIATE STOPS                 │
│   }                                                          │
│                                                              │
│   Walk animation OFF                                         │
│                                                              │
│   SnapPieceToTileCenter(piece, target)                      │
│   ↑                                                          │
│   └─ FORCE EXACT POSITION = endPos                          │
│                                                              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ Piece at EXACT tile center                                  │
│ • No drift                                                   │
│ • No offset                                                  │
│ • No strike distance                                         │
│ • Piece remains selectable                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## Capture Move Flow (WITH Strike Distance)

```
┌─────────────────────────────────────────────────────────────┐
│ User clicks destination tile (occupied by enemy)             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ GameManager.ExecuteMove()                                    │
│ • ChessEngine.MakeMove() [logic update]                     │
│ • if (result.IsCapture && useCinematicCapture)              │
│   ← CAPTURE BRANCH                                           │
│ • UntrackPiece(defender)                                     │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ PieceController.ExecuteCaptureMove(attacker, from, to,      │
│                                     0.7f, battleCallback)    │
│                                                              │
│   fromWorld = GetWorldPosition(from)                        │
│   toWorld = GetWorldPosition(to) ← DEFENDER'S TILE CENTER   │
│                                                              │
│   dir = (toWorld - fromWorld).normalized                    │
│   strikePos = toWorld - dir * 0.7f                          │
│   ↑                                                          │
│   └─ STOP 0.7 TILES AWAY FOR BATTLE                         │
│                                                              │
│   Walk animation ON                                          │
│   piece.rotation = LookRotation(dir)                        │
│                                                              │
│   ┌─────────────────────────────────────────┐               │
│   │ PHASE 1: Move to Strike Position        │               │
│   │ while (elapsed < 0.6s)                  │               │
│   │ {                                       │               │
│   │     piece.position = Lerp(fromWorld,    │               │
│   │                           strikePos, t) │               │
│   │ }                                       │               │
│   └─────────────────────────────────────────┘               │
│                                                              │
│   Walk animation OFF                                         │
│                                                              │
│   ┌─────────────────────────────────────────┐               │
│   │ PHASE 2: Battle Callback                │               │
│   │ • Face each other                       │               │
│   │ • Play battle animation                 │               │
│   │ • Destroy defender                      │               │
│   └─────────────────────────────────────────┘               │
│                                                              │
│   Walk animation ON                                          │
│   finalDir = (toWorld - piece.position).normalized          │
│   piece.rotation = LookRotation(finalDir)                   │
│                                                              │
│   ┌─────────────────────────────────────────┐               │
│   │ PHASE 3: Move to Final Position         │               │
│   │ while (elapsed < 0.3s)                  │               │
│   │ {                                       │               │
│   │     piece.position = Lerp(strikePos,    │               │
│   │                           toWorld, t)   │               │
│   │ }                                       │               │
│   └─────────────────────────────────────────┘               │
│                                                              │
│   Walk animation OFF                                         │
│                                                              │
│   SnapPieceToTileCenter(piece, to)                          │
│   ↑                                                          │
│   └─ FORCE EXACT POSITION = toWorld                         │
│                                                              │
│   RetrackPiece(attacker, from, to)                          │
│                                                              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ Attacker at EXACT tile center (defender's former position)  │
│ • No drift                                                   │
│ • No offset                                                  │
│ • Defender destroyed                                         │
│ • Attacker remains selectable                                │
└─────────────────────────────────────────────────────────────┘
```

---

## Black Piece Orientation Fix

### Before (BROKEN)
```
Black Pawn at (4, 6) - spawns facing white (180° Y)
User moves to (4, 5)

moveDir = (4.5, 0, 5.5) - (4.5, 0, 6.5) = (0, 0, -1)
LookRotation((0, 0, -1)) = 180° Y

BUT: Code didn't use moveDir correctly
Result: Pawn moved backward (away from white)
```

### After (FIXED)
```
Black Pawn at (4, 6) - spawns facing white (180° Y)
User moves to (4, 5)

// In MovePieceTo() line 298-304
Vector3 moveDir = endPos - startPos;  ← ACTUAL MOVEMENT DIRECTION
moveDir.y = 0f;

if (moveDir.sqrMagnitude > 0.001f)
{
    pieceObj.transform.rotation = Quaternion.LookRotation(moveDir);
    ↑
    └─ ALWAYS USE MOVEMENT DIRECTION, NOT SPAWN ROTATION
}

Result: Pawn rotates to face (0, 0, -1) = 180° Y
        Pawn walks FORWARD toward white ✓
```

---

## Key Differences

| Aspect | Normal Move | Capture Move |
|--------|-------------|--------------|
| **Method** | `MovePieceTo()` | `ExecuteCaptureMove()` |
| **Strike Distance** | ❌ NONE | ✅ 0.7 tiles |
| **Intermediate Stops** | ❌ NONE | ✅ Strike position |
| **Movement Phases** | 1 (direct) | 3 (strike → battle → final) |
| **Battle Animation** | ❌ NO | ✅ YES |
| **Final Position** | ✅ Exact tile center | ✅ Exact tile center |
| **Snap Called** | ✅ YES | ✅ YES |

---

## Code Locations

### Normal Move
- **PieceController.MovePieceTo()**: Lines 286-348
- **GameManager routing**: Line 325 `if (!result.IsCapture)`
- **Key line**: `piece.position = Lerp(startPos, endPos, t)` - direct path

### Capture Move
- **PieceController.ExecuteCaptureMove()**: Lines 350-420
- **GameManager routing**: Line 304 `if (useCinematicCapture)`
- **Key line**: `strikePos = toWorld - dir * strikeDistance` - ONLY here

### Black Piece Rotation
- **PieceController.MovePieceTo()**: Lines 298-304
- **Key line**: `Quaternion.LookRotation(moveDir)` where `moveDir = endPos - startPos`

---

## Verification Commands

### Check Normal Move Has No Strike Distance
```bash
grep -n "strikeDistance" Assets/Scripts/Pieces/PieceController.cs
```
**Expected:** Only appears in `ExecuteCaptureMove()` method, NOT in `MovePieceTo()`

### Check Capture Uses Strike Distance
```bash
grep -A 5 "strikePos = toWorld" Assets/Scripts/Pieces/PieceController.cs
```
**Expected:** Found in `ExecuteCaptureMove()` only

### Check Black Piece Rotation
```bash
grep -A 3 "moveDir = endPos - startPos" Assets/Scripts/Pieces/PieceController.cs
```
**Expected:** Followed by `Quaternion.LookRotation(moveDir)`

---

## Summary

✅ **Separation Complete:** Normal moves and captures use different methods  
✅ **No Strike Distance in Normal Moves:** Direct path from start to end  
✅ **Strike Distance Only in Captures:** Temporary positioning for battle  
✅ **Black Pieces Fixed:** Always face movement direction, move forward  
✅ **All Movements Snap:** Every path ends with exact tile center positioning  

**The implementation is correct and complete.**
