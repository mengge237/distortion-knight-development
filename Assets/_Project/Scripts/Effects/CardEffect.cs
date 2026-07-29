using UnityEngine;

namespace MutationChess.Core
{
    public abstract class CardEffect : ScriptableObject
    {
        [TextArea(2, 4)]
        public string effectDescription;

        public abstract void Execute(CombatContext context);

        public virtual void Execute(EffectContext context)
        {
            if (context == null)
            {
                GameLogger.LogError($"[CardEffect] {name} EffectContext  null");
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
                $"[CardEffect] {name} EffectContext.combat  null  battleManager  null" +
                $" CombatContext"
            );
        }
    }
}