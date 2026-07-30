using UnityEngine;

namespace MutationChess.Core
{
    public static class GameLogger
    {
        private const string Tag = "[MutationChess]";

        public static void Log(string message)
        {
            UnityEngine.Debug.Log($"{Tag} {message}");
        }

        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"{Tag} [Warning] {message}");
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError($"{Tag} [Error] {message}");
        }
    }
}
