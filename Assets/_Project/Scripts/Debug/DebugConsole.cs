using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.Battle;
using MutationChess.Core;
using MutationChess.UI;

namespace MutationChess.Debug
{
    /// <summary>
    /// Debug console - press ~ or F1 to toggle, or GameManager ensures creation
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        /// <summary>Called by GameManager.Start to ensure debug console exists</summary>
        public static void EnsureExists()
        {
            if (!IsAllowedByFile())
            {
                UnityEngine.Debug.Log("[DebugConsole] 调试台未启用（正式包需 debug_enable 标记文件）");
                return;
            }
            if (FindObjectOfType<DebugConsole>() != null) return;
            var go = new GameObject("[DebugConsole]");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugConsole>();
            UnityEngine.Debug.Log("[DebugConsole] Debug console created, press ~ or F1 to open");
        }

        /// <summary>
        /// 调试台开关文件化（像其他游戏一样通过文件控制）：
        /// 开发构建/编辑器内始终可用；正式包仅当存在 debug_enable 标记文件时才可用。
        /// 标记文件位置：exe 同级目录，或 StreamingAssets 下的 debug_enable / debug_enable.txt。
        /// 结果缓存 2 秒，避免每帧磁盘 IO；运行中放入文件后最多 2 秒即可生效。
        /// </summary>
        public static bool IsAllowedByFile()
        {
            if (Debug.isDebugBuild || Application.isEditor) return true;

            float now = Time.realtimeSinceStartup;
            if (now - _allowedCheckTime < 2f) return _allowedCached;

            _allowedCheckTime = now;
            _allowedCached = false;

            string[] candidates =
            {
                Application.dataPath + "/../debug_enable",
                Application.dataPath + "/../debug_enable.txt",
                Application.streamingAssetsPath + "/debug_enable",
                Application.streamingAssetsPath + "/debug_enable.txt"
            };
            foreach (string path in candidates)
            {
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        _allowedCached = true;
                        break;
                    }
                }
                catch (System.Exception) { /* 路径不可访问时忽略，继续检查下一个 */ }
            }
            return _allowedCached;
        }

        private static bool _allowedCached;
        private static float _allowedCheckTime = -99f;

        private void Awake()
        {
            if (FindObjectsOfType<DebugConsole>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }
        #region State

        private bool visible;
        private int tabIndex;
        private Vector2 cardsScrollPos, relicsScrollPos, logScrollPos;
        private string cardSearchFilter = "", relicSearchFilter = "";
        private int goldAmount = 100, healAmount = 50, maxHpDelta = 10;
        private int strengthAmount = 3, dexterityAmount = 3;
        private int energyAmount = 3, blockAmount = 20, damageToEnemy = 50;
        private string logBuffer = "";
        private const int MaxLogLines = 200;
        private Font chineseFont; // 调试台中文显示用（IMGUI 默认字体无中文字形，运行时挂系统雅黑）
        private int logLineCount;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogReceived;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        private void OnGUI()
        {
            // Use OnGUI for key detection to bypass EventSystem
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.BackQuote || e.keyCode == KeyCode.F1))
            {
                if (IsAllowedByFile())
                {
                    visible = !visible;
                    e.Use();
                }
            }

            if (!visible)
            {
                // Small button in bottom-right when hidden（文件开关关闭时同样隐藏）
                if (IsAllowedByFile() && GUI.Button(new Rect(Screen.width - 110, Screen.height - 30, 110, 25), "调试 (F1/~)"))
                    visible = true;
                return;
            }

            EnsureChineseFont();

            GUI.color = new Color(0, 0, 0, 0.88f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            DrawHeader();
            DrawTabs();
            GUILayout.Space(4);
            switch (tabIndex)
            {
                case 0: DrawCardsTab(); break;
                case 1: DrawRelicsTab(); break;
                case 2: DrawBattleTab(); break;
                case 3: DrawPlayerTab(); break;
                case 4: DrawMapTab(); break;
                case 5: DrawLogTab(); break;
            }
            GUILayout.EndArea();
        }

        #endregion

        #region Header & Tabs

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            var ts = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            ts.normal.textColor = Color.cyan;
            GUILayout.Label("[ 调试控制台 ]", ts);
            GUILayout.FlexibleSpace();
            GUI.color = Color.gray;
            GUILayout.Label("按 ~ 或 F1 关闭");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUI.color = new Color(0.3f, 0.6f, 0.9f);
            GUILayout.Label(new string('-', 80));
            GUI.color = Color.white;
        }

        private void DrawTabs()
        {
            string[] tabs = { "卡牌", "遗物", "战斗", "玩家", "地图", "日志" };
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
            {
                GUI.color = tabIndex == i ? Color.green : new Color(0.6f, 0.6f, 0.6f);
                if (GUILayout.Button(tabs[i], GUILayout.Height(28), GUILayout.Width(75)))
                    tabIndex = i;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Rich Text Helper

        private static GUIStyle RichLabel
        {
            get
            {
                var s = new GUIStyle(GUI.skin.label);
                s.richText = true;
                return s;
            }
        }

        #endregion

        #region Tab 0: Cards

        private void DrawCardsTab()
        {
            GUILayout.Label("卡牌管理", RichLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("筛选:", GUILayout.Width(40));
            cardSearchFilter = GUILayout.TextField(cardSearchFilter, GUILayout.Width(180));
            if (GUILayout.Button("清除", GUILayout.Width(50)))
                cardSearchFilter = "";
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            cardsScrollPos = GUILayout.BeginScrollView(cardsScrollPos, GUILayout.Height(Screen.height * 0.60f));

            var allNames = CardData.GetAllCardNames();
            foreach (var cn in allNames)
            {
                string name = cn.ToString();
                if (!string.IsNullOrEmpty(cardSearchFilter) &&
                    name.IndexOf(cardSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var template = SafeGetTemplate(cn);
                string info = "";
                if (template != null)
                {
                    info = $"  [{template.rarity}] Cost:{template.cost}";
                    if (template.damage > 0) info += $" Dmg:{template.damage}";
                    if (template.block > 0) info += $" Blk:{template.block}";
                    if (template.magicNumber > 0) info += $" N:{template.magicNumber}";
                    if (template.faction != CardFaction.None) info += $" ({FactionDisplayName(template.faction)})";
                }

                GUILayout.BeginHorizontal();
                GUI.color = GetRarityColor(template?.rarity ?? CardRarity.Common);
                if (GUILayout.Button("[Add]", GUILayout.Width(40)))
                    GiveCard(cn);
                GUI.color = new Color(0.9f, 0.9f, 0.85f);
                GUILayout.Label($"{name}{info}", RichLabel);
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void GiveCard(CardName cardName)
        {
            var hm = HandManager.Instance;
            if (hm == null || !hm.IsInBattle())
            {
                GameLogger.LogWarning("[Debug] Not in battle, can't add card");
                return;
            }

            var card = CardData.CreateCard(cardName);
            if (card != null)
            {
                hm.AddCardToHand(card);
                hm.UpdateHandUI();
                GameLogger.Log($"[Debug] Added card: {card.cardName}");
            }
        }

        private static CardDataAsset SafeGetTemplate(CardName cardName)
        {
            var t = CardData.GetTemplate(cardName);
            return t;
        }

        #endregion

        #region Tab 1: Relics

        private void DrawRelicsTab()
        {
            GUILayout.Label("遗物管理器", RichLabel);

            var rm = RelicManager.Instance;

            GUILayout.BeginHorizontal();
            GUILayout.Label("随机添加:", GUILayout.Width(70));
            if (GUILayout.Button("普通", GUILayout.Width(40))) AddRandomRelic(RelicRarity.Common);
            if (GUILayout.Button("稀有", GUILayout.Width(40))) AddRandomRelic(RelicRarity.Rare);
            if (GUILayout.Button("传说", GUILayout.Width(40))) AddRandomRelic(RelicRarity.Legendary);
            if (GUILayout.Button("Boss", GUILayout.Width(50))) AddRandomRelic(RelicRarity.Special);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空全部", GUILayout.Width(80))) ClearAllRelics();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // 已拥有遗物
            GUILayout.Label("<b>已拥有遗物</b>", RichLabel);
            var owned = rm?.GetAllRelics();
            if (owned != null && owned.Count > 0)
            {
                foreach (var r in owned)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<color=yellow>{r.relicName}</color> <color=gray>[{r.relicId}]</color>", RichLabel);
                    if (GUILayout.Button("移除", GUILayout.Width(40)))
                        rm.RemoveRelic(r.relicId);
                    GUILayout.EndHorizontal();
                }
            }
            else GUILayout.Label("  (无)", RichLabel);

            GUILayout.Space(4);

            var assets = rm?.LoadAllRelicAssets();
            relicsScrollPos = GUILayout.BeginScrollView(relicsScrollPos, GUILayout.Height(Screen.height * 0.45f));

            // ── 阵营遗物：按阵营单独分区（非稀有度查询），添加时自动附带 Boss 激活器 ──
            GUILayout.Label("<b><color=#ffd24d>═══ 阵营遗物 ═══</color></b> <color=gray>（添加时自动附带对应 Boss 激活器，隐藏效果立即生效）</color>", RichLabel);
            if (assets != null)
            {
                foreach (var f in FactionOrder)
                {
                    var factionAssets = assets.Where(a => a.faction == f).ToList();
                    if (factionAssets.Count == 0) continue;
                    GUILayout.Space(2);
                    GUILayout.Label($"<b><color={FactionColorHex(f)}>◆ {FactionDisplayName(f)}阵营 ◆</color></b>", RichLabel);
                    foreach (var a in factionAssets)
                        DrawRelicAddRow(rm, assets, a, true);
                }
            }

            GUILayout.Space(6);

            // ── 全部遗物：原有列表，含隐藏效果的遗物依旧在此直接添加，不过滤阵营 ──
            GUILayout.Label("<b><color=#9adcff>═══ 全部遗物 ═══</color></b>", RichLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(40));
            relicSearchFilter = GUILayout.TextField(relicSearchFilter, GUILayout.Width(180));
            if (GUILayout.Button("清除", GUILayout.Width(50)))
                relicSearchFilter = "";
            GUILayout.EndHorizontal();

            // 调试台列出全部遗物（绕过阵营解锁过滤），否则未解锁阵营的遗物（如鲜血-吸血獠牙）无法添加
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (!string.IsNullOrEmpty(relicSearchFilter) &&
                        a.relicName.IndexOf(relicSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    DrawRelicAddRow(rm, assets, a, false);
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 遗物添加行：[+] 按钮 + 名称/稀有度；阵营行额外标出配套的 Boss 激活器遗物。
        /// </summary>
        private void DrawRelicAddRow(RelicManager rm, List<RelicDataAsset> assets, RelicDataAsset a, bool isFactionRow)
        {
            GUILayout.BeginHorizontal();
            GUI.color = GetRelicRarityColor(a.rarity);
            if (GUILayout.Button("[+]", GUILayout.Width(40)))
            {
                if (isFactionRow) AddFactionRelicWithActivator(rm, a);
                else AddSingleRelic(rm, a);
            }
            GUI.color = Color.white;
            string label = $"<color=white>{a.relicName}</color> <color=gray>[{RelicRarityText(a.rarity)}]</color>";
            if (isFactionRow && !string.IsNullOrEmpty(a.hiddenActivatorRelicId))
            {
                var activator = assets.FirstOrDefault(x => x.relicId == a.hiddenActivatorRelicId);
                label += $" <color=#ffa54d>↔ {(activator != null ? activator.relicName : a.hiddenActivatorRelicId)}</color>";
            }
            GUILayout.Label(label, RichLabel, GUILayout.Width(420));
            GUILayout.EndHorizontal();
        }

        /// <summary>单独添加一个遗物（全部遗物列表用，不做激活器联动）</summary>
        private void AddSingleRelic(RelicManager rm, RelicDataAsset asset)
        {
            if (rm == null) return;
            var r = rm.CreateRelicFromAsset(asset);
            if (r != null) rm.AddRelic(r);
        }

        /// <summary>
        /// 添加阵营遗物，并自动附带其 hiddenActivatorRelicId 指向的 Boss 激活器遗物，
        /// 确保隐藏效果（hiddenEffectIds）生效，避免缺少激活器导致添加无效。
        /// </summary>
        private void AddFactionRelicWithActivator(RelicManager rm, RelicDataAsset asset)
        {
            if (rm == null) return;

            var r = rm.CreateRelicFromAsset(asset);
            if (r != null)
            {
                rm.AddRelic(r);
                GameLogger.Log($"[调试台] 已添加阵营遗物「{asset.relicName}」");
            }

            if (string.IsNullOrEmpty(asset.hiddenActivatorRelicId)) return;

            if (rm.HasRelic(asset.hiddenActivatorRelicId))
            {
                GameLogger.Log($"[调试台] 「{asset.relicName}」的激活器遗物已拥有，无需重复添加");
                return;
            }

            var activator = rm.LoadAllRelicAssets().FirstOrDefault(x => x.relicId == asset.hiddenActivatorRelicId);
            if (activator == null)
            {
                GameLogger.LogWarning($"[调试台] 未找到激活器遗物「{asset.hiddenActivatorRelicId}」，请检查配置");
                return;
            }

            var ar = rm.CreateRelicFromAsset(activator);
            if (ar != null)
            {
                rm.AddRelic(ar);
                GameLogger.Log($"[调试台] 已自动添加 Boss 激活器遗物「{activator.relicName}」，隐藏效果生效");
            }
        }

        private void AddRandomRelic(RelicRarity rarity)
        {
            var rm = RelicManager.Instance;
            // 调试按钮用无过滤池：Boss 遗物在 Obtainable 池中被排除，会导致 Boss 按钮永远无效
            var assets = rm?.LoadAllRelicAssets();
            if (assets == null || assets.Count == 0) return;
            var pool = assets.Where(a => a.rarity == rarity).ToList();
            if (pool.Count == 0) return;
            var relic = rm.CreateRelicFromAsset(pool[UnityEngine.Random.Range(0, pool.Count)]);
            if (relic != null) rm.AddRelic(relic);
        }

        private void ClearAllRelics()
        {
            var rm = RelicManager.Instance;
            var owned = rm?.GetAllRelics();
            if (owned == null) return;
            foreach (var r in owned.ToList())
                rm.RemoveRelic(r.relicId);
            GameLogger.Log("[Debug] All relics cleared");
        }

        #endregion

        #region Tab 2: Battle

        private void DrawBattleTab()
        {
            GUILayout.Label("战斗作弊", RichLabel);

            var bm = FindObjectOfType<BattleManager>();
            var pdm = PlayerDataManager.Instance;
            var hm = HandManager.Instance;

            if (bm == null || pdm == null || hm == null)
            {
                GUILayout.Label("<color=red>不在战斗中</color>", RichLabel);
                return;
            }

            var pd = pdm.GetPlayerData();
            if (pd == null) return;

            int currentBlock = bm.GetPlayerBlock();

            // Player HP（调试台扣血按钮绕过无敌开关，便于测试）
            GUILayout.BeginHorizontal();
            GUILayout.Label($"HP: <color=green>{pd.currentHealth}</color>/{pd.maxHealth}", RichLabel, GUILayout.Width(180));
            if (GUILayout.Button($"+{healAmount}", GUILayout.Width(40))) pdm.Heal(healAmount);
            if (GUILayout.Button($"-{healAmount}", GUILayout.Width(40))) pdm.TakeDamage(healAmount, true);
            healAmount = ParseIntSafe(GUILayout.TextField(healAmount.ToString(), GUILayout.Width(35)));
            GUILayout.EndHorizontal();

            // 无敌开关
            GUILayout.BeginHorizontal();
            bool newInvincible = GUILayout.Toggle(PlayerDataManager.DebugInvincible, "无敌（敌人攻击不掉血）", GUILayout.Width(220));
            if (newInvincible != PlayerDataManager.DebugInvincible)
            {
                PlayerDataManager.DebugInvincible = newInvincible;
                GameLogger.Log(newInvincible ? "[调试台] 无敌已开启：敌人攻击不再扣除生命" : "[调试台] 无敌已关闭");
            }
            GUI.color = PlayerDataManager.DebugInvincible ? Color.green : Color.gray;
            GUILayout.Label(PlayerDataManager.DebugInvincible ? "● ON" : "○ OFF", GUILayout.Width(50));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Max HP
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MaxHP: {pd.maxHealth}", GUILayout.Width(180));
            if (GUILayout.Button($"+{maxHpDelta}", GUILayout.Width(40)))
            { pd.maxHealth += maxHpDelta; pd.currentHealth += maxHpDelta; }
            if (GUILayout.Button($"-{maxHpDelta}", GUILayout.Width(40)))
            { pd.maxHealth = Mathf.Max(1, pd.maxHealth - maxHpDelta); pd.currentHealth = Mathf.Min(pd.currentHealth, pd.maxHealth); }
            maxHpDelta = ParseIntSafe(GUILayout.TextField(maxHpDelta.ToString(), GUILayout.Width(35)));
            GUILayout.EndHorizontal();

            // Block
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Block: <color=cyan>{currentBlock}</color>", RichLabel, GUILayout.Width(180));
            if (GUILayout.Button($"+{blockAmount}", GUILayout.Width(40)))
            { bm.PlayerBlock(blockAmount); GameLogger.Log($"[Debug] Block +{blockAmount}"); }
            if (GUILayout.Button("清零", GUILayout.Width(40)))
            { if (currentBlock > 0) bm.ConsumePlayerBlock(currentBlock); }
            blockAmount = ParseIntSafe(GUILayout.TextField(blockAmount.ToString(), GUILayout.Width(35)));
            GUILayout.EndHorizontal();

            // Energy
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Energy: <color=yellow>{hm.GetCurrentEnergy()}</color>/{hm.GetMaxEnergy()}", RichLabel, GUILayout.Width(180));
            if (GUILayout.Button($"+{energyAmount}", GUILayout.Width(40))) hm.RestoreEnergy(energyAmount);
            energyAmount = ParseIntSafe(GUILayout.TextField(energyAmount.ToString(), GUILayout.Width(35)));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Buffs
            GUILayout.Label("<b>玩家 Buff</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"+{strengthAmount} 力量", GUILayout.Width(70)))
                pd.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            if (GUILayout.Button($"+{dexterityAmount} 敏捷", GUILayout.Width(70)))
                pd.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            if (GUILayout.Button("-力量", GUILayout.Width(50)))
                pd.AddBuff(new Buff { type = BuffType.Strength, amount = -strengthAmount, duration = -1 });
            if (GUILayout.Button("-敏捷", GUILayout.Width(50)))
                pd.AddBuff(new Buff { type = BuffType.Dexterity, amount = -dexterityAmount, duration = -1 });
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Enemy
            GUILayout.Label("<b>敌人控制</b>", RichLabel);
            var enemy = bm.GetCurrentEnemy();
            if (enemy != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{enemy.enemyName} HP: <color=red>{enemy.currentHealth}</color>/{enemy.maxHealth}", RichLabel, GUILayout.Width(280));
                if (GUILayout.Button($"-{damageToEnemy}", GUILayout.Width(50))) enemy.TakeDamage(damageToEnemy);
                if (GUILayout.Button("击杀", GUILayout.Width(50))) enemy.TakeDamage(enemy.currentHealth);
                damageToEnemy = ParseIntSafe(GUILayout.TextField(damageToEnemy.ToString(), GUILayout.Width(35)));
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("无敌人", RichLabel);

            GUILayout.Space(4);

            // Quick actions
            GUILayout.Label("<b>快捷操作</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("结束回合", GUILayout.Width(80))) hm.OnEndTurn();
            if (GUILayout.Button("抽 3 张", GUILayout.Width(60))) hm.DrawCards(3);
            if (GUILayout.Button("满能量", GUILayout.Width(70))) hm.RestoreEnergy(hm.GetMaxEnergy());
            if (GUILayout.Button("满生命", GUILayout.Width(70))) pdm.Heal(pd.maxHealth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("消耗全部手牌", GUILayout.Width(90))) hm.ExhaustHand();
            if (GUILayout.Button("弃全部手牌", GUILayout.Width(80))) hm.DiscardHand();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 3: Player

        private void DrawPlayerTab()
        {
            GUILayout.Label("玩家数据", RichLabel);

            var pdm = PlayerDataManager.Instance;
            if (pdm == null)
            {
                GUILayout.Label("<color=red>PlayerDataManager 未找到</color>", RichLabel);
                return;
            }

            var pd = pdm.GetPlayerData();
            if (pd == null) return;

            GUILayout.Label($"HP: {pd.currentHealth}/{pd.maxHealth}", RichLabel);
            GUILayout.Label($"Gold: {pd.gold}", RichLabel);
            GUILayout.Label($"牌组: {pdm.GetRuntimeDeckCopy()?.Count ?? 0} 张", RichLabel);

            GUILayout.Space(4);
            GUILayout.Label("<b>金币管理</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100", GUILayout.Width(50))) pdm.AddGold(100);
            if (GUILayout.Button("+500", GUILayout.Width(50))) pdm.AddGold(500);
            if (GUILayout.Button("+9999", GUILayout.Width(50))) pdm.AddGold(9999);
            if (GUILayout.Button("Clear", GUILayout.Width(50))) pd.gold = 0;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            goldAmount = ParseIntSafe(GUILayout.TextField(goldAmount.ToString(), GUILayout.Width(60)));
            if (GUILayout.Button("Set", GUILayout.Width(60))) pd.gold = goldAmount;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("<b>生命控制</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Full", GUILayout.Width(50))) pdm.Heal(pd.maxHealth);
            if (GUILayout.Button("+50", GUILayout.Width(50))) pdm.Heal(50);
            if (GUILayout.Button("-10", GUILayout.Width(50))) pdm.TakeDamage(10, true);
            if (GUILayout.Button("To 1", GUILayout.Width(50)))
            {
                int d = pd.currentHealth - 1;
                if (d > 0) pdm.TakeDamage(d, true);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("打印牌组", GUILayout.Width(140)))
            {
                var deck = pdm.GetRuntimeDeckCopy();
                if (deck != null)
                {
                    GameLogger.Log($"=== Deck ({deck.Count} cards) ===");
                    foreach (var c in deck)
                        GameLogger.Log($"  {c.cardName} [{c.cardType}] Cost:{c.cost}");
                }
            }

            GUILayout.Space(4);
            if (GUILayout.Button("重置全部（谨慎）", GUILayout.Width(140)))
            {
                pdm.ResetDeck();
                pdm.ResetData();
                GameLogger.Log("[Debug] Player data reset");
            }
        }

        #endregion

        #region Tab 4: Map

        private void DrawMapTab()
        {
            GUILayout.Label("地图控制", RichLabel);

            var gm = FindObjectOfType<GameManager>();
            if (gm == null)
            {
                GUILayout.Label("<color=red>GameManager 未找到</color>", RichLabel);
                return;
            }

            int cf = gm.GetCurrentFloor();
            int mf = gm.GetMaxFloor();
            GUILayout.Label($"楼层: {cf}/{mf}", RichLabel);
            GUILayout.Label($"进度: {gm.GetFloorProgress():P0}", RichLabel);
            GUILayout.Label($"战斗中: {gm.IsInBattle()}", RichLabel);
            GUILayout.Label($"移动中: {gm.IsMoving()}", RichLabel);

            GUILayout.Space(4);

            GUILayout.Label("<b>楼层操作</b>", RichLabel);
            if (GUILayout.Button("下一层", GUILayout.Width(140)))
            {
                if (cf < mf) gm.AdvanceToNextFloor();
            }

            GUILayout.Space(4);

            GUILayout.Label("<b>阵营</b>", RichLabel);
            var fs = FactionUnlockService.Instance;
            if (fs != null)
            {
                var allFactions = (CardFaction[])Enum.GetValues(typeof(CardFaction));
                GUILayout.BeginHorizontal();
                foreach (var f in allFactions)
                {
                    if (f == CardFaction.None) continue;
                    bool u = fs.IsFactionUnlocked(f);
                    GUI.color = u ? Color.green : Color.red;
                    if (GUILayout.Button($"{(u ? "√" : "×")} {FactionDisplayName(f)}", GUILayout.Width(75)))
                    { if (!u) fs.UnlockFaction(f); }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("FactionUnlockService 未找到", RichLabel);

            GUILayout.Space(4);

            GUILayout.Label("<b>重置操作</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("清空遗物", GUILayout.Width(100))) ClearAllRelics();
            var pdm = PlayerDataManager.Instance;
            if (GUILayout.Button("重置玩家", GUILayout.Width(100)))
            { pdm?.ResetDeck(); pdm?.ResetData(); GameLogger.Log("[Debug] Player reset"); }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 5: Log

        private bool autoScrollLog = true;

        private void DrawLogTab()
        {
            GUILayout.Label("运行时日志", RichLabel);

            GUILayout.BeginHorizontal();
            autoScrollLog = GUILayout.Toggle(autoScrollLog, "自动滚动");
            if (GUILayout.Button("清除", GUILayout.Width(50)))
            { logBuffer = ""; logLineCount = 0; }
            GUILayout.EndHorizontal();

            if (autoScrollLog)
                logScrollPos = new Vector2(0, float.MaxValue);

            logScrollPos = GUILayout.BeginScrollView(logScrollPos, GUILayout.Height(Screen.height * 0.65f));
            var ls = new GUIStyle(GUI.skin.label) { fontSize = 11, richText = true, wordWrap = true };
            GUILayout.Label(logBuffer, ls);
            GUILayout.EndScrollView();
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            string color = type switch
            {
                LogType.Error => "red",
                LogType.Warning => "yellow",
                LogType.Assert => "orange",
                _ => "white"
            };

            string time = DateTime.Now.ToString("HH:mm:ss");
            logBuffer += $"<color={color}>[{time}] {condition}</color>\n";
            logLineCount++;

            while (logLineCount > MaxLogLines)
            {
                int idx = logBuffer.IndexOf('\n');
                if (idx < 0) break;
                logBuffer = logBuffer.Substring(idx + 1);
                logLineCount--;
            }
        }

        #endregion

        #region Helpers

        private static int ParseIntSafe(string s)
        {
            return int.TryParse(s, out int v) ? v : 0;
        }

        private static Color GetRarityColor(CardRarity r)
        {
            return r switch
            {
                CardRarity.Common => new Color(0.95f, 0.95f, 0.95f),
                CardRarity.Uncommon => new Color(0.4f, 0.8f, 1f),
                CardRarity.Rare => new Color(1f, 0.9f, 0.2f),
                CardRarity.Legendary => new Color(1f, 0.55f, 0.1f),
                CardRarity.Colorless => new Color(0.7f, 0.7f, 0.7f),
                CardRarity.Cursed => new Color(0.8f, 0.3f, 0.8f),
                _ => Color.white
            };
        }

        private static Color GetRelicRarityColor(RelicRarity r)
        {
            return r switch
            {
                RelicRarity.Starting => new Color(1f, 0.6f, 0.2f),
                RelicRarity.Common => new Color(0.9f, 0.9f, 0.9f),
                RelicRarity.Rare => new Color(0.4f, 0.7f, 1f),
                RelicRarity.Legendary => new Color(1f, 0.5f, 0.1f),
                RelicRarity.Special => new Color(1f, 0.25f, 0.25f),
                _ => Color.white
            };
        }

        /// <summary>阵营遗物分区显示顺序（调试台用）</summary>
        private static readonly CardFaction[] FactionOrder =
        {
            CardFaction.Blood, CardFaction.Frost, CardFaction.Shadow,
            CardFaction.Slime, CardFaction.Corrupt, CardFaction.Reluctant
        };

        /// <summary>阵营主题色（中国特色配色：朱红/冰蓝/暗紫/凝绿/腐橙/鎏金）</summary>
        private static string FactionColorHex(CardFaction f)
        {
            switch (f)
            {
                case CardFaction.Blood: return "#e04848";
                case CardFaction.Frost: return "#5fc3e8";
                case CardFaction.Shadow: return "#a86fe0";
                case CardFaction.Slime: return "#7ddf7d";
                case CardFaction.Corrupt: return "#d07a2f";
                case CardFaction.Reluctant: return "#d9b45b";
                default: return "white";
            }
        }

        private static string FactionDisplayName(CardFaction f)
        {
            var fs = FactionUnlockService.Instance;
            return fs != null ? fs.GetFactionDisplayName(f) : f.ToString();
        }

        private static string RelicRarityText(RelicRarity r)
        {
            return r switch
            {
                RelicRarity.Starting => "初始",
                RelicRarity.Common => "普通",
                RelicRarity.Rare => "稀有",
                RelicRarity.Legendary => "传说",
                RelicRarity.Special => "Boss",
                _ => r.ToString()
            };
        }

        /// <summary>
        /// IMGUI 默认字体不含中文字形。Windows 目标平台直接挂系统雅黑动态字体，
        /// 保证调试台中文界面正常显示。
        /// </summary>
        private void EnsureChineseFont()
        {
            if (chineseFont == null)
                chineseFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "SimSun" }, 14);
            if (chineseFont != null && GUI.skin.font != chineseFont)
                GUI.skin.font = chineseFont;
        }

        #endregion
    }
}
