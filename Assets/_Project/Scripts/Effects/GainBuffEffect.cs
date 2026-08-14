using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 通用属性增减效果：给玩家或敌人施加任意 BuffType 的层数（正=获得，负=失去）。
    /// 由 EffectMergeMigration 工具从以下 11 个同构效果类合并而来：
    /// GainStrengthEffect / GainStrengthBattleStartEffect / GainStrength2BattleStartEffect /
    /// GainDexterityBattleStartEffect / ApplyDexterityEffect / ApplyTemporaryStrengthEffect /
    /// TempDexterity3TurnsEffect / ReduceStrengthEffect / LoseStrengthNextTurnEffect /
    /// LoseDexterityNextTurnEffect / ApplyShadowStrengthEffect
    /// （触发时机由遗物配置/卡牌决定，与效果类本身无关）
    /// </summary>
    [CreateAssetMenu(fileName = "GainBuffEffect", menuName = "MutationChess/Effects/Gain Buff")]
    public class GainBuffEffect : CardEffect
    {
        public enum TargetMode
        {
            Auto = 0,   // 优先玩家，玩家为空时施加给敌人
            Player = 1, // 仅玩家
            Enemy = 2   // 仅敌人
        }

        [Header("Buff 配置")]
        [Tooltip("施加的 Buff 类型")]
        public BuffType buffType = BuffType.Strength;

        [Tooltip("层数（负数=失去）")]
        public int amount = 1;

        [Tooltip("持续回合数（-1=永久）")]
        public int duration = -1;

        [Tooltip("卡牌 magicNumber>0 时覆盖 amount")]
        public bool useMagicNumber = false;

        [Tooltip("卡牌 magicNumber>0 时覆盖 duration")]
        public bool magicNumberAsDuration = false;

        [Tooltip("取反：施加 amount 的相反数（用于失去力量/敏捷类效果，magicNumber 覆盖后仍生效）")]
        public bool invert = false;

        [Tooltip("标记为暗影力量（可被暗影爆发触发）")]
        public bool isShadow = false;

        [Tooltip("施加目标")]
        public TargetMode targetMode = TargetMode.Auto;

        private static string BuffName(BuffType type)
        {
            switch (type)
            {
                case BuffType.Strength: return "力量";
                case BuffType.Dexterity: return "敏捷";
                case BuffType.Shield: return "格挡";
                case BuffType.Poison: return "中毒";
                case BuffType.Vulnerability: return "易伤";
                case BuffType.Weak: return "虚弱";
                case BuffType.Frail: return "脆弱";
                case BuffType.Thorns: return "荆棘";
                default: return type.ToString();
            }
        }

        public override string GetDescription(Card card)
        {
            int amt = amount;
            int dur = duration;
            if (card != null && card.magicNumber > 0)
            {
                if (useMagicNumber) amt = card.magicNumber;
                if (magicNumberAsDuration) dur = card.magicNumber;
            }
            if (invert) amt = -amt;
            string durText = dur < 0 ? "永久" : $"{dur} 回合";
            string shadow = isShadow ? "暗影" : "";
            return amt >= 0
                ? $"获得 {amt} 点{shadow}{BuffName(buffType)}（{durText}）"
                : $"失去 {-amt} 点{shadow}{BuffName(buffType)}（{durText}）";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amt = amount;
            int dur = duration;

            Card src = context.sourceCard;
            if (src != null && src.magicNumber > 0)
            {
                if (useMagicNumber) amt = src.magicNumber;
                if (magicNumberAsDuration) dur = src.magicNumber;
            }
            if (invert) amt = -amt;

            var buff = new Buff { type = buffType, amount = amt, duration = dur, isShadow = isShadow };

            bool applied = false;
            switch (targetMode)
            {
                case TargetMode.Player:
                    applied = context.targetPlayer != null;
                    if (applied) context.targetPlayer.AddBuff(buff);
                    break;
                case TargetMode.Enemy:
                    applied = context.targetEnemy != null;
                    if (applied) context.targetEnemy.AddBuff(buff);
                    break;
                default:
                    if (context.targetPlayer != null)
                    {
                        context.targetPlayer.AddBuff(buff);
                        applied = true;
                    }
                    else if (context.targetEnemy != null)
                    {
                        context.targetEnemy.AddBuff(buff);
                        applied = true;
                    }
                    break;
            }

            if (applied)
            {
                string durText = dur < 0 ? "永久" : $"{dur} 回合";
                string verb = amt >= 0 ? "获得" : "失去";
                int shown = amt >= 0 ? amt : -amt;
                context.battleManager?.AddLog($"{verb} {shown} 点{BuffName(buffType)}（{durText}）");
                GameLogger.Log($"[GainBuff] {BuffName(buffType)} {amt:+0;-0}，持续 {durText}");
            }
            else
            {
                GameLogger.LogWarning($"[GainBuff] 目标为空（buffType={buffType}），未施加");
            }
        }
    }
}
