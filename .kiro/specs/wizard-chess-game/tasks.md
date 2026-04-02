# Задачи: Wizard Chess Game

## Задача 1: Структура проекта и интерфейсы
- [x] 1. Структура проекта и интерфейсы
  - [x] 1.1 Создать структуру папок Unity-проекта (Scripts/, Scripts/Core/, Scripts/Board/, Scripts/Pieces/, Scripts/Animation/, Scripts/Battle/, Scripts/Camera/, Scripts/Interfaces/, Prefabs/, Materials/, Animations/)
  - [x] 1.2 Реализовать типы данных: PieceType, PieceColor, GameState (enum), BoardPosition, ChessPiece, Move, MoveResult (struct)
  - [x] 1.3 Реализовать интерфейсы: IChessEngine, IBoardManager, IPieceController, IAnimationController, IBattleSystem, ICameraSystem

## Задача 2: Chess_Engine (Pure C#)
- [x] 2. Chess_Engine — шахматный движок (чистый C#, без зависимостей Unity)
  - [x] 2.1 Реализовать ChessEngine: InitializeBoard(), начальная расстановка фигур, GetPieceAt(), GetAllPieces(), GetCurrentTurn()
  - [x] 2.2 Реализовать генерацию допустимых ходов GetValidMoves() для всех типов фигур (Pawn, Rook, Knight, Bishop, Queen, King) с учётом шаха
  - [x] 2.3 Реализовать MakeMove(): выполнение хода, взятие, рокировка, взятие на проходе, превращение пешки
  - [x] 2.4 Реализовать определение состояния игры: IsInCheck(), IsCheckmate(), IsStalemate(), GetGameState()
  - [x] 2.5 Реализовать FEN сериализацию/десериализацию: ToFen(), LoadFromFen()
  - [x] 2.6 Property-тест: FEN round-trip — сериализация в FEN с последующей десериализацией воспроизводит эквивалентную позицию
  - [x] 2.7 Unit-тесты: валидация ходов, шах, мат, пат, рокировка, взятие на проходе, превращение пешки


## Задача 3: Board_Manager — шахматная доска
- [x] 3. Board_Manager — шахматная доска 8×8
  - [x] 3.1 Реализовать BoardManager: генерация доски 8×8 с чередующимися светлыми/тёмными Tile, координатная система (a-h, 1-8)
  - [x] 3.2 Реализовать BoardToWorldPosition() и WorldToBoardPosition() — конвертация между координатами доски и мировыми координатами
  - [x] 3.3 Реализовать HighlightValidMoves() / ClearHighlights() — подсветка допустимых ходов (Valid_Move_Highlight)
  - [x] 3.4 Реализовать обработку касаний: OnTileClicked event, raycast по доске
  - [x] 3.5 Реализовать PlacePiece() / RemovePiece() — размещение и удаление фигур на доске

## Задача 4: Piece_Controller — управление фигурами
- [x] 4. Piece_Controller — фигуры и управление
  - [x] 4.1 Создать Placeholder_Model (примитивы) для каждого типа фигуры (6 типов × 2 цвета) с различимыми формами
  - [x] 4.2 Реализовать PieceController: SpawnPieces() — создание всех фигур начальной расстановки из Placeholder_Model
  - [x] 4.3 Реализовать SelectPiece() / DeselectPiece() — выбор фигуры касанием, визуальная обратная связь
  - [x] 4.4 Реализовать MovePieceTo() — пошаговое анимированное перемещение фигуры между клетками (не скольжение)
  - [x] 4.5 Реализовать контроль очерёдности: SetInputEnabled() — ввод принимается только от игрока текущего хода

## Задача 5: GameManager — координатор
- [x] 5. GameManager — основной игровой цикл
  - [x] 5.1 Реализовать GameManager: инициализация всех модулей (ChessEngine, BoardManager, PieceController)
  - [x] 5.2 Реализовать обработку выбора фигуры: tap → GetValidMoves → HighlightValidMoves
  - [x] 5.3 Реализовать обработку хода: tap на целевую клетку → MakeMove → MovePieceTo → ClearHighlights
  - [x] 5.4 Реализовать базовое взятие: обнаружение capture → удаление захваченной фигуры → перемещение атакующей
  - [x] 5.5 Реализовать обработку состояний игры: шах (уведомление), мат/пат (завершение игры)
  - [x] 5.6 Реализовать превращение пешки: UI выбора фигуры при достижении последнего ряда


## Задача 6: Animation_Controller — система анимаций
- [x] 6. Animation_Controller — система анимаций
  - [x] 6.1 Реализовать AnimationController: PlayAnimation() для 5 состояний (Idle, Move, Attack, Hit_Reaction, Death)
  - [x] 6.2 Определить Animator State Machine структуру с переходами между всеми состояниями
  - [x] 6.3 Реализовать конвенцию именования: {PieceType}_{AnimationState} (Pawn_Idle, Queen_Attack и т.д.)
  - [x] 6.4 Реализовать PlayDeathEffect(): Stone Break, Magic Dissolve, Heavy Impact Fall — назначение по типу фигуры
  - [x] 6.5 Реализовать Idle-анимацию с лёгким движением (дыхание статуи) для всех фигур на доске

## Задача 7: Battle_System — кинематографические бои
- [x] 7. Battle_System — система взятия с анимациями
  - [x] 7.1 Реализовать BattleSystem: ExecuteCapture() — оркестрация боевой сцены (1.5–3 сек)
  - [x] 7.2 Реализовать стили атаки по типу фигуры: Pawn→быстрый удар, Rook→тяжёлый, Knight→разбег, Bishop→магия, Queen→комбо, King→мощный удар
  - [x] 7.3 Реализовать блокировку ввода (SetInputEnabled(false)) на время Battle_Animation
  - [x] 7.4 Интегрировать Battle_System в GameManager: замена базового взятия на кинематографическое

## Задача 8: Camera_System — система камеры
- [x] 8. Camera_System — игровая и боевая камера
  - [x] 8.1 Реализовать CameraSystem: SetGameplayView() — верхний ракурс под углом, читаемый на мобильном экране
  - [x] 8.2 Реализовать TransitionToBattleView() — плавный переход камеры на крупный план участников боя
  - [x] 8.3 Реализовать ReturnToGameplayView() — плавный возврат камеры после боя
  - [x] 8.4 Реализовать ApplyCameraShake() — тряска камеры при ударе во время Battle_Animation
  - [x] 8.5 Интегрировать Camera_System в Battle_System: автоматическое переключение ракурсов при взятии

## Задача 9: Визуальное окружение
- [x] 9. Визуальный стиль и окружение
  - [x] 9.1 Создать материалы мраморной доски: светлый и тёмный мрамор для Tile
  - [x] 9.2 Создать материалы фигур: светлый камень + золото + голубое свечение (белые), тёмный камень + металл + красно-фиолетовое свечение (чёрные)
  - [x] 9.3 Настроить окружение тёмного магического зала: атмосферное освещение, туман, мягкое свечение
  - [x] 9.4 Оптимизировать материалы и шейдеры для мобильных GPU
