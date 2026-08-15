using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒结算模式：
    /// Inactive = 未持有/被黑烛免疫；Active = 常规生效；Inverted = 反咒之镜反转。
    /// </summary>
    public enum CurseMode
    {
        Inactive,
        Active,
        Inverted
    }

    /// <summary>
    /// 诅咒体系静态结算器（对标《以撒的结合》黑蜡烛）：
    /// - 黑烛（Shop_BlackCandle）：免疫一切诅咒，诅咒无法降临，已有诅咒失效
    /// - 反咒之镜（Shop_CurseMirror）：持有的诅咒效果反转，祸患化为祝福
    /// 所有诅咒效果挂点一律通过 GetCurseMode 判定，禁止散落 HasRelic 直接判定。
    /// </summary>
    public static class CurseSystem
    {
        /// <summary>全部诅咒 ID（发放池统计与"持有诅咒数量"结算依据，新增诅咒时在此登记）。</summary>
        private static readonly string[] AllCurseIds =
        {
            RelicIds.Curse_FogOfWar, RelicIds.Curse_Greed, RelicIds.Curse_Weakness,
            RelicIds.Curse_Bloodthirst, RelicIds.Curse_Rust,
            RelicIds.Curse_Drowsy, RelicIds.Curse_PhantomPain
        };

        /// <summary>黑烛护体：免疫一切诅咒。</summary>
        public static bool IsImmune(RelicManager rm)
        {
            return rm != null && rm.HasRelic(RelicIds.Shop_BlackCandle);
        }

        /// <summary>反咒之镜：持有的诅咒效果反转。</summary>
        public static bool IsInverting(RelicManager rm)
        {
            return rm != null && rm.HasRelic(RelicIds.Shop_CurseMirror);
        }

        /// <summary>判定指定诅咒的结算模式。</summary>
        public static CurseMode GetCurseMode(string curseId)
        {
            RelicManager rm = RelicManager.Instance;
            if (rm == null || string.IsNullOrEmpty(curseId) || !rm.HasRelic(curseId))
                return CurseMode.Inactive;
            if (IsImmune(rm)) return CurseMode.Inactive;
            if (IsInverting(rm)) return CurseMode.Inverted;
            return CurseMode.Active;
        }

        /// <summary>当前持有的诅咒数量（承咒之鼎等按此结算）。</summary>
        public static int HeldCurseCount(RelicManager rm)
        {
            if (rm == null) return 0;
            int count = 0;
            foreach (var relic in rm.GetAllRelics())
            {
                if (relic != null && IsCurseId(relic.relicId)) count++;
            }
            return count;
        }

        /// <summary>该遗物 ID 是否为诅咒。</summary>
        public static bool IsCurseId(string relicId)
        {
            return !string.IsNullOrEmpty(relicId) && AllCurseIds.Contains(relicId);
        }

        /// <summary>从 Resources/Relics 加载诅咒资产池（isCurse=true）。</summary>
        public static List<RelicDataAsset> LoadCursePool()
        {
            RelicDataAsset[] all = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            return all.Where(a => a != null && a.isCurse).ToList();
        }

        /// <summary>
        /// 尝试发放指定诅咒。黑烛免疫时直接拦截，重复持有时跳过。返回是否实际发放。
        /// </summary>
        public static bool TryGrantCurse(RelicManager rm, string curseId, string source)
        {
            if (rm == null || string.IsNullOrEmpty(curseId)) return false;
            if (IsImmune(rm))
            {
                GameLogger.Log($"[诅咒] {source}：黑烛护体，诅咒「{curseId}」无法降临");
                return false;
            }
            if (rm.HasRelic(curseId)) return false;

            RelicDataAsset asset = LoadCursePool().FirstOrDefault(a => a.relicId == curseId);
            if (asset == null)
            {
                GameLogger.LogWarning($"[诅咒] 未找到诅咒资产：{curseId}");
                return false;
            }
            Relic relic = rm.CreateRelicFromAsset(asset);
            if (relic == null) return false;
            rm.AddRelic(relic);
            GameLogger.Log($"[诅咒] {source}：诅咒降临「{relic.relicName}」——{asset.description}");
            return true;
        }

        /// <summary>从诅咒池随机发放 count 个不同诅咒，返回实际发放数量（黑烛免疫时 0）。</summary>
        public static int GrantRandomCurses(RelicManager rm, int count, string source)
        {
            if (rm == null || count <= 0) return 0;
            if (IsImmune(rm))
            {
                GameLogger.Log($"[诅咒] {source}：黑烛护体，诅咒无法降临");
                return 0;
            }
            List<RelicDataAsset> pool = LoadCursePool()
                .Where(a => !rm.HasRelic(a.relicId))
                .ToList();

            int granted = 0;
            while (granted < count && pool.Count > 0)
            {
                int idx = Random.Range(0, pool.Count);
                RelicDataAsset curse = pool[idx];
                pool.RemoveAt(idx);
                if (TryGrantCurse(rm, curse.relicId, source)) granted++;
            }
            return granted;
        }
    }
}
