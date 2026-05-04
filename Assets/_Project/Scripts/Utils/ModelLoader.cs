using UnityEngine;

namespace Alchemy.Utils
{
    /// <summary>
    /// Загружает 3D-модели из Resources/Models/* и инстанцирует их.
    /// Если модель не найдена — возвращает null, чтобы вызывающий код
    /// мог откатиться на капсулу/куб.
    /// Применяется как мягкая замена примитивов реальными префабами.
    /// </summary>
    public static class ModelLoader
    {
        public const string CharactersPath = "Models/Characters/";
        public const string PropsPath      = "Models/Props/";
        public const string FloorPath      = "Models/Floor/";

        /// <summary>
        /// Пытается загрузить и заинстанцировать модель из Resources/{path}{name}.
        /// Возвращает null, если ассета нет (например, glTFast ещё не подтянулся).
        /// </summary>
        public static GameObject TryInstantiate(string path, string name, Transform parent = null)
        {
            var prefab = Resources.Load<GameObject>(path + name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            return go;
        }

        public static GameObject TryInstantiateCharacter(string name, Transform parent = null)
            => TryInstantiate(CharactersPath, name, parent);

        public static GameObject TryInstantiateProp(string name, Transform parent = null)
            => TryInstantiate(PropsPath, name, parent);

        /// <summary>
        /// Перекрашивает все рендереры объекта в указанный цвет.
        /// Полезно для тонировки клиентских моделей под цвет рецепта.
        /// </summary>
        public static void Tint(GameObject go, Color color, float blend = 1f)
        {
            if (go == null) return;
            // Покрываем и обычные и скиннед-рендереры (KayKit-персонажи рижированы).
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var basis = (r.sharedMaterial != null) ? r.sharedMaterial.color : Color.white;
                mat.color = (blend >= 0.999f) ? color : Color.Lerp(basis, color, blend);
                r.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// Удаляет коллайдеры из импортированной модели — они нам не нужны
        /// (NavMeshAgent не использует физику для движения).
        /// </summary>
        public static void StripColliders(GameObject go)
        {
            if (go == null) return;
            var cols = go.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols) Object.Destroy(c);
        }
    }
}
