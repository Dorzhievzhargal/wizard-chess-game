# Movement and Capture Verification Checklist

## Quick Test Procedure

### Setup
1. Open Unity project
2. Load MainScene
3. Enter Play mode
4. Have console visible to see debug logs

### Test 1: White Pawn Normal Move ✓
**Steps:**
1. Click white pawn at e2
2. Click e4 (2-tile forward move)

**Expected:**
- [ ] Pawn highlights when selected
- [ ] Valid moves show at e3 and e4
- [ ] Pawn walks forward (faces positive Z)
- [ ] Pawn ends EXACTLY at center of e4
- [ ] Console shows: "NormalMove complete: from (4,1) to (4,3)"
- [ ] No drift warnings in console
- [ ] Pawn remains selectable after move

**Failure Indicators:**
- ❌ Pawn stops between tiles
- ❌ Pawn offset from tile center
- ❌ Console shows drift > 0.01
- ❌ Pawn cannot be selected after move

---

### Test 2: Black Pawn Normal Move ✓
**Steps:**
1. Move white pawn (any move)
2. Click black pawn at e7
3. Click e5 (2-tile forward move)

**Expected:**
- [ ] Pawn spawns facing white (toward negative Z)
- [ ] Pawn rotates to face movement direction
- [ ] Pawn walks FORWARD (not backward)
- [ ] Pawn ends EXACTLY at center of e5
- [ ] Console shows: "NormalMove complete: from (4,6) to (4,4)"
- [ ] No drift warnings in console

**Failure Indicators:**
- ❌ Pawn moves backward (away from white)
- ❌ Pawn faces wrong direction
- ❌ Pawn offset from tile center

---

### Test 3: White Pawn Capture ✓
**Steps:**
1. Move white pawn e2 → e4
2. Move black pawn d7 → d5
3. Click white pawn at e4
4. Click d5 (diagonal capture)

**Expected:**
- [ ] Pawn walks diagonally toward d5
- [ ] Pawn faces diagonal movement direction
- [ ] Black pawn removed
- [ ] White pawn ends EXACTLY at center of d5
- [ ] Console shows: "NormalMove complete: from (4,3) to (3,4)"
- [ ] No drift warnings

**Failure Indicators:**
- ❌ Pawn stops before reaching d5
- ❌ Pawn offset from tile center
- ❌ Black pawn not removed

---

### Test 4: Black Pawn Capture ✓
**Steps:**
1. Move white pawn e2 → e4
2. Move black pawn d7 → d5
3. Move white pawn e4 → e5
4. Move black pawn d5 → d4
5. Move white pawn c2 → c3
6. Click black pawn at d4
7. Click c3 (diagonal capture)

**Expected:**
- [ ] Black pawn walks diagonally toward c3
- [ ] Black pawn faces diagonal movement direction (toward white)
- [ ] White pawn removed
- [ ] Black pawn ends EXACTLY at center of c3
- [ ] No backward movement

**Failure Indicators:**
- ❌ Black pawn moves backward
- ❌ Pawn offset from tile center

---

### Test 5: Cinematic Capture (if BattleSystem enabled) ✓
**Steps:**
1. Set up a capture scenario (e.g., knight takes pawn)
2. Execute the capture

**Expected:**
- [ ] Attacker walks to strike position (stops ~0.7 tiles from defender)
- [ ] Both pieces face each other
- [ ] Battle animation plays
- [ ] Defender destroyed
- [ ] Attacker walks to EXACT CENTER of defender's tile
- [ ] Console shows: "CaptureMove complete: from (...) to (...)"
- [ ] No drift warnings

**Failure Indicators:**
- ❌ Attacker stops at strike position permanently
- ❌ Attacker offset from tile center after battle
- ❌ Console shows drift > 0.01

---

### Test 6: Castling ✓
**Steps:**
1. Move pieces to enable kingside castling (e.g., white)
2. Move knight from g1
3. Move bishop from f1
4. Click white king at e1
5. Click g1 (kingside castle)

**Expected:**
- [ ] King moves from e1 to g1
- [ ] Rook moves from h1 to f1
- [ ] Both pieces end EXACTLY at tile centers
- [ ] Console shows two "NormalMove complete" messages
- [ ] No drift warnings
- [ ] Both pieces remain selectable

**Failure Indicators:**
- ❌ King or rook offset from tile center
- ❌ Rook doesn't move
- ❌ Pieces cannot be selected after castling

---

### Test 7: En Passant ✓
**Steps:**
1. Move white pawn e2 → e4
2. Move black pawn a7 → a6 (any move)
3. Move white pawn e4 → e5
4. Move black pawn d7 → d5 (2-tile move, now adjacent to white pawn)
5. Click white pawn at e5
6. Click d6 (en passant capture)

**Expected:**
- [ ] White pawn moves to d6
- [ ] Black pawn at d5 removed
- [ ] White pawn ends EXACTLY at center of d6
- [ ] No drift warnings

**Failure Indicators:**
- ❌ Black pawn not removed
- ❌ White pawn offset from tile center

---

## Console Debug Patterns

### Normal Move Success
```
[PieceController] NormalMove complete: from (4,1) to (4,3), piece at (4.5, 0, 3.5), expected (4.5, 0, 3.5)
```

### Capture Move Success
```
[PieceController] CaptureMove complete: from (4,3) to (3,4), piece at (3.5, 0, 4.5), expected (3.5, 0, 4.5)
```

### Drift Warning (BAD)
```
[WizardChess] Position drift detected! drift=0.0234
```

### Position Mismatch (BAD)
```
[WizardChess] No piece found at (4,3) after move — dictionary may be out of sync!
```

---

## Common Issues and Solutions

### Issue: Black pieces move backward
**Cause:** Rotation logic not using movement direction
**Check:** PieceController.MovePieceTo() line ~268
**Expected:** `Quaternion.LookRotation(moveDir)` where moveDir is actual movement direction

### Issue: Pieces offset from tile center
**Cause:** SnapPieceToTileCenter not called or called too early
**Check:** PieceController.MovePieceTo() and ExecuteCaptureMove()
**Expected:** SnapPieceToTileCenter called AFTER all movement complete

### Issue: Pieces stop at strike position during normal moves
**Cause:** Strike distance logic leaking into normal moves
**Check:** GameManager.ExecuteMove() - ensure normal moves use MovePieceTo() only
**Expected:** No strike distance calculation for normal moves

### Issue: Drift warnings after every move
**Cause:** Snap not working correctly or coordinate system mismatch
**Check:** BoardManager.BoardToWorldPosition() and PieceController.GetWorldPosition()
**Expected:** Both use same coordinate system, snap sets exact position

---

## Performance Check

After all tests pass:
- [ ] No memory leaks (piece count stable)
- [ ] No console errors
- [ ] Smooth animations (no stuttering)
- [ ] Consistent frame rate
- [ ] All pieces remain selectable throughout game

---

## Sign-Off

**Tester:** _______________  
**Date:** _______________  
**All Tests Passed:** [ ] YES [ ] NO  
**Notes:**

_______________________________________________
_______________________________________________
_______________________________________________
