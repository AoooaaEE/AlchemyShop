using UnityEngine;
using Alchemy.Gameplay;

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
        {
            var go = TryInstantiate(CharactersPath, name, parent);
            if (go != null) AttachAnimator(go, CharactersPath + name);
            return go;
        }

        /// <summary>
        /// Цепляет CharacterAnimator на корневой GameObject и подгружает Idle/Running_A
        /// из AnimationClip'ов, что glTFast импортировал внутри .glb.
        /// </summary>
        private static void AttachAnimator(GameObject go, string resourcePath)
        {
            // Animator уже создан glTFast'ом на корне модели.
            var animator = go.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[ModelLoader] Animator не найден у {go.name}. Анимаций не будет.");
                return;
            }
            animator.applyRootMotion = false;

            // AnimationClip'ы — sub-asset'ы импортированного glb.
            var clips = Resources.LoadAll<AnimationClip>(resourcePath);
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"[ModelLoader] {resourcePath}: не нашёл AnimationClip'ов через Resources.LoadAll. " +
                                 $"Avatar={(animator.avatar != null ? animator.avatar.name : \"null\")}");
                return;
            }
            Debug.Log($"[ModelLoader] {resourcePath}: загружено {clips.Length} клипов. " +
                      $"Avatar={(animator.avatar != null ? animator.avatar.name : \"null\")}, " +
                      $"isHuman={(animator.avatar != null && animator.avatar.isHuman)}");

            AnimationClip idle = null, walk = null;
            foreach (var c in clips)
            {
                if (c == null) continue;
                if (idle == null && c.name == "Idle")       idle = c;
                if (walk == null && c.name == "Running_A")  walk = c;
            }
            // Запасной поиск, если имена изменили.
            if (idle == null) foreach (var c in clips)
                if (c != null && c.name.ToLower().Contains("idle")) { idle = c; break; }
            if (walk == null) foreach (var c in clips)
                if (c != null && (c.name.ToLower().Contains("run") || c.name.ToLower().Contains("walk")))
                { walk = c; break; }

            Debug.Log($"[ModelLoader] {resourcePath}: idle={(idle != null ? idle.name : \"NONE\")}, " +
                      $"walk={(walk != null ? walk.name : \"NONE\")}");
            if (idle == null && walk == null) return;

            var ca = animator.gameObject.GetComponent<CharacterAnimator>();
            if (ca == null) ca = animator.gameObject.AddComponent<CharacterAnimator>();
            ca.Init(idle, walk);
        }

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
