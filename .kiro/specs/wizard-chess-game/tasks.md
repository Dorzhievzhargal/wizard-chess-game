# Задачи: Wizard Chess Game

## Задача 1: Структура проекта и интерфейсы
- [x] 1. Структура проекта и интерфейсы
  - [x] 1.1 Создать структуру папок Unity-проекта (Scripts/, Scripts/Core/, Scripts/Board/, Scripts/Pieces/, Scripts/Animation/, Scripts/Battle/, Scripts/Camera/, Scripts/Interfaces/, Prefabs/, Materials/, Animations/)
  - [x] 1.2 Реализовать типы данных: PieceType, PieceColor, GameState (enum), BoardPosition, ChessPiece, Move, MoveResult (struct)
  - [x] 1.3 Реализовать интерфейсы: IChessEngine, IBoardManager, IPieceController, IAnimationController, IBattleSystem, ICameraSystem

## Задача 2: Chess_Engine (Pure C#)
- [ ] 2. Chess_Engine — шахматный движок (чистый C#, без зависимостей Unity)
  - [ ] 2.1 Реализовать ChessEngine: InitializeBoard(), начальная расстановка фигур, GetPieceAt(), GetAllPieces(), GetCurrentTurn()
  - [ ] 2.2 Реализовать генерацию допустимых ходов GetValidMoves() для всех типов фигур (Pawn, Rook, Knight, Bishop, Queen, King) с учётом шаха
  - [ ] 2.3 Реализовать MakeMove(): выполнение хода, взятие, рокировка, взятие на проходе, превращение пешки
  - [ ] 2.4 Реализовать определение состояния игры: IsInCheck(), IsCheckmate(), IsStalemate(), GetGameState()
  - [ ] 2.5 Реализовать FEN сериализацию/десериализацию: ToFen(), LoadFromFen()
  - [ ]\* 2.6 Property-тест: FEN round-trip — сериализация в FEN с последующей десериализацией воспроизводит эквивалентную позицию
  - [ ]\* 2.7 Unit-тесты: валидация ходов, шах, мат, пат, рокировка, взятие на проходе, превращение пешки


## Задача 3: Board_Manager — шахматная доска
- [ ] 3. Board_Manager — шахматная доска 8×8
  - [ ] 3.1 Реализовать BoardManager: генерация доски 8×8 с чередующимися светлыми/тёмными Tile, координатная система (a-h, 1-8)
  - [ ] 3.2 Реализовать BoardToWorldPosition() и WorldToBoardPosition() — конвертация между координатами доски и мировыми координатами
  - [ ] 3.3 Реализовать HighlightValidMoves() / ClearHighlights() — подсветка допустимых ходов (Valid_Move_Highlight)
  - [ ] 3.4 Реализовать обработку касаний: OnTileClicked event, raycast по доске
  - [ ] 3.5 Реализовать PlacePiece() / RemovePiece() — размещение и удаление фигур на доске

## Задача 4: Piece_Controller — управление фигурами
- [ ] 4. Piece_Controller — фигуры и управление
  - [ ] 4.1 Создать Placeholder_Model (примитивы) для каждого типа фигуры (6 типов × 2 цвета) с различимыми формами
  - [ ] 4.2 Реализовать PieceController: SpawnPieces() — создание всех фигур начальной расстановки из Placeholder_Model
  - [ ] 4.3 Реализовать SelectPiece() / DeselectPiece() — выбор фигуры касанием, визуальная обратная связь
  - [ ] 4.4 Реализовать MovePieceTo() — пошаговое анимированное перемещение фигуры между клетками (не скольжение)
  - [ ] 4.5 Реализовать контроль очерёдности: SetInputEnabled() — ввод принимается только от игрока текущего хода

## Задача 5: GameManager — координатор
- [ ] 5. GameManager — основной игровой цикл
  - [ ] 5.1 Реализовать GameManager: инициализация всех модулей (ChessEngine, BoardManager, PieceController)
  - [ ] 5.2 Реализовать обработку выбора фигуры: tap → GetValidMoves → HighlightValidMoves
  - [ ] 5.3 Реализовать обработку хода: tap на целевую клетку → MakeMove → MovePieceTo → ClearHighlights
  - [ ] 5.4 Реализовать базовое взятие: обнаружение capture → удаление захваченной фигуры → перемещение атакующей
  - [ ] 5.5 Реализовать обработку состояний игры: шах (уведомление), мат/пат (завершение игры)
  - [ ] 5.6 Реализовать превращение пешки: UI выбора фигуры при достижении последнего ряда


## Задача 6: Animation_Controller — система анимаций
- [ ] 6. Animation_Controller — система анимаций
  - [ ] 6.1 Реализовать AnimationController: PlayAnimation() для 5 состояний (Idle, Move, Attack, Hit_Reaction, Death)
  - [ ] 6.2 Определить Animator State Machine структуру с переходами между всеми состояниями
  - [ ] 6.3 Реализовать конвенцию именования: {PieceType}_{AnimationState} (Pawn_Idle, Queen_Attack и т.д.)
  - [ ] 6.4 Реализовать PlayDeathEffect(): Stone Break, Magic Dissolve, Heavy Impact Fall — назначение по типу фигуры
  - [ ] 6.5 Реализовать Idle-анимацию с лёгким движением (дыхание статуи) для всех фигур на доске

## Задача 7: Battle_System — кинематографические бои
- [ ] 7. Battle_System — система взятия с анимациями
  - [ ] 7.1 Реализовать BattleSystem: ExecuteCapture() — оркестрация боевой сцены (1.5–3 сек)
  - [ ] 7.2 Реализовать стили атаки по типу фигуры: Pawn→быстрый удар, Rook→тяжёлый, Knight→разбег, Bishop→магия, Queen→комбо, King→мощный удар
  - [ ] 7.3 Реализовать блокировку ввода (SetInputEnabled(false)) на время Battle_Animation
  - [ ] 7.4 Интегрировать Battle_System в GameManager: замена базового взятия на кинематографическое

## Задача 8: Camera_System — система камеры
- [ ] 8. Camera_System — игровая и боевая камера
  - [ ] 8.1 Реализовать CameraSystem: SetGameplayView() — верхний ракурс под углом, читаемый на мобильном экране
  - [ ] 8.2 Реализовать TransitionToBattleView() — плавный переход камеры на крупный план участников боя
  - [ ] 8.3 Реализовать ReturnToGameplayView() — плавный возврат камеры после боя
  - [ ] 8.4 Реализовать ApplyCameraShake() — тряска камеры при ударе во время Battle_Animation
  - [ ] 8.5 Интегрировать Camera_System в Battle_System: автоматическое переключение ракурсов при взятии

## Задача 9: Визуальное окружение
- [ ] 9. Визуальный стиль и окружение
  - [ ] 9.1 Создать материалы мраморной доски: светлый и тёмный мрамор для Tile
  - [ ] 9.2 Создать материалы фигур: светлый камень + золото + голубое свечение (белые), тёмный камень + металл + красно-фиолетовое свечение (чёрные)
  - [ ] 9.3 Настроить окружение тёмного магического зала: атмосферное освещение, туман, мягкое свечение
  - [ ] 9.4 Оптимизировать материалы и шейдеры для мобильных GPU
