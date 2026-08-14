using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒效果抽象基类。
    /// 
    /// 设计说明：
    /// - 诅咒效果**不通过标准的 Execute(CombatContext) 管线生效**。
    /// - 实际逻辑由 HandManager 的专用方法（TriggerCurseDecayEffects 等）按类型匹配具体子类并读取字段值触发。
    /// - 所有诅咒卡继承此类，仅需声明自己的配置字段即可，无需重写 Execute。
    /// </summary>
    public abstract class CurseEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            // 诅咒效果由 HandManager 专用流程读取字段值，不走标准 Execute 管线
        }
    }
}
