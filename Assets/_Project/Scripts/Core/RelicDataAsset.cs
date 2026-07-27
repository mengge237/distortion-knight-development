using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "RelicDataAsset", menuName = "MutationChess/Relic Data Asset")]
    public class RelicDataAsset : ScriptableObject
    {
        [Header("基本信息")]
        public string relicId;
        public string relicName;
        public RelicRarity rarity;
        public CardFaction faction;

        [Header("效果")]
        public RelicEffectType effectType = RelicEffectType.None;
        public float effectValue;  // 效果数值：BonusDamage=额外伤害值, VictoryGoldPercent=百分比(0.01=1%), OncePerBattleAttackBoost=攻击力加成

        [Header("描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("售价")]
        public int price = 100;

        [Header("图标路径")]
        public string iconPath;
    }
}
