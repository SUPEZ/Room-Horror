# Level Generation Notes

Рабочая шпаргалка по текущему генератору уровня в проекте.

## TL;DR

- Это не самописный генератор на наших `Assets/Scripts/LevelGeneration/*`.
- Реальная генерация построена на плагине `FImpossible Creations / PGG`.
- Главные source-of-truth ассеты:
  - `Assets/LevelGeneration/WareHouseBuildPlanner.asset`
  - `Assets/LevelGeneration/Warehouse.asset`
- Главный сценовый раннер:
  - `Assets/Scenes/MainHideoutScene3D.unity` -> объект `WareHouse build Planner Executor`
- В сцене уже лежит сгенерированный результат:
  - объект `MainWareHouse` с `GridPainter`
- Сейчас генерация не стартует автоматически в Play Mode:
  - у executor `GenerateOnStart = 0`
  - у generated `GridPainter` тоже `GenerateOnStart = 0`

Итог: сцена использует уже сохраненный результат генерации. Если хотим новую раскладку, нужно править preset/setup и регенерировать в редакторе.

## Что здесь чем является

### 1. BuildPlannerPreset

Файл: `Assets/LevelGeneration/WareHouseBuildPlanner.asset`

Это схема высокого уровня:
- какие `FieldPlanner` участвуют
- какие у них shape generators
- сколько инстансов каждого планировщика
- какие planner-graph ноды выполняются

В этом preset есть два важных `FieldPlanner`:

1. `MainWareHouse`
- Shape generator: статический прямоугольник `5 x 1 x 5`
- `Instances = 1`
- Это базовое ядро/главный warehouse-блок

2. `0LevelRoomWareHouse`
- Shape generator: `SG_RandomTunnelsLimited`
- Параметры из asset:
  - `BranchLength = 5..7`
  - `TargetBranches = 1..1`
  - `BranchThickness = 1..1`
  - `SeparationFactor = 15`
  - лимиты `MinSize = (-10,-10,-10)`, `MaxSize = (10,10,10)`
- `Instances = 4`
- Это дочерние/дополнительные куски layout, которые пристраиваются к главному блоку

### 2. FieldSetup

Файл: `Assets/LevelGeneration/Warehouse.asset`

Это уже не layout, а правила превращения клеток/инструкций в реальные объекты:
- размер клетки: `3 x 1 x 3`
- root modificator pack
- door / wall removal rules
- prefab spawning rules
- cell instructions

Ключевые instructions в `Warehouse.asset`:
- `Door Hole`
  - target modification: `FieldMod_Warehouse_Doorway`
- `Clear Wall`
  - target modification: `FieldMod_WarehouseWallRemover`

То есть planner сначала строит абстрактную сетку и cell instructions, а `FieldSetup` уже решает, какие префабы/модификаторы применять к клеткам.

### 3. BuildPlannerExecutor

Сцена: `Assets/Scenes/MainHideoutScene3D.unity`
Объект: `WareHouse build Planner Executor`

На нем висит `BuildPlannerExecutor`, который ссылается на:
- `BuildPlannerPreset = Assets/LevelGeneration/WareHouseBuildPlanner.asset`
- `FieldSetup composition = Assets/LevelGeneration/Warehouse.asset`

Его реальный пайплайн такой:

1. `Generate()`
- чистит прошлую генерацию
- делает `GeneratePreview()`

2. `GeneratePreview()`
- делает `DeepCopy()` от `BuildPlannerPreset`
- применяет overrides из `_plannerPrepare`
- запускает `RunProceduresAndGeneratePrint(seed)`
- получает `PlanGenerationPrint`

3. После завершения preview
- executor конвертирует abstract planner result в `GridPainter` или `FlexibleGenerator`
- у нас используется обычный `GridPainter`

4. `GridPainter.GenerateObjects()`
- читает grid + instructions
- через `FieldSetup` порождает реальные объекты

### 4. GridPainter

Сцена: `MainWareHouse`

Это уже concrete generated result:
- хранит grid
- хранит `CellsInstructions`
- хранит ссылку на `FieldPreset`
- инстанцирует реальные объекты сцены

Очень важный practical point:

`MainWareHouse` в сцене уже сериализован вместе с generated children.  
То есть это не "runtime-only output", а сохраненный результат работы генератора.

## Как именно строится текущая схема

### MainWareHouse

`MainWareHouse` почти без логики:
- shape = прямоугольник
- выступает как основа, к которой дальше пытаются "прижаться" остальные поля

### 0LevelRoomWareHouse

У `0LevelRoomWareHouse` логика уже есть:

- старт нода `PE_Start`
- дальше `PR_TightPlacement`
- если placement не найден, срабатывает `PR_DiscardField`
- есть `PR_GetFieldPlannerVariable`
- есть function node `Add Counter Cell Instructions`

Практический смысл такой:

1. Генерируется форма room/tunnel-поля.
2. `PR_TightPlacement` пытается прижать это поле к уже существующему полю так, чтобы:
- не влезть в коллизии
- получить компактный общий bounds
- найти контактные клетки между двумя полями

3. Если контакт найден:
- сохраняются `ContactCell` и `AlignedToCell`
- дальше в обе клетки пишутся counter-instructions

4. Если placement не найден:
- planner discard'ится
- такой room просто не идет дальше в генерацию

## Откуда берутся двери и вырезы в стенах

Это важная часть, чтобы быстро понимать "почему тут дверь/дырка".

### Planner variables

В `FieldPlanner` используются переменные:
- `Door Command`
- `Allign to ID` (опечатка в имени, но это текущее имя в asset)

`PR_GetFieldPlannerVariable` читает эти переменные.

Дальше function node `Add Counter Cell Instructions` добавляет reciprocal instructions в пару контактных клеток.

Смысл:
- одна сторона контакта получает инструкцию "сделай doorway / opening"
- вторая сторона получает связанную counter-инструкцию

Потом `Warehouse.asset` интерпретирует эти instructions:
- `FieldMod_Warehouse_Doorway` ставит doorway prefab
- `FieldMod_WarehouseWallRemover` убирает лишний wall spawn

### Что важно помнить

Если "геометрия поля" правильная, но двери/проемы странные, смотреть нужно не в shape generator, а в:
- `CellsInstructions` generated `GridPainter`
- `Warehouse.asset`
- doorway / wall remover modifications

## Почему в `Assets/Scripts/LevelGeneration` почти пусто

Потому что текущая генерация живет в:
- scene YAML
- `BuildPlannerPreset`
- `FieldSetup`
- коде плагина PGG

Наш `Assets/Scripts/LevelGeneration/OrientationSwitcher.cs` не управляет layout generation.  
Это просто вспомогательный runtime-компонент для переключения объектов по углу камеры.

## Где искать правду, если нужно что-то менять

### Если нужно менять форму/структуру layout

Идти сюда:
- `Assets/LevelGeneration/WareHouseBuildPlanner.asset`

Меняется там:
- shape generators
- instances count
- planner graph
- tight placement behavior

### Если нужно менять, какие объекты спавнятся на клетках

Идти сюда:
- `Assets/LevelGeneration/Warehouse.asset`

Меняется там:
- cell size
- modificator packs
- doorway/wall logic
- prefab spawning rules

### Если нужно понять, что уже реально сгенерировано в сцене

Смотреть сюда:
- `Assets/Scenes/MainHideoutScene3D.unity`
- объект `WareHouse build Planner Executor`
- объект `MainWareHouse`
- компонент `BuildPlannerReference` на generated sections

## Самая полезная ментальная модель

Думать об этом так:

1. `BuildPlannerPreset` = "абстрактный план комнат/секций"
2. `PlanGenerationPrint` = "результат layout-расчета"
3. `GridPainter` = "перенос layout-результата в генератор объектов"
4. `FieldSetup` = "правила, какие префабы и модификаторы рождаются из клеток и инструкций"

То есть:

`Planner` строит форму ->  
`Executor` получает print ->  
`GridPainter` получает grid + instructions ->  
`FieldSetup` спавнит реальные объекты

## Что важно не забыть в следующий раз

- Сейчас layout не регенерируется автоматически при старте сцены.
- В сцене лежит уже сохраненный generated output.
- Если править не тот слой, будет казаться, что "генератор не работает":
  - правим planner, но смотрим на старый saved result
  - правим field setup, но не перегенерируем output
  - правим сценовый clone, хотя source-of-truth лежит в asset

## Быстрый checklist для себя

Если нужно быстро разобраться при следующем заходе:

1. Открыть `Assets/Scenes/MainHideoutScene3D.unity`
2. Найти `WareHouse build Planner Executor`
3. Проверить:
- какой preset подключен
- `GenerateOnStart`
- список generated generators/painters
4. Найти child `MainWareHouse`
5. Проверить `GridPainter`:
- `FieldPreset`
- `CellsInstructions`
- generated children
6. Если проблема в форме:
- открыть `Assets/LevelGeneration/WareHouseBuildPlanner.asset`
7. Если проблема в doorway/walls/prefabs:
- открыть `Assets/LevelGeneration/Warehouse.asset`
