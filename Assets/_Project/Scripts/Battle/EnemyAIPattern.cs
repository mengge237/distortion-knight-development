using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Battle
{
    [Serializable]
    public class EnemyAIPattern
    {
        public string patternName;
        public List<EnemyAction> actions = new List<EnemyAction>();
        public bool shuffleActions = false;
        public int repeatCount = 1;
        public bool loopAfterFinish = true;

        public EnemyAIPattern() { }

        public EnemyAIPattern(string name, List<EnemyAction> actionList, bool shuffle = false, int repeat = -1, bool loop = true)
        {
            patternName = name;
            actions = actionList;
            shuffleActions = shuffle;
            repeatCount = repeat;
            loopAfterFinish = loop;
        }
    }

    [Serializable]
    public class EnemyAction
    {
        public EnemyIntentType intentType;
        public int baseValue;
        public int valueVariance = 3;

        public bool conditionCheck = false;
        public ConditionType conditionType;
        public int conditionThreshold;

        public EnemyAction() { }

        public EnemyAction(EnemyIntentType intent, int baseVal, int variance = 3)
        {
            intentType = intent;
            baseValue = baseVal;
            valueVariance = variance;
        }

        public EnemyAction(EnemyIntentType intent, int baseVal, int variance, ConditionType condition, int threshold)
        {
            intentType = intent;
            baseValue = baseVal;
            valueVariance = variance;
            conditionCheck = true;
            conditionType = condition;
            conditionThreshold = threshold;
        }

        public int GetFinalValue()
        {
            if (intentType == EnemyIntentType.Wait)
                return 0;
            return Mathf.Max(1, baseValue + UnityEngine.Random.Range(-valueVariance, valueVariance + 1));
        }
    }

    public enum ConditionType
    {
        EnemyHealthBelow,
        PlayerHealthBelow,
        EnemyHasBuff,
        TurnCount,
        EnemyHealthAbove,
        Always
    }

    public static class EnemyAIManager
    {
        private static Dictionary<string, EnemyAIPattern> patterns = new Dictionary<string, EnemyAIPattern>();

        static EnemyAIManager()
        {
            InitializePatterns();
        }

        private static void InitializePatterns()
        {
            patterns["CorruptedSoldier"] = new EnemyAIPattern(
                "腐化士兵",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 8, 3),
                    new EnemyAction(EnemyIntentType.Attack, 10, 2),
                    new EnemyAction(EnemyIntentType.Defend, 5, 2),
                },
                false, -1, true
            );

            patterns["MutantHound"] = new EnemyAIPattern(
                "畸变猎犬",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 9, 4),
                    new EnemyAction(EnemyIntentType.Attack, 12, 3),
                    new EnemyAction(EnemyIntentType.Attack, 6, 2),
                },
                false, -1, true
            );

            patterns["PlagueAcolyte"] = new EnemyAIPattern(
                "",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 6, 2),
                    new EnemyAction(EnemyIntentType.Defend, 4, 2),
                    new EnemyAction(EnemyIntentType.Attack, 8, 3),
                },
                false, -1, true
            );

            patterns["AbyssGrub"] = new EnemyAIPattern(
                "",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 8, 3),
                    new EnemyAction(EnemyIntentType.Attack, 10, 4),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                },
                false, -1, true
            );

            patterns["CorruptedKnight"] = new EnemyAIPattern(
                "腐蚀骑士",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Defend, 8, 3),
                    new EnemyAction(EnemyIntentType.Attack, 14, 4),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 20, 3),
                    new EnemyAction(EnemyIntentType.Defend, 6, 2),
                },
                false, -1, true
            );

            patterns["HellInquisitor"] = new EnemyAIPattern(
                "炼狱审判官",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 12, 4),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Special, 18, 5),
                    new EnemyAction(EnemyIntentType.Defend, 6, 2),
                },
                false, -1, true
            );

            patterns["VoidWizard"] = new EnemyAIPattern(
                "虚空法师",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Defend, 10, 3),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Special, 18, 4),
                    new EnemyAction(EnemyIntentType.Attack, 12, 3),
                    new EnemyAction(EnemyIntentType.Buff, 2, 0),
                },
                false, -1, true
            );

            patterns["CorruptedGolem"] = new EnemyAIPattern(
                "腐化魔像",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Defend, 12, 4),
                    new EnemyAction(EnemyIntentType.Attack, 13, 3),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Special, 15, 3),
                    new EnemyAction(EnemyIntentType.Attack, 10, 2),
                },
                false, -1, true
            );


            patterns["AbyssLord"] = new EnemyAIPattern(
                "深渊之主",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Defend, 12, 4),
                    new EnemyAction(EnemyIntentType.Attack, 18, 5),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Special, 25, 5),
                    new EnemyAction(EnemyIntentType.Attack, 22, 4),
                    new EnemyAction(EnemyIntentType.Buff, 3, 0),

                    new EnemyAction(EnemyIntentType.Wait, 0, 0, ConditionType.EnemyHealthBelow, 50),
                    new EnemyAction(EnemyIntentType.Special, 35, 5, ConditionType.EnemyHealthBelow, 50),

                    new EnemyAction(EnemyIntentType.Wait, 0, 0, ConditionType.EnemyHealthBelow, 30),
                    new EnemyAction(EnemyIntentType.Special, 45, 5, ConditionType.EnemyHealthBelow, 30),
                },
                false, -1, true
            );


            patterns["CorruptedKing"] = new EnemyAIPattern(
                "腐化君王",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Defend, 10, 4),
                    new EnemyAction(EnemyIntentType.Attack, 16, 4),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 22, 4),
                    new EnemyAction(EnemyIntentType.Special, 20, 5),
                    new EnemyAction(EnemyIntentType.Buff, 2, 0),

                    new EnemyAction(EnemyIntentType.Wait, 0, 0, ConditionType.EnemyHealthBelow, 70),
                    new EnemyAction(EnemyIntentType.Attack, 28, 4, ConditionType.EnemyHealthBelow, 70),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0, ConditionType.EnemyHealthBelow, 50),
                    new EnemyAction(EnemyIntentType.Special, 35, 5, ConditionType.EnemyHealthBelow, 50),
                    new EnemyAction(EnemyIntentType.Wait, 0, 0, ConditionType.EnemyHealthBelow, 30),
                    new EnemyAction(EnemyIntentType.Special, 45, 5, ConditionType.EnemyHealthBelow, 30),
                },
                false, -1, true
            );
        }

        public static EnemyAIPattern GetPattern(string patternName)
        {
            if (patterns.TryGetValue(patternName, out var pattern))
            {
                return ClonePattern(pattern);
            }
            return GetDefaultPattern();
        }

        public static EnemyAIPattern GetPatternByEnemyName(string enemyName)
        {
            foreach (var key in patterns.Keys)
            {
                if (enemyName.Contains(key))
                    return ClonePattern(patterns[key]);
            }
            return GetDefaultPattern();
        }

        private static EnemyAIPattern ClonePattern(EnemyAIPattern source)
        {
            var clone = new EnemyAIPattern();
            clone.patternName = source.patternName;
            clone.shuffleActions = source.shuffleActions;
            clone.repeatCount = source.repeatCount;
            clone.loopAfterFinish = source.loopAfterFinish;

            foreach (var action in source.actions)
            {
                clone.actions.Add(new EnemyAction
                {
                    intentType = action.intentType,
                    baseValue = action.baseValue,
                    valueVariance = action.valueVariance,
                    conditionCheck = action.conditionCheck,
                    conditionType = action.conditionType,
                    conditionThreshold = action.conditionThreshold
                });
            }

            return clone;
        }

        private static EnemyAIPattern GetDefaultPattern()
        {
            return new EnemyAIPattern(
                "",
                new List<EnemyAction>
                {
                    new EnemyAction(EnemyIntentType.Wait, 0, 0),
                    new EnemyAction(EnemyIntentType.Attack, 8, 3),
                    new EnemyAction(EnemyIntentType.Attack, 10, 4),
                    new EnemyAction(EnemyIntentType.Defend, 4, 2),
                },
                false, -1, true
            );
        }
    }
}