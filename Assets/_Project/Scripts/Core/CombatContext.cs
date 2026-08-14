using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    public class CombatContext
    {
        public BattleManager battleManager;
        public Enemy targetEnemy;
        public PlayerData targetPlayer;
        public Card sourceCard;

        public CombatContext(BattleManager battleManager, Enemy targetEnemy, PlayerData targetPlayer, Card sourceCard)
        {
            this.battleManager = battleManager;
            this.targetEnemy = targetEnemy;
            this.targetPlayer = targetPlayer;
            this.sourceCard = sourceCard;
        }
    }
}