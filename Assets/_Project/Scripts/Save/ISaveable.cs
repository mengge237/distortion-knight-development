namespace MutationChess.Core
{
    /// <summary>
    /// 存档接口：任何需要持久化的系统实现本接口并向 SaveService.Register 注册。
    /// SerializeState/DeserializeState 内部使用 JsonUtility 序列化各自的 DTO。
    /// </summary>
    public interface ISaveable
    {
        /// <summary>存档条目唯一键（SaveService 收集/回填时按此匹配）。</summary>
        string SaveKey { get; }

        /// <summary>序列化当前状态为 JSON 字符串。</summary>
        string SerializeState();

        /// <summary>从 JSON 字符串恢复状态。</summary>
        void DeserializeState(string json);
    }
}
