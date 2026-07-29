using UnityEngine;
using System.Collections.Generic;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BackgroundConfig", menuName = "MutationChess/Background Config")]
    public class BackgroundConfig : ScriptableObject
    {
        [Header("敌人背景映射表")]
        public List<BackgroundMapping> mappings = new List<BackgroundMapping>();

        public Sprite GetBackground(EnemyType enemyType)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.enemyType == enemyType && mapping.background != null)
                {
                    return mapping.background;
                }
            }
            return mappings.Count > 0 ? mappings[0].background : null;
        }

        public Sprite GetBackground(EnemyNameOption enemyName)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.enemyName == enemyName && mapping.background != null)
                {
                    return mapping.background;
                }
            }
            return mappings.Count > 0 ? mappings[0].background : null;
        }

        public Sprite GetBackgroundByName(string enemyName)
        {
            if (System.Enum.TryParse(enemyName, out EnemyNameOption parsedName))
            {
                return GetBackground(parsedName);
            }

            foreach (var mapping in mappings)
            {
                if (mapping.enemyName.ToString() == enemyName && mapping.background != null)
                {
                    return mapping.background;
                }
            }
            return mappings.Count > 0 ? mappings[0].background : null;
        }
    }

    [System.Serializable]
    public class BackgroundMapping
    {
        [Header("敌人类型信息")]
        public EnemyType enemyType;

        [Header("敌人名称映射")]
        public EnemyNameOption enemyName;

        [Header("背景图片")]
        public Sprite background;
    }

    public enum EnemyNameOption
    {
        CorruptedSoldier,
        AberrantHound,
        PlagueAcolyte,
        AbyssWorm,
        CorrodedKnight,
        HellInquisitor,
        VoidWizard,
        CorruptedBehemoth,
        AbyssLordCthulhuShadow,
        HellVengeanceKnight
    }
}