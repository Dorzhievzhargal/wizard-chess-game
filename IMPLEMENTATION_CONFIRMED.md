# Implementation Confirmation: Movement and Capture Separation

## ✓ CONFIRMED: Normal and Capture Movements Are Completely Separated

### Normal Move Path (NO Strike Distance)
```
GameManager.ExecuteMove()
    ↓
if (!result.IsCapture)  ← NORMAL MOVE BRANCH
    ↓
PieceController.MovePieceTo(from, to)
    ↓
    • Calculate moveDir = endPos - startPos
    • Rotate to face moveDir (fixes black piece orientation)
    • Walk animation ON
    • Lerp DIRECTLY from startPos to endPos  ← NO STRIKE DISTANCE
    • Walk animation OFF
    • SnapPieceToTileCenter(target)  ← EXACT CENTER
    ↓
Piece ends at EXACT tile center
```

**Code Location:** `PieceController.cs` lines 286-348
**Key Feature:** `Vector3.Lerp(startPos, endPos, t)` - direct interpolation, no intermediate positions

### Capture Move Path (WITH Strike Distance)
```
GameManager.ExecuteMove()
    ↓
if (result.IsCapture && useCinematicCapture)  ← CAPTURE BRANCH
    ↓
PieceController.ExecuteCaptureMove(attackerObj, from, to, 0.7f, battleCallback)
    ↓
    • Calculate strikePos = toWorld - dir * strikeDistance  ← ONLY IN CAPTURE
    • Move to strikePos (0.7 tiles from defender)
    • Invoke battleCallback:
        - Face each other
        - Battle animation
        - Destroy defender
    • Move to toWorld (exact tile center)
    • SnapPieceToTileCenter(to)  ← EXACT CENTER
    ↓
Piece ends at EXACT tile center
```

**Code Location:** `PieceController.cs` lines 350-420
**Key Feature:** Strike distance ONLY used in this method, never in MovePieceTo

### Fallback Capture Path (NO Strike Distance)
```
GameManager.ExecuteMove()
    ↓
if (result.IsCapture && !useCinematicCapture)  ← FALLBACK CAPTURE
    ↓
pieceController.RemovePiece(move.To)  ← Remove defender first
    ↓
PieceController.MovePieceTo(from, to)  ← Use normal move
    ↓
Piece ends at EXACT tile center
```

**Code Location:** `GameManager.cs` lines 318-323
**Key Feature:** Uses normal MovePieceTo after removing defender - no strike distance

---

## ✓ CONFIRMED: Black Piece Orientation Fixed

### Problem (Before)
- Black pieces spawn with 180° rotation (correct)
- But `Quaternion.LookRotation(moveDir)` didn't account for this
- Result: Black pieces appeared to move backward

### Solution (Now)
```csharp
// In PieceController.MovePieceTo() line 298-304
Vector3 moveDir = endPos - startPos;
moveDir.y = 0f;

// Rotate piece to face movement direction
// CRITICAL: Use actual movement direction, not model's initial facing
if (moveDir.sqrMagnitude > 0.001f)
{
    pieceObj.transform.rotation = Quaternion.LookRotation(moveDir);
}
```

**How It Works:**
1. Black pawn spawns at (4, 6) facing toward white (180° Y rotation)
2. Player moves to (4, 5)
3. `moveDir = (4.5, 0, 5.5) - (4.5, 0, 6.5) = (0, 0, -1)` ← toward white
4. `Quaternion.LookRotation((0, 0, -1))` = 180° Y rotation ← correct facing
5. Pawn walks FORWARD toward white

**Result:** Black pieces always face their movement direction, moving forward not backward

---

## Code Verification

### 1. MovePieceTo - Normal Move Only ✓
**File:** `Assets/Scripts/Pieces/PieceController.cs`
**Lines:** 286-348
**Verification:**
- ✓ No `strikeDistance` variable
- ✓ No `strikePos` calculation
- ✓ Direct `Vector3.Lerp(startPos, endPos, t)`
- ✓ Ends with `SnapPieceToTileCenter(pieceObj, target)`
- ✓ Comment: "This is for NORMAL MOVES ONLY - no combat offset, no strike distance"

### 2. ExecuteCaptureMove - Capture Only ✓
**File:** `Assets/Scripts/Pieces/PieceController.cs`
**Lines:** 350-420
**Verification:**
- ✓ Has `strikeDistance` parameter
- ✓ Calculates `strikePos = toWorld - dir * strikeDistance`
- ✓ Two-phase movement: strike position → battle → final position
- ✓ Ends with `SnapPieceToTileCenter(attackerObj, to)`
- ✓ Comment: "CAPTURE FLOW: 1. Attacker moves to strike position..."

### 3. GameManager - Correct Routing ✓
**File:** `Assets/Scripts/Core/GameManager.cs`
**Lines:** 289-327
**Verification:**
- ✓ Line 325: `if (!result.IsCapture)` → calls `MovePieceTo()` (normal move)
- ✓ Line 304: `if (useCinematicCapture)` → calls `ExecuteCaptureMove()` (capture with strike)
- ✓ Line 318: `else` (fallback capture) → removes defender, then calls `MovePieceTo()` (normal move)
- ✓ No strike distance calculation in GameManager
- ✓ All strike distance logic encapsulated in PieceController.ExecuteCaptureMove()

### 4. Black Piece Rotation ✓
**File:** `Assets/Scripts/Pieces/PieceController.cs`
**Lines:** 298-304
**Verification:**
- ✓ `moveDir = endPos - startPos` (actual movement direction)
- ✓ `Quaternion.LookRotation(moveDir)` (face movement direction)
- ✓ No special case for black pieces (works for all colors)
- ✓ Comment: "CRITICAL: Use actual movement direction, not model's initial facing"

---

## Test Scenarios

### Scenario 1: White Pawn Normal Move
**Input:** e2 → e4
**Expected:** Walk directly from e2 to e4, no stops
**Code Path:** `MovePieceTo(e2, e4)`
**Strike Distance:** NONE ✓

### Scenario 2: Black Pawn Normal Move
**Input:** e7 → e5
**Expected:** Walk forward (toward white), face movement direction
**Code Path:** `MovePieceTo(e7, e5)`
**Rotation:** `LookRotation((0, 0, -1))` = 180° ✓
**Strike Distance:** NONE ✓

### Scenario 3: Cinematic Capture
**Input:** Knight takes Pawn
**Expected:** Walk to strike position, battle, walk to final position
**Code Path:** `ExecuteCaptureMove(knight, from, to, 0.7f, battleCallback)`
**Strike Distance:** 0.7 tiles (ONLY in this path) ✓

### Scenario 4: Fallback Capture
**Input:** Pawn takes Pawn (no battle system)
**Expected:** Remove defender, walk directly to destination
**Code Path:** `RemovePiece(to)` → `MovePieceTo(from, to)`
**Strike Distance:** NONE ✓

---

## Summary

✅ **Normal moves:** Use `MovePieceTo()` - direct path, NO strike distance  
✅ **Cinematic captures:** Use `ExecuteCaptureMove()` - strike position, battle, final position  
✅ **Fallback captures:** Use `RemovePiece()` + `MovePieceTo()` - NO strike distance  
✅ **Black pieces:** Face movement direction using `LookRotation(moveDir)` - move forward  
✅ **All movements:** End with `SnapPieceToTileCenter()` - exact tile center  

**Separation is complete and correct.**
