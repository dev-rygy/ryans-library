using System.IO;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary
{
    public static class ToolsMenu
    {
        [MenuItem("Tools/Setup/Create Default Folders")]
        public static void CreateDefaultFolders()
        {
            MkDir("Assets", "Art", "Input System", "Physic Materials", "Prefabs", "Scenes", "ScriptableObjects", "Scripts",
                "Tests", "URP");
            AssetDatabase.Refresh();
        }

        public static void MkDir(string root, params string[] dir)
        {
            var fullpath = Path.Combine(Application.dataPath, root);
            foreach (var newDirectory in dir)
            {
                Directory.CreateDirectory(Path.Combine(fullpath, newDirectory));
            }
        }
    }
}
