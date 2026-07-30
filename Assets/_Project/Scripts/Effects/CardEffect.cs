using UnityEngine;

namespace MutationChess.Core
{
    public abstract class CardEffect : ScriptableObject
    {
        [TextArea(2, 4)]
        public string effectDescription;

        public abstract void Execute(CombatContext context);

        /// <summary>
        /// 根据关联卡牌动态生成效果描述（带数值）。默认返回静态 effectDescription。
        /// 子类可以 override 以输出带具体数值的中文描述。
        /// </summary>
        public virtual string GetDescription(Card card)
        {
            return effectDescription ?? "";
        }

        public virtual void Execute(EffectContext context)
        {
            if (context == null)
            {
                GameLogger.LogError($"[CardEffect] {name} EffectContext 为 null");
                return;
            }

            if (context.combat != null)
            {
                Execute(context.combat);
                return;
            }


            var bm = context.battleManager;
            if (bm != null)
            {
                var dataMgr = PlayerDataManager.Instance;
                PlayerData playerData = dataMgr != null ? dataMgr.GetPlayerData() : null;

                CombatContext fallback = new CombatContext(
                    bm,
                    bm.GetCurrentEnemy(),
                    playerData,
                    context.tag as Card
                );
                Execute(fallback);
                return;
            }


            GameLogger.LogError(
                $"[CardEffect] {name} EffectContext.combat 为 null 且 battleManager 为 null，无法创建 CombatContext"
            );
        }
    }
}
