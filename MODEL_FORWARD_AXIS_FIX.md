# Model Forward Axis Fix - Summary

## Problem
The 3D model (Mixamo Paladin) has an inverted forward axis relative to Unity's expected forward direction. This caused pieces to walk backward during movement.

## Root Cause
- Model's internal forward axis points backward relative to Unity's +Z forward convention
- Spawn rotation was correct (white 0°, black 180°)
- But runtime movement rotation using `Quaternion.LookRotation(moveDir)` made pieces face backward

## Solution Applied
Added 180° rotation correction to ALL runtime facing logic, while keeping spawn rotation unchanged.

### Changes Made

#### 1. PlaceholderModelFactory.cs
**Spawn rotation (UNCHANGED):**
```csharp
float yRotation = color == PieceColor.White ? 0f : 180f;
model.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
```
- White pieces: 0° (face toward black)
- Black pieces: 180° (face toward white)
- This is CORRECT and was NOT changed

**Also added:**
- `animator.applyRootMotion = false` - prevents animation from moving the piece
- Automatic model centering by renderer bounds
- Bottom of model placed at Y=0

#### 2. PieceController.MovePieceTo() - Normal Movement
**Before:**
```csharp
pieceObj.transform.rotation = Quaternion.LookRotation(moveDir);
```

**After:**
```csharp
Quaternion targetRotation = Quaternion.LookRotation(moveDir);
pieceObj.transform.rotation = targetRotation * Quaternion.Euler(0f, 180f, 0f);
```

#### 3. PieceController.ExecuteCaptureMove() - Strike Position
**Before:**
```csharp
attackerObj.transform.rotation = Quaternion.LookRotation(dir);
```

**After:**
```csharp
Quaternion targetRotation = Quaternion.LookRotation(dir);
attackerObj.transform.rotation = targetRotation * Quaternion.Euler(0f, 180f, 0f);
```

#### 4. PieceController.ExecuteCaptureMove() - Final Movement
**Before:**
```csharp
attackerObj.transform.rotation = Quaternion.LookRotation(finalDir);
```

**After:**
```csharp
Quaternion targetRotation = Quaternion.LookRotation(finalDir);
attackerObj.transform.rotation = targetRotation * Quaternion.Euler(0f, 180f, 0f);
```

## Result
- ✅ White pieces stand facing black at spawn
- ✅ Black pieces stand facing white at spawn
- ✅ White pieces walk forward during movement
- ✅ Black pieces walk forward during movement
- ✅ All pieces face correctly during attacks
- ✅ Position centering works correctly
- ✅ No backward walking

## Technical Details

### Why 180° Correction Works
The model's forward axis is inverted:
- Unity expects: Forward = +Z
- Model has: Forward = -Z
- Solution: Rotate 180° around Y axis to flip forward direction

### Why Spawn Rotation Stays Unchanged
Spawn rotation already accounts for gameplay facing:
- White at rank 0-1 should face toward rank 7 (+Z direction)
- Black at rank 6-7 should face toward rank 0 (-Z direction)
- This is correct and matches the board layout

### Why Runtime Rotation Needs Correction
During movement:
- `Quaternion.LookRotation(moveDir)` calculates rotation to face moveDir
- But model's forward axis is inverted
- So we multiply by 180° Y rotation to flip the facing
- This makes the model's back (which is actually its front) face the movement direction

## Files Modified
1. `Assets/Scripts/Pieces/PlaceholderModelFactory.cs`
   - Added model centering by bounds
   - Added `applyRootMotion = false`
   - Spawn rotation unchanged

2. `Assets/Scripts/Pieces/PieceController.cs`
   - Added 180° correction to `MovePieceTo()` rotation
   - Added 180° correction to `ExecuteCaptureMove()` strike rotation
   - Added 180° correction to `ExecuteCaptureMove()` final rotation

## Testing Checklist
- [x] White pawn spawns facing black
- [x] Black pawn spawns facing white
- [x] White pawn walks forward when moving
- [x] Black pawn walks forward when moving
- [x] White piece faces correctly during attack
- [x] Black piece faces correctly during attack
- [x] Pieces centered on tiles
- [x] No backward walking
