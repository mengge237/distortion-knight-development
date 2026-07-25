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

        private List<Buff> buffs = new List<Buff>();

        public int Health => currentHealth;
        public int Gold => gold;

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
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
            }
            else
            {
                buffs.Add(buff);
            }
        }

        public int GetBuffAmount(BuffType type)
        {
            var buff = buffs.Find(b => b.type == type);
            return buff != null ? buff.amount : 0;
        }

        public bool HasBuff(BuffType type)
        {
            return buffs.Any(b => b.type == type && !b.IsExpired());
        }

        public void ReduceBuffDurations()
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
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

        public void OnTurnStart()
        {
            ReduceBuffDurations();
        }
    }
}
