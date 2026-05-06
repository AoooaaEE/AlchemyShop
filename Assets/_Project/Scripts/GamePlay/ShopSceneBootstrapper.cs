using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Alchemy.Utils;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Собирает сцену Shop при старте: пол, камера, свет, рабочие столы и алхимик.
    /// Бейкает NavMesh после расстановки всего, что должно быть на полу.
    /// </summary>
    public class ShopSceneBootstrapper : MonoBehaviour
    {
        [Header("Пол")]
        [SerializeField] private Vector3 floorScale = new Vector3(6, 1, 6);

        [Header("Камера")]
        [SerializeField] private Vector3 cameraPos = new Vector3(0, 12, -8);
        [SerializeField] private Vector3 cameraRot = new Vector3(55, 0, 0);
        [SerializeField] private float   cameraFov = 40f;

        [Header("Свет")]
        [SerializeField] private Vector3 lightPos = new Vector3(0, 8, 0);
        [SerializeField] private Vector3 lightRot = new Vector3(50, -30, 0);
        [SerializeField] private float   lightIntensity = 1f;

        [Header("Алхимик (плейсхолдер)")]
        [SerializeField] private Vector3 alchemistPos = new Vector3(0, 1, -18);
        [SerializeField] private float   agentRadius  = 0.4f;
        [SerializeField] private float   agentHeight  = 2f;
        [SerializeField] private float   agentSpeed   = 3.5f;

                // Активный подмастерье в сцене (если куплен апгрейд).
        private GameObject apprenticeRef;

        // Поляна — открытый круг (без деревьев), радиусом ClearingR с центром в ClearingCenter.
        // Хижина и все рабочие столы появляются строго внутри неё.
        private static readonly Vector3 ClearingCenter = new Vector3(0f, 0f, -3f);
        private const float ClearingR = 9f;
        private static readonly Vector3 HutPos      = new Vector3( 0f,   0f, -7f);
        private static readonly Vector3 ShelfPos    = new Vector3(-5.5f, 0f, -3f);
        private static readonly Vector3 CauldronPos = new Vector3( 0f,   0f, -3.5f);
        private static readonly Vector3 TablePos    = new Vector3( 5.5f, 0f, -3f);

        // Ссылка на пад-постройку для каждого id (чтобы спрятать призрак при покупке).
        private readonly System.Collections.Generic.Dictionary<string, GameObject> buildSlots
            = new System.Collections.Generic.Dictionary<string, GameObject>();

                    private void Awake()
        {
            CreateFloor();
            var camGo = CreateCamera();
            CreateLight();
            CreateForest();
            CreatePath();
            CreateWorkstations();
            CreateCustomerSystem();
            CreateQueueSeller();
            // Апгрейд-пады (подмастерье/цена/расширение) появляются только после постройки хижины.
            if (GetBuildLevel("build_hut") >= 1) CreateUpgradePads();
            var alchemist  = CreateAlchemist();
            camGo.GetComponent<Alchemy.Gameplay.IsoCameraFollow>()?.SetTarget(alchemist.transform); 
                        BakeNavMesh();
            CreateDecor();
            AddAgentTo(alchemist);

            // Превращаем алхимика в игрока: снимаем AI-контроллер, ставим PlayerController.
            var alchemistAi = alchemist.GetComponent<Alchemy.Gameplay.AlchemistController>();
            if (alchemistAi != null) Destroy(alchemistAi);
            alchemist.AddComponent<Alchemy.Gameplay.PlayerController>();
            alchemist.AddComponent<Alchemy.Gameplay.PlayerCarry>();

            CreateUI();

            // Подмастерье спавним только если он уже куплен.
            EnsureApprenticeFromUpgrades();
            if (Alchemy.Gameplay.UpgradeService.Instance != null)
                Alchemy.Gameplay.UpgradeService.Instance.OnUpgradeChanged += OnUpgradeChanged;

            Debug.Log("[ShopSceneBootstrapper] Сцена собрана.");
        }
                private void CreateUpgradePads()
        {
            CreateUpgradePad("potion_price",    new Vector3(-6f, 0f,  0f), new Color(0.75f, 0.55f, 0.95f));
            CreateUpgradePad("expansion",       new Vector3(-6f, 0f,  2.5f), new Color(0.55f, 0.75f, 0.95f));
            CreateUpgradePad("hire_apprentice", new Vector3(-6f, 0f, -2.5f), new Color(0.55f, 0.95f, 0.55f));
        }

        private void CreateUpgradePad(string id, Vector3 pos, Color color)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad_" + id;
            pad.transform.SetParent(transform, false);
            pad.transform.position   = new Vector3(pos.x, 0.025f, pos.z);
            pad.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
            ApplyColor(pad, color);

            var col = pad.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var mod = pad.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.ignoreFromBuild = true;

            var logic = pad.AddComponent<Alchemy.Gameplay.UpgradePad>();
            logic.Configure(id);
        }

                private void OnDestroy()
        {
            if (Alchemy.Gameplay.UpgradeService.Instance != null)
                Alchemy.Gameplay.UpgradeService.Instance.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, int level)
        {
            if (id == "hire_apprentice" && level >= 1) EnsureApprentice();
            if (id == "build_hut"      && level >= 1) OnHutBuilt();
        }

        private void OnHutBuilt()
        {
            HideBuildSlot("build_hut");
            CreateHut(HutPos);

            // В хижине сразу появляются все рабочие столы и апгрейд-пады.
            CreateRealWorkstation(WorkstationType.Shelf,         ShelfPos);
            CreateRealWorkstation(WorkstationType.Cauldron,      CauldronPos);
            CreateRealWorkstation(WorkstationType.BottlingTable, TablePos);
            CreateUpgradePads();

            // Лавка открылась — пускаем клиентов.
            if (CustomerSpawner.Instance != null)
                CustomerSpawner.Instance.SetSpawningEnabled(true);
        }

        private void CreateRealWorkstation(WorkstationType type, Vector3 pos)
        {
            switch (type)
            {
                case WorkstationType.Shelf:
                    CreateWorkstation(WorkstationType.Shelf, pos,
                        size:   new Vector3(1.2f, 1.5f, 1f),
                        color:  new Color(0.55f, 0.35f, 0.18f),
                        input:  Alchemy.Gameplay.CarryItem.None,
                        output: Alchemy.Gameplay.CarryItem.Ingredient);
                    break;
                case WorkstationType.Cauldron:
                    CreateWorkstation(WorkstationType.Cauldron, pos,
                        size:   new Vector3(1.4f, 1.0f, 1.4f),
                        color:  new Color(0.30f, 0.30f, 0.32f),
                        input:  Alchemy.Gameplay.CarryItem.Ingredient,
                        output: Alchemy.Gameplay.CarryItem.BrewedPotion);
                    break;
                case WorkstationType.BottlingTable:
                    CreateWorkstation(WorkstationType.BottlingTable, pos,
                        size:   new Vector3(1.4f, 1.0f, 1f),
                        color:  new Color(0.20f, 0.45f, 0.65f),
                        input:  Alchemy.Gameplay.CarryItem.BrewedPotion,
                        output: Alchemy.Gameplay.CarryItem.BottledPotion);
                    break;
            }
        }

        private void HideBuildSlot(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (buildSlots.TryGetValue(id, out var go) && go != null)
            {
                Destroy(go);
                buildSlots.Remove(id);
            }
        }

        private void EnsureApprenticeFromUpgrades()
        {
            var svc = Alchemy.Gameplay.UpgradeService.Instance;
            if (svc != null && svc.GetLevel("hire_apprentice") >= 1) EnsureApprentice();
        }

        private void EnsureApprentice()
        {
            if (apprenticeRef != null) return;

            var go = CreateApprentice();
            AddAgentTo(go);

            var unwanted = go.GetComponent<Alchemy.Gameplay.AlchemistController>();
            if (unwanted != null) Destroy(unwanted);
            go.AddComponent<Alchemy.Gameplay.ApprenticeController>();

            apprenticeRef = go;
        }
 

        private void CreateFloor()
        {
            // Базовый плейн для NavMesh и света.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(transform, false);
            floor.transform.localScale = floorScale;
            ApplyColor(floor, new Color(0.32f, 0.24f, 0.18f));

            // Сверху раскладываем декоративные каменные тайлы в сетку.
            // Размер одного тайла KayKit — 4×4 ед., скейлируем.
            int gridX = Mathf.Max(1, Mathf.RoundToInt(floorScale.x * 10f / 4f));
            int gridZ = Mathf.Max(1, Mathf.RoundToInt(floorScale.z * 10f / 4f));
            float halfX = (gridX - 1) * 2f;
            float halfZ = (gridZ - 1) * 2f;
            for (int ix = 0; ix < gridX; ix++)
            for (int iz = 0; iz < gridZ; iz++)
            {
                var tile = ModelLoader.TryInstantiate(ModelLoader.FloorPath, "FloorTile", transform);
                if (tile == null) return; // без модели — оставляем базовый плейн.
                tile.transform.position = new Vector3(ix * 4f - halfX, 0.01f, iz * 4f - halfZ);
                ModelLoader.StripColliders(tile);
                var mod = tile.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                mod.ignoreFromBuild = true; // NavMesh берём с базового Floor.
            }
        }

                private GameObject CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetParent(transform, false);
            go.transform.position    = cameraPos;
            go.transform.eulerAngles = cameraRot;

            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = cameraFov;
            cam.clearFlags  = CameraClearFlags.Skybox;

            go.AddComponent<AudioListener>();
            go.AddComponent<Alchemy.Gameplay.IsoCameraFollow>();

            return go;
        }

        private void CreateLight()
        {
            var go = new GameObject("Directional Light");
            go.transform.SetParent(transform, false);
            go.transform.position    = lightPos;
            go.transform.eulerAngles = lightRot;

            var l = go.AddComponent<Light>();
            l.type      = LightType.Directional;
            l.intensity = lightIntensity;
            l.color     = new Color(1f, 0.95f, 0.85f); // тёплый солнечный свет
            l.shadows   = LightShadows.Soft;

            // Мягкий ambient — лавка, не открытое поле.
            RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor  = new Color(0.55f, 0.50f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.40f, 0.50f);
            RenderSettings.ambientGroundColor  = new Color(0.20f, 0.15f, 0.18f);
        }

        /// <summary>
        /// Декоративные пропсы вдоль стен лавки. Добавляются после NavMesh-бейка,
        /// чтобы не запекаться в навигацию (всё равно с NavMeshModifier ignoreFromBuild).
        /// </summary>
        private void CreateDecor()
        {
            // ВАЖНО: декор уходит ТОЛЬКО на края лавки, чтобы не закрывать
            // путь от спавна клиентов (z≈9) до очереди (z≈2.5) и до котла (z≈-3.5).
            // Бочки и ящики — далеко в углах, за пределами рабочей зоны.
            SpawnProp("BarrelDecor", new Vector3(-7f, 0f, -5.5f), 0f, new Vector3(0.9f, 1.2f, 0.9f));
            SpawnProp("BarrelDecor", new Vector3( 7f, 0f, -5.5f), 0f, new Vector3(0.9f, 1.2f, 0.9f));
            SpawnProp("CratesDecor", new Vector3( 7f, 0f,  5.5f), 0f, new Vector3(1.5f, 1.5f, 1.5f));

            // Колонны — только в дальних углах за пределами навигации.
            var pillarBox = new Vector3(0.7f, 3f, 0.7f);
            SpawnProp("Pillar", new Vector3(-8f, 0f,  8f), 0f, pillarBox);
            SpawnProp("Pillar", new Vector3( 8f, 0f,  8f), 0f, pillarBox);
            SpawnProp("Pillar", new Vector3(-8f, 0f, -7f), 0f, pillarBox);
            SpawnProp("Pillar", new Vector3( 8f, 0f, -7f), 0f, pillarBox);

            // Сундук с золотом — слева у апгрейдов, не на пути.
            SpawnProp("ChestGold", new Vector3(-7f, 0f, 5.5f), 0f, new Vector3(1f, 0.7f, 0.7f));
        }

        private GameObject SpawnProp(string modelName, Vector3 position, float yaw = 0f,
            Vector3? obstacleSize = null)
        {
            var go = ModelLoader.TryInstantiateProp(modelName, transform);
            if (go == null) return null;
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            ModelLoader.StripColliders(go);

            // Декоративный пропс — персонажи должны его обходить.
            var size = obstacleSize ?? new Vector3(1f, 1.2f, 1f);
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape   = NavMeshObstacleShape.Box;
            obstacle.center  = new Vector3(0f, size.y * 0.5f, 0f);
            obstacle.size    = size;
            obstacle.carving = true;
            return go;
        }

                    private void CreateWorkstations()
        {
            // Единственный билд-пад — хижина. Внутри неё после постройки появляются
            // все рабочие столы и апгрейд-пады разом, никакой отдельной покупки полки/котла/стола.
            int hutLvl = GetBuildLevel("build_hut");
            if (hutLvl >= 1)
            {
                CreateHut(HutPos);
                CreateRealWorkstation(WorkstationType.Shelf,         ShelfPos);
                CreateRealWorkstation(WorkstationType.Cauldron,      CauldronPos);
                CreateRealWorkstation(WorkstationType.BottlingTable, TablePos);
            }
            else
            {
                CreateBuildSlot("build_hut", HutPos, ghostKind: GhostKind.Hut);
            }
        }

        private enum GhostKind { Hut }

        private static int GetBuildLevel(string id)
        {
            return UpgradeService.Instance != null ? UpgradeService.Instance.GetLevel(id) : 0;
        }

        /// <summary>
        /// Создаёт пад-постройку: визуальный «призрак» нужного объекта + UpgradePad,
        /// который списывает золото за fillTime секунд и вызывает OnUpgradeChanged.
        /// </summary>
        private void CreateBuildSlot(string upgradeId, Vector3 pos, GhostKind ghostKind)
        {
            var root = new GameObject("BuildSlot_" + upgradeId);
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(pos.x, 0f, pos.z);

            // 1) Призрачная модель сверху над падом.
            GameObject ghost = null;
            switch (ghostKind)
            {
                case GhostKind.Hut:
                    ghost = ModelLoader.TryInstantiateBuilding("building_home_A_red", root.transform);
                    if (ghost != null) ghost.transform.localScale = Vector3.one * 2.0f;
                    break;
            }
            if (ghost != null)
            {
                ghost.transform.localPosition = Vector3.zero;
                ModelLoader.StripColliders(ghost);
                ModelLoader.MakeGhost(ghost, new Color(0.55f, 0.85f, 1f), alpha: 0.40f);
                // Призрак не блокирует NavMesh.
                var mod = ghost.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                mod.ignoreFromBuild = true;
            }

            // 2) Сам пад — плоский квадрат у земли, как у обычных апгрейд-падов.
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            pad.transform.SetParent(root.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            pad.transform.localScale    = new Vector3(1.6f, 0.05f, 1.6f);
            ApplyColor(pad, new Color(0.95f, 0.85f, 0.45f));
            var col = pad.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            var padMod = pad.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            padMod.ignoreFromBuild = true;
            var logic = pad.AddComponent<Alchemy.Gameplay.UpgradePad>();
            logic.Configure(upgradeId);

            buildSlots[upgradeId] = root;
        }

        /// <summary>
        /// Ставит хижину-фон позади рабочего ряда. Чисто визуальный объект — лавка «открылась».
        /// </summary>
        private void CreateHut(Vector3 pos)
        {
            var go = ModelLoader.TryInstantiateBuilding("building_home_A_red", transform);
            if (go == null) return;
            go.name = "Hut";
            go.transform.position   = new Vector3(pos.x, 0f, pos.z);
            go.transform.localScale = Vector3.one * 2.0f;
            go.transform.rotation   = Quaternion.Euler(0f, 180f, 0f); // дверью к игроку
            ModelLoader.StripColliders(go);

            // Хижина не блокирует путь — визуальный фон.
            var mod = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.ignoreFromBuild = true;
        }

        /// <summary>
        /// Версия CreateWorkstation для рантайма (после покупки build_*). Вызывается
        /// после BakeNavMesh, поэтому полагается на NavMeshObstacle.carving для динамики.
        /// </summary>
        private void CreateWorkstation(WorkstationType type, Vector3 position,
            Vector3 size, Color color, Alchemy.Gameplay.CarryItem input,
            Alchemy.Gameplay.CarryItem output)
        {
            CreateWorkstationInternal(type, position, size, color, input, output);
        }
    
                private void CreateWorkstationInternal(WorkstationType type, Vector3 position, Vector3 size, Color color,
            Alchemy.Gameplay.CarryItem input, Alchemy.Gameplay.CarryItem output)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);

            // Сначала пробуем красивую KayKit-модель. Если её нет — оставляем
            // подкрашенный кубик-плейсхолдер (старое поведение).
            string modelName = type switch
            {
                WorkstationType.Shelf         => "Shelf",
                WorkstationType.Cauldron      => "Cauldron",
                WorkstationType.BottlingTable => "BottlingTable",
                _ => null
            };
            GameObject visual = string.IsNullOrEmpty(modelName)
                ? null
                : ModelLoader.TryInstantiateProp(modelName, go.transform);

            if (visual != null)
            {
                visual.transform.localPosition = Vector3.zero;
                ModelLoader.StripColliders(visual);

                // Поверх «котла» добавляем светящуюся жижу нужного цвета.
                if (type == WorkstationType.Cauldron)
                    AddCauldronLiquid(visual.transform);
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Visual";
                cube.transform.SetParent(go.transform, false);
                cube.transform.localPosition = new Vector3(0f, position.y, 0f);
                cube.transform.localScale    = size;
                ApplyColor(cube, color);
                ModelLoader.StripColliders(cube);
            }

            // Точка взаимодействия — перед столом, ближе к центру лавки.
            var interaction = new GameObject("Interaction");
            interaction.transform.SetParent(go.transform, false);
            interaction.transform.position = new Vector3(position.x, 0f, position.z + 1.5f);

            var ws = go.AddComponent<Workstation>();
            ws.Setup(type, interaction.transform);

            // Авто-обработка для игрока.
            var processor = go.AddComponent<Alchemy.Gameplay.WorkstationProcessor>();
            processor.Configure(input, output, time: 1.2f);

            // NavMeshObstacle с carving — персонажи будут обтекать стол/полку,
            // а не проходить сквозь.
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape   = NavMeshObstacleShape.Box;
            obstacle.center  = new Vector3(0f, size.y * 0.5f, 0f);
            obstacle.size    = new Vector3(size.x, size.y, size.z);
            obstacle.carving = true;
        }

        private static void AddCauldronLiquid(Transform parent)
        {
            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Liquid";
            liquid.transform.SetParent(parent, false);
            liquid.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            liquid.transform.localScale    = new Vector3(0.55f, 0.05f, 0.55f);
            var col = liquid.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = liquid.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                mat.color = new Color(0.55f, 0.25f, 0.85f, 1f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.55f, 0.25f, 0.85f) * 0.6f);
                }
                rend.sharedMaterial = mat;
            }
        }

        

                private GameObject CreateAlchemist()
        {
            // Пытаемся взять модель Мага — это наш алхимик.
            var go = ModelLoader.TryInstantiateCharacter("Mage", transform);
            bool isModel = (go != null);
            if (!isModel)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ApplyColor(go, new Color(0.85f, 0.75f, 0.95f));
                go.transform.SetParent(transform, false);
            }
            go.name = "Alchemist";
            // Модель: pivot у ступней — y=0. Капсула: pivot в центре — y из настройки.
            go.transform.position = isModel
                ? new Vector3(alchemistPos.x, 0f, alchemistPos.z)
                : alchemistPos;
            ModelLoader.StripColliders(go);

            var mod = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.ignoreFromBuild = true;

            return go;
        }

        private void BakeNavMesh()
        {
            var surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
        }

        private void AddAgentTo(GameObject alchemist)
        {
            var agent = alchemist.AddComponent<NavMeshAgent>();
            agent.radius       = agentRadius;
            agent.height       = agentHeight;
            agent.speed        = agentSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            // Модели KayKit имеют pivot у ступней — обнуляем baseOffset, иначе
            // персонаж парит над NavMesh.
            bool isModel = alchemist.GetComponent<MeshFilter>() == null;
            agent.baseOffset = isModel ? 0f : 1f;

            alchemist.AddComponent<AlchemistController>();
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<MeshRenderer>();
            if (rend == null) return;

            // URP-материал по умолчанию.
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            rend.sharedMaterial = mat;
        }
            private void CreateCustomerSystem()
        {
            var queue = gameObject.AddComponent<CustomerQueue>();
            queue.Configure(
                frontSlot: new Vector3(0f, 0f, 2.5f),
                spacing:   2.5f,    // модели KayKit шире капсул — больший шаг
                max:       4);

            var spawner = gameObject.AddComponent<CustomerSpawner>();
            spawner.Configure(
                spawn:    new Vector3(0f, 1f, 9f),
                exit:     new Vector3(0f, 1f, 12f),
                interval: 3.5f);

            // До покупки хижины клиентов нет — лавка ещё не открылась.
            bool hutBuilt = GetBuildLevel("build_hut") >= 1;
            spawner.SetSpawningEnabled(hutBuilt);
        }

        /// <summary>
        /// Раскидывает деревья по всей карте кроме поляны (большой круг в центре),
        /// тропы (узкий коридор от спавна на юг к поляне) и северного коридора (откуда
        /// приходят клиенты). Игрок спавнится в плотной чаще.
        /// </summary>
        private void CreateForest()
        {
            // Только высокие деревья — игрок стоит «в высоком лесу».
            string[] big = { "trees_A_large", "trees_B_medium", "trees_A_medium" };
            const float pathHalfWidth   = 1.8f; // полуширина свободной тропы
            const float spawnClearR     = 1.4f; // вокруг точки спавна — пусто
            const float clearingPad     = 1.5f; // запас вокруг поляны (без деревьев)

            // Грид-сэмплинг — гарантирует плотный, но не слипающийся лес.
            // Шаг ~2.6 ед даёт хорошую плотность при крупном масштабе деревьев.
            const float step = 2.6f;
            const float halfMap = 22f;
            for (float x = -halfMap; x <= halfMap; x += step)
            {
                for (float z = -halfMap; z <= halfMap; z += step)
                {
                    float jx = x + Random.Range(-0.5f, 0.5f);
                    float jz = z + Random.Range(-0.5f, 0.5f);
                    var pos = new Vector3(jx, 0f, jz);

                    // 1) Внутри поляны — никаких деревьев.
                    if (Vector3.Distance(pos, ClearingCenter) < ClearingR + clearingPad) continue;

                    // 2) Тропа от поляны до спавна игрока — узкий пустой коридор по оси X≈0.
                    //    Идёт от южного края поляны (z = ClearingCenter.z - ClearingR) до спавна.
                    float pathZSouth = ClearingCenter.z - ClearingR;
                    float pathZNorth = pathZSouth + clearingPad;
                    if (pos.z < pathZNorth && pos.z > alchemistPos.z - 1f &&
                        Mathf.Abs(pos.x - alchemistPos.x) < pathHalfWidth) continue;

                    // 3) Сама точка спавна — пятачок без деревьев чтобы игрок видел вокруг себя.
                    if (Vector3.Distance(pos, alchemistPos) < spawnClearR) continue;

                    // 4) Северный коридор — оттуда приходят клиенты.
                    float customerCorridorZ = ClearingCenter.z + ClearingR;
                    if (pos.z > customerCorridorZ - clearingPad && Mathf.Abs(pos.x) < 2.8f) continue;

                    // Плотность: ближе к карте = чаще, дальние углы — реже.
                    float distFromCenter = pos.magnitude;
                    float skipChance = distFromCenter > 16f ? 0.6f : 0.15f;
                    if (Random.value < skipChance) continue;

                    SpawnTree(big[Random.Range(0, big.Length)], pos,
                              yaw:   Random.Range(0f, 360f),
                              scale: Random.Range(2.0f, 3.0f));
                }
            }
        }

        /// <summary>
        /// Каменная тропинка из плиток из леса (спавн игрока) к паду хижины.
        /// </summary>
        private void CreatePath()
        {
            float startZ = alchemistPos.z + 1.5f; // немного впереди игрока
            float endZ   = HutPos.z       - 1.0f; // чуть не доходя до пада
            int   steps  = Mathf.Max(2, Mathf.RoundToInt((endZ - startZ) / 1.4f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float z = Mathf.Lerp(startZ, endZ, t);
                float x = Mathf.Sin(t * Mathf.PI * 0.7f) * 0.55f; // лёгкая змейка
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "PathTile";
                tile.transform.SetParent(transform, false);
                tile.transform.position   = new Vector3(x, 0.025f, z);
                tile.transform.localScale = new Vector3(1.05f, 0.05f, 0.7f);
                tile.transform.rotation   = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);
                ApplyColor(tile, new Color(0.42f, 0.36f, 0.28f));
                var col = tile.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                var mod = tile.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                mod.ignoreFromBuild = true;
            }
        }

        private void SpawnTree(string modelName, Vector3 pos, float yaw, float scale)
        {
            var go = ModelLoader.TryInstantiateNature(modelName, transform);
            if (go == null) return;
            go.name = modelName;
            go.transform.position   = pos;
            go.transform.rotation   = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            ModelLoader.StripColliders(go);

            // Деревья на NavMesh не запекаем — просто декор.
            var mod = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.ignoreFromBuild = true;
        }
       
                        private void CreateUI()
        {
            var canvasGo = new GameObject("UICanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode   = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                        // Золото — иконкой-каплей в правом верхнем.
            var goldText = CreateCurrencyHud(canvasGo.transform);
            goldText.text = "0";

            // Очередь — слева сверху.
            var queueText = CreateHudLabel(
                canvasGo.transform, "QueueText",
                anchor: new Vector2(0f, 1f),
                anchoredPos: new Vector2(40f, -40f),
                align: TMPro.TextAlignmentOptions.TopLeft);
            queueText.text = "Очередь: 0";

            var upgrades = new System.Collections.Generic.List<Alchemy.UI.UIController.UpgradeButtonBinding>();

            CreateOfflinePopup(canvasGo.transform,
                out var offlinePanel, out var offlineLabel,
                out var watchAdBtn, out var watchAdLabel,
                out var skipBtn);

            var ui = canvasGo.AddComponent<Alchemy.UI.UIController>();
            ui.Setup(goldText, queueText, upgrades,
                     offlinePanel, offlineLabel, watchAdBtn, watchAdLabel, skipBtn);

            CreateVirtualJoystick(canvasGo.transform);

            EnsureEventSystem();
        }
                private TMPro.TextMeshProUGUI CreateCurrencyHud(Transform parent)
        {
            var go = new GameObject("CurrencyHud");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -40f);
            rt.sizeDelta        = new Vector2(260f, 90f);

            // Икона-капля слева.
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin        = new Vector2(0f, 0.5f);
            iconRt.anchorMax        = new Vector2(0f, 0.5f);
            iconRt.pivot            = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, 0f);
            iconRt.sizeDelta        = new Vector2(70f, 80f);
            var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
            iconImg.sprite         = Alchemy.Gameplay.UpgradePad.GetDropletSprite();
            iconImg.color          = new Color(0.7f, 0.4f, 0.95f);
            iconImg.preserveAspect = true;

            // Число справа.
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.offsetMin = new Vector2(85f, 0f);
            txtRt.offsetMax = new Vector2(-5f, 0f);
            var txt = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.alignment        = TMPro.TextAlignmentOptions.MidlineLeft;
            txt.fontStyle        = TMPro.FontStyles.Bold;
            txt.color            = Color.white;
            txt.enableAutoSizing = true;
            txt.fontSizeMin      = 30f;
            txt.fontSizeMax      = 80f;
            txt.outlineColor     = new Color32(0, 0, 0, 200);
            txt.outlineWidth     = 0.18f;
            return txt;
        }
        private TMPro.TextMeshProUGUI CreateHudLabel(Transform parent, string name,
            Vector2 anchor, Vector2 anchoredPos, TMPro.TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.pivot            = anchor;
            rt.sizeDelta        = new Vector2(500f, 100f);
            rt.anchoredPosition = anchoredPos;

            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            text.fontSize  = 48;
            text.color     = Color.white;
            text.alignment = align;
            return text;
        } 
    
                private UnityEngine.UI.Button CreateUpgradeButton(Transform parent, string name,
            float yOffset, out TMPro.TextMeshProUGUI label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(720f, 180f);
            rt.anchoredPosition = new Vector2(0f, yOffset);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);

            var button = go.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            colors.normalColor   = Color.white;
            button.colors = colors;
            button.targetGraphic = image;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20, 10);
            textRt.offsetMax = new Vector2(-20, -10);

            label = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            label.fontSize  = 42;
            label.color     = Color.white;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.text      = "—";

            return button;
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;

            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    
                private GameObject CreateApprentice()
        {
            // Подмастерье — Плут в капюшоне (RogueHooded), визуально отделён от мага-игрока и от клиентов.
            var go = ModelLoader.TryInstantiateCharacter("RogueHooded", transform);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                ApplyColor(go, new Color(0.4f, 0.7f, 1f));
            }
            go.name = "Apprentice";
            go.transform.position = new Vector3(2f, 0f, -2f);
            ModelLoader.StripColliders(go);

            var mod = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.ignoreFromBuild = true;

            return go;
        }
    
        private void CreateWelcomePopup(Transform parent,
            out GameObject panel, out TMPro.TextMeshProUGUI label, out UnityEngine.UI.Button closeBtn)
        {
            panel = new GameObject("WelcomePanel");
            panel.transform.SetParent(parent, false);

            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = true;

            // Карточка по центру
            var card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(800f, 600f);

            var cardImg = card.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.15f, 0.1f, 0.25f, 0.95f);

            // Лейбл сверху
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(card.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.4f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(40, 0);
            labelRt.offsetMax = new Vector2(-40, -40);

            label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            label.fontSize  = 56;
            label.color     = Color.white;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.text      = "С возвращением!";

            // Кнопка «Забрать» снизу
            var btnGo = new GameObject("CloseButton");
            btnGo.transform.SetParent(card.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot     = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(400f, 140f);
            btnRt.anchoredPosition = new Vector2(0f, 50f);

            var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.3f, 0.5f, 0.3f, 1f);

            closeBtn = btnGo.AddComponent<UnityEngine.UI.Button>();
            closeBtn.targetGraphic = btnImg;

            var btnLabelGo = new GameObject("Label");
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnLabelRt = btnLabelGo.AddComponent<RectTransform>();
            btnLabelRt.anchorMin = Vector2.zero;
            btnLabelRt.anchorMax = Vector2.one;
            btnLabelRt.offsetMin = Vector2.zero;
            btnLabelRt.offsetMax = Vector2.zero;
            var btnLabel = btnLabelGo.AddComponent<TMPro.TextMeshProUGUI>();
            btnLabel.fontSize  = 48;
            btnLabel.color     = Color.white;
            btnLabel.alignment = TMPro.TextAlignmentOptions.Center;
            btnLabel.text      = "Забрать";

            panel.SetActive(false); // по умолчанию скрыт
        }
    
        private void CreateOfflinePopup(Transform parent,
            out GameObject panel,
            out TMPro.TextMeshProUGUI title,
            out UnityEngine.UI.Button watchAdBtn,
            out TMPro.TextMeshProUGUI watchAdLabel,
            out UnityEngine.UI.Button skipBtn)
        {
            panel = new GameObject("OfflinePanel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(900f, 800f);
            var cardImg = card.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.15f, 0.1f, 0.25f, 0.97f);

            title = CreatePanelLabel(card.transform, "Title",
                new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                new Vector2(40, 0), new Vector2(-40, -40),
                fontSize: 56,
                text: "Пока тебя не было…");

            watchAdBtn = CreatePanelButton(card.transform, "WatchAdButton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(700f, 180f),
                new Vector2(0f, 320f),
                bgColor: new Color(0.25f, 0.55f, 0.3f, 1f),
                out watchAdLabel,
                labelText: "Смотреть рекламу",
                fontSize: 50);

            skipBtn = CreatePanelButton(card.transform, "SkipButton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(700f, 140f),
                new Vector2(0f, 100f),
                bgColor: new Color(0.4f, 0.3f, 0.3f, 1f),
                out var skipLabel,
                labelText: "Пропустить",
                fontSize: 42);

            panel.SetActive(false);
        }

        private TMPro.TextMeshProUGUI CreatePanelLabel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            float fontSize, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var label = go.AddComponent<TMPro.TextMeshProUGUI>();
            label.fontSize  = fontSize;
            label.color     = Color.white;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.text      = text;
            return label;
        }

        private UnityEngine.UI.Button CreatePanelButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 size, Vector2 anchoredPos,
            Color bgColor,
            out TMPro.TextMeshProUGUI label,
            string labelText, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = bgColor;

            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(20, 10);
            labelRt.offsetMax = new Vector2(-20, -10);

            label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            label.fontSize  = fontSize;
            label.color     = Color.white;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.text      = labelText;

            return btn;
        }
    
                private void CreateVirtualJoystick(Transform parent)
        {
            // Фон джойстика (мягкий круг).
            var bgGo = new GameObject("Joystick_Background");
            bgGo.transform.SetParent(parent, false);

            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin        = new Vector2(0f, 0f);
            bgRt.anchorMax        = new Vector2(0f, 0f);
            bgRt.pivot            = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta        = new Vector2(280f, 280f);
            bgRt.anchoredPosition = new Vector2(220f, 220f);

            var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.sprite        = CreateCircleSprite(256, softness: 0.08f);
            bgImg.color         = new Color(0.85f, 0.85f, 1f, 0.18f);
            bgImg.raycastTarget = true;

            // Ручка (более мягкий круг внутри).
            var knobGo = new GameObject("Joystick_Knob");
            knobGo.transform.SetParent(bgGo.transform, false);

            var knobRt = knobGo.AddComponent<RectTransform>();
            knobRt.anchorMin        = new Vector2(0.5f, 0.5f);
            knobRt.anchorMax        = new Vector2(0.5f, 0.5f);
            knobRt.pivot            = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta        = new Vector2(120f, 120f);
            knobRt.anchoredPosition = Vector2.zero;

            var knobImg = knobGo.AddComponent<UnityEngine.UI.Image>();
            knobImg.sprite        = CreateCircleSprite(128, softness: 0.12f);
            knobImg.color         = new Color(0.85f, 0.8f, 0.95f, 0.55f);
            knobImg.raycastTarget = false;

            var joy = bgGo.AddComponent<Alchemy.UI.VirtualJoystick>();
            joy.Setup(bgRt, knobRt, radius: 100f);
        }
    
        private Sprite CreateCircleSprite(int size, float softness = 0.06f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = size * 0.5f;
            float radius = size * 0.5f;
            float edgeStart = radius * (1f - softness);
            float edgeEnd   = radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx   = x - center + 0.5f;
                    float dy   = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float a;
                    if (dist <= edgeStart)        a = 1f;
                    else if (dist >= edgeEnd)     a = 0f;
                    else
                    {
                        float t = (dist - edgeStart) / (edgeEnd - edgeStart);
                        a = 1f - Mathf.SmoothStep(0f, 1f, t);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    
        private void CreateQueueSeller()
        {
            var go = new GameObject("QueueSeller");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, 1.2f); // чуть южнее front slot z=2
            go.AddComponent<Alchemy.Gameplay.QueueSeller>();
        }
    }
}