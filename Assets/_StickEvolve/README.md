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

- 1 герой слева (синий стикмен), стреляет автоматически.
- Волны врагов справа (5 типов: Fighter / Runner / Tank / Mage / Boss).
- Между волнами — окно «Выбери карту» (3 случайные карты из 11).
- HUD сверху: золото / номер волны / общий HP героев.
- Game Over → «Повторить».
- Сейв в `Application.persistentDataPath/stickevolve_save.json` (отдельно от AlchemyShop).

## Что НЕ в прототипе (будет позже)

- Звук, музыка
- Спрайт-арт (всё на цветных прямоугольниках)
- Эры (Каменный век → Средневековье → …)
- Idle-режим (заработок в фоне)
- Реклама, IAP
- Локализация
- Сложные карточки (пока 11 базовых)

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
    ├── UI/          HUDController, GameOverUI
    ├── Data/        StickSaveData, StickSaveSystem
    ├── Bootstrap/   PrototypeBootstrapper
    └── Editor/      StickEvolveMenu (Unity menu)
```

## Удалить сейв

**StickEvolve → Delete Save File** — снести JSON-сейв (для отладки баланса).

## Существующий AlchemyShop остаётся нетронутым

Папка `Assets/_Project/` — старая алхимическая лавка, не пересекается с `Assets/_StickEvolve/`. PR #1 (хижина) и PR #2 (стенд) остаются открытыми как историческое.
