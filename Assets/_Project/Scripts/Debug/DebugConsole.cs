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
                visible = !visible;
                e.Use();
            }

            if (!visible)
            {
                // Small button in bottom-right when hidden
                if (GUI.Button(new Rect(Screen.width - 110, Screen.height - 30, 110, 25), "Debug (F1/~)"))
                    visible = true;
                return;
            }

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
            GUILayout.Label("[ Debug Console ]", ts);
            GUILayout.FlexibleSpace();
            GUI.color = Color.gray;
            GUILayout.Label("Press ~ to close");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUI.color = new Color(0.3f, 0.6f, 0.9f);
            GUILayout.Label(new string('-', 80));
            GUI.color = Color.white;
        }

        private void DrawTabs()
        {
            string[] tabs = { "Cards", "Relics", "Battle", "Player", "Map", "Log" };
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
            GUILayout.Label("Card Manager", RichLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(40));
            cardSearchFilter = GUILayout.TextField(cardSearchFilter, GUILayout.Width(180));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
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
                    if (template.faction != CardFaction.None) info += $" ({template.faction})";
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
            GUILayout.Label("Relic Manager", RichLabel);

            var rm = RelicManager.Instance;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Add:", GUILayout.Width(40));
            if (GUILayout.Button("Comm", GUILayout.Width(40))) AddRandomRelic(RelicRarity.Common);
            if (GUILayout.Button("Rare", GUILayout.Width(40))) AddRandomRelic(RelicRarity.Rare);
            if (GUILayout.Button("Legd", GUILayout.Width(40))) AddRandomRelic(RelicRarity.Legendary);
            if (GUILayout.Button("Boss", GUILayout.Width(50))) AddRandomRelic(RelicRarity.Special);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All", GUILayout.Width(80))) ClearAllRelics();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // Current relics
            GUILayout.Label("<b>Owned Relics</b>", RichLabel);
            var owned = rm?.GetAllRelics();
            if (owned != null && owned.Count > 0)
            {
                foreach (var r in owned)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<color=yellow>{r.relicName}</color> <color=gray>[{r.relicId}]</color>", RichLabel);
                    if (GUILayout.Button("X", GUILayout.Width(30)))
                        rm.RemoveRelic(r.relicId);
                    GUILayout.EndHorizontal();
                }
            }
            else GUILayout.Label("  (none)", RichLabel);

            GUILayout.Space(4);

            // All relics
            GUILayout.Label("<b>All Relics</b>", RichLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(40));
            relicSearchFilter = GUILayout.TextField(relicSearchFilter, GUILayout.Width(180));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                relicSearchFilter = "";
            GUILayout.EndHorizontal();

            relicsScrollPos = GUILayout.BeginScrollView(relicsScrollPos, GUILayout.Height(220));
            var assets = rm?.LoadAllObtainableRelicAssets();
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (!string.IsNullOrEmpty(relicSearchFilter) &&
                        a.relicName.IndexOf(relicSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    GUILayout.BeginHorizontal();
                    GUI.color = GetRelicRarityColor(a.rarity);
                    if (GUILayout.Button("[+]", GUILayout.Width(40)))
                    {
                        var r = rm.CreateRelicFromAsset(a);
                        if (r != null) rm.AddRelic(r);
                    }
                    GUI.color = Color.white;
                    GUILayout.Label($"<color=white>{a.relicName}</color> <color=gray>[{a.rarity}]</color>", RichLabel, GUILayout.Width(320));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        private void AddRandomRelic(RelicRarity rarity)
        {
            var rm = RelicManager.Instance;
            var assets = rm?.LoadAllObtainableRelicAssets();
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
            GUILayout.Label("Battle Cheats", RichLabel);

            var bm = FindObjectOfType<BattleManager>();
            var pdm = PlayerDataManager.Instance;
            var hm = HandManager.Instance;

            if (bm == null || pdm == null || hm == null)
            {
                GUILayout.Label("<color=red>Not in battle</color>", RichLabel);
                return;
            }

            var pd = pdm.GetPlayerData();
            if (pd == null) return;

            int currentBlock = bm.GetPlayerBlock();

            // Player HP
            GUILayout.BeginHorizontal();
            GUILayout.Label($"HP: <color=green>{pd.currentHealth}</color>/{pd.maxHealth}", RichLabel, GUILayout.Width(180));
            if (GUILayout.Button($"+{healAmount}", GUILayout.Width(40))) pdm.Heal(healAmount);
            if (GUILayout.Button($"-{healAmount}", GUILayout.Width(40))) pdm.TakeDamage(healAmount);
            healAmount = ParseIntSafe(GUILayout.TextField(healAmount.ToString(), GUILayout.Width(35)));
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
            if (GUILayout.Button("Clear", GUILayout.Width(40)))
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
            GUILayout.Label("<b>Player Buffs</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"+{strengthAmount} Str", GUILayout.Width(60)))
                pd.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            if (GUILayout.Button($"+{dexterityAmount} Dex", GUILayout.Width(60)))
                pd.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            if (GUILayout.Button("-Str", GUILayout.Width(40)))
                pd.AddBuff(new Buff { type = BuffType.Strength, amount = -strengthAmount, duration = -1 });
            if (GUILayout.Button("-Dex", GUILayout.Width(40)))
                pd.AddBuff(new Buff { type = BuffType.Dexterity, amount = -dexterityAmount, duration = -1 });
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Enemy
            GUILayout.Label("<b>Enemy Control</b>", RichLabel);
            var enemy = bm.GetCurrentEnemy();
            if (enemy != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{enemy.enemyName} HP: <color=red>{enemy.currentHealth}</color>/{enemy.maxHealth}", RichLabel, GUILayout.Width(280));
                if (GUILayout.Button($"-{damageToEnemy}", GUILayout.Width(50))) enemy.TakeDamage(damageToEnemy);
                if (GUILayout.Button("Kill", GUILayout.Width(50))) enemy.TakeDamage(enemy.currentHealth);
                damageToEnemy = ParseIntSafe(GUILayout.TextField(damageToEnemy.ToString(), GUILayout.Width(35)));
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("No enemy", RichLabel);

            GUILayout.Space(4);

            // Quick actions
            GUILayout.Label("<b>Quick Actions</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("End Turn", GUILayout.Width(80))) hm.OnEndTurn();
            if (GUILayout.Button("Draw 3", GUILayout.Width(60))) hm.DrawCards(3);
            if (GUILayout.Button("Full MP", GUILayout.Width(70))) hm.RestoreEnergy(hm.GetMaxEnergy());
            if (GUILayout.Button("Full HP", GUILayout.Width(70))) pdm.Heal(pd.maxHealth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Exhaust All", GUILayout.Width(90))) hm.ExhaustHand();
            if (GUILayout.Button("Discard All", GUILayout.Width(80))) hm.DiscardHand();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 3: Player

        private void DrawPlayerTab()
        {
            GUILayout.Label("Player Data", RichLabel);

            var pdm = PlayerDataManager.Instance;
            if (pdm == null)
            {
                GUILayout.Label("<color=red>PlayerDataManager not found</color>", RichLabel);
                return;
            }

            var pd = pdm.GetPlayerData();
            if (pd == null) return;

            GUILayout.Label($"HP: {pd.currentHealth}/{pd.maxHealth}", RichLabel);
            GUILayout.Label($"Gold: {pd.gold}", RichLabel);
            GUILayout.Label($"Deck: {pdm.GetRuntimeDeckCopy()?.Count ?? 0} cards", RichLabel);

            GUILayout.Space(4);
            GUILayout.Label("<b>Gold Management</b>", RichLabel);
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
            GUILayout.Label("<b>Health Control</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Full", GUILayout.Width(50))) pdm.Heal(pd.maxHealth);
            if (GUILayout.Button("+50", GUILayout.Width(50))) pdm.Heal(50);
            if (GUILayout.Button("-10", GUILayout.Width(50))) pdm.TakeDamage(10);
            if (GUILayout.Button("To 1", GUILayout.Width(50)))
            {
                int d = pd.currentHealth - 1;
                if (d > 0) pdm.TakeDamage(d);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("Print Deck", GUILayout.Width(140)))
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
            if (GUILayout.Button("Reset All (Caution)", GUILayout.Width(140)))
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
            GUILayout.Label("Map Control", RichLabel);

            var gm = FindObjectOfType<GameManager>();
            if (gm == null)
            {
                GUILayout.Label("<color=red>GameManager not found</color>", RichLabel);
                return;
            }

            int cf = gm.GetCurrentFloor();
            int mf = gm.GetMaxFloor();
            GUILayout.Label($"Floor: {cf}/{mf}", RichLabel);
            GUILayout.Label($"Progress: {gm.GetFloorProgress():P0}", RichLabel);
            GUILayout.Label($"In Battle: {gm.IsInBattle()}", RichLabel);
            GUILayout.Label($"Moving: {gm.IsMoving()}", RichLabel);

            GUILayout.Space(4);

            GUILayout.Label("<b>Floor Actions</b>", RichLabel);
            if (GUILayout.Button("Next Floor", GUILayout.Width(140)))
            {
                if (cf < mf) gm.AdvanceToNextFloor();
            }

            GUILayout.Space(4);

            GUILayout.Label("<b>Factions</b>", RichLabel);
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
                    if (GUILayout.Button($"{(u ? "V" : "X")} {f}", GUILayout.Width(75)))
                    { if (!u) fs.UnlockFaction(f); }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("FactionUnlockService not found", RichLabel);

            GUILayout.Space(4);

            GUILayout.Label("<b>Reset Actions</b>", RichLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Relics", GUILayout.Width(100))) ClearAllRelics();
            var pdm = PlayerDataManager.Instance;
            if (GUILayout.Button("Reset Player", GUILayout.Width(100)))
            { pdm?.ResetDeck(); pdm?.ResetData(); GameLogger.Log("[Debug] Player reset"); }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 5: Log

        private bool autoScrollLog = true;

        private void DrawLogTab()
        {
            GUILayout.Label("Runtime Log", RichLabel);

            GUILayout.BeginHorizontal();
            autoScrollLog = GUILayout.Toggle(autoScrollLog, "Auto Scroll");
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
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

        #endregion
    }
}
