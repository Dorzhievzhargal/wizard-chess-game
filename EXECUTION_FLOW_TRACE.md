# Movement Execution Flow - Before and After Fix

## Normal Move Flow (e.g., Pawn e2 → e3)

### BEFORE FIX (Broken)

1. **User clicks tile e3**
   - `GameManager.HandleTileClicked(e3)` called

2. **GameManager.ExecuteMove(move)** starts
   ```csharp
   Vector3 toWorld = boardManager.BoardToWorldPosition(move.To);
   // toWorld = (4.5, 0.0, 2.5)  ← Y = 0 ❌
   ```

3. **PieceController.MovePieceTo(e3)** animates
   ```csharp
   Vector3 endPos = GetWorldPosition(target);
   // endPos = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   
   // Animation lerps from current position to endPos
   while (elapsed < duration) {
       pieceObj.transform.position = Vector3.Lerp(startPos, endPos, t);
   }
   ```

4. **SnapPieceToTileCenter(e3)** called
   ```csharp
   Vector3 exactCenter = GetWorldPosition(position);
   // exactCenter = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   pieceObj.transform.position = exactCenter;
   ```

5. **GameManager verifies position**
   ```csharp
   Vector3 expectedPos = boardManager.BoardToWorldPosition(move.To);
   // expectedPos = (4.5, 0.0, 2.5)  ← Y = 0 ❌
   Vector3 actualPos = movedPiece.transform.position;
   // actualPos = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   
   // Drift calculation (X/Z only):
   float drift = Distance((4.5, 0, 2.5), (4.5, 0, 2.5)) = 0.0
   // No drift detected, but Y mismatch hidden!
   ```

**Result**: Animation works correctly (Y=0.5), but logging shows wrong expected position (Y=0). Confusing for debugging.

---

### AFTER FIX (Correct)

1. **User clicks tile e3**
   - `GameManager.HandleTileClicked(e3)` called

2. **GameManager.ExecuteMove(move)** starts
   ```csharp
   Vector3 toWorld = pieceController.GetWorldPositionForTile(move.To);
   // toWorld = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   ```

3. **PieceController.MovePieceTo(e3)** animates
   ```csharp
   Vector3 endPos = GetWorldPosition(target);
   // endPos = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   
   // Animation lerps from current position to endPos
   while (elapsed < duration) {
       pieceObj.transform.position = Vector3.Lerp(startPos, endPos, t);
   }
   ```

4. **SnapPieceToTileCenter(e3)** called
   ```csharp
   Vector3 exactCenter = GetWorldPosition(position);
   // exactCenter = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   pieceObj.transform.position = exactCenter;
   ```

5. **GameManager verifies position**
   ```csharp
   Vector3 expectedPos = pieceController.GetWorldPositionForTile(move.To);
   // expectedPos = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   Vector3 actualPos = movedPiece.transform.position;
   // actualPos = (4.5, 0.5, 2.5)  ← Y = 0.5 ✓
   
   // Drift calculation (X/Z only):
   float drift = Distance((4.5, 0, 2.5), (4.5, 0, 2.5)) = 0.0
   // No drift, positions match perfectly!
   ```

**Result**: All positions consistent, accurate logging, no confusion.

---

## Cinematic Capture Flow (e.g., Pawn e4 captures d5)

### BEFORE FIX (Broken)

1. **GameManager.ExecuteMove(capture move)** starts
   ```csharp
   Vector3 fromWorld = boardManager.BoardToWorldPosition(move.From);
   // fromWorld = (4.5, 0.0, 3.5)  ← Y = 0 ❌
   Vector3 toWorld = boardManager.BoardToWorldPosition(move.To);
   // toWorld = (3.5, 0.0, 4.5)  ← Y = 0 ❌
   ```

2. **Calculate strike position**
   ```csharp
   Vector3 dir = (toWorld - fromWorld).normalized;
   Vector3 strikePos = toWorld - dir * 0.7f;
   // strikePos = (3.5, 0.0, 4.5) - (normalized_dir * 0.7)
   // strikePos ≈ (4.0, 0.0, 4.0)  ← Y = 0 ❌
   ```

3. **Move attacker to strike position**
   ```csharp
   yield return MoveToWorldPosition(attackerObj, strikePos, 0.6f);
   // Attacker moves to Y = 0 ❌
   // But defender is at Y = 0.5 ✓
   // Attacker appears to attack "below" the defender!
   ```

4. **Battle animation plays**
   - Attacker at Y = 0 ❌
   - Defender at Y = 0.5 ✓
   - Visual mismatch!

5. **Move attacker to target**
   ```csharp
   yield return MoveToWorldPosition(attackerObj, toWorld, 0.3f);
   // Attacker moves to (3.5, 0.0, 4.5)  ← Y = 0 ❌
   ```

6. **Snap to tile center**
   ```csharp
   pieceController.SnapPieceToTileCenter(attackerObj, move.To);
   // Snaps to (3.5, 0.5, 4.5)  ← Y = 0.5 ✓
   // VISIBLE JUMP from Y=0 to Y=0.5! ❌
   ```

**Result**: Attacker approaches at wrong height, visible jump at end, confusing visuals.

---

### AFTER FIX (Correct)

1. **GameManager.ExecuteMove(capture move)** starts
   ```csharp
   Vector3 fromWorld = pieceController.GetWorldPositionForTile(move.From);
   // fromWorld = (4.5, 0.5, 3.5)  ← Y = 0.5 ✓
   Vector3 toWorld = pieceController.GetWorldPositionForTile(move.To);
   // toWorld = (3.5, 0.5, 4.5)  ← Y = 0.5 ✓
   ```

2. **Calculate strike position**
   ```csharp
   Vector3 dir = (toWorld - fromWorld).normalized;
   Vector3 strikePos = toWorld - dir * 0.7f;
   // strikePos = (3.5, 0.5, 4.5) - (normalized_dir * 0.7)
   // strikePos ≈ (4.0, 0.5, 4.0)  ← Y = 0.5 ✓
   ```

3. **Move attacker to strike position**
   ```csharp
   yield return MoveToWorldPosition(attackerObj, strikePos, 0.6f);
   // Attacker moves to Y = 0.5 ✓
   // Defender is at Y = 0.5 ✓
   // Both at same height! ✓
   ```

4. **Battle animation plays**
   - Attacker at Y = 0.5 ✓
   - Defender at Y = 0.5 ✓
   - Perfect alignment!

5. **Move attacker to target**
   ```csharp
   yield return MoveToWorldPosition(attackerObj, toWorld, 0.3f);
   // Attacker moves to (3.5, 0.5, 4.5)  ← Y = 0.5 ✓
   ```

6. **Snap to tile center**
   ```csharp
   pieceController.SnapPieceToTileCenter(attackerObj, move.To);
   // Snaps to (3.5, 0.5, 4.5)  ← Y = 0.5 ✓
   // NO JUMP - already at correct position! ✓
   ```

**Result**: Smooth animation, correct alignment, no visible jumps.

---

## Key Differences Summary

| Aspect | Before Fix | After Fix |
|--------|-----------|-----------|
| Animation Target Y | 0 (wrong) | 0.5 (correct) |
| Final Position Y | 0.5 (correct) | 0.5 (correct) |
| Visual Result | Jump from 0→0.5 | Smooth, no jump |
| Battle Alignment | Misaligned heights | Perfect alignment |
| Debug Logging | Confusing (wrong expected) | Accurate |
| Drift Detection | Hidden by Y mismatch | Accurate |

---

## Console Output Comparison

### Before Fix
```
[WizardChess] ExecuteMove: Pawn from (4,1) to (4,2) isCapture=False | fromWorld=(4.5, 0.0, 1.5) toWorld=(4.5, 0.0, 2.5)
[PieceController] MovePieceTo complete: piece at (4.5, 0.5, 2.5), expected (4.5, 0.5, 2.5), tile (4,2)
[PieceController] SnapPieceToTileCenter: piece snapped to (4.5, 0.5, 2.5) at tile (4,2)
[WizardChess] Move complete verification: tile (4,2), actual=(4.5, 0.5, 2.5), expected=(4.5, 0.0, 2.5), drift=0.0000
                                                                                              ^^^^ Wrong Y!
```

### After Fix
```
[WizardChess] ExecuteMove: Pawn from (4,1) to (4,2) isCapture=False | fromWorld=(4.5, 0.5, 1.5) toWorld=(4.5, 0.5, 2.5)
[PieceController] MovePieceTo complete: piece at (4.5, 0.5, 2.5), expected (4.5, 0.5, 2.5), tile (4,2)
[PieceController] SnapPieceToTileCenter: piece snapped to (4.5, 0.5, 2.5) at tile (4,2)
[WizardChess] Move complete verification: tile (4,2), actual=(4.5, 0.5, 2.5), expected=(4.5, 0.5, 2.5), drift=0.0000
                                                                                              ^^^^ Correct Y!
```

---

## Conclusion

The fix ensures that:
1. **Animation targets** use correct Y coordinate (0.5)
2. **Final positions** use correct Y coordinate (0.5)
3. **No jumps** occur because animation and final position match
4. **Debug logging** shows accurate expected positions
5. **Battle alignment** is perfect because all pieces at same height
