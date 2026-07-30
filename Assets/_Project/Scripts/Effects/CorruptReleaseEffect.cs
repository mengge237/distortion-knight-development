using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "CorruptReleaseEffect", menuName = "MutationChess/Potion Effects/Corrupt Release")]
    public class CorruptReleaseEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            ConversionModifier.AllCardsNoExhaustThisTurn = true;
            GameLogger.Log("[CorruptRelease] ��һ�ø����ͷţ����غ����п��Ʋ��ᱻ����");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog("��һ�ø����ͷţ����غ����п��Ʋ��ᱻ����");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.AllCardsNoExhaustThisTurn = true;
            GameLogger.Log("[CorruptRelease] ��һ�ø����ͷţ����غ����п��Ʋ��ᱻ����");
        }
    }
}


