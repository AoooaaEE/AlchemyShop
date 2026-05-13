using System.IO;
using UnityEngine;

namespace StickEvolve.Data
{
    /// <summary>
    /// Сейв прототипа Stickman Evolve.
    /// Отдельный файл (stickevolve_save.json) — не конфликтует с AlchemyShop save.json.
    /// </summary>
    public static class StickSaveSystem
    {
        private const string FileName = "stickevolve_save.json";
        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(FilePath);

        public static void Save(StickSaveData data)
        {
            try
            {
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[StickSaveSystem] Сохранено: {FilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StickSaveSystem] Ошибка сохранения: {e.Message}");
            }
        }

        public static StickSaveData Load()
        {
            if (!Exists()) return new StickSaveData();
            try
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<StickSaveData>(json);
                return data ?? new StickSaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StickSaveSystem] Ошибка загрузки: {e.Message}");
                return new StickSaveData();
            }
        }

        public static void Delete()
        {
            if (!Exists()) return;
            File.Delete(FilePath);
            Debug.Log("[StickSaveSystem] Сейв удалён.");
        }
    }
}
