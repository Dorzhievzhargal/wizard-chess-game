# Implementation Plan: Epic Chess Visuals

## Overview

Enhance Wizard Chess with detailed procedural piece models, emission-enabled materials, a runtime VFX particle system, epic per-piece-type attack/death animations, and improved battle camera. All changes extend existing classes without breaking public APIs. A new `VFXSystem` class is added. Changes are made in `Assets/Scripts/` and synced to `Scripts/`.

## Tasks

- [x] 1. Enhance MaterialFactory with emission and mobile caps
  - [x] 1.1 Add emission glow to accent materials in MaterialFactory
    - Add `SetEmission(Material, Color, float)` private helper that enables `_EMISSION` keyword and sets `_EmissionColor`, guarded by `HasProperty("_EmissionColor")`
    - Modify `CreateWhitePieceAccentMaterial()` to call `SetEmission(mat, new Color(0.4f, 0.6f, 1.0f), 0.3f)`
    - Modify `CreateBlackPieceAccentMaterial()` to call `SetEmission(mat, new Color(0.8f, 0.2f, 0.5f), 0.3f)`
    - _Requirements: 2.4_

  - [x] 1.2 Add automatic mobile caps to material creation
    - Update `ApplyMobileCaps` to also cap `_Smoothness` property (not just `_Glossiness`) at 0.5
    - Modify `CreateStandardMaterial()` to call `ApplyMobileCaps(mat)` when `IsMobile()` returns true before returning
    - Ensure smoothness ≤ 0.5, metallic ≤ 0.3, emission intensity ≤ 0.3 on mobile
    - _Requirements: 2.5_

  - [ ]* 1.3 Write property test for emission on accent materials (Property 3)
    - **Property 3: Emission glow on accent materials**
    - For both `CreateWhitePieceAccentMaterial` and `CreateBlackPieceAccentMaterial`, verify `_EMISSION` keyword is enabled and `_EmissionColor` intensity is between 0.1 and 0.5
    - **Validates: Requirements 2.4**

  - [ ]* 1.4 Write property test for mobile material caps (Property 4)
    - **Property 4: Mobile material caps**
    - For any material created when `IsMobile()` is true, verify smoothness ≤ 0.5, metallic ≤ 0.3, emission intensity ≤ 0.3
    - **Validates: Requirements 2.5**

  - [x] 1.5 Sync MaterialFactory changes to Scripts/Visual/MaterialFactory.cs
    - Copy updated `Assets/Scripts/Visual/MaterialFactory.cs` to `Scripts/Visual/MaterialFactory.cs`
    - _Requirements: 7.1_

- [x] 2. Rewrite PlaceholderModelFactory with detailed piece models
  - [x] 2.1 Rewrite BuildPawn with 6+ primitives
    - Add Base pedestal, Body, ShoulderArmor, Head, Helmet, Spear elements
    - Ensure minimum 6 primitives, maximum 15
    - _Requirements: 1.1, 1.3, 1.9_

  - [x] 2.2 Rewrite BuildRook with 8+ primitives
    - Add wide Base, BodyLower, BodyUpper, 4 Merlon elements, Platform
    - Ensure minimum 8 primitives, maximum 15
    - _Requirements: 1.1, 1.4, 1.9_

  - [x] 2.3 Rewrite BuildKnight with 8+ primitives
    - Add Base, HorseBody, Neck, Head, Muzzle, EarL, EarR, Mane
    - Ensure minimum 8 primitives, maximum 15
    - _Requirements: 1.1, 1.5, 1.9_

  - [x] 2.4 Rewrite BuildBishop with 8+ primitives
    - Add Base, RobedBody, Head, Mitre, Staff, StaffOrb, ShoulderCapeL, ShoulderCapeR
    - Ensure minimum 8 primitives, maximum 15
    - _Requirements: 1.1, 1.6, 1.9_

  - [x] 2.5 Rewrite BuildQueen with 10+ primitives
    - Add Base, Body, Neck, Head, CrownRing, 4 CrownPoint elements, Orb
    - Ensure minimum 10 primitives, maximum 15
    - _Requirements: 1.1, 1.7, 1.9_

  - [x] 2.6 Rewrite BuildKing with 10+ primitives
    - Add Base, Body, Head, Crown, CrossVertical, CrossHorizontal, PauldronL, PauldronR, SwordBlade, SwordGuard
    - Ensure minimum 10 primitives, maximum 15
    - _Requirements: 1.1, 1.8, 1.9_

  - [x] 2.7 Update ApplyColor to handle new part names for material categorization
    - Accent parts: Crown, CrossVertical, CrossHorizontal, Mitre, Orb, CrownRing, CrownPoint*, StaffOrb
    - Weapon parts: Spear, Staff, SwordBlade, SwordGuard
    - All other parts: base material
    - _Requirements: 2.3_

  - [ ]* 2.8 Write property test for primitive count bounds (Property 1)
    - **Property 1: Primitive count bounds per piece type**
    - For any PieceType and PieceColor, verify child primitive count is within [min, 15] where min is type-specific (6/8/8/8/10/10)
    - **Validates: Requirements 1.1, 1.9**

  - [ ]* 2.9 Write property test for material assignment by part category (Property 2)
    - **Property 2: Material assignment by part category**
    - For any PieceType and PieceColor, verify decorative elements have accent material properties and weapon elements have weapon material properties
    - **Validates: Requirements 2.3**

  - [ ]* 2.10 Write property test for collider correctness (Property 13)
    - **Property 13: Collider correctness on piece models**
    - For any PieceType and PieceColor, verify exactly 1 BoxCollider on root and 0 Colliders on children
    - **Validates: Requirements 7.3, 7.4**

  - [x] 2.11 Sync PlaceholderModelFactory changes to Scripts/Pieces/PlaceholderModelFactory.cs
    - Copy updated `Assets/Scripts/Pieces/PlaceholderModelFactory.cs` to `Scripts/Pieces/PlaceholderModelFactory.cs`
    - _Requirements: 7.1_

- [x] 3. Checkpoint - Ensure models and materials work
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Create VFXSystem for runtime particle effects
  - [x] 4.1 Create VFXSystem MonoBehaviour class
    - Create `Assets/Scripts/Visual/VFXSystem.cs` with namespace `WizardChess.Visual`
    - Add `DeathVFXType` enum: StoneFragments, MagicSparkles, CombinedDebris, KingShockwave
    - Implement `SpawnImpactBurst(Vector3 position, Color color, int particleCount = 12)` — creates ParticleSystem with burst of 8+ particles, lifetime 0.3–0.8s
    - Implement `SpawnMagicProjectile(Vector3 from, Vector3 to, float duration, Color color)` — creates trail ParticleSystem moving from attacker to defender
    - Implement `SpawnDeathDebris(Vector3 position, DeathVFXType type, Color color)` — spawns type-appropriate debris particles
    - Implement `SpawnShockwave(Vector3 position, float radius = 2f, float duration = 0.5f)` — expanding ring for King death
    - Implement `DestroyAfterLifetime(GameObject, float)` coroutine for auto-cleanup
    - All ParticleSystem components created at runtime, no prefabs
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [x] 4.2 Add mobile particle reduction to VFXSystem
    - Check `MaterialFactory.IsMobile()` in each spawn method
    - Reduce particle counts by 50% on mobile
    - Disable particle collision on mobile
    - _Requirements: 5.7_

  - [ ]* 4.3 Write property test for VFX creates ParticleSystem (Property 8)
    - **Property 8: VFX creates ParticleSystem components at runtime**
    - For any VFX spawn method, verify the created GameObject contains at least one ParticleSystem and no prefab references
    - **Validates: Requirements 5.5**

  - [ ]* 4.4 Write property test for VFX auto-cleanup (Property 9)
    - **Property 9: VFX auto-cleanup after particle lifetime**
    - For any VFX spawn, verify cleanup coroutine is scheduled
    - **Validates: Requirements 5.6**

  - [ ]* 4.5 Write property test for mobile particle reduction (Property 10)
    - **Property 10: Mobile particle reduction**
    - For any VFX spawn when IsMobile() is true, verify particle count ≤ 50% of desktop count and collision disabled
    - **Validates: Requirements 5.7**

  - [ ]* 4.6 Write property test for death VFX type mapping (Property 7)
    - **Property 7: Death VFX type matches piece death type**
    - For any PieceType, verify correct DeathVFXType mapping: Pawn/Rook/Knight → StoneFragments, Bishop → MagicSparkles, Queen → CombinedDebris, King → KingShockwave
    - **Validates: Requirements 5.3**

  - [x] 4.7 Sync VFXSystem to Scripts/Visual/VFXSystem.cs
    - Copy `Assets/Scripts/Visual/VFXSystem.cs` to `Scripts/Visual/VFXSystem.cs`
    - _Requirements: 7.1_

- [x] 5. Enhance BattleSystem with VFX integration and King-specific handling
  - [x] 5.1 Add VFXSystem dependency to BattleSystem
    - Add `[SerializeField] private MonoBehaviour vfxSystemComponent` field
    - Add `private VFXSystem _vfxSystem` field and resolve in `Awake()`
    - Add `SetVFXSystem(VFXSystem)` public setter method
    - Guard all VFX calls with `if (_vfxSystem != null)`
    - _Requirements: 7.2_

  - [x] 5.2 Integrate VFX calls into ExecuteCapture sequence
    - After attack animation impact: call `_vfxSystem.SpawnImpactBurst(defenderPos, color)`
    - For Bishop/Queen attacks: call `_vfxSystem.SpawnMagicProjectile(attackerPos, defenderPos, duration, color)`
    - At death effect start: call `_vfxSystem.SpawnDeathDebris(defenderPos, deathType, color)`
    - For King death: call `_vfxSystem.SpawnShockwave(defenderPos)`
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 5.3 Add King-specific camera parameters in ExecuteCapture
    - When `defenderType == PieceType.King`: use transition duration 0.8s (pass to `TransitionToBattleView` overload)
    - When `defenderType == PieceType.King`: use shake intensity 0.25 instead of 0.15
    - _Requirements: 6.3_

  - [x] 5.4 Enhance Pawn attack animation with weapon rotation
    - In `PawnAttack()`, add 30-degree forward rotation on the Spear child element during lunge phase
    - Restore weapon rotation on return
    - _Requirements: 3.1_

  - [x] 5.5 Enhance Bishop attack to spawn magic projectile VFX
    - In `BishopAttack()`, at the pulse moment spawn a projectile VFX toward the defender position
    - Requires passing defender position into attack animation or storing it as battle context
    - _Requirements: 3.4_

  - [ ]* 5.6 Write property test for attack phase timing (Property 5)
    - **Property 5: Attack animation fits within phase fraction**
    - For any PieceType, verify sum of sub-phase durations equals total attack phase duration (35% of battle duration)
    - **Validates: Requirements 3.7**

  - [ ]* 5.7 Write property test for input blocked during battle (Property 14)
    - **Property 14: Input blocked during battle**
    - For any ExecuteCapture call, verify input is disabled before animations and re-enabled after completion (including error cases)
    - **Validates: Requirements 7.5, 7.6**

  - [x] 5.8 Sync BattleSystem changes to Scripts/Battle/BattleSystem.cs
    - Copy updated `Assets/Scripts/Battle/BattleSystem.cs` to `Scripts/Battle/BattleSystem.cs`
    - _Requirements: 7.2_

- [x] 6. Checkpoint - Ensure VFX and battle integration work
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Enhance AnimationController with transform restoration guarantee
  - [x] 7.1 Add explicit localScale restoration to AttackAnimationCoroutine and HitReactionCoroutine
    - Save `originalScale` at start of each coroutine
    - Restore `piece.transform.localScale = originalScale` at end alongside position restoration
    - _Requirements: 7.7_

  - [ ]* 7.2 Write property test for transform restoration (Property 15)
    - **Property 15: Transform restored after non-death animations**
    - For any piece and non-death animation (Attack, Hit_Reaction), verify position and localScale are restored to original values after coroutine completes
    - **Validates: Requirements 7.7**

  - [ ]* 7.3 Write property test for death effect deactivation (Property 6)
    - **Property 6: Death effect deactivates piece**
    - For any PieceType, after death effect coroutine completes, verify `piece.activeSelf == false`
    - **Validates: Requirements 4.7**

  - [x] 7.4 Sync AnimationController changes to Scripts/Animation/AnimationController.cs
    - Copy updated `Assets/Scripts/Animation/AnimationController.cs` to `Scripts/Animation/AnimationController.cs`
    - _Requirements: 7.7_

- [x] 8. Enhance CameraSystem with King-specific battle parameters
  - [x] 8.1 Add TransitionToBattleView overload with custom duration
    - Add `public IEnumerator TransitionToBattleView(Vector3 attackerPos, Vector3 defenderPos, float customDuration)` overload
    - Existing method delegates to overload with default `transitionDuration`
    - King captures pass 0.8s duration from BattleSystem
    - _Requirements: 6.1, 6.3_

  - [ ]* 8.2 Write property test for camera battle view geometry (Property 11)
    - **Property 11: Camera battle view geometry**
    - For any two world positions, after TransitionToBattleView, verify camera is perpendicular to battle axis at ~3 units distance and ~2 units height
    - **Validates: Requirements 6.1**

  - [ ]* 8.3 Write property test for camera returns to gameplay (Property 12)
    - **Property 12: Camera returns to gameplay position**
    - After any battle sequence, verify camera position equals gameplayPosition and rotation equals gameplayRotation
    - **Validates: Requirements 6.4**

  - [x] 8.4 Sync CameraSystem changes to Scripts/Camera/CameraSystem.cs
    - Copy updated `Assets/Scripts/Camera/CameraSystem.cs` to `Scripts/Camera/CameraSystem.cs`
    - _Requirements: 6.3, 6.5_

- [x] 9. Final checkpoint - Full integration verification
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests validate universal correctness properties from the design document
- All changes in `Assets/Scripts/` must be synced to `Scripts/` folder
- All VFX are created at runtime using ParticleSystem — no prefab assets required
- Public APIs (`CreatePieceModel`, `ExecuteCapture`) remain unchanged
