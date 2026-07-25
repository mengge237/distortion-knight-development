using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MutationChess.Battle
{
    public class Enemy
    {
        public string enemyName;
        public int maxHealth;
        public int currentHealth;
        public int baseAttackDamage;
        public int currentAttackDamage;
        public EnemyType enemyType;
        public string description;

        public Sprite enemySprite;
        public GameObject modelInstance;
        public EnemyData data;

        private bool isSecondForm = false;
        private string firstFormSpriteName;
        private string secondFormSpriteName;

        private EnemyAIPattern aiPattern;
        private int currentActionIndex = 0;
        private int currentLoopCount = 0;
        private int turnCount = 0;
        private bool isPatternInitialized = false;

        private List<Buff> buffs = new List<Buff>();
        private List<DelayedDamage> delayedDamages = new List<DelayedDamage>();

        public Enemy(EnemyData data)
        {
            this.data = data;
            enemyName = data.enemyName;
            maxHealth = data.maxHealth;
            currentHealth = data.maxHealth;
            baseAttackDamage = data.attackDamage;
            currentAttackDamage = data.attackDamage;
            enemyType = data.enemyType;
            description = data.description;

            LoadSprite();
            InitializeAIPattern();
        }

        private void LoadSprite()
        {
            if (string.IsNullOrEmpty(data.spriteName))
            {
                data.spriteName = enemyName;
            }

            string folderPath = GetSpriteFolderPath();
            string fullPath = $"{folderPath}/{data.spriteName}";

            enemySprite = Resources.Load<Sprite>(fullPath);

            if (enemySprite == null)
            {
                enemySprite = Resources.Load<Sprite>($"EnemySprites/{data.spriteName}");
            }

            if (enemySprite == null)
            {
                Debug.LogWarning($"未找到敌人图片: {fullPath}");
            }
        }

        private string GetSpriteFolderPath()
        {
            switch (enemyType)
            {
                case EnemyType.Normal:
                    return "EnemySprites/Normal";
                case EnemyType.Elite:
                    return "EnemySprites/Elite";
                case EnemyType.Boss:
                    return "EnemySprites/Boss";
                default:
                    return "EnemySprites";
            }
        }

        public void SwitchToSecondForm()
        {
            if (isSecondForm) return;
            isSecondForm = true;

            if (!string.IsNullOrEmpty(secondFormSpriteName))
            {
                string folderPath = GetSpriteFolderPath();
                string fullPath = $"{folderPath}/{secondFormSpriteName}";
                Sprite newSprite = Resources.Load<Sprite>(fullPath);

                if (newSprite == null)
                {
                    newSprite = Resources.Load<Sprite>($"EnemySprites/{secondFormSpriteName}");
                }

                if (newSprite != null)
                {
                    enemySprite = newSprite;
                }
                else
                {
                    Debug.LogWarning($"未找到第二形态图片: {secondFormSpriteName}");
                }
            }

            currentAttackDamage += 5;
        }

        public int GetKingPowerBonus()
        {
            float hpPercent = GetHealthPercentage();

            if (hpPercent < 0.2f)
                return 10;
            else if (hpPercent < 0.35f)
                return 7;
            else if (hpPercent < 0.5f)
                return 5;
            else if (hpPercent < 0.7f)
                return 3;
            else
                return 0;
        }

        private void InitializeAIPattern()
        {
            if (!string.IsNullOrEmpty(data.aiPatternName))
            {
                aiPattern = EnemyAIManager.GetPattern(data.aiPatternName);
            }
            else
            {
                aiPattern = EnemyAIManager.GetPatternByEnemyName(enemyName);
            }

            isPatternInitialized = true;
            currentActionIndex = 0;
            currentLoopCount = 0;
            turnCount = 0;

        }

        public EnemyAction GetNextAction()
        {
            if (!isPatternInitialized)
                InitializeAIPattern();

            if (aiPattern == null || aiPattern.actions.Count == 0)
            {
                return new EnemyAction(EnemyIntentType.Wait, 0, 0);
            }

            foreach (var action in aiPattern.actions)
            {
                if (action.conditionCheck && CheckCondition(action))
                {
                    return action;
                }
            }

            if (currentActionIndex >= aiPattern.actions.Count)
            {
                if (aiPattern.loopAfterFinish)
                {
                    currentActionIndex = 0;
                    currentLoopCount++;

                    if (aiPattern.repeatCount > 0 && currentLoopCount >= aiPattern.repeatCount)
                    {
                        currentLoopCount = 0;
                        currentActionIndex = aiPattern.actions.Count - 1;
                    }
                }
                else
                {
                    currentActionIndex = aiPattern.actions.Count - 1;
                }
            }

            EnemyAction selectedAction = aiPattern.actions[currentActionIndex];
            currentActionIndex++;

            return selectedAction;
        }

        private bool CheckCondition(EnemyAction action)
        {
            switch (action.conditionType)
            {
                case ConditionType.EnemyHealthBelow:
                    return GetHealthPercentage() < (action.conditionThreshold / 100f);
                case ConditionType.EnemyHealthAbove:
                    return GetHealthPercentage() > (action.conditionThreshold / 100f);
                case ConditionType.EnemyHasBuff:
                    return HasBuff(BuffType.Poison) || HasBuff(BuffType.Vulnerability);
                case ConditionType.TurnCount:
                    return turnCount >= action.conditionThreshold && turnCount % action.conditionThreshold == 0;
                case ConditionType.Always:
                    return true;
                default:
                    return false;
            }
        }

        public void OnTurnStart()
        {
            turnCount++;
            ReduceBuffDurations();

            if (enemyName.Contains("深渊之主") && !isSecondForm && GetHealthPercentage() < 0.5f)
            {
                SwitchToSecondForm();
            }

            if (enemyName.Contains("腐化君王"))
            {
                int bonus = GetKingPowerBonus();
                if (bonus > 0)
                {
                }
            }

        }

        public void OnTurnEnd()
        {
            ExecuteDelayedDamages();
        }

        public int GetAttackDamage()
        {
            int strength = GetBuffAmount(BuffType.Strength) * 2;
            int vulnerability = GetBuffAmount(BuffType.Vulnerability);
            int weak = GetBuffAmount(BuffType.Weak);

            int damage = currentAttackDamage + strength;

            if (enemyName.Contains("腐化君王"))
            {
                damage += GetKingPowerBonus();
            }

            damage += UnityEngine.Random.Range(-3, 4);

            if (vulnerability > 0)
            {
                damage = Mathf.RoundToInt(damage * (1 + vulnerability * 0.2f));
            }

            if (weak > 0)
            {
                damage = Mathf.RoundToInt(damage * (1 - weak * 0.2f));
            }

            return Mathf.Max(1, damage);
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

        public void TakeDamage(int damage)
        {
            int vulnerability = GetBuffAmount(BuffType.Vulnerability);
            if (vulnerability > 0)
            {
                damage = Mathf.RoundToInt(damage * (1 + vulnerability * 0.2f));
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);

        }

        public bool IsDead() => currentHealth <= 0;
        public float GetHealthPercentage() => (float)currentHealth / maxHealth;

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

        public void AddDelayedDamage(int amount)
        {
            if (amount <= 0) return;
            delayedDamages.Add(new DelayedDamage { damageAmount = amount });
        }

        public int ExecuteDelayedDamages()
        {
            int totalDamage = 0;
            foreach (var damage in delayedDamages)
            {
                totalDamage += damage.damageAmount;
            }
            delayedDamages.Clear();

            if (totalDamage > 0)
            {
                TakeDamage(totalDamage);
            }
            return totalDamage;
        }

        public Sprite GetSprite()
        {
            return enemySprite;
        }

        public GameObject SpawnModel(Transform parent)
        {
            if (data == null || data.enemyPrefab == null) return null;

            modelInstance = Object.Instantiate(data.enemyPrefab, parent);
            modelInstance.transform.localPosition = data.modelOffset;
            modelInstance.transform.localScale = data.modelScale;
            modelInstance.transform.localRotation = Quaternion.Euler(0, data.modelRotationY, 0);

            return modelInstance;
        }

        public void PlayAnimation(string animationName)
        {
            if (modelInstance == null) return;
            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator != null) animator.Play(animationName);
        }

        public void PlayIdle() => PlayAnimation(data?.idleAnimationName ?? "Idle");
        public void PlayAttack() => PlayAnimation(data?.attackAnimationName ?? "Attack");
        public void PlayHurt() => PlayAnimation(data?.hurtAnimationName ?? "Hurt");
        public void PlayDeath() => PlayAnimation(data?.deathAnimationName ?? "Death");

        public List<Buff> GetBuffs()
        {
            return new List<Buff>(buffs);
        }

        // ==================== 工厂方法 ====================

        public static Enemy CreateCorruptedSoldier()
        {
            var data = new EnemyData("腐化士兵", 30, 7, EnemyType.Normal);
            data.aiPatternName = "CorruptedSoldier";
            data.spriteName = "腐化士兵";
            return new Enemy(data);
        }

        public static Enemy CreateMutantHound()
        {
            var data = new EnemyData("畸变猎犬", 25, 9, EnemyType.Normal);
            data.aiPatternName = "MutantHound";
            data.spriteName = "畸变猎犬";
            return new Enemy(data);
        }

        public static Enemy CreatePlagueAcolyte()
        {
            var data = new EnemyData("瘟疫侍僧", 28, 6, EnemyType.Normal);
            data.aiPatternName = "PlagueAcolyte";
            data.spriteName = "瘟疫侍僧";
            return new Enemy(data);
        }

        public static Enemy CreateAbyssGrub()
        {
            var data = new EnemyData("深渊蛆虫", 22, 8, EnemyType.Normal);
            data.aiPatternName = "AbyssGrub";
            data.spriteName = "深渊蛆虫";
            return new Enemy(data);
        }

        public static Enemy CreateCorruptedKnight()
        {
            var data = new EnemyData("腐蚀骑士", 65, 14, EnemyType.Elite);
            data.aiPatternName = "CorruptedKnight";
            data.spriteName = "腐蚀骑士";
            return new Enemy(data);
        }

        public static Enemy CreateHellInquisitor()
        {
            var data = new EnemyData("地狱审判官", 60, 16, EnemyType.Elite);
            data.aiPatternName = "HellInquisitor";
            data.spriteName = "地狱审判官";
            return new Enemy(data);
        }

        public static Enemy CreateVoidWizard()
        {
            var data = new EnemyData("虚空巫师", 55, 12, EnemyType.Elite);
            data.aiPatternName = "VoidWizard";
            data.spriteName = "虚空巫师";
            return new Enemy(data);
        }

        public static Enemy CreateCorruptedGolem()
        {
            var data = new EnemyData("腐化巨兽", 80, 13, EnemyType.Elite);
            data.aiPatternName = "CorruptedGolem";
            data.spriteName = "腐化巨兽";
            return new Enemy(data);
        }

        public static Enemy CreateAbyssLord()
        {
            var data = new EnemyData("深渊之主", 150, 22, EnemyType.Boss);
            data.aiPatternName = "AbyssLord";
            data.spriteName = "深渊之主";

            var enemy = new Enemy(data);
            enemy.firstFormSpriteName = "深渊之主";
            enemy.secondFormSpriteName = "深渊之主·克苏鲁之影";

            return enemy;
        }

        public static Enemy CreateCorruptedKing()
        {
            var data = new EnemyData("腐化君王·最后的哀鸣", 200, 28, EnemyType.Boss);
            data.aiPatternName = "CorruptedKing";
            data.spriteName = "腐化君王";
            return new Enemy(data);
        }

        public static Enemy CreateGoblin() => CreateCorruptedSoldier();
        public static Enemy CreateElite() => CreateCorruptedKnight();
        public static Enemy CreateBoss() => CreateAbyssLord();
    }

    [System.Serializable]
    public class Buff
    {
        public BuffType type;
        public int amount;
        public int duration;

        public void ReduceDuration() { duration--; }
        public bool IsExpired() => duration <= 0;
    }

    public enum BuffType
    {
        Strength,
        Dexterity,
        Shield,
        Poison,
        Vulnerability,
        Weak,
        Frail
    }

    [System.Serializable]
    public class DelayedDamage
    {
        public int damageAmount;
    }
}
