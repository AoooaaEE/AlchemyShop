# _StickEvolve / Stickman Army Evolve — Прототип

2D-прототип «армия стикменов отбивается от волн врагов». Spec: см. <ref_file file="/home/ubuntu/game_proposal.md" />.

## Как запустить (один раз)

1. Открой проект в Unity 6000.0.74f1.
2. В верхнем меню: **StickEvolve → Create Prototype Scene**.
3. Откроется сцена `Assets/_StickEvolve/Scenes/Prototype.unity`. Нажми **Play**.

Альтернативно (если не хочешь создавать новую сцену):
- В любой открытой сцене → **StickEvolve → Add Bootstrap To Current Scene**.
- Нажми **Play**.

## Что в прототипе

- **Карта уровней** — главное меню с выбором уровней, разбитых по биомам.
- **20 уровней** в 2 биомах: Равнины (1-10) и Ледяной биом (11-20).
- Каждый уровень = 10 волн, на 10-й волне — босс.
- 1 герой слева (синий стикмен), стреляет автоматически.
- Волны врагов справа (10 базовых типов + 5 ледяных).
- Между волнами — окно «Выбери карту» (3 случайные карты).
- HUD сверху: золото / уровень+волна / общий HP героев.
- Game Over → возврат на карту уровней.
- Прохождение уровня → возврат на карту, следующий уровень открывается.
- Сейв в `Application.persistentDataPath/stickevolve_save.json`.

## Биомы

- **Равнины** (уровни 1-10): стандартные враги, зелёный фон, солнечный день.
- **Ледяной биом** (уровни 11-20): ледяные враги (IceFighter, IceRunner, IceTank, IceMage, IceBoss), зимний фон.

## Что НЕ в прототипе (будет позже)

- Звук, музыка
- Спрайт-арт (всё на цветных прямоугольниках)
- Ещё биомы (Пустыня, Вулкан, …)
- Idle-режим (заработок в фоне)
- Реклама, IAP
- Локализация

## Структура папок

```
Assets/_StickEvolve/
├── README.md
├── Scenes/                                 # создаётся через меню
└── Scripts/
    ├── Core/        StickGame, StickEconomy
    ├── Combat/      Hero, Enemy, Bullet, Health, TeamMember, DamageNumber
    ├── Wave/        WaveSpawner, WaveConfig, EnemyFactory
    ├── Cards/       CardSO, CardCatalog, CardEffect, CardChoiceUI
    ├── Economy/     SpriteFactory, GoldDrop
    ├── Levels/      BiomeType, BiomeTheme, LevelData, LevelCatalog, LevelMapUI
    ├── UI/          HUDController, GameOverUI
    ├── Data/        StickSaveData, StickSaveSystem
    ├── Bootstrap/   PrototypeBootstrapper
    └── Editor/      StickEvolveMenu (Unity menu)
```

## Удалить сейв

**StickEvolve → Delete Save File** — снести JSON-сейв (для отладки баланса).

## Существующий AlchemyShop остаётся нетронутым

Папка `Assets/_Project/` — старая алхимическая лавка, не пересекается с `Assets/_StickEvolve/`. PR #1 (хижина) и PR #2 (стенд) остаются открытыми как историческое.
