using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>图鉴类别（命令与面板共用的解析目标）。</summary>
    public enum CodexCategory
    {
        Card,
        Relic,
        Potion
    }

    /// <summary>
    /// 图鉴 ID 前缀体系（参照《以撒的结合》道具编号）：
    /// k=卡牌 / r=遗物 / p=药水，各自从 1 独立递增（k5=5号卡牌、r7=7号遗物、p3=3号药水），
    /// 类别间编号互不占用、无上限，不再受固定段宽限制。
    /// ID 由编辑器脚本 CodexIdAssigner 一次性分配并写入资产（codexId 字段），
    /// 之后永不变化；新增资产取同类别内下一个空闲号，老 ID 不漂移。
    /// </summary>
    public static class CodexIds
    {
        public const char CardPrefix = 'k';
        public const char RelicPrefix = 'r';
        public const char PotionPrefix = 'p';

        public static char PrefixOf(CodexCategory c)
        {
            switch (c)
            {
                case CodexCategory.Card: return CardPrefix;
                case CodexCategory.Relic: return RelicPrefix;
                case CodexCategory.Potion: return PotionPrefix;
                default: return '?';
            }
        }

        /// <summary>展示形式：K5 / R7 / P3（大写前缀+类别内编号，图鉴徽标与命令日志统一用）。</summary>
        public static string Format(CodexCategory c, int id) => char.ToUpperInvariant(PrefixOf(c)) + id.ToString();

        /// <summary>解析前缀形式 k5 / r7 / p3（不区分大小写）→ 类别+编号；非前缀形式返回 false。</summary>
        public static bool TryParse(string token, out CodexCategory category, out int id)
        {
            category = CodexCategory.Card;
            id = 0;
            if (string.IsNullOrEmpty(token)) return false;
            string t = token.Trim();
            if (t.Length < 2) return false;
            CodexCategory cat;
            switch (char.ToLowerInvariant(t[0]))
            {
                case CardPrefix: cat = CodexCategory.Card; break;
                case RelicPrefix: cat = CodexCategory.Relic; break;
                case PotionPrefix: cat = CodexCategory.Potion; break;
                default: return false;
            }
            if (!int.TryParse(t.Substring(1), out int n) || n <= 0) return false;
            category = cat;
            id = n;
            return true;
        }

        /// <summary>类别中文名（控制台帮助/日志用）。</summary>
        public static string CategoryName(CodexCategory c)
        {
            switch (c)
            {
                case CodexCategory.Card: return "卡牌";
                case CodexCategory.Relic: return "遗物";
                case CodexCategory.Potion: return "药水";
                default: return c.ToString();
            }
        }

        /// <summary>
        /// 旧段式 ID 迁移（8.16.3 早期存档兼容）：遗物 1001-1999 → 1-999、药水 2001-2999 → 1-999；
        /// 卡牌旧段本就是 1-999 原样保留。前缀体系新 ID 原样返回。
        /// </summary>
        public static int MigrateLegacyId(CodexCategory cat, int id)
        {
            if (id <= 0) return 0;
            switch (cat)
            {
                case CodexCategory.Relic: return id >= 1001 && id <= 1999 ? id - 1000 : id;
                case CodexCategory.Potion: return id >= 2001 && id <= 2999 ? id - 2000 : id;
                default: return id;
            }
        }
    }

    /// <summary>
    /// 图鉴 ID 注册表：codexId ↔ 资产 双向查找（懒加载，命令台与图鉴面板共用）。
    /// 资产 codexId &lt;= 0（未分配）不进入索引，仅可通过名称查找。
    /// </summary>
    public static class CodexIdRegistry
    {
        private static Dictionary<int, CardDataAsset> _cardsById;
        private static Dictionary<int, RelicDataAsset> _relicsById;
        private static Dictionary<int, PotionDataAsset> _potionsById;
        private static Dictionary<string, CardDataAsset> _cardsByName;
        private static Dictionary<string, RelicDataAsset> _relicsByName;
        private static Dictionary<string, RelicDataAsset> _relicsByAssetId;
        private static Dictionary<string, PotionDataAsset> _potionsByName;

        private static bool _loaded;

        /// <summary>强制重建索引（资产热更/测试用）。</summary>
        public static void ResetCache()
        {
            _loaded = false;
            // 链式赋值 a = b = c = null 要求类型可隐式转换，
            // Dictionary<int, PotionDataAsset> 不能赋给 Dictionary<int, RelicDataAsset>（CS0029），必须分行。
            _cardsById = null;
            _relicsById = null;
            _potionsById = null;
            _cardsByName = null;
            _relicsByName = null;
            _relicsByAssetId = null;
            _potionsByName = null;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _cardsById = new Dictionary<int, CardDataAsset>();
            _cardsByName = new Dictionary<string, CardDataAsset>();
            _relicsById = new Dictionary<int, RelicDataAsset>();
            _relicsByName = new Dictionary<string, RelicDataAsset>();
            _relicsByAssetId = new Dictionary<string, RelicDataAsset>();
            _potionsById = new Dictionary<int, PotionDataAsset>();
            _potionsByName = new Dictionary<string, PotionDataAsset>();

            foreach (var c in Resources.LoadAll<CardDataAsset>(ResourcePaths.Cards))
            {
                if (c == null || string.IsNullOrEmpty(c.cardName)) continue;
                if (!_cardsByName.ContainsKey(c.cardName)) _cardsByName[c.cardName] = c;
                if (c.codexId > 0 && !_cardsById.ContainsKey(c.codexId)) _cardsById[c.codexId] = c;
            }

            foreach (var r in Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics))
            {
                if (r == null || string.IsNullOrEmpty(r.relicName)) continue;
                if (!_relicsByName.ContainsKey(r.relicName)) _relicsByName[r.relicName] = r;
                if (!string.IsNullOrEmpty(r.relicId) && !_relicsByAssetId.ContainsKey(r.relicId))
                    _relicsByAssetId[r.relicId] = r;
                if (r.codexId > 0 && !_relicsById.ContainsKey(r.codexId)) _relicsById[r.codexId] = r;
            }

            foreach (var p in Resources.LoadAll<PotionDataAsset>(ResourcePaths.Potions))
            {
                if (p == null || string.IsNullOrEmpty(p.potionName)) continue;
                if (!_potionsByName.ContainsKey(p.potionName)) _potionsByName[p.potionName] = p;
                if (p.codexId > 0 && !_potionsById.ContainsKey(p.codexId)) _potionsById[p.codexId] = p;
            }
        }

        #region ID 查找

        public static CardDataAsset GetCard(int codexId)
        {
            EnsureLoaded();
            _cardsById.TryGetValue(codexId, out CardDataAsset a);
            return a;
        }

        public static RelicDataAsset GetRelic(int codexId)
        {
            EnsureLoaded();
            _relicsById.TryGetValue(codexId, out RelicDataAsset a);
            return a;
        }

        public static PotionDataAsset GetPotion(int codexId)
        {
            EnsureLoaded();
            _potionsById.TryGetValue(codexId, out PotionDataAsset a);
            return a;
        }

        #endregion

        #region 名称查找

        public static CardDataAsset FindCardByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureLoaded();
            _cardsByName.TryGetValue(name, out CardDataAsset a);
            return a;
        }

        public static RelicDataAsset FindRelicByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureLoaded();
            _relicsByName.TryGetValue(name, out RelicDataAsset a);
            return a;
        }

        /// <summary>按资产 relicId（如 Boss_ReluctantChain）查找遗物资产。</summary>
        public static RelicDataAsset FindRelicByAssetId(string relicId)
        {
            if (string.IsNullOrEmpty(relicId)) return null;
            EnsureLoaded();
            _relicsByAssetId.TryGetValue(relicId, out RelicDataAsset a);
            return a;
        }

        public static PotionDataAsset FindPotionByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureLoaded();
            _potionsByName.TryGetValue(name, out PotionDataAsset a);
            return a;
        }

        #endregion

        #region 按 ID 排序的完整列表（图鉴面板浏览用）

        /// <summary>全部已分配 ID 的卡牌资产，按 codexId 升序。</summary>
        public static List<CardDataAsset> GetCardsByIdOrdered()
        {
            EnsureLoaded();
            var list = new List<CardDataAsset>(_cardsById.Values);
            list.Sort((a, b) => a.codexId.CompareTo(b.codexId));
            return list;
        }

        /// <summary>全部已分配 ID 的遗物资产，按 codexId 升序。</summary>
        public static List<RelicDataAsset> GetRelicsByIdOrdered()
        {
            EnsureLoaded();
            var list = new List<RelicDataAsset>(_relicsById.Values);
            list.Sort((a, b) => a.codexId.CompareTo(b.codexId));
            return list;
        }

        /// <summary>全部已分配 ID 的药水资产，按 codexId 升序。</summary>
        public static List<PotionDataAsset> GetPotionsByIdOrdered()
        {
            EnsureLoaded();
            var list = new List<PotionDataAsset>(_potionsById.Values);
            list.Sort((a, b) => a.codexId.CompareTo(b.codexId));
            return list;
        }

        #endregion

        #region 命令解析

        /// <summary>
        /// 解析命令参数为图鉴类别+ID。支持：
        ///   前缀形式（以撒式）："k5" / "r7" / "p3"（不区分大小写）、
        ///   "card 5" / "relic 7" / "potion 3"、
        ///   名称（"card 攻击" 或直接中文名，跨类别模糊匹配）。
        /// 裸数字不再支持（类别歧义，由控制台给出前缀提示）。
        /// </summary>
        public static bool TryResolve(string arg, out CodexCategory category, out int codexId)
        {
            category = CodexCategory.Card;
            codexId = 0;
            if (string.IsNullOrWhiteSpace(arg)) return false;

            string[] parts = arg.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                // 前缀形式 k5 / r7 / p3 → 直接定位
                if (CodexIds.TryParse(parts[0], out CodexCategory cat, out int id))
                {
                    category = cat;
                    codexId = id;
                    return true;
                }
                // 裸名称 → 跨类别模糊匹配
                return TryResolveByName(parts[0], out category, out codexId);
            }

            // "card 5" / "relic x" / "potion x"
            string prefix = parts[0].ToLowerInvariant();
            string rest = string.Join(" ", parts, 1, parts.Length - 1);
            CodexCategory? c = prefix switch
            {
                "card" or "卡牌" => CodexCategory.Card,
                "relic" or "遗物" => CodexCategory.Relic,
                "potion" or "药水" => CodexCategory.Potion,
                _ => null
            };
            if (c == null) return false;
            category = c.Value;

            if (int.TryParse(rest, out int n))
            {
                codexId = n;
                return true;
            }
            return TryResolveByNameIn(rest, category, out codexId);
        }

        private static bool TryResolveByName(string name, out CodexCategory category, out int codexId)
        {
            var card = FindCardByName(name);
            if (card != null && card.codexId > 0) { category = CodexCategory.Card; codexId = card.codexId; return true; }
            var relic = FindRelicByName(name);
            if (relic != null && relic.codexId > 0) { category = CodexCategory.Relic; codexId = relic.codexId; return true; }
            var potion = FindPotionByName(name);
            if (potion != null && potion.codexId > 0) { category = CodexCategory.Potion; codexId = potion.codexId; return true; }
            category = CodexCategory.Card; // 失败路径也必须给 out 参数赋值（CS0177）
            codexId = 0;
            return false;
        }

        private static bool TryResolveByNameIn(string name, CodexCategory category, out int codexId)
        {
            switch (category)
            {
                case CodexCategory.Card:
                    var card = FindCardByName(name);
                    if (card != null && card.codexId > 0) { codexId = card.codexId; return true; }
                    break;
                case CodexCategory.Relic:
                    var relic = FindRelicByName(name);
                    if (relic != null && relic.codexId > 0) { codexId = relic.codexId; return true; }
                    break;
                case CodexCategory.Potion:
                    var potion = FindPotionByName(name);
                    if (potion != null && potion.codexId > 0) { codexId = potion.codexId; return true; }
                    break;
            }
            codexId = 0;
            return false;
        }

        #endregion
    }
}
