#if UNITY_EDITOR
using System.IO;
using StickEvolve.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StickEvolve.EditorTools
{
    /// <summary>
    /// Меню Unity: один клик создаёт прототип-сцену с бутстраппером.
    /// </summary>
    public static class StickEvolveMenu
    {
        private const string ScenePath = "Assets/_StickEvolve/Scenes/Prototype.unity";

        [MenuItem("StickEvolve/Create Prototype Scene", priority = 1)]
        public static void CreatePrototypeScene()
        {
            // Создать папку Scenes если нет
            Directory.CreateDirectory("Assets/_StickEvolve/Scenes");

            // Спросить — заменить существующую?
            if (File.Exists(ScenePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Перезаписать?",
                    $"Сцена уже есть: {ScenePath}\nПерезаписать?",
                    "Перезаписать", "Отмена");
                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Prototype";

            var bootstrapGO = new GameObject("_StickEvolveBootstrap");
            bootstrapGO.AddComponent<PrototypeBootstrapper>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[StickEvolve] Создана сцена: {ScenePath}. Нажми Play.");
            EditorUtility.DisplayDialog("Готово",
                $"Сцена создана: {ScenePath}\n\nТеперь:\n1. Открой её (Project window → двойной клик)\n2. Нажми Play.\n\nИграется чистый прототип Stickman Evolve.",
                "OK");
        }

        [MenuItem("StickEvolve/Add Bootstrap To Current Scene", priority = 2)]
        public static void AddToCurrentScene()
        {
            var existing = Object.FindFirstObjectByType<PrototypeBootstrapper>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Уже добавлен",
                    "PrototypeBootstrapper уже есть в текущей сцене.", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject("_StickEvolveBootstrap");
            go.AddComponent<PrototypeBootstrapper>();
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[StickEvolve] PrototypeBootstrapper добавлен в текущую сцену.");
        }

        [MenuItem("StickEvolve/Delete Save File", priority = 20)]
        public static void DeleteSave()
        {
            var path = Path.Combine(Application.persistentDataPath, "stickevolve_save.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[StickEvolve] Удалён сейв: {path}");
            }
            else
            {
                Debug.Log("[StickEvolve] Сейв-файла нет.");
            }
        }
    }
}
#endif
