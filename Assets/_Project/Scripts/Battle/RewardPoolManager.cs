using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Battle
{
    /// <summary>
    /// 奖励池管理器 - 统一管理所有奖励池的配置
    /// </summary>
    public static class RewardPoolManager
    {
        // ===== 卡牌稀有度分组 =====

        private static readonly List<CardName> CommonCards = new List<CardName>
        {
            CardName.攻击,
            CardName.防御,
            CardName.痛击,
        };

        private static readonly List<CardName> UncommonCards = new List<CardName>
        {
            CardName.后发制人,
            CardName.预知仪式,
            CardName.加固,
        };

        private static readonly List<CardName> RareCards = new List<CardName>
        {
            CardName.暮光仪式,
        };

        // ===== 各角色专属卡牌（扩展预留） =====

        private static readonly List<CardName> IroncladCards = new List<CardName>
        {
            // 战士专属卡（预留）
        };

        private static readonly List<CardName> SilentCards = new List<CardName>
        {
            // 猎手专属卡（预留）
        };

        private static readonly List<CardName> DefectCards = new List<CardName>
        {
            // 机器人专属卡（预留）
        };

        // ===== 公共接口 =====

        /// <summary>
        /// 获取普通战斗奖励池
        /// </summary>
        public static List<CardName> GetCommonPool()
        {
            List<CardName> pool = new List<CardName>();
            pool.AddRange(CommonCards);
            pool.AddRange(UncommonCards);
            return pool;
        }

        /// <summary>
        /// 获取精英战斗奖励池
        /// </summary>
        public static List<CardName> GetElitePool()
        {
            List<CardName> pool = new List<CardName>();
            pool.AddRange(CommonCards);
            pool.AddRange(UncommonCards);
            pool.AddRange(RareCards);
            return pool;
        }

        /// <summary>
        /// 获取 Boss 战斗奖励池
        /// </summary>
        public static List<CardName> GetBossPool()
        {
            List<CardName> pool = new List<CardName>();
            pool.AddRange(UncommonCards);
            pool.AddRange(RareCards);
            return pool;
        }

        /// <summary>
        /// 根据类型获取奖励池
        /// </summary>
        public static List<CardName> GetPoolByType(RewardPoolType type)
        {
            switch (type)
            {
                case RewardPoolType.Common:
                    return GetCommonPool();
                case RewardPoolType.Elite:
                    return GetElitePool();
                case RewardPoolType.Boss:
                    return GetBossPool();
                case RewardPoolType.Shop:
                    return GetShopPool();
                case RewardPoolType.Event:
                    return GetEventPool();
                default:
                    return GetCommonPool();
            }
        }

        /// <summary>
        /// 初始化 RewardPool 资产（在编辑器或运行时调用）
        /// </summary>
        public static void InitializeRewardPool(RewardPool rewardPool)
        {
            if (rewardPool == null) return;

            List<CardName> poolData = GetPoolByType(rewardPool.poolType);
            rewardPool.RefreshAvailableRewards(poolData);

        }

        /// <summary>
        /// 初始化所有奖励池
        /// </summary>
        public static void InitializeAllPools(RewardPool common, RewardPool elite, RewardPool boss)
        {
            if (common != null) InitializeRewardPool(common);
            if (elite != null) InitializeRewardPool(elite);
            if (boss != null) InitializeRewardPool(boss);
        }

        // ===== 扩展池（预留） =====

        private static List<CardName> GetShopPool()
        {
            List<CardName> pool = new List<CardName>();
            pool.AddRange(CommonCards);
            pool.AddRange(UncommonCards);
            pool.AddRange(RareCards);
            return pool;
        }

        private static List<CardName> GetEventPool()
        {
            List<CardName> pool = new List<CardName>();
            pool.AddRange(UncommonCards);
            pool.AddRange(RareCards);
            return pool;
        }

        /// <summary>
        /// 获取角色的专属卡牌（扩展）
        /// </summary>
        public static List<CardName> GetCharacterCards(string characterId)
        {
            switch (characterId)
            {
                case "Ironclad":
                    return IroncladCards;
                case "Silent":
                    return SilentCards;
                case "Defect":
                    return DefectCards;
                default:
                    return new List<CardName>();
            }
        }
    }
}