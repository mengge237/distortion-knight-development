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
            // 控制台开关统一由 DevConfig 判定（debug_config.json 的 consoleEnabled，
            // 上架包在文件中置 true 即可开启控制台继续调试；兼容旧 debug_enable 标记文件）
            if (!DevConfig.ConsoleEnabled)
            {
                UnityEngine.Debug.Log($"[DebugConsole] 调试台未启用（编辑 {DevConfig.FilePath} 将 consoleEnabled 置 true 可开启）");
                return;
            }
            if (FindObjectOfType<DebugConsole>() != null) return;
            var go = new GameObject("[DebugConsole]");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugConsole>();
            UnityEngine.Debug.Log("[DebugConsole] Debug console created, press ~ or F1 to open");
        }

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
        private string commandInput = ""; // 图鉴命令输入框（以撒式 give/see/list）

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
            // 控制台总开关（debug_config.json consoleEnabled；上架包默认可关）
            if (!DevConfig.ConsoleEnabled)
            {
                visible = false;
                return;
            }

            // Use OnGUI for key detection to bypass EventSystem
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.BackQuote || e.keyCode == KeyCode.F1))
            {
                visible = !visible;
                e.Use();
            }

            if (!visible)
            {
                // Small button in bottom-right when hidden
                if (GUI.Button(new Rect(Screen.width - 110, Screen.height - 30, 110, 25), "调试 (F1/~)"))
                    visible = true;
                return;
            }

            EnsureChineseFont();

            GUI.color = new Color(0, 0, 0, 0.88f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            DrawHeader();
            DrawCommandLine();
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

            // ── 稀有度式双列布局 ──
            // 左列「阵营遗物」：按阵营分区，隐藏效果遗物作为子集显示，Boss解锁器仅此列显示；
            // 右列「常规遗物」：仅无阵营协同的遗物正常显示。
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(440));
            GUILayout.Label("<b><color=#ffd24d>═══ 阵营遗物 ═══</color></b>", RichLabel);
            GUILayout.Label("<color=gray>隐藏效果为子集；Boss解锁器仅此列显示（不作为常规显示）</color>", RichLabel);
            if (assets != null)
            {
                foreach (var f in FactionOrder)
                {
                    var factionAssets = assets.Where(a => a.faction == f).ToList();
                    if (factionAssets.Count == 0) continue;
                    GUILayout.Space(2);
                    GUILayout.Label($"<b><color={FactionColorHex(f)}>◆ {FactionDisplayName(f)}阵营 ◆</color></b>", RichLabel);

                    // ① 常规阵营遗物（无隐藏效果）
                    foreach (var a in factionAssets.Where(a => !a.isFactionUnlocker && !HasHiddenEffect(a)))
                        DrawRelicAddRow(rm, assets, a, true);

                    // ② 隐藏效果子集（需对应 Boss 激活器后生效）
                    var hiddenOnes = factionAssets.Where(a => !a.isFactionUnlocker && HasHiddenEffect(a)).ToList();
                    if (hiddenOnes.Count > 0)
                    {
                        GUILayout.Label("　└ <color=#ffa54d>隐藏效果子集（需Boss激活器）</color>", RichLabel);
                        foreach (var a in hiddenOnes)
                            DrawRelicAddRow(rm, assets, a, true, "　　", true);
                    }

                    // ③ Boss解锁器（仅Boss战后可选，不作为常规显示）
                    var unlockers = factionAssets.Where(a => a.isFactionUnlocker).ToList();
                    if (unlockers.Count > 0)
                    {
                        GUILayout.Label("　└ <color=#ff7b7b>Boss解锁器（仅Boss战后选择）</color>", RichLabel);
                        foreach (var a in unlockers)
                            DrawRelicAddRow(rm, assets, a, false, "　　");
                    }
                }

                // 兜底：阵营为None的Boss解锁器也显示在左列
                var orphanUnlockers = assets.Where(a => a.isFactionUnlocker && a.faction == CardFaction.None).ToList();
                if (orphanUnlockers.Count > 0)
                {
                    GUILayout.Label("<b><color=#ff7b7b>◆ 通用Boss解锁器 ◆</color></b>", RichLabel);
                    foreach (var a in orphanUnlockers)
                        DrawRelicAddRow(rm, assets, a, false);
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(440));
            GUILayout.Label("<b><color=#9adcff>═══ 常规遗物 ═══</color></b>", RichLabel);
            GUILayout.Label("<color=gray>无阵营协同的遗物正常显示；阵营遗物与Boss解锁器不在此列</color>", RichLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(40));
            relicSearchFilter = GUILayout.TextField(relicSearchFilter, GUILayout.Width(180));
            if (GUILayout.Button("清除", GUILayout.Width(50)))
                relicSearchFilter = "";
            GUILayout.EndHorizontal();

            if (assets != null)
            {
                foreach (var a in assets)
                {
                    // 阵营遗物与Boss解锁器只显示在左列
                    if (a.faction != CardFaction.None || a.isFactionUnlocker) continue;
                    if (!string.IsNullOrEmpty(relicSearchFilter) &&
                        a.relicName.IndexOf(relicSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    DrawRelicAddRow(rm, assets, a, false);
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        /// <summary>判断遗物是否带隐藏效果（以 RelicBalanceConfig 为准，资产字段兜底）。</summary>
        private static bool HasHiddenEffect(RelicDataAsset a)
        {
            if (a == null) return false;
            var cfg = RelicBalanceConfig.CreateDefaultConfig().GetEntry(a.relicId);
            if (cfg != null && (!string.IsNullOrEmpty(cfg.hiddenActivatorRelicId) ||
                                (cfg.hiddenEffectIds != null && cfg.hiddenEffectIds.Count > 0)))
                return true;
            return !string.IsNullOrEmpty(a.hiddenActivatorRelicId) ||
                   (a.hiddenEffectIds != null && a.hiddenEffectIds.Count > 0);
        }

        /// <summary>
        /// 遗物添加行：[+] 按钮 + 名称/稀有度；阵营行额外标出配套的 Boss 激活器遗物。
        /// </summary>
        private void DrawRelicAddRow(RelicManager rm, List<RelicDataAsset> assets, RelicDataAsset a,
                                     bool isFactionRow, string indent = "", bool isHiddenRow = false)
        {
            GUILayout.BeginHorizontal();
            GUI.color = GetRelicRarityColor(a.rarity);
            if (GUILayout.Button("[+]", GUILayout.Width(40)))
            {
                if (isFactionRow) AddFactionRelicWithActivator(rm, a);
                else AddSingleRelic(rm, a);
            }
            GUI.color = Color.white;
            string label = indent + $"<color=white>{a.relicName}</color> <color=gray>[{RelicRarityText(a.rarity)}]</color>";
            if (isHiddenRow)
                label += " <color=#ffa54d>[隐藏]</color>";
            if (isFactionRow)
            {
                // 激活器以资产字段为准，缺失时回退到平衡配置（如老资产血护符）
                string activatorId = a.hiddenActivatorRelicId;
                if (string.IsNullOrEmpty(activatorId))
                {
                    var cfg = RelicBalanceConfig.CreateDefaultConfig().GetEntry(a.relicId);
                    if (cfg != null) activatorId = cfg.hiddenActivatorRelicId;
                }
                if (!string.IsNullOrEmpty(activatorId))
                {
                    var activator = assets.FirstOrDefault(x => x.relicId == activatorId);
                    label += $" <color=#ffa54d>↔ {(activator != null ? activator.relicName : activatorId)}</color>";
                }
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

            // 激活器以资产字段为准，缺失时回退到平衡配置（老资产如血护符）
            string activatorId = asset.hiddenActivatorRelicId;
            if (string.IsNullOrEmpty(activatorId))
            {
                var cfg = RelicBalanceConfig.CreateDefaultConfig().GetEntry(asset.relicId);
                if (cfg != null) activatorId = cfg.hiddenActivatorRelicId;
            }
            if (string.IsNullOrEmpty(activatorId)) return;

            if (rm.HasRelic(activatorId))
            {
                GameLogger.Log($"[调试台] 「{asset.relicName}」的激活器遗物已拥有，无需重复添加");
                return;
            }

            var activator = rm.LoadAllRelicAssets().FirstOrDefault(x => x.relicId == activatorId);
            if (activator == null)
            {
                GameLogger.LogWarning($"[调试台] 未找到激活器遗物「{activatorId}」，请检查配置");
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

            AppendLogLine($"<color={color}>[{DateTime.Now:HH:mm:ss}] {condition}</color>");
        }

        /// <summary>追加一行日志并裁剪行数上限。</summary>
        private void AppendLogLine(string line)
        {
            logBuffer += line + "\n";
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

        #region 图鉴命令（以撒式 give/see/list）

        private const string CommandInputName = "DebugCommandInput";

        private void DrawCommandLine()
        {
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("命令:", GUILayout.Width(45));
            GUI.SetNextControlName(CommandInputName);
            commandInput = GUILayout.TextField(commandInput, GUILayout.Height(26));
            bool submitted = GUILayout.Button("执行", GUILayout.Width(60), GUILayout.Height(26));
            // 输入框内回车提交
            if (!submitted && Event.current != null && Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == CommandInputName)
            {
                submitted = true;
                Event.current.Use();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("<color=gray>k5 / r7 / p3 直接获得物品（k=卡牌 r=遗物 p=药水）· 输入 help 查看全部命令</color>", RichLabel);
            GUILayout.Space(4);

            if (submitted && !string.IsNullOrWhiteSpace(commandInput))
                ExecuteCommand(commandInput);
        }

        private void ExecuteCommand(string cmdLine)
        {
            string cmd = cmdLine.Trim();
            commandInput = "";
            if (string.IsNullOrEmpty(cmd))
            {
                PrintCommandHelp();
                return;
            }

            string[] parts = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string verb = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? string.Join(" ", parts, 1, parts.Length - 1) : "";

            // 裸数字 → 类别歧义已停用，提示前缀形式
            if (parts.Length == 1 && int.TryParse(parts[0], out _))
            {
                CmdLog($"裸数字无法判断类别，请用前缀形式：k{parts[0]}（卡牌）/ r{parts[0]}（遗物）/ p{parts[0]}（药水）");
                return;
            }

            switch (verb)
            {
                case "help":
                case "帮助":
                    PrintCommandHelp();
                    break;
                case "give":
                case "给":
                    CmdGive(arg);
                    break;
                case "see":
                case "见":
                    CmdSee(arg);
                    break;
                case "seeall":
                    CmdSeeAll();
                    break;
                case "list":
                case "列表":
                    CmdList(arg);
                    break;
                case "devmode":
                case "开发者":
                    CmdDevMode(arg);
                    break;
                default:
                    // 未知动词 → 若整体可解析为前缀编号/物品名称则按 give 处理（如直接输入 k5 或"回春"）
                    if (CodexIdRegistry.TryResolve(cmd, out _, out _))
                        CmdGive(cmd);
                    else
                        CmdLog($"未知命令：\"{cmd}\"（输入 help 查看用法）");
                    break;
            }
        }

        private void CmdGive(string arg)
        {
            if (!CodexIdRegistry.TryResolve(arg, out CodexCategory cat, out int id))
            {
                CmdLog($"无法解析：\"{arg}\"（list 查看编号，help 查看用法）");
                return;
            }

            switch (cat)
            {
                case CodexCategory.Card: GiveCardById(id); break;
                case CodexCategory.Relic: GiveRelicById(id); break;
                case CodexCategory.Potion: GivePotionById(id); break;
            }
        }

        private void GiveCardById(int id)
        {
            var asset = CodexIdRegistry.GetCard(id);
            if (asset == null) { CmdLog($"卡牌 {CodexIds.Format(CodexCategory.Card, id)} 无对应资产"); return; }
            var hm = HandManager.Instance;
            if (hm == null || !hm.IsInBattle()) { CmdLog("需进入战斗后使用（首页无法获得卡牌）"); return; }
            var card = CardData.CreateCardFromAsset(asset);
            if (card == null) { CmdLog($"创建卡牌失败：{asset.cardName}"); return; }
            hm.AddCardToHand(card);
            hm.UpdateHandUI();
            CmdLog($"已获得卡牌 {CodexIds.Format(CodexCategory.Card, id)} 「{asset.cardName}」");
        }

        private void GiveRelicById(int id)
        {
            var asset = CodexIdRegistry.GetRelic(id);
            if (asset == null) { CmdLog($"遗物 {CodexIds.Format(CodexCategory.Relic, id)} 无对应资产"); return; }
            var rm = RelicManager.Instance;
            if (rm == null) { CmdLog("需进入战斗后使用（首页无法获得遗物）"); return; }
            var relic = rm.CreateRelicFromAsset(asset);
            if (relic == null) { CmdLog($"创建遗物失败：{asset.relicName}"); return; }
            rm.AddRelic(relic);
            CmdLog($"已获得遗物 {CodexIds.Format(CodexCategory.Relic, id)} 「{asset.relicName}」");
        }

        private void GivePotionById(int id)
        {
            var asset = CodexIdRegistry.GetPotion(id);
            if (asset == null) { CmdLog($"药水 {CodexIds.Format(CodexCategory.Potion, id)} 无对应资产"); return; }
            var pdm = PlayerDataManager.Instance;
            var pd = pdm?.GetPlayerData();
            if (pd == null) { CmdLog("需进入战斗后使用（首页无法获得药水）"); return; }
            var potion = new Potion(asset.potionId, asset.potionName, asset.rarity, asset.description, asset.price);
            if (!pd.AddPotion(potion)) { CmdLog($"药水已满（{pd.maxPotions}），无法添加"); return; }
            CmdLog($"已获得药水 {CodexIds.Format(CodexCategory.Potion, id)} 「{asset.potionName}」");
        }

        private void CmdSee(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) { CmdLog("用法：see <前缀编号|名称>（如 see r7 / see 不舍锁链）"); return; }
            if (!CodexIdRegistry.TryResolve(arg, out CodexCategory cat, out int id))
            {
                CmdLog($"无法解析：\"{arg}\"");
                return;
            }
            if (CodexProgress.UnlockOne(cat, id))
                CmdLog($"已解锁图鉴 {CodexIds.Format(cat, id)}");
        }

        /// <summary>开发者模式开关：图鉴是否隐藏未见过条目，写入 debug_config.json 持久化。</summary>
        private void CmdDevMode(string arg)
        {
            bool? on = arg.Trim().ToLowerInvariant() switch
            {
                "1" or "on" or "true" or "开" => true,
                "0" or "off" or "false" or "关" => false,
                _ => (bool?)null
            };
            if (on == null)
            {
                CmdLog("用法：devmode 1|0（开发者模式=图鉴显示全部条目，不隐藏未见过内容）");
                return;
            }
            DevConfig.SetDevMode(on.Value);
            CmdLog($"开发者模式已{(on.Value ? "开启" : "关闭")}（已写入 {DevConfig.FilePath}）");
        }

        private void CmdSeeAll()
        {
            CodexProgress.Instance.UnlockAll();
        }

        private void CmdList(string arg)
        {
            string[] parts = (arg ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            CodexCategory? cat = parts.Length > 0
                ? parts[0].ToLowerInvariant() switch
                {
                    "card" or "卡牌" => CodexCategory.Card,
                    "relic" or "遗物" => CodexCategory.Relic,
                    "potion" or "药水" => CodexCategory.Potion,
                    _ => (CodexCategory?)null
                }
                : null;

            if (cat == null)
            {
                CmdLog("用法：list card|relic|potion [关键词]（✓=图鉴已见）");
                return;
            }

            string filter = parts.Length > 1 ? string.Join(" ", parts, 1, parts.Length - 1) : "";
            var cp = CodexProgress.Instance;
            int shown = 0;
            switch (cat.Value)
            {
                case CodexCategory.Card:
                    foreach (var a in CodexIdRegistry.GetCardsByIdOrdered())
                    {
                        if (!string.IsNullOrEmpty(filter) && a.cardName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        CmdLog($"{CodexIds.Format(CodexCategory.Card, a.codexId)} {(cp.IsCardSeen(a.codexId) ? "✓" : "✗")} {a.cardName}");
                        shown++;
                    }
                    break;
                case CodexCategory.Relic:
                    foreach (var a in CodexIdRegistry.GetRelicsByIdOrdered())
                    {
                        if (!string.IsNullOrEmpty(filter) && a.relicName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        CmdLog($"{CodexIds.Format(CodexCategory.Relic, a.codexId)} {(cp.IsRelicSeen(a.codexId) ? "✓" : "✗")} {a.relicName}");
                        shown++;
                    }
                    break;
                case CodexCategory.Potion:
                    foreach (var a in CodexIdRegistry.GetPotionsByIdOrdered())
                    {
                        if (!string.IsNullOrEmpty(filter) && a.potionName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        CmdLog($"{CodexIds.Format(CodexCategory.Potion, a.codexId)} {(cp.IsPotionSeen(a.codexId) ? "✓" : "✗")} {a.potionName}");
                        shown++;
                    }
                    break;
            }
            CmdLog($"共 {shown} 条");
        }

        private void PrintCommandHelp()
        {
            CmdLog("=== 图鉴命令（以撒式前缀编号）===");
            CmdLog("k5 / r7 / p3      直接获得对应编号物品（k=卡牌 r=遗物 p=药水）");
            CmdLog("give <编号|名称>  获得物品，如 give k5 / give r7 / give 遗物 不舍锁链");
            CmdLog("see <编号|名称>   仅解锁图鉴条目（不获得），如 see r7");
            CmdLog("seeall           解锁全部图鉴条目");
            CmdLog("list card|relic|potion [关键词]  列出编号（✓=已见）");
            CmdLog("devmode 1|0      开发者模式开关（图鉴显示全部/隐藏未见过，写入配置文件）");
            CmdLog("help             显示本帮助");
            CmdLog($"调试配置文件：{DevConfig.FilePath}");
        }

        /// <summary>命令结果输出到日志缓冲并自动切到日志页签。</summary>
        private void CmdLog(string msg)
        {
            AppendLogLine($"<color=#8affa0>[{DateTime.Now:HH:mm:ss}] {msg}</color>");
            tabIndex = 5; // 日志页签
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
