using UnityEngine;

namespace MutationChess.Core
{
    public static class GameLogger
    {
        private const string Tag = "[MutationChess]";

        public static void Log(string message)
        {
            Debug.Log($"{Tag} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Tag} [Warning] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{Tag} [Error] {message}");
        }
    }
}
