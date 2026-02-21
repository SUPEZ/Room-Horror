# Рекомендации по промту для Copilot в проекте Room-Horror

## Анализ проекта

После сканирования проекта выявлены следующие ключевые аспекты:

### Текущая структура скриптов (7 классов):

| Файл | Описание |
|------|----------|
| `CameraManager.cs` | Singleton для управления камерой, два режима (Free/Character) |
| `CameraModeSwitcher.cs` | Legacy-компонент переключения режимов |
| `FreeCameraController.cs` | Контроллер свободной камеры (WASD + мышь) |
| `OrientationSwitcher.cs` | Видимость объектов по углу камеры |
| `RotateTowardsCamera.cs` | Поворот объекта к камере (billboard) |
| `CameraAngleObserver.cs` | Пустой скрипт (заготовка) |
| `NavigationContext.cs` | Пустой скрипт (заготовка) |

### Используемые технологии:
- **TopDown Engine** (MoreMountains) - LevelManager, MMCameraEvent, CharacterAbility
- **Zenject** - DI система (не активно используется в скриптах)
- **New Input System** - Keyboard.current, Mouse.current
- **URP** - Universal Render Pipeline с 2D Lighting
- **Процедурная генерация уровней** - система на базе Warehouse prefabs

### Целевая платформа:
- **PC (Windows)** - полный доступ к возможностям .NET

---

## Критичные требования для промта

### 1. Платформа PC (Windows)
```
✅ ДОСТУПНО:
- async/await, Task, Threading
- File I/O операции
- Полный .NET API
- Native плагины

⚠️ РЕКОМЕНДАЦИИ:
- Coroutines для Unity-специфичных операций
- async/await для I/O операций
- Threading для тяжёлых вычислений (не в главном потоке)
```

### 2. Производительность
- Избегать аллокаций в Update()
- Кэшировать компоненты в Awake/Start
- Не использовать LINQ в hot-paths
- Пулинг объектов

### 3. Интеграция с TopDown Engine
```csharp
// Паттерн доступа к игроку
if (LevelManager.HasInstance && LevelManager.Instance.Players?.Count > 0)
{
    var player = LevelManager.Instance.Players[0];
}

// Управление камерой
MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
MMCameraEvent.Trigger(MMCameraEventTypes.StopFollowing);
```

### 4. Стиль кода
```csharp
private float _privateField;           // underscore prefix
public float PublicProperty { get; }   // PascalCase
const float MAX_SPEED = 10f;           // UPPER_SNAKE_CASE
```

---

## Пример идеального промта для инструкции

```markdown
# Copilot Instructions

## Контекст проекта
Unity3D хоррор-игра для PC на базе TopDown Engine (MoreMountains).
Процедурная генерация уровней на основе Warehouse prefabs.

## Обязательные правила
1. **Комментарии на английском**
2. **Производительность** - кэширование, без аллокаций в Update
3. **XML документация** для public методов
4. **Проверяй архитектуру** - следуй лучшим практикам Unity

## Стиль кода
- Private fields: _camelCase
- Public: PascalCase  
- Constants: UPPER_SNAKE_CASE
- [SerializeField] вместо public полей

## Ключевые системы
- CameraManager.Instance для камеры
- LevelManager.HasInstance для проверки игрока
- MMCameraEvent для событий камеры
- Процедурная генерация через Warehouse assets

## Тестирование
- Компиляция без ошибок
- Проверка в Play mode
- Null checks обязательны
```

---

## Что можно улучшить в текущих скриптах

1. **CameraAngleObserver.cs** и **NavigationContext.cs** - пустые, требуют реализации
2. **CameraModeSwitcher.cs** - дублирует функционал CameraManager, можно удалить
3. **FreeCameraController.cs** - содержит дублирующий код с CameraManager
4. **OrientationSwitcher.cs** - хорошо структурирован, использовать как пример

---

## Рекомендуемая структура md/

```
md/
├── prompt-analysis.md      # Этот файл
├── camera-system.md        # Описание системы камеры
├── level-generation.md     # Процедурная генерация уровней
└── architecture.md         # Общая архитектура
```

