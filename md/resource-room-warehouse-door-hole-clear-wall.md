# ResourceRoomWareHouse: Door Hole + Clear Wall

Эта заметка описывает только текущую рабочую операцию постановки дверного проёма и очистки стены для `ResourceRoomWareHouse`.

## Что делает planner

Источник логики: `Assets/LevelGeneration/DungeonBuildPlanner.asset`

Текущая активная цепочка у `ResourceRoomWareHouse`:

1. `Quick Align` ставит комнату к `SideCorridor`.
2. `Get Field Position` берёт позицию самой комнаты.
3. `Get Nearest Cell In` по `SideCorridor` находит ближайшую клетку коридора к комнате.
4. `Get Cell Position` переводит найденную corridor-cell в world position.
5. `Get Nearest Cell In` по `ResourceRoomWareHouse` находит ближайшую room-cell к этой точке.
6. `Get Direction from to` считает направление `room -> corridor`.
7. `Add Cell Instruction` на стороне комнаты пишет `Door Command` в найденную room-cell с направлением дверного проёма.
8. `Invert Value` берёт обратное направление от door direction.
9. `Add Cell Instruction` на стороне коридора пишет `Allign to ID` в найденную corridor-cell с инвертированным направлением.

Итого:

- `Door Hole` ставится на клетке комнаты.
- `Clear Wall` ставится на клетке коридора.
- направление `Clear Wall` всегда равно обратному направлению дверного проёма.

## Какие инструкции реально пишутся

У `ResourceRoomWareHouse` локальная переменная:

- `Door Command = 1`

У `SideCorridor` используются переменные:

- `Door Command = 1`
- `Allign to ID = 0`

Поэтому в текущей схеме:

- room-side `Add Cell Instruction` пишет ID `1`
- corridor-side `Add Cell Instruction` пишет ID `0`

## Как это маппится в Warehouse.asset

Источник правил: `Assets/LevelGeneration/Warehouse.asset`

Таблица соответствия:

- `Clear Wall` -> `InstructionType: 3` -> `FieldMod_WarehouseWallRemover`
- `Door Hole` -> `InstructionType: 1` -> `FieldMod_Warehouse_Doorway`

Это значит:

- planner не спавнит doorway напрямую
- planner только пишет cell instructions
- `Warehouse.asset` уже решает, какой modificator/префаб применить

## Почему очистка стены идёт только в нужную сторону

`FieldMod_WarehouseWallRemover` работает направленно:

- `RemoveWhen: 3` -> удаление только `InFront`
- `UseSelfRotation: 2` -> направление берётся из rotation/dir самой инструкции

Так как corridor-side инструкция получает `Invert(door direction)`, очистка стены идёт только в сторону дверного прохода со стороны коридора, а не по любой стене клетки.

## Критичный смысл операции

Это две связанные, но разные операции:

- комната получает `Door Hole`
- коридор получает `Clear Wall`

Их нельзя сводить в одну клетку без изменения поведения.

Также нельзя заменить corridor direction на тот же direction, что у двери, если не меняется логика wall remover в `Warehouse.asset`.

## Полезные точки входа

- `Assets/LevelGeneration/DungeonBuildPlanner.asset` -> `ResourceRoomWareHouse`
- `Assets/LevelGeneration/DungeonBuildPlanner.asset` -> `Get Direction from to`
- `Assets/LevelGeneration/DungeonBuildPlanner.asset` -> `Invert Value`
- `Assets/LevelGeneration/DungeonBuildPlanner.asset` -> оба `Add Cell Instruction`
- `Assets/LevelGeneration/Warehouse.asset` -> `Clear Wall`
- `Assets/LevelGeneration/Warehouse.asset` -> `Door Hole`
- `Assets/LevelGeneration/Warehouse.asset` -> `FieldMod_Warehouse_Doorway`
- `Assets/LevelGeneration/Warehouse.asset` -> `FieldMod_WarehouseWallRemover`

