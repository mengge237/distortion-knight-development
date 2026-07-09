using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [System.Serializable]
    public class PlayerData
    {
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int gold = 200;

        // 添加属性以便外部访问
        public int Health => currentHealth;
        public int Gold => gold;

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        public void TakeDamage(int damage)
        {
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
    }
}