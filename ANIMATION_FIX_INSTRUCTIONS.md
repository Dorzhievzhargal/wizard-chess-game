# Animation Forward Axis Fix - Unity Inspector Instructions

## Problem Identified
The walk animation clip has incorrect root transform settings. The animation is baked with forward motion in the wrong direction, causing pieces to walk backward even when the GameObject faces forward.

## Root Cause
In the FBX import settings for "Sword And Shield Walk.fbx", the animation clip settings show:
- `keepOriginalOrientation: 0` - Animation doesn't preserve original orientation
- `keepOriginalPositionXZ: 0` - Animation doesn't preserve XZ position
- `loopBlendPositionXZ: 0` - No position blending

The animation's root motion is baked with an inverted forward direction.

## Solution: Fix Animation Import Settings in Unity

### Step 1: Select the Walk Animation FBX
1. In Unity Project window, navigate to:
   `Assets/Resources/Models/Pawn/`
2. Click on: `Paladin WProp J Nordstrom@Sword And Shield Walk.fbx`

### Step 2: Open Animation Import Settings
1. In the Inspector, click the "Animation" tab
2. Find the clip named "Sword And Shield Walk" in the Clips list
3. Click on it to expand settings

### Step 3: Fix Root Transform Rotation
Look for "Root Transform Rotation" section:
- **Change "Bake Into Pose" to: CHECKED ✓**
- **Set "Based Upon" to: "Original"**
- **Set "Offset" to: 180** (this inverts the forward direction)

### Step 4: Fix Root Transform Position (XZ)
Look for "Root Transform Position (XZ)" section:
- **Keep "Bake Into Pose" UNCHECKED** (we want root motion for walking)
- **Set "Based Upon" to: "Original"**

### Step 5: Apply Changes
1. Click "Apply" button at the bottom of the Inspector
2. Wait for Unity to reimport the animation

## Alternative Solution: Rotate Model in Code

If the above doesn't work, add a 180° Y rotation to the model container in `TryBuildFromPrefab`:

```csharp
// After instantiating the model
var model = Object.Instantiate(prefab, root.transform);
model.name = "Model";

// Add a wrapper to handle rotation offset
GameObject modelWrapper = new GameObject("ModelWrapper");
modelWrapper.transform.SetParent(root.transform, false);
modelWrapper.transform.localPosition = Vector3.zero;
modelWrapper.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Invert forward

// Re-parent model under wrapper
model.transform.SetParent(modelWrapper.transform, false);
model.transform.localPosition = Vector3.zero;
model.transform.localRotation = Quaternion.identity;

// Continue with scaling, animator setup, etc.
```

## Alternative Solution 2: Mirror the Animation

If root transform settings don't work:

1. Select the walk animation FBX
2. In Animation tab, find "Sword And Shield Walk" clip
3. Check the "Mirror" checkbox
4. Click Apply

This will flip the animation left-to-right, which may correct the forward direction.

## Alternative Solution 3: Use Different Animation

If the current walk animation is fundamentally broken:

1. Download a different walk animation from Mixamo
2. Ensure it's for the same character rig
3. When downloading, select "In Place" option (not "With Root Motion")
4. Import to Unity and replace in the Animator Controller

## Testing After Fix

1. Start Play mode
2. Move a white pawn forward
3. Move a black pawn forward
4. Both should walk facing their movement direction (not backward)

## Current Settings (Before Fix)

From `Paladin WProp J Nordstrom@Sword And Shield Walk.fbx.meta`:
```yaml
clipAnimations:
- name: Sword And Shield Walk
  orientationOffsetY: 0          # ← Should be 180
  keepOriginalOrientation: 0     # ← Should be 1
  keepOriginalPositionY: 1       # ← Correct
  keepOriginalPositionXZ: 0      # ← Should be 0 (correct for root motion)
  loopBlendPositionXZ: 0         # ← Correct for walking
```

## Expected Settings (After Fix)

```yaml
clipAnimations:
- name: Sword And Shield Walk
  orientationOffsetY: 180        # ← CHANGED: Invert forward
  keepOriginalOrientation: 1     # ← CHANGED: Preserve orientation
  keepOriginalPositionY: 1       # ← Keep
  keepOriginalPositionXZ: 0      # ← Keep (allows root motion)
  loopBlendPositionXZ: 0         # ← Keep
```

## Why This Works

The animation was exported from Mixamo with the character facing one direction, but Unity's coordinate system expects the opposite. By adding 180° to `orientationOffsetY`, we rotate the animation's forward direction to match Unity's +Z forward convention.

This fix is applied at the animation clip level, not in code, so it affects the animation data itself rather than requiring runtime rotation corrections.

## Files to Modify (Unity Inspector Only)

1. `Assets/Resources/Models/Pawn/Paladin WProp J Nordstrom@Sword And Shield Walk.fbx`
   - Animation tab → Sword And Shield Walk clip
   - Root Transform Rotation → Offset: 180

No code changes needed if this works correctly.
