using MutationChess.Battle;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 战斗上下文 - 用于卡牌效果执行时传递参数
    /// </summary>
    public class CombatContext
    {
        public BattleManager battleManager;  // 战斗管理器
        public Enemy targetEnemy;            // 目标敌人
        public PlayerData targetPlayer;      // 目标玩家
        public Card sourceCard;              // 触发效果的卡牌

        public CombatContext(BattleManager battleManager, Enemy targetEnemy, PlayerData targetPlayer, Card sourceCard)
        {
            this.battleManager = battleManager;
            this.targetEnemy = targetEnemy;
            this.targetPlayer = targetPlayer;
            this.sourceCard = sourceCard;

        }
    }
}