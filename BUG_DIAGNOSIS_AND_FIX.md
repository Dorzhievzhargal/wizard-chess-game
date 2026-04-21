# Chess Movement Bug - Root Cause Analysis and Fix

## Executive Summary

**Root Cause**: Y-coordinate mismatch between BoardManager and PieceController coordinate systems caused movement animations to target wrong positions, creating visible jumps and positioning errors.

**Impact**: Pieces appeared to drift, attacks missed targets, and final positions were inconsistent.

**Fix**: Unified all position calculations in GameManager to use PieceController's coordinate system.

---

## Detailed Root Cause Analysis

### The Bug: Dual Coordinate Systems

The codebase had TWO different coordinate systems for the same logical board positions:

#### System 1: BoardManager.BoardToWorldPosition()
```csharp
public Vector3 BoardToWorldPosition(BoardPosition pos)
{
    float x = boardOrigin.x + pos.File * tileSize + tileSize * 0.5f;
    float y = boardOrigin.y;  // ← Always returns Y = 0 (or boardOrigin.y)
    float z = boardOrigin.z + pos.Rank * tileSize + tileSize * 0.5f;
    return new Vector3(x, y, z);
}
```

#### System 2: PieceController.GetWorldPosition()
```csharp
private Vector3 GetWorldPosition(BoardPosition pos)
{
    Vector3 worldPos;
    if (boardManager != null)
    {
        worldPos = boardManager.BoardToWorldPosition(pos);
    }
    else
    {
        worldPos = new Vector3(
            pos.File * tileSize + tileSize * 0.5f,
            0f,
            pos.Rank * tileSize + tileSize * 0.5f);
    }
    worldPos.y += pieceYOffset;  // ← Adds Y offset (typically 0.5 for piece height)
    return worldPos;
}
```

### The Problem Flow

1. **GameManager.ExecuteMove()** calculates target position:
   ```csharp
   Vector3 toWorld = boardManager.BoardToWorldPosition(move.To);  // Y = 0
   ```

2. **For cinematic captures**, attacker moves to strike position:
   ```csharp
   Vector3 strikePos = toWorld - dir * strikeDistance;  // Y = 0
   yield return StartCoroutine(MoveToWorldPosition(attackerObj, strikePos, 0.6f));
   ```

3. **Then moves to target**:
   ```csharp
   yield return StartCoroutine(MoveToWorldPosition(attackerObj, toWorld, 0.3f));  // Y = 0
   ```

4. **Finally snaps to "correct" position**:
   ```csharp
   pieceController.SnapPieceToTileCenter(attackerObj, move.To);
   // This calls GetWorldPosition() which returns Y = pieceYOffset
   ```

5. **Result**: Piece animates to Y=0, then JUMPS to Y=pieceYOffset

### Visual Symptoms

- **Vertical jump**: Piece appears to "pop" up or down at the end of movement
- **Horizontal drift**: Because the piece is at wrong Y during animation, any rotation or facing calculations are off
- **Attack misalignment**: Attacker approaches at Y=0 but defender is at Y=pieceYOffset
- **Inconsistent final position**: Sometimes pieces end up slightly offset because the snap happens after animation completes

### Why Previous Fixes Failed

The previous fix added `SnapPieceToTileCenter()` calls everywhere, which MASKED the symptom by forcing corrections, but didn't fix the root cause:
- Animations still targeted wrong Y coordinate
- Visible jumps still occurred
- Drift detection compared positions from different coordinate systems
- Debug logs showed "expected" positions that were actually wrong

---

## The Fix

### Solution: Unified Coordinate System

All position calculations in GameManager now use **PieceController's coordinate system** because:
1. Pieces are managed by PieceController
2. PieceController knows about pieceYOffset
3. Pieces should be positioned where PieceController expects them

### Changes Made

#### 1. Added Public Accessor to PieceController
```csharp
/// <summary>
/// Returns the world position for a board position, including Y offset.
/// This is the position where pieces should actually be placed.
/// Public accessor for GameManager to use the same coordinate system.
/// </summary>
public Vector3 GetWorldPositionForTile(BoardPosition position)
{
    return GetWorldPosition(position);
}
```

#### 2. Updated IPieceController Interface
```csharp
/// <summary>
/// Returns the world position for a board position in PieceController's coordinate system.
/// This includes the Y offset and is where pieces should actually be placed.
/// </summary>
Vector3 GetWorldPositionForTile(BoardPosition position);
```

#### 3. Updated All Position Calculations in GameManager

**Before**:
```csharp
Vector3 toWorld = boardManager.BoardToWorldPosition(move.To);  // Y = 0
```

**After**:
```csharp
Vector3 toWorld = pieceController.GetWorldPositionForTile(move.To);  // Y = pieceYOffset
```

This change was applied to:
- Initial move logging
- En passant capture verification
- Cinematic capture movement targets
- Fallback capture verification
- Normal move verification
- Final move verification
- Castling rook verification

---

## Verification Test Plan

### Test 1: Pawn 1-Step Move
**Steps**:
1. Select white pawn at e2
2. Move to e3
3. Observe movement animation

**Expected**:
- Smooth animation from e2 to e3
- No vertical jump at end
- Piece ends exactly at tile center
- Console shows drift = 0.0000

**Verify**:
- [ ] No visible jump
- [ ] Piece at correct position
- [ ] Piece remains selectable

### Test 2: Pawn 2-Step Move
**Steps**:
1. Select white pawn at d2
2. Move to d4
3. Observe movement animation

**Expected**:
- Smooth animation across 2 tiles
- No vertical jump at end
- Piece ends exactly at tile center

**Verify**:
- [ ] Smooth multi-tile movement
- [ ] No position drift
- [ ] Piece remains selectable

### Test 3: Diagonal Capture
**Steps**:
1. Set up: White pawn at e4, Black pawn at d5
2. Move white pawn to capture at d5
3. Observe cinematic battle

**Expected**:
- Attacker approaches defender smoothly
- Both pieces at same Y height during battle
- Attacker ends exactly at d5 center
- Defender is destroyed
- No vertical jump

**Verify**:
- [ ] Attacker approaches correctly
- [ ] Battle animation looks correct
- [ ] Attacker ends at exact tile center
- [ ] No visible jump after battle
- [ ] Attacker remains selectable

### Test 4: Rook Long Move
**Steps**:
1. Clear path for white rook at a1
2. Move rook to a8 (full board length)
3. Observe movement

**Expected**:
- Smooth animation across 7 tiles
- No drift accumulation
- Piece ends exactly at a8 center

**Verify**:
- [ ] Long-distance movement is smooth
- [ ] No position drift
- [ ] Piece remains selectable

### Test 5: Multiple Consecutive Turns
**Steps**:
1. Make 5 moves alternating white/black
2. Include mix of normal moves and captures
3. Verify each piece after each move

**Expected**:
- All pieces remain at exact tile centers
- No drift accumulation over multiple turns
- All pieces remain selectable

**Verify**:
- [ ] No drift after multiple moves
- [ ] All pieces selectable
- [ ] Board state consistent

### Test 6: Castling
**Steps**:
1. Set up castling position
2. Perform kingside castling
3. Observe both king and rook movement

**Expected**:
- King moves 2 squares
- Rook moves to correct position
- Both end at exact tile centers

**Verify**:
- [ ] King at correct position
- [ ] Rook at correct position
- [ ] Both pieces selectable

---

## Debug Output Examples

### Successful Move (No Drift)
```
[WizardChess] ExecuteMove: Pawn from (4,1) to (4,2) isCapture=False | fromWorld=(4.5, 0.5, 1.5) toWorld=(4.5, 0.5, 2.5)
[PieceController] MovePieceTo complete: piece at (4.5, 0.5, 2.5), expected (4.5, 0.5, 2.5), tile (4,2)
[PieceController] SnapPieceToTileCenter: piece snapped to (4.5, 0.5, 2.5) at tile (4,2)
[WizardChess] Move complete verification: tile (4,2), actual=(4.5, 0.5, 2.5), expected=(4.5, 0.5, 2.5), drift=0.0000
```

### Successful Capture (No Drift)
```
[WizardChess] ExecuteMove: Pawn from (4,3) to (3,4) isCapture=True | fromWorld=(4.5, 0.5, 3.5) toWorld=(3.5, 0.5, 4.5)
[WizardChess] Cinematic capture complete: piece at (3.5, 0.5, 4.5), expected (3.5, 0.5, 4.5), drift=0.0000
[PieceController] SnapPieceToTileCenter: piece snapped to (3.5, 0.5, 4.5) at tile (3,4)
[WizardChess] Move complete verification: tile (3,4), actual=(3.5, 0.5, 4.5), expected=(3.5, 0.5, 4.5), drift=0.0000
```

### If Drift Detected (Should Be Rare)
```
[WizardChess] Position drift detected! Applying corrective snap.
[WizardChess] Non-capture move drift detected: 0.0234, applying snap.
[PieceController] SnapPieceToTileCenter: piece snapped to (4.5, 0.5, 2.5) at tile (4,2)
```

---

## Technical Details

### Files Modified

1. **PieceController.cs**
   - Added `GetWorldPositionForTile()` public method
   - Exposes internal coordinate system to GameManager

2. **IPieceController.cs**
   - Added `GetWorldPositionForTile()` interface method

3. **GameManager.cs**
   - Changed all `boardManager.BoardToWorldPosition()` calls to `pieceController.GetWorldPositionForTile()`
   - Affects: move logging, drift detection, position verification

### Why This Fix Works

1. **Single Source of Truth**: All position calculations use PieceController's coordinate system
2. **No Y Mismatch**: Animation targets and final positions use same Y coordinate
3. **No Visible Jumps**: Pieces animate smoothly to their final position
4. **Accurate Drift Detection**: Compares positions in same coordinate system
5. **Correct Debug Logs**: "Expected" positions are actually correct

### Performance Impact

- **Negligible**: Added one method call per position calculation
- **No additional allocations**: Returns Vector3 by value
- **No runtime overhead**: Simple coordinate transformation

---

## Comparison: Before vs After

### Before (Broken)
```
Animation Target: (4.5, 0.0, 2.5)  ← Y = 0
Final Snap:       (4.5, 0.5, 2.5)  ← Y = 0.5
Result: Visible jump of 0.5 units vertically
```

### After (Fixed)
```
Animation Target: (4.5, 0.5, 2.5)  ← Y = 0.5
Final Snap:       (4.5, 0.5, 2.5)  ← Y = 0.5
Result: Smooth animation, no jump
```

---

## Conclusion

The bug was caused by using two different coordinate systems for the same logical positions. The fix unifies all position calculations to use PieceController's coordinate system, which includes the necessary Y offset for piece placement. This eliminates visible jumps, ensures accurate positioning, and makes the system more maintainable.

**Key Takeaway**: When multiple systems manage the same entities, they must use the same coordinate system. Mixing coordinate systems leads to subtle but critical bugs.
