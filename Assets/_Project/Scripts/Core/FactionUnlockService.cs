using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 阵营解锁服务，管理卡牌阵营的解锁状态与通知
    /// </summary>
    public class FactionUnlockService : MonoBehaviour
    {
        private static FactionUnlockService _instance;

        public static FactionUnlockService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<FactionUnlockService>();
                return _instance;
            }
        }

        [SerializeField] private bool debugMode = true;

        private HashSet<CardFaction> unlockedFactions = new HashSet<CardFaction>();

        public event System.Action<CardFaction> OnFactionUnlocked;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void UnlockFaction(CardFaction faction)
        {
            if (faction == CardFaction.None) return;
            if (unlockedFactions.Add(faction))
            {
                if (debugMode)
                    GameLogger.Log($"[FactionUnlockService] 解锁阵营：{GetFactionDisplayName(faction)}");
                OnFactionUnlocked?.Invoke(faction);
            }
        }

        public bool IsFactionUnlocked(CardFaction faction)
        {
            if (faction == CardFaction.None) return true;
            return unlockedFactions.Contains(faction);
        }

        public string GetFactionDisplayName(CardFaction faction)
        {
            switch (faction)
            {
                case CardFaction.Slime: return "粘液";
                case CardFaction.Reluctant: return "不舍";
                case CardFaction.Blood: return "鲜血";
                case CardFaction.Frost: return "寒霜";
                case CardFaction.Shadow: return "暗影";
                case CardFaction.Corrupt: return "腐化";
                default: return "未知";
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
