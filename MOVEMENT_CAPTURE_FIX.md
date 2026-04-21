# Movement and Capture Architecture Fix

## Root Cause Summary

### Issue 1: Black Pieces Moving Backward
**Root Cause:** Black pieces spawn with 180° Y rotation (correct for facing opponent), but `Quaternion.LookRotation(moveDir)` in `MovePieceTo()` always uses absolute movement direction without accounting for the model's initial rotation.

**Result:** Black pawns appear to move backward on first move because LookRotation overwrites their spawn rotation.

**Fix:** Use actual movement direction for ALL pieces. The spawn rotation is only for initial facing; during movement, pieces should always face their movement direction regardless of color.

### Issue 2: Normal Move vs Capture Not Separated
**Root Cause:** Mixed movement logic - normal moves and captures used the same code paths with conditional branches, making it unclear which positioning logic applied when.

**Result:** 
- Pieces don't end exactly at tile center after normal moves
- Combat offset positioning leaks into normal moves
- Multiple verification/snap points create confusion

**Fix:** Architectural separation:
- `MovePieceTo()` - ONLY for normal moves, direct path to exact tile center
- `ExecuteCaptureMove()` - ONLY for captures, handles strike position, battle, then exact tile center

### Issue 3: Strike Distance Leaking
**Root Cause:** Cinematic captures calculated strike positions inline in GameManager, mixing orchestration with visual logic.

**Result:** Complex code path with multiple snap points, unclear final positioning responsibility.

**Fix:** Move all capture movement logic into PieceController.ExecuteCaptureMove(), GameManager only provides battle callback.

## Changes Made

### 1. PieceController.cs

#### MovePieceTo() - Normal Moves Only
```csharp
public IEnumerator MovePieceTo(BoardPosition from, BoardPosition target)
```
- **Purpose:** ONLY for normal moves (no capture, no combat)
- **Flow:**
  1. Calculate movement direction
  2. Rotate piece to face movement direction (works for both white and black)
  3. Walk animation ON
  4. Smooth lerp from start to end
  5. Walk animation OFF
  6. **CRITICAL:** Force snap to EXACT tile center
  7. Update visual mapping
- **No combat offset, no strike distance, no partial stopping**

#### ExecuteCaptureMove() - Captures Only
```csharp
public IEnumerator ExecuteCaptureMove(GameObject attackerObj, BoardPosition from, BoardPosition to, 
    float strikeDistance, System.Func<IEnumerator> onBattlePosition)
```
- **Purpose:** ONLY for cinematic captures
- **Flow:**
  1. Move attacker to strike position (strikeDistance away from defender)
  2. Invoke battle callback (caller handles facing, battle animation, defender destruction)
  3. Move attacker to EXACT CENTER of defender's tile
  4. **CRITICAL:** Force snap to EXACT tile center
  5. Re-register attacker at new position
- **Encapsulates all capture movement logic**

#### MoveToWorldPositionInternal() - Helper
```csharp
private IEnumerator MoveToWorldPositionInternal(GameObject obj, Vector3 target, float duration)
```
- **Purpose:** Internal smooth movement helper
- **Used by:** ExecuteCaptureMove for strike position and final position movements

### 2. GameManager.cs

#### ExecuteMove() - Simplified
- **Normal moves:** Call `MovePieceTo()` directly
- **En passant:** Remove defender, then call `MovePieceTo()`
- **Cinematic captures:** Call `ExecuteCaptureMove()` with battle callback
- **Fallback captures:** Remove defender, then call `MovePieceTo()`
- **Removed:** All inline verification/snap code (now handled by PieceController)

#### ExecuteBattleSequence() - New Helper
```csharp
private IEnumerator ExecuteBattleSequence(GameObject attackerObj, GameObject defenderObj, 
    PieceType attackerType, PieceType defenderType)
```
- **Purpose:** Battle callback for ExecuteCaptureMove
- **Flow:**
  1. Face each other
  2. Play battle animation
  3. Destroy defender
  4. Ensure attacker is active

#### HandleCastlingRook() - Simplified
- **Removed:** Selection, verification, snap code
- **Now:** Just calls `MovePieceTo()` for the rook (normal move)

### 3. IPieceController.cs

#### Added Method
```csharp
IEnumerator ExecuteCaptureMove(GameObject attackerObj, BoardPosition from, BoardPosition to, 
    float strikeDistance, System.Func<IEnumerator> onBattlePosition);
```

## Architectural Separation

### Normal Move Flow
```
User clicks destination
    ↓
GameManager.ExecuteMove()
    ↓
ChessEngine.MakeMove() [logic update]
    ↓
PieceController.MovePieceTo() [visual]
    ├─ Walk from start tile center
    ├─ To destination tile center
    └─ Snap to EXACT center
```

### Capture Move Flow
```
User clicks destination
    ↓
GameManager.ExecuteMove()
    ↓
ChessEngine.MakeMove() [logic update]
    ↓
PieceController.UntrackPiece(defender)
    ↓
PieceController.ExecuteCaptureMove() [visual]
    ├─ Walk to strike position
    ├─ Callback: ExecuteBattleSequence()
    │   ├─ Face each other
    │   ├─ Battle animation
    │   └─ Destroy defender
    ├─ Walk to destination tile center
    ├─ Snap to EXACT center
    └─ Re-register at new position
```

## Black Piece Orientation Fix

### Spawn Rotation
- **White pieces:** 0° Y rotation (face toward black, positive Z)
- **Black pieces:** 180° Y rotation (face toward white, negative Z)
- **Location:** `PlaceholderModelFactory.TryBuildFromPrefab()` line 95

### Movement Rotation
- **All pieces:** `Quaternion.LookRotation(moveDir)` where moveDir is actual movement direction
- **Result:** Pieces always face their movement direction during moves
- **Black pieces:** First move overwrites spawn rotation with movement direction (correct behavior)

## Verification Steps

### Test Cases

#### 1. White Pawn Moving 2 Tiles
- **Expected:** Pawn walks forward from rank 1 to rank 3
- **Expected:** Pawn faces forward (positive Z) during movement
- **Expected:** Pawn ends EXACTLY at center of destination tile
- **Verify:** No drift, no offset, piece remains selectable

#### 2. White Pawn Capturing Diagonally
- **Expected:** Pawn walks diagonally to capture
- **Expected:** Pawn faces diagonal movement direction
- **Expected:** Pawn ends EXACTLY at center of captured tile
- **Verify:** Defender removed, no drift

#### 3. Black Pawn Moving Forward
- **Expected:** Pawn spawns facing white (toward negative Z)
- **Expected:** On first move, pawn rotates to face movement direction (toward white)
- **Expected:** Pawn walks FORWARD (not backward)
- **Expected:** Pawn ends EXACTLY at center of destination tile
- **Verify:** No backward movement, correct facing

#### 4. Black Pawn Capturing
- **Expected:** Pawn walks diagonally toward white side
- **Expected:** Pawn faces diagonal movement direction
- **Expected:** Pawn ends EXACTLY at center of captured tile
- **Verify:** Defender removed, no drift

#### 5. Cinematic Capture (Any Piece)
- **Expected:** Attacker walks to strike position (0.7 tiles from defender)
- **Expected:** Both pieces face each other
- **Expected:** Battle animation plays
- **Expected:** Defender destroyed
- **Expected:** Attacker walks to EXACT CENTER of defender's tile
- **Expected:** Attacker snapped to exact center
- **Verify:** No drift (<0.01 units), piece remains selectable

#### 6. Castling
- **Expected:** King moves 2 tiles
- **Expected:** Rook moves to correct side of king
- **Expected:** Both pieces end EXACTLY at tile centers
- **Verify:** No drift, both pieces selectable

## Key Principles

1. **MovePieceTo() = Normal Move ONLY**
   - Direct path from start to end
   - No combat logic
   - Always snaps to exact tile center

2. **ExecuteCaptureMove() = Capture ONLY**
   - Strike position for battle
   - Battle callback
   - Final position at exact tile center
   - Always snaps to exact tile center

3. **Rotation = Movement Direction**
   - All pieces use `Quaternion.LookRotation(moveDir)`
   - Spawn rotation is only for initial facing
   - Movement rotation is based on actual movement direction

4. **Snap = Single Source of Truth**
   - `SnapPieceToTileCenter()` called at end of EVERY movement
   - Board logic (tile center) is ALWAYS the final authority
   - No drift, no offset, no partial positioning

5. **Clean Separation**
   - PieceController handles ALL visual movement logic
   - GameManager orchestrates and provides callbacks
   - No inline verification/snap in GameManager

## Files Modified

1. `Assets/Scripts/Pieces/PieceController.cs`
   - Updated `MovePieceTo()` - clarified normal move only
   - Added `ExecuteCaptureMove()` - new capture-specific method
   - Added `MoveToWorldPositionInternal()` - internal helper

2. `Assets/Scripts/Core/GameManager.cs`
   - Simplified `ExecuteMove()` - removed inline verification
   - Added `ExecuteBattleSequence()` - battle callback
   - Simplified `HandleCastlingRook()` - removed verification

3. `Assets/Scripts/Interfaces/IPieceController.cs`
   - Added `ExecuteCaptureMove()` method signature

## Expected Results

- **Normal moves:** Pieces walk directly to destination, end at exact tile center
- **Captures:** Pieces walk to strike position, battle, then walk to exact tile center
- **Black pieces:** Face opponent at spawn, rotate to face movement direction during moves
- **No drift:** All pieces end within 0.01 units of tile center
- **No backward movement:** Black pieces move forward toward white side
- **Clean code:** Clear separation between normal move and capture logic
