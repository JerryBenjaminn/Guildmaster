using System.IO;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// JSON persistence to the three save files (CLAUDE.md §3.5). Folded out of a
    /// dedicated manager (the §3.4 manager set is fixed at 7); GameManager is the
    /// save orchestrator and calls into this static helper. Writes to
    /// Application.persistentDataPath, which survives app kill and reinstall on
    /// Android. Local save only; cloud is deferred.
    /// </summary>
    public static class SaveSystem
    {
        public const string PersistentFile = "persistent.json";
        public const string LegacyFile = "legacy.json";
        public const string CurrentFile = "current.json";

        private static string PathFor(string fileName) =>
            Path.Combine(Application.persistentDataPath, fileName);

        public static void Save<T>(string fileName, T data)
        {
            string json = JsonUtility.ToJson(data, true);
            string path = PathFor(fileName);
            // Write to a temp file first, then move, so a crash mid-write can't
            // corrupt an existing good save.
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static T Load<T>(string fileName) where T : new()
        {
            string path = PathFor(fileName);
            if (!File.Exists(path)) return new T();

            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json)) return new T();

            T data = JsonUtility.FromJson<T>(json);
            return data != null ? data : new T();
        }

        public static bool Exists(string fileName) => File.Exists(PathFor(fileName));

        public static void Delete(string fileName)
        {
            string path = PathFor(fileName);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
