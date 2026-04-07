# Requirements Document

## Introduction

Улучшение визуального качества шахматных фигур и добавление эпичных анимаций боя при взятии фигур в игре Wizard Chess. Текущие фигуры — простые геометрические примитивы (кубы, цилиндры, сферы) из PlaceholderModelFactory. Текущие анимации боя — базовые трансформации (перемещение, масштабирование). Цель — сделать фигуры визуально детализированными и добавить зрелищные эффекты уничтожения, оставаясь в рамках процедурной генерации (без внешних 3D-ассетов).

## Glossary

- **Model_Factory**: Статическая фабрика (PlaceholderModelFactory.cs), создающая 3D-модели фигур из Unity-примитивов
- **Battle_System**: Система (BattleSystem.cs), оркестрирующая анимации боя при взятии фигур
- **Animation_Controller**: Контроллер (AnimationController.cs), управляющий процедурными анимациями фигур
- **Material_Factory**: Фабрика (MaterialFactory.cs), создающая материалы для фигур программно
- **Camera_System**: Система камеры (CameraSystem.cs), управляющая переходами между игровым и боевым видами
- **Piece_Model**: Процедурная 3D-модель шахматной фигуры, составленная из Unity-примитивов
- **VFX_System**: Система визуальных эффектов для боевых анимаций (частицы, вспышки, следы)
- **Piece_Controller**: Контроллер (PieceController.cs), управляющий спавном, выделением и перемещением фигур

## Requirements

### Requirement 1: Детализированные модели фигур

**User Story:** Как игрок, я хочу видеть красивые и детализированные шахматные фигуры, чтобы игра выглядела визуально привлекательно.

#### Acceptance Criteria

1. WHEN a chess piece is spawned, THE Model_Factory SHALL create a Piece_Model composed of at least 6 Unity primitives for Pawn, at least 8 for Rook, Knight, and Bishop, and at least 10 for Queen and King
2. THE Model_Factory SHALL create each Piece_Model with a visually distinct silhouette recognizable from the gameplay camera distance of 14 units
3. WHEN a Pawn Piece_Model is created, THE Model_Factory SHALL include a base pedestal, body, shoulder armor, head, helmet, and a weapon element
4. WHEN a Rook Piece_Model is created, THE Model_Factory SHALL include a wide base, layered tower body, battlements with at least 4 merlon elements, and an inner platform
5. WHEN a Knight Piece_Model is created, THE Model_Factory SHALL include a base, horse body, arched neck, head with muzzle, two ears, and a mane element
6. WHEN a Bishop Piece_Model is created, THE Model_Factory SHALL include a base pedestal, robed body, head, pointed mitre hat, a staff with orb tip, and a shoulder cape element
7. WHEN a Queen Piece_Model is created, THE Model_Factory SHALL include a base pedestal, elegant body, neck, head, a crown with at least 4 point elements, and an orb
8. WHEN a King Piece_Model is created, THE Model_Factory SHALL include a base pedestal, armored body, head, a crown with cross, shoulder pauldrons, and a sword with guard and pommel
9. THE Model_Factory SHALL maintain a total vertex-equivalent primitive count of 15 or fewer per Piece_Model to preserve mobile GPU performance

### Requirement 2: Улучшенные материалы фигур

**User Story:** Как игрок, я хочу чтобы фигуры имели качественные материалы с визуальными эффектами, чтобы белые и чёрные фигуры выглядели эффектно и различимо.

#### Acceptance Criteria

1. THE Material_Factory SHALL create white piece materials with a warm stone base color (RGB approximately 0.90, 0.85, 0.75), metallic gold accents (metallic value 0.6 or higher), and smoothness of 0.4 or higher for the base
2. THE Material_Factory SHALL create black piece materials with a dark stone base color (RGB approximately 0.20, 0.15, 0.20), metallic silver accents (metallic value 0.7 or higher), and smoothness of 0.45 or higher for the base
3. WHEN a Piece_Model is created, THE Model_Factory SHALL apply accent materials to decorative elements (crowns, crosses, orbs, mitre tips) and weapon materials to weapon elements (swords, staffs, spears)
4. THE Material_Factory SHALL apply emission glow to accent material elements with an intensity between 0.1 and 0.5 for URP Lit shader
5. WHILE the application is running on a mobile platform, THE Material_Factory SHALL cap smoothness at 0.5, metallic at 0.3, and emission intensity at 0.3

### Requirement 3: Эпичные анимации атаки

**User Story:** Как игрок, я хочу видеть уникальные и зрелищные анимации атаки для каждого типа фигуры, чтобы взятие фигур ощущалось эпично.

#### Acceptance Criteria

1. WHEN a Pawn executes a capture, THE Battle_System SHALL play a quick spear thrust animation: lunge forward 0.5 units over 40% of attack phase, rotate the weapon element 30 degrees forward during lunge, and return to original position over remaining 60%
2. WHEN a Rook executes a capture, THE Battle_System SHALL play a heavy slam animation: pull back 0.3 units with 15% scale increase over 40% of attack phase, slam forward 0.8 units with ease-in curve over 30%, and recover over remaining 30%
3. WHEN a Knight executes a capture, THE Battle_System SHALL play a charging attack animation: retreat 0.8 units over 30% of attack phase, charge forward 1.2 units with vertical gallop bounce (amplitude 0.15 units, 3 oscillations) over 40%, and return over remaining 30%
4. WHEN a Bishop executes a capture, THE Battle_System SHALL play a magic beam animation: scale up to 1.25x over 50% of attack phase, pulse to 1.4x with sine curve over 30%, spawn a projectile VFX toward the defender, and recover over remaining 20%
5. WHEN a Queen executes a capture, THE Battle_System SHALL play a combo animation: melee lunge 0.6 units over 30% of attack phase, return over 15%, magic blast scale pulse of 1.35x over 30%, and recover over remaining 25%
6. WHEN a King executes a capture, THE Battle_System SHALL play a sword strike animation: rise 0.5 units with 10% scale increase over 40% of attack phase, strike forward 0.7 units with ease-in curve over 30%, and recover over remaining 30%
7. THE Battle_System SHALL complete each attack animation within the configured attack phase fraction (35% of total battle duration)

### Requirement 4: Эпичные эффекты смерти

**User Story:** Как игрок, я хочу видеть зрелищные эффекты уничтожения фигур, чтобы каждое взятие ощущалось как победа.

#### Acceptance Criteria

1. WHEN a Pawn is captured, THE Animation_Controller SHALL play a Heavy Impact Fall effect: topple in a random direction with accelerating rotation up to 90 degrees, move downward 0.2 units, and shrink to zero scale in the final 30% of the death duration
2. WHEN a Rook is captured, THE Animation_Controller SHALL play a Stone Break effect: apply scale jitter (0.95x to 1.05x) with random rotation perturbation (up to 2 degrees per axis), and shrink to zero scale with quadratic easing over the death duration
3. WHEN a Knight is captured, THE Animation_Controller SHALL play a Heavy Impact Fall effect with the same parameters as the Pawn death effect
4. WHEN a Bishop is captured, THE Animation_Controller SHALL play a Magic Dissolve effect: scale up to 1.1x over the first 20% of duration, then shrink to zero over the remaining 80%, while shifting material color toward purple (RGB 0.6, 0.2, 0.8) with alpha fading to 0
5. WHEN a Queen is captured, THE Animation_Controller SHALL play a combined Magic Dissolve and Stone Break effect: dissolve with color shift over the first 50% of duration, then crumble with scale jitter and rotation perturbation over the remaining 50%
6. WHEN a King is captured, THE Animation_Controller SHALL play a dramatic death effect lasting 2x the base death duration: shake with glow pulsing over the first 30%, rise upward 0.3 units over the next 30%, then slow fall with rotation, shrink, and color fade to gold (RGB 1.0, 0.8, 0.0) over the final 40%
7. THE Animation_Controller SHALL deactivate the piece GameObject after the death effect completes

### Requirement 5: Визуальные эффекты боя (VFX)

**User Story:** Как игрок, я хочу видеть визуальные эффекты (частицы, вспышки, следы) во время боя, чтобы сражения выглядели зрелищно.

#### Acceptance Criteria

1. WHEN an attack animation reaches the impact moment, THE VFX_System SHALL spawn an impact particle burst at the defender position consisting of at least 8 particles with a lifetime between 0.3 and 0.8 seconds
2. WHEN a Bishop or Queen executes a magic attack, THE VFX_System SHALL spawn a magic projectile trail VFX that travels from the attacker to the defender over the attack phase duration
3. WHEN a piece death effect begins, THE VFX_System SHALL spawn debris particles appropriate to the death type: stone fragments for Stone Break, magic sparkles for Magic Dissolve, and a combination for the Queen combo effect
4. WHEN a King death effect reaches the shake phase, THE VFX_System SHALL spawn a radial shockwave ring that expands from the King position to a radius of 2 units over 0.5 seconds
5. THE VFX_System SHALL create all particle effects using Unity ParticleSystem components instantiated at runtime without requiring pre-made prefab assets
6. THE VFX_System SHALL automatically destroy particle GameObjects after all particles have finished their lifetime
7. WHILE the application is running on a mobile platform, THE VFX_System SHALL reduce particle counts by 50% and disable particle collision

### Requirement 6: Улучшенная боевая камера

**User Story:** Как игрок, я хочу чтобы камера эффектно показывала бой при взятии фигур, чтобы усилить ощущение эпичности.

#### Acceptance Criteria

1. WHEN a capture battle begins, THE Camera_System SHALL transition to a close-up battle view positioned perpendicular to the battle axis at a distance of 3 units and height of 2 units, looking at the midpoint between attacker and defender
2. WHEN the attack animation reaches the impact moment, THE Camera_System SHALL apply a camera shake with intensity of 0.15 units and duration of 0.25 seconds with linear decay
3. WHEN a King is being captured, THE Camera_System SHALL use a slower transition duration of 0.8 seconds (compared to the standard 0.6 seconds) and increase shake intensity to 0.25 units
4. WHEN the battle completes, THE Camera_System SHALL smoothly return to the gameplay view using SmoothStep interpolation over the transition duration
5. THE Camera_System SHALL complete all transitions using SmoothStep interpolation to avoid abrupt camera movements

### Requirement 7: Интеграция и производительность

**User Story:** Как разработчик, я хочу чтобы все визуальные улучшения были интегрированы в существующую архитектуру без нарушения текущей функциональности.

#### Acceptance Criteria

1. THE Model_Factory SHALL maintain the existing public API signature: CreatePieceModel(PieceType, PieceColor, float) returning a GameObject
2. THE Battle_System SHALL maintain the existing public API signature: ExecuteCapture(GameObject, GameObject, PieceType, PieceType) returning IEnumerator
3. WHEN a new Piece_Model is created, THE Model_Factory SHALL add a single BoxCollider to the root GameObject sized to encompass the piece for raycasting
4. THE Model_Factory SHALL remove all colliders from child primitive GameObjects so only the root BoxCollider is used
5. WHILE a battle animation is in progress, THE Battle_System SHALL block player input via PieceController.SetInputEnabled(false) and re-enable input after the battle completes
6. IF an error occurs during a battle animation, THEN THE Battle_System SHALL ensure player input is re-enabled via the finally block
7. THE Animation_Controller SHALL restore the piece Transform (position and scale) to its original values after any non-death animation completes
