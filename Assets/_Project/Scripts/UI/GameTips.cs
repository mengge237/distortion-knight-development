using UnityEngine;

namespace MutationChess.UI
{
    /// <summary>
    /// 游戏常识与隐藏效果提示集（开局加载滚动文字 + 难度面板"冒险须知"共用）：
    /// 内容揭示机制常识与不易察觉的隐藏效果，帮助新玩家了解深渊规则。
    /// </summary>
    public static class GameTips
    {
        public static readonly string[] All =
        {
            "黑烛护体可免疫一切诅咒降临；净秽香炉将每层诅咒概率减半。",
            "反咒之镜持有者：诅咒效果全部反转——嗜血诅咒反而回血，迷雾诅咒反而全图透视。",
            "迷雾诅咒降临后，地图下一行的节点类型被「命运问号」隐藏；罗盘可多探一行，星图直接全图揭示。",
            "观星镜能看穿迷雾中的精英节点，并预览抽牌堆的下一张牌。",
            "寻宝针可看穿迷雾中的宝箱节点，宝光透雾。",
            "黄金王国·金从第 3 层起降临（概率 8%+每层4%，封顶 35%），降临后顶替本场常规遗物掉落，一局至多一件。",
            "持有黄金王国·金或银时，罗盘与星图的胜利金币加成大幅强化（共鸣）。",
            "罗盘每次胜利额外 +5 金币，星图 +15 金币——情报型遗物也能生财。",
            "每场战斗胜利后回复 20% 最大生命值，败北则结束旅程。",
            "最多携带 3 瓶药水；药水瓶满时，新获得的药水自动折算成金币。",
            "鲜血系卡牌可用血量支付费用（3 血 = 1 能量）；寒霜系可用格挡补足（5 格挡 = 1 能量）。",
            "虚弱每层降低 20% 伤害，最多叠 4 层；易伤每层增加 20% 所受伤害。",
            "带有「消耗」的卡牌打出后进入消耗堆，本场战斗不再出现。",
            "腐化君王血量低于 50% 时切换第二形态，攻击力提升。",
            "馈赠卡在回合开始或回合结束时自动触发，无需打出。",
            "图鉴采用「见过才解锁」：卡牌 / 遗物 / 药水按 k / r / p 编号，只显示你见过的条目。",
            "开发者模式下图鉴显示全部条目；控制台输入 give k5 / r7 / p3 可直接获得物品。",
            "炼狱及以上难度开局即携带诅咒；难度越高，每层诅咒降临概率越高。",
            "诅咒卡会停留在手牌中持续生效——寻找消耗手段处理它们。",
            "冰霜心脏与不情愿锁链激活时，对应阵营的效果翻倍。",
            "凤凰羽能让你在死亡时浴火重生——真正的保命符。",
            "商店刷新护符可以让商店重新进货。",
            "游戏自动存档：退出时保留地图与战斗进度，继续游戏可回到退出前的时刻。",
            "图鉴编号就是调试台的物品命令：k 卡牌、r 遗物、p 药水。",
        };

        /// <summary>须知分页每页条数。</summary>
        public const int TipsPerPage = 4;

        public static int PageCount => Mathf.Max(1, Mathf.CeilToInt((float)All.Length / TipsPerPage));

        /// <summary>取某页提示（自动越界修正），返回序号+内容文本。</summary>
        public static string GetPageText(int pageIndex)
        {
            int page = Mathf.Clamp(pageIndex, 0, PageCount - 1);
            int start = page * TipsPerPage;
            var sb = new System.Text.StringBuilder();
            for (int i = start; i < All.Length && i < start + TipsPerPage; i++)
            {
                sb.Append("◆ ").Append(All[i]);
                if (i < All.Length - 1 && i < start + TipsPerPage - 1)
                    sb.Append("\n\n");
            }
            return sb.ToString();
        }
    }
}
