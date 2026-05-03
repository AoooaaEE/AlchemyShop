#if UNITY_EDITOR
using Alchemy.Utils;
using UnityEditor;
using UnityEngine;

namespace Alchemy.EditorTools
{
    public static class SaveDebugMenu
    {
        [MenuItem("Tools/Alchemy/Clear Save")]
        public static void ClearSave()
        {
            SaveSystem.Delete();
            Debug.Log("[SaveDebugMenu] Сейв очищен.");
        }

        [MenuItem("Tools/Alchemy/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath + "/save.json");
        }
    }
}
#endif