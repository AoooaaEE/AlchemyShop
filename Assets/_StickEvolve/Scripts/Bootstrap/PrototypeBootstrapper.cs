using System.Collections;
using System.Collections.Generic;
using StickEvolve.Cards;
using StickEvolve.Combat;
using StickEvolve.Core;
using StickEvolve.Economy;
using StickEvolve.UI;
using StickEvolve.Wave;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.Bootstrap
{
    /// <summary>
    /// Точка входа прототипа. Кладёшь этот компонент на пустой GameObject в сцене —
    /// он собирает всё остальное (камера, фон, герой, UI, спавнер, карты).
    /// </summary>
    public class PrototypeBootstrapper : MonoBehaviour
    {
        [Header("Конфиг")]
        [SerializeField] private int wavesToPlay = 50;

        private StickGame _game;
        private WaveSpawner _spawner;
        private HUDController _hud;
        private CardChoiceUI _cardUI;
        private GameOverUI _gameOverUI;
        private Camera _cam;
        private Canvas _canvas;

        private readonly List<Hero> _heroes = new();
        private float _heroSpacing = 1.5f;
        private float _heroX = -5f;
        private float _enemyX = 6f;

        private void Start()
        {
            BuildCamera();
            ComputePlayfieldBounds();
            BuildBackground();
            BuildCanvas();
            BuildGameManager();
            BuildSpawner();
            BuildHUD();
            BuildCardUI();
            BuildGameOverUI();

            CardEffect.ExtraHeroSpawner = SpawnExtraHero;

            SpawnInitialHero();
            BeginGame();
        }

        private void BuildCamera()
        {
            var existing = Camera.main;
            if (existing != null)
            {
                _cam = existing;
            }
            else
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                _cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 5.5f;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        }

        private void ComputePlayfieldBounds()
        {
            float halfHeight = _cam.orthographicSize;
            float halfWidth = halfHeight * Mathf.Max(_cam.aspect, 0.5f);
            // Hero внутри левого края, спавн врагов чуть за правым краем
            _heroX = -halfWidth + 1.5f;
            _enemyX = halfWidth + 1f;
        }

        private void BuildBackground()
        {
            // Тёмная полоса земли
            var ground = new GameObject("Ground");
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White();
            sr.color = new Color(0.13f, 0.15f, 0.2f);
            sr.sortingOrder = -10;
            ground.transform.position = new Vector3(0f, -3.2f, 0f);
            ground.transform.localScale = new Vector3(20f, 4f, 1f);

            // Светлая полоса неба (просто более светлый прямоугольник сверху)
            var sky = new GameObject("Sky");
            var skySR = sky.AddComponent<SpriteRenderer>();
            skySR.sprite = SpriteFactory.White();
            skySR.color = new Color(0.18f, 0.22f, 0.3f);
            skySR.sortingOrder = -11;
            sky.transform.position = new Vector3(0f, 2f, 0f);
            sky.transform.localScale = new Vector3(20f, 8f, 1f);
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("UICanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem (нужен для кликов по кнопкам)
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void BuildGameManager()
        {
            var gameGO = new GameObject("StickGame");
            _game = gameGO.AddComponent<StickGame>();
        }

        private void BuildSpawner()
        {
            var spawnerGO = new GameObject("WaveSpawner");
            _spawner = spawnerGO.AddComponent<WaveSpawner>();
            _spawner.spawnX = _enemyX;
            _spawner.spawnYMin = -_cam.orthographicSize * 0.5f;
            _spawner.spawnYMax = _cam.orthographicSize * 0.5f;
            _spawner.Reset(BuildWaves());
            _spawner.OnWaveStarted += (n) => _game.NotifyWaveStarted(n);
            _spawner.OnWaveCompleted += OnWaveCompleted;
            _spawner.OnAllWavesCompleted += OnAllWavesCompleted;
            _game.Spawner = _spawner;
        }

        private void BuildHUD()
        {
            _hud = HUDController.Create(_canvas, _game);
        }

        private void BuildCardUI()
        {
            _cardUI = CardChoiceUI.Create(_canvas);
        }

        private void BuildGameOverUI()
        {
            _gameOverUI = GameOverUI.Create(_canvas, RestartGame);
            _game.OnGameOver += OnGameOver;
        }

        private List<WaveConfig> BuildWaves()
        {
            var waves = new List<WaveConfig>();
            for (int i = 1; i <= wavesToPlay; i++)
            {
                var w = new WaveConfig
                {
                    waveNumber = i,
                    spawnInterval = Mathf.Max(0.30f, 0.85f - i * 0.035f),
                    postWaveDelay = 1.0f,
                    enemyHpMultiplier = 1f + (i - 1) * 0.22f,
                    enemyDamageMultiplier = 1f + (i - 1) * 0.14f,
                    enemyGoldDrop = 1 + i / 2,
                    enemies = new List<WaveEnemy>()
                };
                // Постепенный ввод типов.
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 3 + i / 2 });
                if (i >= 2) w.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner, count = 1 + (i - 1) / 3 });
                if (i >= 3) w.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 1 + (i - 3) / 4 });
                if (i >= 4) w.enemies.Add(new WaveEnemy { kind = EnemyKind.Mage, count = 1 + (i - 4) / 5 });
                if (i % 5 == 0) w.enemies.Add(new WaveEnemy { kind = EnemyKind.Boss, count = 1 });
                waves.Add(w);
            }
            return waves;
        }

        private void SpawnInitialHero()
        {
            var h = SpawnHeroAt(new Vector3(_heroX, 0f, 0f), new Color(0.4f, 0.7f, 1f));
            _heroes.Add(h);
            HookHeroDeath(h);

            // Если из карточного прогресса уже накоплены дополнительные герои — спавним их.
            var defaults = CardProgression.Compute();
            for (int i = 0; i < defaults.extraHeroes; i++)
            {
                SpawnExtraHero();
            }
        }

        private Hero SpawnExtraHero()
        {
            int idx = _heroes.Count;
            float y = (idx % 2 == 0 ? 1f : -1f) * Mathf.Ceil(idx / 2f) * _heroSpacing;
            var h = SpawnHeroAt(new Vector3(_heroX - (idx * 0.2f), y, 0f), new Color(0.45f, 0.85f, 0.55f));
            _heroes.Add(h);
            HookHeroDeath(h);
            return h;
        }

        private Hero SpawnHeroAt(Vector3 pos, Color tint)
        {
            var go = new GameObject("Hero");
            go.transform.position = pos;

            var cfg = StickmanConfig.Default(tint);
            cfg.raiseRightArm = true;
            StickmanBuilder.Build(go, cfg);

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.4f);
            col.isTrigger = true;

            go.AddComponent<TeamMember>();
            go.AddComponent<Health>();

            var hero = go.AddComponent<Hero>();
            hero.bulletColor = new Color(Mathf.Clamp01(tint.r + 0.1f), Mathf.Clamp01(tint.g + 0.2f), 1f);

            // Применяем накопленные апгрейды из карт (или базу при чистом сейве).
            CardEffect.ApplyDefaultsToNewHero(hero);
            return hero;
        }

        private void HookHeroDeath(Hero hero)
        {
            var hp = hero.GetComponent<Health>();
            if (hp == null) return;
            hp.OnDeath += () =>
            {
                if (hero != null && hero.gameObject != null)
                    hero.gameObject.SetActive(false);
                if (HeroRegistry.Instance.Alive.Count == 0)
                    StartCoroutine(CheckGameOverAfterDelay());
            };
        }

        // После смерти всех героев ждём пару кадров: если волна в этот момент завершилась (босс убит
        // в той же кадр), OnWaveCompleted ревайвнет героев, и геймовер не нужен.
        private IEnumerator CheckGameOverAfterDelay()
        {
            yield return null;
            yield return null;
            if (HeroRegistry.Instance.Alive.Count == 0 && EnemyRegistry.Instance.Alive.Count > 0)
                _game.TriggerGameOver();
        }

        private void BeginGame()
        {
            _game.NotifyWaveStarted(_spawner.waves[0].waveNumber);
            _spawner.StartNextWave();
        }

        private void OnWaveCompleted(int waveNum)
        {
            _game.NotifyWaveCompleted(waveNum);
            // Если все волны пройдены — событие OnAllWavesCompleted сработает из RunWave, не вызываем здесь
            if (_spawner.CurrentWaveIndex >= _spawner.waves.Count) return;

            // Между волнами: поднимаем всех павших героев и лечим всех живых.
            ReviveAndHealHeroes();

            var options = CardCatalog.RollThree();
            _cardUI.Show(options, OnCardPicked);
        }

        private void ReviveAndHealHeroes()
        {
            var d = CardProgression.Compute();
            for (int i = 0; i < _heroes.Count; i++)
            {
                var h = _heroes[i];
                if (h == null) continue;
                if (!h.gameObject.activeSelf) h.gameObject.SetActive(true);
                var hp = h.GetComponent<Health>();
                if (hp != null) hp.Configure(d.maxHp, fullHeal: true);
            }
        }

        private void OnCardPicked(CardSO card)
        {
            CardEffect.Apply(card);
            _game.PersistSave();
            // Стартуем следующую волну
            _game.NotifyWaveStarted(_spawner.waves[_spawner.CurrentWaveIndex].waveNumber);
            _spawner.StartNextWave();
        }

        private void OnAllWavesCompleted()
        {
            _game.NotifyAllWavesCompleted();
            _gameOverUI.Show(_game.CurrentWaveNumber);
            Debug.Log("[StickEvolve] Все волны пройдены!");
        }

        private void OnGameOver()
        {
            _gameOverUI.Show(_game.CurrentWaveNumber);
        }

        private void RestartGame()
        {
            // Удалить всех старых героев и врагов
            for (int i = _heroes.Count - 1; i >= 0; i--)
                if (_heroes[i] != null) Destroy(_heroes[i].gameObject);
            _heroes.Clear();

            _spawner.ForceKillAll();
            EnemyRegistry.Instance.Clear();
            HeroRegistry.Instance.Clear();

            _spawner.Reset(BuildWaves());
            _game.Restart();

            SpawnInitialHero();
            BeginGame();
        }

        private void OnDestroy()
        {
            CardEffect.ExtraHeroSpawner = null;
            if (_game != null) _game.OnGameOver -= OnGameOver;
            if (_spawner != null)
            {
                _spawner.OnWaveCompleted -= OnWaveCompleted;
                _spawner.OnAllWavesCompleted -= OnAllWavesCompleted;
            }
        }
    }
}
