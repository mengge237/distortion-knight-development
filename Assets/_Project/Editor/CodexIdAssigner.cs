using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MutationChess.Core;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 图鉴 ID 分配器（参照《以撒的结合》道具编号，前缀体系）：
    /// k=卡牌 / r=遗物 / p=药水，各自从 1 独立递增，类别间编号互不占用、无上限。
    /// 仅在资产 codexId &lt;= 0（未分配）时写入：首次批量分配后 ID 永久固化，
    /// 之后新增资产取同类别内下一个空闲号，老 ID 不漂移（存档中的 seen 记录不会失效）。
    /// 域重载后自动检查（幂等，无未分配资产时不写盘），也可用菜单 Tools/分配图鉴ID 手动触发。
    /// </summary>
    [InitializeOnLoad]
    public static class CodexIdAssigner
    {
        private const string CardsFolder = "Assets/_Project/Resources/Cards";
        private const string RelicsFolder = "Assets/_Project/Resources/Relics";
        private const string PotionsFolder = "Assets/_Project/Resources/Potions";

        static CodexIdAssigner()
        {
            EditorTaskGuard.RunWhenSafe(AutoAssignIfNeeded);
        }

        private static void AutoAssignIfNeeded()
        {
            // 幂等：全部已分配时直接跳过，避免每次域重载都写盘
            if (CountUnassigned() == 0) return;
            AssignAll();
        }

        [MenuItem("Tools/分配图鉴ID")]
        public static void AssignAllFromMenu()
        {
            AssignAll();
        }

        private static int CountUnassigned()
        {
            return LoadAssets<CardDataAsset>("t:CardDataAsset", CardsFolder).Count(a => a.codexId <= 0)
                 + LoadAssets<RelicDataAsset>("t:RelicDataAsset", RelicsFolder).Count(a => a.codexId <= 0)
                 + LoadAssets<PotionDataAsset>("t:PotionDataAsset", PotionsFolder).Count(a => a.codexId <= 0);
        }

        private static void AssignAll()
        {
            int dirty = 0;

            dirty += AssignCards();
            dirty += AssignRelics();
            dirty += AssignPotions();

            if (dirty > 0)
            {
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log($"[CodexIdAssigner] 已为 {dirty} 个资产分配图鉴 ID 并保存");
            }
            else
            {
                UnityEngine.Debug.Log("[CodexIdAssigner] 所有图鉴资产均已分配 ID，无需处理");
            }
        }

        private static int AssignCards()
        {
            var cards = LoadAssets<CardDataAsset>("t:CardDataAsset", CardsFolder);
            CheckDuplicates(cards.Select(c => c.codexId).Where(id => id > 0).ToList(), "卡牌");

            var unassigned = cards.Where(c => c.codexId <= 0).ToList();
            // 排序与 CardData.GetAllCardAssets 完全一致：阵营升 → 稀有度降 → 费用升 → 名称序数
            unassigned.Sort((a, b) =>
            {
                int cmp = a.faction.CompareTo(b.faction);
                if (cmp != 0) return cmp;
                cmp = b.rarity.CompareTo(a.rarity);
                if (cmp != 0) return cmp;
                cmp = a.cost.CompareTo(b.cost);
                if (cmp != 0) return cmp;
                return string.Compare(a.cardName, b.cardName, System.StringComparison.Ordinal);
            });

            var used = new HashSet<int>(cards.Where(c => c.codexId > 0).Select(c => c.codexId));
            int next = 1;
            int assigned = 0;
            foreach (var c in unassigned)
            {
                while (used.Contains(next)) next++;
                c.codexId = next++;
                EditorUtility.SetDirty(c);
                assigned++;
            }
            if (assigned > 0)
                UnityEngine.Debug.Log($"[CodexIdAssigner] 卡牌：分配 {assigned} 个 ID（1-{next - 1}）");
            return assigned;
        }

        private static int AssignRelics()
        {
            var relics = LoadAssets<RelicDataAsset>("t:RelicDataAsset", RelicsFolder);
            CheckDuplicates(relics.Select(r => r.codexId).Where(id => id > 0).ToList(), "遗物");

            var unassigned = relics.Where(r => r.codexId <= 0).ToList();
            // 稀有度升（初始遗物在前）→ 阵营升 → 名称序数
            unassigned.Sort((a, b) =>
            {
                int cmp = a.rarity.CompareTo(b.rarity);
                if (cmp != 0) return cmp;
                cmp = a.faction.CompareTo(b.faction);
                if (cmp != 0) return cmp;
                return string.Compare(a.relicName, b.relicName, System.StringComparison.Ordinal);
            });

            var used = new HashSet<int>(relics.Where(r => r.codexId > 0).Select(r => r.codexId));
            int next = 1;
            int assigned = 0;
            foreach (var r in unassigned)
            {
                while (used.Contains(next)) next++;
                r.codexId = next++;
                EditorUtility.SetDirty(r);
                assigned++;
            }
            if (assigned > 0)
                UnityEngine.Debug.Log($"[CodexIdAssigner] 遗物：分配 {assigned} 个 ID（1-{next - 1}）");
            return assigned;
        }

        private static int AssignPotions()
        {
            var potions = LoadAssets<PotionDataAsset>("t:PotionDataAsset", PotionsFolder);
            CheckDuplicates(potions.Select(p => p.codexId).Where(id => id > 0).ToList(), "药水");

            var unassigned = potions.Where(p => p.codexId <= 0).ToList();
            // 稀有度升 → 名称序数
            unassigned.Sort((a, b) =>
            {
                int cmp = a.rarity.CompareTo(b.rarity);
                if (cmp != 0) return cmp;
                return string.Compare(a.potionName, b.potionName, System.StringComparison.Ordinal);
            });

            var used = new HashSet<int>(potions.Where(p => p.codexId > 0).Select(p => p.codexId));
            int next = 1;
            int assigned = 0;
            foreach (var p in unassigned)
            {
                while (used.Contains(next)) next++;
                p.codexId = next++;
                EditorUtility.SetDirty(p);
                assigned++;
            }
            if (assigned > 0)
                UnityEngine.Debug.Log($"[CodexIdAssigner] 药水：分配 {assigned} 个 ID（1-{next - 1}）");
            return assigned;
        }

        /// <summary>数据完整性检查：同一类别内 ID 重复时告警（不自动修复，避免覆盖既有存档记录）。</summary>
        private static void CheckDuplicates(List<int> ids, string categoryName)
        {
            var dupes = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupes.Count > 0)
                UnityEngine.Debug.LogWarning($"[CodexIdAssigner] {categoryName}存在重复图鉴 ID：{string.Join(", ", dupes)}，请手动检查资产");
        }

        private static List<T> LoadAssets<T>(string filter, string folder) where T : Object
        {
            var list = new List<T>();
            var guids = AssetDatabase.FindAssets(filter, new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) list.Add(asset);
            }
            return list;
        }
    }
}
