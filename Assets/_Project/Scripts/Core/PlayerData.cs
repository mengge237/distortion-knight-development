using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [System.Serializable]
    public class PlayerData
    {
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int gold = 200;

        [Tooltip("最大药水携带数量")]
        public int maxPotions = 3;

        private List<Buff> buffs = new List<Buff>();
        private List<Potion> potions = new List<Potion>();

        public int Health => currentHealth;
        public int Gold => gold;
        public List<Potion> Potions => potions;
        public int PotionCount => potions.Count;

        /// <summary>
        /// 从 GameConfig 获取默认值（在 PlayerDataManager.Awake 中调用）。
        /// 仅当字段仍为默认值时才覆盖，避免覆盖 Inspector 设置的值。
        /// </summary>
        public void InitFromConfig()
        {
            var config = GameConfig.Instance;
            if (config == null) return;

            // 只在仍使用默认值时才覆盖，避免覆盖 Inspector 设置的值
            if (maxHealth == 100) maxHealth = config.maxHealth;
            if (currentHealth == 100) currentHealth = config.maxHealth;
            if (maxPotions == 3) maxPotions = config.maxPotions;
            if (gold == 200) gold = config.startingGold;
        }

        public int Heal(int amount)
        {
            int previous = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            return currentHealth - previous;
        }

        public void TakeDamage(int damage)
        {
            int vulnerability = GetBuffAmount(BuffType.Vulnerability);
            if (vulnerability > 0)
            {
                damage = Mathf.RoundToInt(damage * (1 + vulnerability * 0.2f));
            }
            currentHealth = Mathf.Max(0, currentHealth - damage);
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }

        public bool RemoveGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                return true;
            }
            return false;
        }

        public bool IsDead()
        {
            return currentHealth <= 0;
        }

        public float GetHealthPercentage()
        {
            return (float)currentHealth / maxHealth;
        }

        public void AddBuff(Buff buff)
        {
            if (buff == null) return;

            var existing = buffs.Find(b => b.type == buff.type);
            if (existing != null)
            {
                existing.amount += buff.amount;
                existing.duration = Mathf.Max(existing.duration, buff.duration);
                // 合并时暗影属性也一并覆盖，只要有一个为暗影则合并后为暗影
                if (buff.isShadow) existing.isShadow = true;
            }
            else
            {
                buffs.Add(buff);
            }
        }

        /// <summary>
        /// 移除所有 isShadow=true 的力量 buff（暗影回合结束时调用，用于清除暗影临时获得的力量）。
        /// 返回被移除的力量值。
        /// </summary>
        public int RemoveShadowStrengthBuffs()
        {
            int removedAmount = 0;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].type == BuffType.Strength && buffs[i].isShadow)
                {
                    removedAmount += buffs[i].amount;
                    buffs.RemoveAt(i);
                }
            }
            return removedAmount;
        }

        public int GetBuffAmount(BuffType type)
        {
            var buff = buffs.Find(b => b.type == type);
            return buff != null ? buff.amount : 0;
        }

        public void ReduceBuffDurations()
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                // 暗影回合且开启 ShadowStrengthNoDecay 时，暗影力量不衰减
                if (ConversionModifier.ShadowStrengthNoDecay && buffs[i].isShadow)
                {
                    continue;
                }
                buffs[i].ReduceDuration();
                if (buffs[i].IsExpired())
                {
                    buffs.RemoveAt(i);
                }
            }
        }

        public List<Buff> GetBuffs()
        {
            return new List<Buff>(buffs);
        }

        public int GetModifiedBlock(int baseBlock)
        {
            int frail = GetBuffAmount(BuffType.Frail);
            if (frail > 0)
            {
                return Mathf.RoundToInt(baseBlock * (1 - frail * 0.2f));
            }
            return baseBlock;
        }

        public int GetModifiedDamage(int baseDamage)
        {
            int weak = GetBuffAmount(BuffType.Weak);
            if (weak > 0)
            {
                return Mathf.RoundToInt(baseDamage * (1 - weak * 0.2f));
            }
            return baseDamage;
        }

        public void ClearBuffs()
        {
            buffs.Clear();
        }

        /// <summary>
        /// 移除指定类型的所有buff（包括正面效果也一并移除）。
        /// </summary>
        public int RemoveBuffsByType(BuffType type)
        {
            int removed = 0;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].type == type)
                {
                    buffs.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// 移除指定类型的 debuff（仅移除 amount<0 的 debuff 记录）。
        /// </summary>
        public int RemoveDebuffsByType(BuffType type)
        {
            int removed = 0;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].type == type && buffs[i].amount < 0)
                {
                    buffs.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        public void OnTurnStart()
        {
            ReduceBuffDurations();
        }

        public bool AddPotion(Potion potion)
        {
            if (potion == null) return false;
            if (potions.Count >= maxPotions)
            {
                GameLogger.LogWarning($"[PlayerData] 药水已满 ({maxPotions})，无法添加 {potion.potionName}");
                return false;
            }
            potions.Add(potion);
            return true;
        }

        public bool RemovePotion(string potionId)
        {
            var potion = potions.FirstOrDefault(p => p.potionId == potionId);
            if (potion != null)
            {
                potions.Remove(potion);
                return true;
            }
            return false;
        }

        public List<Potion> GetPotions()
        {
            return new List<Potion>(potions);
        }

        public void ClearPotions()
        {
            potions.Clear();
        }
    }
}
