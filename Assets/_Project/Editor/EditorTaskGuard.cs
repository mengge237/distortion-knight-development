using UnityEditor;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 编辑器延迟任务守卫：delayCall 可能赶在进入 Play 模式或脚本重编译期间执行，
    /// 此时 AssetDatabase / EditorSceneManager 等编辑器 API 会抛异常
    /// （如 "This cannot be used during play mode"）。
    /// 统一等到安全时机（非 Play 模式且编译完成）再执行，等待期间不丢失任务。
    /// </summary>
    public static class EditorTaskGuard
    {
        /// <summary>域重载后投递任务；若当前不安全则挂到 update 轮询，退出 Play / 编译完成后自动执行。</summary>
        public static void RunWhenSafe(System.Action task)
        {
            EditorApplication.delayCall += () => ExecuteWhenSafe(task);
        }

        private static void ExecuteWhenSafe(System.Action task)
        {
            if (IsSafe())
            {
                task();
                return;
            }

            EditorApplication.CallbackFunction waiter = null;
            waiter = () =>
            {
                if (!IsSafe()) return;
                EditorApplication.update -= waiter;
                task();
            };
            EditorApplication.update += waiter;
        }

        private static bool IsSafe()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }
    }
}
