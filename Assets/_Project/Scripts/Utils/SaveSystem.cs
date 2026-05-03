using System.IO;
using Alchemy.Data;
using UnityEngine;

namespace Alchemy.Utils
{
    /// <summary>
    /// Чтение/запись сейва в Application.persistentDataPath/save.json.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(FilePath);

        public static void Save(SaveData data)
        {
            try
            {
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[SaveSystem] Сохранено: {FilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Ошибка сохранения: {e.Message}");
            }
        }

        public static SaveData Load()
        {
            if (!Exists()) return new SaveData();

            try
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Ошибка загрузки: {e.Message}");
                return new SaveData();
            }
        }

        public static void Delete()
        {
            if (!Exists()) return;
            File.Delete(FilePath);
            Debug.Log("[SaveSystem] Сейв удалён.");
        }
    }
}