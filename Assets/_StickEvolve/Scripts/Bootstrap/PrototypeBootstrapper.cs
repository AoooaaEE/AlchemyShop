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
        [Header("Кампания")]
        [SerializeField] private int currentLevelNumber = 1;
        public static PrototypeBootstrapper Instance { get; private set; }
        public int CurrentLevelNumber => currentLevelNumber;
        public LevelDefinition CurrentLevel { get; private set; }

        public void SetLevelNumber(int level)
        {
            currentLevelNumber = Mathf.Clamp(level, 1, CampaignBuilder.TotalLevels);
        }

        [SerializeField] private float heroMoveSpeed = 4.5f;
        [SerializeField] private Sprite backgroundSprite;

        private StickGame _game;
        private WaveSpawner _spawner;
        private HUDController _hud;
        private CardChoiceUI _cardUI;
        private GameOverUI _gameOverUI;
        private LevelCompleteUI _levelCompleteUI;
        private WorldMapUI _worldMapUI;
        private Camera _cam;
        private Canvas _canvas;

        private readonly List<Hero> _heroes = new();
        private float _heroSpacing = 1.5f;
        private float _heroX = -5f;
        private float _enemyX = 6f;

        private int _rerollCount;
        private long _goldAtLevelStart;
        private const int RerollBaseCost = 3;

        // Стоимость "Купить всё" растёт от номера волны, иначе к 10-й волне это становится бесплатным.
        private int CurrentBuyAllCost => 25 + _spawner.CurrentWaveIndex * 8;
        // Реролл тоже немного дороже каждой следующей волны.
        private int CurrentRerollBaseCost => RerollBaseCost + _spawner.CurrentWaveIndex / 4;

        private static readonly HeroClass[] ExtraClassPool =
        {
            HeroClass.Archer, HeroClass.Mage, HeroClass.Tank,
            HeroClass.Archer, HeroClass.Tank, HeroClass.Berserker,
        };

        private void Start()
        {
            Instance = this;
            BuildCamera();
            ComputePlayfieldBounds();
            BuildBackground();
            BuildCanvas();
            BuildGameManager();
            BuildSpawner();
            BuildHUD();
            BuildCardUI();
            BuildGameOverUI();
            BuildLevelCompleteUI();
            BuildWorldMapUI();

            CardEffect.ExtraHeroSpawner = SpawnExtraHero;
            CardEffect.ExtraHeroSpawnerByClass = SpawnExtraHeroOfClass;
            Hero.CloneSpawnerFunc = SpawnHeroClone;

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
            _cam.backgroundColor = ColorPalette.SkyTop;
        }

        private void ComputePlayfieldBounds()
        {
            float halfHeight = _cam.orthographicSize;
            float halfWidth = halfHeight * Mathf.Max(_cam.aspect, 0.5f);
            // Hero внутри левого края, спавн врагов чуть за правым краем
            _heroX = -halfWidth + 1.5f;
            _enemyX = halfWidth + 1f;
        }

        private static Sprite _battleOverlaySprite;

        private void BuildBackground()
        {
            if (_cam != null) _cam.backgroundColor = ColorPalette.SkyMid;

            if (backgroundSprite != null)
            {
                var bgGO = new GameObject("BackgroundImage");
                var bgSR = bgGO.AddComponent<SpriteRenderer>();
                bgSR.sprite = backgroundSprite;
                bgSR.sortingOrder = -1000;
                bgGO.transform.position = new Vector3(0f, 0f, 0f);

                float camHeight = _cam.orthographicSize * 2f;
                float camWidth = camHeight * _cam.aspect;
                float spriteWidth = backgroundSprite.bounds.size.x;
                float spriteHeight = backgroundSprite.bounds.size.y;
                float cover = Mathf.Max(camWidth / spriteWidth, camHeight / spriteHeight);
                bgGO.transform.localScale = new Vector3(cover, cover, 1f);

                CreateBottomBattleOverlay(camWidth, 2.8f);
                return;
            }

            // Если спрайт не задан, оставляем только камеру и аналогичную цветовую подложку.
            var fallback = new GameObject("BackgroundFallback");
            var fallbackSR = fallback.AddComponent<SpriteRenderer>();
            fallbackSR.sprite = SpriteFactory.White();
            fallbackSR.color = ColorPalette.SkyMid;
            fallbackSR.sortingOrder = -1000;
            fallback.transform.position = new Vector3(0f, 0f, 0f);
            fallback.transform.localScale = new Vector3(40f, _cam.orthographicSize * 2f / 1f, 1f);

            CreateBottomBattleOverlay(_cam.orthographicSize * 2f * _cam.aspect, 2.8f);
        }

        private void CreateBottomBattleOverlay(float width, float height)
        {
            var overlay = new GameObject("BackgroundBattleOverlay");
            var sr = overlay.AddComponent<SpriteRenderer>();
            sr.sprite = GetBattleOverlaySprite();
            sr.sortingOrder = -950;
            overlay.transform.position = new Vector3(0f, -_cam.orthographicSize, 0f);
            overlay.transform.localScale = new Vector3(width / sr.sprite.bounds.size.x, height / sr.sprite.bounds.size.y, 1f);
        }

        private static Sprite GetBattleOverlaySprite()
        {
            if (_battleOverlaySprite != null) return _battleOverlaySprite;

            const int texWidth = 16;
            const int texHeight = 128;
            var tex = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };

            for (int y = 0; y < texHeight; y++)
            {
                float t = (float)y / (texHeight - 1);
                var color = new Color(0.02f, 0.06f, 0.18f, Mathf.Lerp(0.72f, 0f, t));
                for (int x = 0; x < texWidth; x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();

            _battleOverlaySprite = Sprite.Create(tex, new Rect(0f, 0f, texWidth, texHeight), new Vector2(0.5f, 0f), texHeight / 2f);
            _battleOverlaySprite.name = "BattleOverlayGradient";
            return _battleOverlaySprite;
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
            _cardUI.OnRerollClicked += OnShopReroll;
            _cardUI.OnBuyAllClicked += OnShopBuyAll;
        }

        private void BuildGameOverUI()
        {
            _gameOverUI = GameOverUI.Create(_canvas, RestartGame);
            _game.OnGameOver += OnGameOver;
        }

        private void BuildLevelCompleteUI()
        {
            _levelCompleteUI = LevelCompleteUI.Create(_canvas, GoToNextLevel, ShowWorldMap);
        }

        private void BuildWorldMapUI()
        {
            _worldMapUI = WorldMapUI.Create(_canvas, OnLevelChosenFromMap, OnMapClosed);
        }

        public void ShowWorldMap()
        {
            if (_worldMapUI != null) _worldMapUI.Show();
        }

        private void OnLevelChosenFromMap(int level)
        {
            SetLevelNumber(level);
            if (_worldMapUI != null) _worldMapUI.Hide();
            RestartGame();
        }

        private void OnMapClosed()
        {
            // Если игрок закрыл карту через X — продолжаем с того уровня, на котором были.
        }

        private void GoToNextLevel()
        {
            SetLevelNumber(Mathf.Clamp(currentLevelNumber + 1, 1, CampaignBuilder.TotalLevels));
            if (_worldMapUI != null) _worldMapUI.Hide();
            RestartGame();
        }

        private List<WaveConfig> BuildWaves()
        {
            CurrentLevel = CampaignBuilder.Build(currentLevelNumber);
            return CurrentLevel.waves;
        }

        private void SpawnInitialHero()
        {
            var h = SpawnHeroAt(new Vector3(_heroX, 0f, 0f), HeroClass.Warrior);
            _heroes.Add(h);
            HookHeroDeath(h);

            // Сначала восстанавливаем героев, нанятых картами конкретных классов…
            var defaults = CardProgression.Compute();
            for (int i = 0; i < defaults.classHires.Count; i++)
                SpawnExtraHeroOfClass(defaults.classHires[i]);

            // …потом обычные «none-specific» дополнительные герои.
            for (int i = 0; i < defaults.extraHeroes; i++)
                SpawnExtraHero();
        }

        private Hero SpawnExtraHero()
        {
            var cls = ExtraClassPool[Random.Range(0, ExtraClassPool.Length)];
            return SpawnExtraHeroOfClass(cls);
        }

        private Hero SpawnExtraHeroOfClass(HeroClass cls)
        {
            int idx = _heroes.Count;
            float y = (idx % 2 == 0 ? 1f : -1f) * Mathf.Ceil(idx / 2f) * _heroSpacing;
            var h = SpawnHeroAt(new Vector3(_heroX - (idx * 0.2f), y, 0f), cls);
            _heroes.Add(h);
            HookHeroDeath(h);
            return h;
        }

        // Клон Ninja: спавнится временно, не входит в постоянный отряд и не триггерит GameOver на смерти.
        private Hero SpawnHeroClone(HeroClass cls, Vector3 pos)
        {
            return SpawnHeroAt(pos, cls);
        }

        private Hero SpawnHeroAt(Vector3 pos, HeroClass cls)
        {
            var s = HeroClassStats.Get(cls);
            var go = new GameObject($"Hero_{cls}");
            go.transform.position = pos;

            var cfg = StickmanConfig.Default(s.tint);
            cfg.bodyScale = s.bodyScale;
            cfg.hasHat = s.hasHat;
            cfg.hatColor = new Color(s.tint.r * 0.4f, s.tint.g * 0.4f, s.tint.b * 0.6f);
            cfg.wideShoulders = s.wideShoulders;
            cfg.raiseRightArm = true;
            if (s.hasCape)
            {
                cfg.hasCape = true;
                cfg.capeColor = s.capeColor;
            }
            // Снайпер/Маг — лёгкие защитные перчатки
            if (cls == HeroClass.Sniper || cls == HeroClass.Mage)
            {
                cfg.handColor = ColorPalette.HudPanel;
            }
            // Танк/Берсерк — щитоподобная фигура, без видимых перчаток-кистей
            if (cls == HeroClass.Tank || cls == HeroClass.Berserker)
            {
                cfg.handSize = 0.13f; // крупнее кулаки
            }
            StickmanBuilder.Build(go, cfg);

            // Тень под героем —
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            sh.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            sh.transform.localScale = new Vector3(0.85f, 0.22f, 1f);
            var shSR = sh.AddComponent<SpriteRenderer>();
            shSR.sprite = SpriteFactory.SoftCircle();
            shSR.color = ColorPalette.HpBarBack;
            shSR.sortingOrder = 1;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.4f);
            col.isTrigger = true;

            go.AddComponent<TeamMember>();
            go.AddComponent<Health>();

            var hero = go.AddComponent<Hero>();
            hero.HeroClass = cls;
            hero.bulletColor = ColorPalette.BulletAlly;

            // Применяем накопленные апгрейды из карт (или базу при чистом сейве) + классовые множители.
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
                if (CountNonCloneAlive() == 0)
                    StartCoroutine(CheckGameOverAfterDelay());
            };
        }

        // Клоны (Ninja) не входят в постоянный отряд: их живые объекты не должны блокировать GameOver.
        private static int CountNonCloneAlive()
        {
            int n = 0;
            var alive = HeroRegistry.Instance.Alive;
            for (int i = 0; i < alive.Count; i++)
            {
                var h = alive[i];
                if (h != null && !h.isClone) n++;
            }
            return n;
        }

        // После смерти всех героев ждём пару кадров: если волна в этот момент завершилась (босс убит
        // в той же кадр), OnWaveCompleted ревайвнет героев, и геймовер не нужен.
        private IEnumerator CheckGameOverAfterDelay()
        {
            yield return null;
            yield return null;
            if (CountNonCloneAlive() == 0 && EnemyRegistry.Instance.Alive.Count > 0)
                _game.TriggerGameOver();
        }

        private void BeginGame()
        {
            _goldAtLevelStart = _game != null ? _game.Economy.Gold : 0;
            _game.NotifyWaveStarted(_spawner.waves[0].waveNumber);
            _spawner.StartNextWave();
        }

        private void Update()
        {
            if (_heroes.Count == 0 || _cam == null) return;
            if (_cardUI != null && _cardUI.IsOpen) return;
            if (_game != null && _game.IsGameOver) return;

            float vert = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vert += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vert -= 1f;
            if (Mathf.Approximately(vert, 0f)) return;

            float dy = vert * heroMoveSpeed * Time.deltaTime;
            float halfH = _cam.orthographicSize - 0.6f;
            for (int i = 0; i < _heroes.Count; i++)
            {
                var h = _heroes[i];
                if (h == null) continue;
                if (!h.gameObject.activeSelf) continue;
                var p = h.transform.position;
                p.y = Mathf.Clamp(p.y + dy, -halfH, halfH);
                h.transform.position = p;
            }
        }

        private void OnWaveCompleted(int waveNum)
        {
            _game.NotifyWaveCompleted(waveNum);
            // Если все волны пройдены — событие OnAllWavesCompleted сработает из RunWave, не вызываем здесь
            if (_spawner.CurrentWaveIndex >= _spawner.waves.Count) return;

            // Между волнами: поднимаем всех павших героев и лечим всех живых.
            ReviveAndHealHeroes();

            _rerollCount = 0;
            var options = CardCatalog.RollThree();
            _cardUI.Show(options, _game.Economy.Gold, CurrentRerollBaseCost, CurrentBuyAllCost, OnCardPicked);
        }

        private void OnShopReroll()
        {
            int cost = CurrentRerollBaseCost + _rerollCount;
            if (_game.Economy.Gold < cost) return;
            _game.Economy.TrySpend(cost);
            _rerollCount++;
            var newOptions = CardCatalog.RollThree();
            _cardUI.ReplaceCards(newOptions);
            _cardUI.RefreshShop(_game.Economy.Gold, CurrentRerollBaseCost + _rerollCount, CurrentBuyAllCost);
        }

        private void OnShopBuyAll()
        {
            int allCost = CurrentBuyAllCost;
            if (_game.Economy.Gold < allCost) return;
            var current = _cardUI.CurrentOptions;
            if (current == null || current.Count == 0) return;
            _game.Economy.TrySpend(allCost);
            for (int i = 0; i < current.Count; i++)
                CardEffect.Apply(current[i]);
            _cardUI.Hide();
            _game.PersistSave();
            _game.NotifyWaveStarted(_spawner.waves[_spawner.CurrentWaveIndex].waveNumber);
            _spawner.StartNextWave();
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
            if (_spawner != null) _spawner.StopCurrent();

            int stars = ComputeStars();
            LevelProgress.MarkCompleted(currentLevelNumber, stars);
            if (_game != null) _game.PersistSave();

            ReviveAndHealHeroes();
            long goldEarned = _game.Economy.Gold - _goldAtLevelStart;
            if (goldEarned < 0) goldEarned = _game.Economy.Gold;
            _levelCompleteUI?.Show(currentLevelNumber, goldEarned, stars);
        }

        private int ComputeStars()
        {
            float totalCur = 0f, totalMax = 0f;
            for (int i = 0; i < _heroes.Count; i++)
            {
                var h = _heroes[i];
                if (h == null) continue;
                var hp = h.GetComponent<Health>();
                if (hp == null) continue;
                totalCur += Mathf.Max(0f, hp.CurrentHp);
                totalMax += hp.MaxHp;
            }
            if (totalMax <= 0f) return 1;
            float ratio = totalCur / totalMax;
            if (ratio >= 0.75f) return 3;
            if (ratio >= 0.40f) return 2;
            return 1;
        }

        private void OnGameOver()
        {
            // Останавливаем волну: без этого новые враги продолжают спавниться и двигаться поверх экрана Game Over.
            if (_spawner != null) _spawner.StopCurrent();
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
            CardEffect.ExtraHeroSpawnerByClass = null;
            Hero.CloneSpawnerFunc = null;
            if (_game != null) _game.OnGameOver -= OnGameOver;
            if (_spawner != null)
            {
                _spawner.OnWaveCompleted -= OnWaveCompleted;
                _spawner.OnAllWavesCompleted -= OnAllWavesCompleted;
            }
            if (_cardUI != null)
            {
                _cardUI.OnRerollClicked -= OnShopReroll;
                _cardUI.OnBuyAllClicked -= OnShopBuyAll;
            }
        }
    }
}
