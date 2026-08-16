using UnityEngine;
using MutationChess.Core;
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

            // 1. 优先按类型文件夹 + spriteName 加载
            enemySprite = Resources.Load<Sprite>($"{folderPath}/{data.spriteName}");

            // 2. 回退：EnemySprites 根目录 + spriteName
            if (enemySprite == null)
            {
                enemySprite = Resources.Load<Sprite>($"{ResourcePaths.EnemySprites}/{data.spriteName}");
            }

            // 3. 回退：按类型文件夹 + enemyName 加载（覆盖 spriteName 与文件名不一致的情况）
            if (enemySprite == null && !string.IsNullOrEmpty(enemyName))
            {
                enemySprite = Resources.Load<Sprite>($"{folderPath}/{enemyName}");
            }

            // 4. 回退：EnemySprites 根目录 + enemyName
            if (enemySprite == null && !string.IsNullOrEmpty(enemyName))
            {
                enemySprite = Resources.Load<Sprite>($"{ResourcePaths.EnemySprites}/{enemyName}");
            }

            if (enemySprite == null)
            {
                GameLogger.LogWarning($"[Enemy] 未找到精灵图：spriteName={data.spriteName}, enemyName={enemyName}, path={folderPath}");
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
                    newSprite = Resources.Load<Sprite>($"{ResourcePaths.EnemySprites}/{secondFormSpriteName}");
                }

                if (newSprite != null)
                {
                    enemySprite = newSprite;
                }
                else
                {
                    GameLogger.LogWarning($"[Enemy] 未找到第二形态精灵图：{secondFormSpriteName}");
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

            // 君王型敌人每回合获得力量加成
            if (enemyName.Contains("君王") || enemyName.Contains("之主"))
            {
                int bonus = GetKingPowerBonus();
                if (bonus > 0)
                {
                    currentAttackDamage += bonus;
                    GameLogger.Log($"[Enemy] {enemyName} 力量加成 +{bonus}，当前攻击力：{currentAttackDamage}");
                }
            }

            if (enemyName.Contains("君王") && !isSecondForm && GetHealthPercentage() < 0.5f)
            {
                SwitchToSecondForm();
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
                if (buff.isShadow) existing.isShadow = true;
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
                // 暗影力量不衰减时跳过持续时间递减
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

        public void PlayAnimation(string animationName)
        {
            if (modelInstance == null) return;
            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator != null) animator.Play(animationName);
        }

        public void PlayIdle() => PlayAnimation(data?.idleAnimationName ?? "Idle");
        public void PlayAttack() => PlayAnimation(data?.attackAnimationName ?? "Attack");
        public void PlayHurt() => PlayAnimation(data?.hurtAnimationName ?? "Hurt");

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
            var data = new EnemyData("瘟疫信徒", 28, 6, EnemyType.Normal);
            data.aiPatternName = "PlagueAcolyte";
            data.spriteName = "瘟疫侍僧";
            return new Enemy(data);
        }

        public static Enemy CreateAbyssGrub()
        {
            var data = new EnemyData("深渊幼虫", 22, 8, EnemyType.Normal);
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
            var data = new EnemyData("炼狱审判官", 60, 16, EnemyType.Elite);
            data.aiPatternName = "HellInquisitor";
            data.spriteName = "地狱审判官";
            return new Enemy(data);
        }

        public static Enemy CreateVoidWizard()
        {
            var data = new EnemyData("虚空法师", 55, 12, EnemyType.Elite);
            data.aiPatternName = "VoidWizard";
            data.spriteName = "虚空巫师";
            return new Enemy(data);
        }

        public static Enemy CreateCorruptedGolem()
        {
            var data = new EnemyData("腐化魔像", 80, 13, EnemyType.Elite);
            data.aiPatternName = "CorruptedGolem";
            data.spriteName = "腐化巨兽";
            return new Enemy(data);
        }

        public static Enemy CreateAbyssLord()
        {
            var data = new EnemyData("腐化君王", 150, 22, EnemyType.Boss);
            data.aiPatternName = "AbyssLord";
            data.spriteName = "腐化君王";

            var enemy = new Enemy(data);
            enemy.secondFormSpriteName = "深渊之主";

            return enemy;
        }

        /// <summary>按敌人名重建敌人实例（存档读档用）。未知名称返回 null。</summary>
        public static Enemy CreateByName(string enemyName)
        {
            switch (enemyName)
            {
                case "腐化士兵": return CreateCorruptedSoldier();
                case "畸变猎犬": return CreateMutantHound();
                case "瘟疫信徒": return CreatePlagueAcolyte();
                case "深渊幼虫": return CreateAbyssGrub();
                case "腐蚀骑士": return CreateCorruptedKnight();
                case "炼狱审判官": return CreateHellInquisitor();
                case "虚空法师": return CreateVoidWizard();
                case "腐化魔像": return CreateCorruptedGolem();
                case "腐化君王": return CreateAbyssLord();
                default:
                    GameLogger.LogWarning($"[Enemy] 未知敌人名：{enemyName}");
                    return null;
            }
        }

        /// <summary>敌人战斗内完整状态快照（存档保留"战斗中的时刻"）。</summary>
        public EnemyStateSnapshot CreateSnapshot()
        {
            var snap = new EnemyStateSnapshot
            {
                enemyName = enemyName,
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                baseAttackDamage = baseAttackDamage,
                currentAttackDamage = currentAttackDamage,
                enemyType = (int)enemyType,
                isSecondForm = isSecondForm,
                currentActionIndex = currentActionIndex,
                currentLoopCount = currentLoopCount,
                turnCount = turnCount
            };
            foreach (var b in buffs)
                if (b != null) snap.buffs.Add(b);
            foreach (var d in delayedDamages)
                snap.delayedDamages.Add(d.damageAmount);
            return snap;
        }

        /// <summary>从快照恢复战斗内状态（读档后调用；不重触发任何开局副作用）。</summary>
        public void RestoreFromSnapshot(EnemyStateSnapshot snap)
        {
            if (snap == null) return;

            maxHealth = Mathf.Max(1, snap.maxHealth);
            currentHealth = Mathf.Clamp(snap.currentHealth, 0, maxHealth);
            baseAttackDamage = snap.baseAttackDamage;
            enemyType = (EnemyType)snap.enemyType;
            currentActionIndex = Mathf.Max(0, snap.currentActionIndex);
            currentLoopCount = Mathf.Max(0, snap.currentLoopCount);
            turnCount = Mathf.Max(0, snap.turnCount);

            // 二阶段：重新应用换肤（SwitchToSecondForm 自带 +5 攻击，随后用快照值覆盖）
            bool wantSecondForm = snap.isSecondForm;
            isSecondForm = false;
            if (wantSecondForm) SwitchToSecondForm();
            currentAttackDamage = snap.currentAttackDamage;

            buffs = new List<Buff>();
            if (snap.buffs != null)
                foreach (var b in snap.buffs)
                    if (b != null && !b.IsExpired()) buffs.Add(b);

            delayedDamages = new List<DelayedDamage>();
            if (snap.delayedDamages != null)
                foreach (int dmg in snap.delayedDamages)
                    delayedDamages.Add(new DelayedDamage { damageAmount = dmg });
        }
    }

    /// <summary>敌人战斗内状态快照（可序列化，随 BattleSaveData 存档）。</summary>
    [System.Serializable]
    public class EnemyStateSnapshot
    {
        public string enemyName;
        public int maxHealth;
        public int currentHealth;
        public int baseAttackDamage;
        public int currentAttackDamage;
        public int enemyType;            // (int)EnemyType
        public bool isSecondForm;
        public int currentActionIndex;   // AI 模式当前动作索引
        public int currentLoopCount;     // AI 模式当前循环次数
        public int turnCount;
        public List<Buff> buffs = new List<Buff>();
        public List<int> delayedDamages = new List<int>();
    }

    [System.Serializable]
    public class Buff
    {
        public BuffType type;
        public int amount;
        public int duration;
        // 是否为暗影系列buff（不衰减持续时间）
        public bool isShadow;

        public void ReduceDuration() { if (duration > 0) duration--; }
        public bool IsExpired() => duration == 0;
    }

    public enum BuffType
    {
        Strength,
        Dexterity,
        Shield,
        Poison,
        Vulnerability,
        Weak,
        Frail,
        Thorns
    }

    [System.Serializable]
    public class DelayedDamage
    {
        public int damageAmount;
    }
}

