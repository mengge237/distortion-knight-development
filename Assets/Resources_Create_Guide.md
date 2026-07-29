# 《畸变骑士》Unity 资源创建清单

> **项目名称**：畸变骑士 (Mutation Chess)
>
> **重要说明**
> - 所有 .asset 文件名必须与配置中的 effectId/cardName 完全一致
> - 保存路径影响 Resources.Load 的查找，必须放在指定目录
> - 创建方法：在 Project 窗口右键 → Create → 对应菜单路径 → 重命名为指定文件名
> - **稀有度规则**：CardRarity = Common/Uncommon/Rare/Legendary/Colorless/Cursed；没有Mythic
> - **遗物稀有度** = Starting(起始) / Common(普通) / Rare(稀有) / Legendary(传说) / Special(特殊，Boss遗物)
> - **诅咒稀有度** = Cursed（负面卡，无法主动获得，由事件/宝箱注入牌组）
> - **诅咒卡效果触发**：由 HandManager 在特定时机检查手牌中的诅咒卡并触发
>   - 衰败：回合结束损失HP | 迷雾：手牌上限-1 | 枷锁：每回合抽牌-1 | 噬命：打出其他卡时-1HP | 虚耗：纯占位

---

## 一、效果资源 (.asset)

保存目录：`Assets/TextMesh Pro/Resources/Effects/`

### 1.1 已有效果（需重命名，文件名与 effectId 不匹配）

| 当前文件名 | 应改为(effectId) | 菜单路径 |
|---|---|---|
| DealDamage | DealDamageEffect | MutationChess/Effects/Deal Damage |
| ApplyBlock | ApplyBlockEffect | MutationChess/Effects/Apply Block |
| DrawCards | DrawCardsEffect | MutationChess/Effects/Draw Cards |
| HealPlayer | HealPlayerEffect | MutationChess/Effects/Heal Player |
| ApplyDexterity | ApplyDexterityEffect | MutationChess/Effects/Apply Dexterity |
| ApplyVulnerabilityEffect | ApplyVulnerabilityEffect | 无需改名 |
| ApplyWeakEffect | ApplyWeakEffect | 无需改名 |
| ApplyTemporaryStrength | ApplyTemporaryStrengthEffect | MutationChess/Effects/Apply Temporary Strength |
| DealDamageNextTurn | DealDamageNextTurnEffect | MutationChess/Effects/Deal Damage Next Turn |
| DiscardRandomCard | DiscardRandomCardEffect | MutationChess/Effects/Discard Random Card |
| ExhaustCard | ExhaustCardEffect | MutationChess/Effects/Exhaust Card |
| ModifyCost | ModifyCostEffect | MutationChess/Effects/Modify Cost |

### 1.2 需要新建的效果 .asset

#### 卡牌效果（10个）

| # | .asset文件名 | 菜单路径 | 参数设置 | 用途 |
|---|---|---|---|---|
| 1 | AddCardToDeckEffect | MutationChess/Card Effects/Add Card To Deck | selectionMode=RandomByTag, filterTag=Corrupt, count=2 | 暗影腐化卡 |
| 2 | InspectEffect | MutationChess/Card Effects/Inspect | inspectCount=3 | 预知仪式/预知 |
| 3 | DiscoverEffect | MutationChess/Card Effects/Discover | discoverCount=3 | 探索/神秘卷轴 |
| 4 | GiftEffect | MutationChess/Card Effects/Gift | — | 礼物之力 |
| 5 | TreasureEffect | MutationChess/Card Effects/Treasure | count=2 | 宝藏卡 |
| 6 | ApplyThornsEffect | MutationChess/Card Effects/Apply Thorns | thornsAmount=3 | 寒霜反击 |
| 7 | ReduceStrengthEffect | MutationChess/Card Effects/Reduce Strength | reduceAmount=3 | 粘腻爱意 |
| 8 | ApplyShadowStrengthEffect | MutationChess/Effects/Apply Shadow Strength | strengthAmount=2 | 暗影蓄力/影舞 |
| 9 | ShadowBurstEffect | MutationChess/Effects/Shadow Burst | damageMultiplier=2 | 暗影爆发 |
| 10 | BlockToAttackEffect | MutationChess/Effects/Block To Attack | multiplier=2 | 霜影斩 |

#### 6个系列 Boss 遗物核心效果（Boss专属·仅Boss掉落·激活系列隐藏效果）

| # | .asset文件名 | 菜单路径 | 参数设置 | 对应Boss遗物（6系） |
|---|---|---|---|---|
| B1 | BossBloodVeinEffect | MutationChess/Relic Effects/Boss/Blood Vein | maxHp=-5, strengthPerMaxHp=0.5 | Boss_鲜血脉络 |
| B2 | BossFrostHeartEffect | MutationChess/Relic Effects/Boss/Frost Heart | dexterity=1, frostBonusBlock=1 | Boss_寒霜之心 |
| B3 | BossCorruptLiverEffect | MutationChess/Relic Effects/Boss/Corrupt Liver | energyOnExhaust=1, drawOnExhaust=1 | Boss_腐化肝脏 |
| B4 | BossSlimeGlandEffect | MutationChess/Relic Effects/Boss/Slime Gland | slimePerTurn=3, debuffStacks=1 | Boss_粘液腺体 |
| B5 | BossReluctantChainEffect | MutationChess/Relic Effects/Boss/Reluctant Chain | hpOnExhaustDraw=1 | Boss_不舍锁链 |
| B6 | BossMemoryLensEffect | MutationChess/Relic Effects/Boss/Memory Lens | shadowDmg=3, startBlock=15, loseDexPer4Turns=1 | Boss_记忆晶状体（暗影Boss·每4回合-1敏捷负面） |

#### 2026新增 - 遗物改动效果（16个 · 替换掉旧版解锁器遗物）

| # | .asset文件名 | 菜单路径 | 参数 | 对应遗物 |
|---|---|---|---|---|
| N1 | AttackHeal1Effect | MutationChess/Relic Effects/Attack Heal 1 | healAmount=1 | 吸血獠牙（使用攻击牌+1HP） |
| N2 | BloodAltarEffect | MutationChess/Relic Effects/Blood Altar | healthCost=3, strengthGain=2 | 血祭坛（失3血→2力量） |
| N3 | BloodAltarBoostedEffect | MutationChess/Relic Effects/Blood Altar Boosted | healthCost=-1, strengthGain=+1 | 血祭坛隐藏（有Boss_鲜血脉络后） |
| N4 | FrostPermafrostEffect | MutationChess/Relic Effects/Frost Permafrost | startBlock=15 | 永冻土 |
| N5 | FrostBonusBlockEffect | MutationChess/Relic Effects/Frost Bonus Block | blockPerFrostCard=8 | 霜巨人（寒霜牌+8格挡） |
| N6 | FrostSnowflakeEffect | MutationChess/Relic Effects/Frost Snowflake | startBlock=10 | 雪符 |
| N7 | ShadowCardBonusDamageEffect | MutationChess/Relic Effects/Shadow Card Bonus Dmg | bonus=3 | 记忆晶状体Boss（暗影牌+3伤害） |
| N8 | Every4TurnsLoseDexEffect | MutationChess/Relic Effects/Every 4 Turns Lose Dex | loseDex=1, turns=4 | 记忆晶状体Boss负面 |
| N9 | ReluctantBonusDrawEffect | MutationChess/Relic Effects/Reluctant Bonus Draw | drawPerCard=1 | 回响戒（不舍牌+1抽） |
| N10 | ReluctantBlockBonusEffect | MutationChess/Relic Effects/Reluctant Block Bonus | blockPerCard=2 | 怀旧链（不舍牌+2格挡） |
| N11 | PhantomAfter5CardsEffect | MutationChess/Relic Effects/Phantom After 5 Cards | cardThreshold=5, dmgReduction=5 | 幻影面具（5张后减伤5） |
| N12 | PhantomAfter5CardsBoostEffect | MutationChess/Relic Effects/Phantom Boost | extraReduction=3 | 幻影面具隐藏效果（有Boss记忆晶状体） |
| N13 | AbyssEvery4AttackEffect | MutationChess/Relic Effects/Abyss Every 4 Attack | threshold=4, dmgMultiplier=2 | 深渊凝视（4张攻击后翻倍） |
| N14 | AbyssReduceThresholdEffect | MutationChess/Relic Effects/Abyss Reduce Threshold | reduce=1 | 深渊凝视隐藏（3张触发） |
| N15 | AcidicCoreDebuff3StacksEffect | MutationChess/Relic Effects/Acidic Core 3 Stacks | weak=3, frail=3, vulnerable=3 | 酸核（3种各3层debuff） |
| N16 | AcidicCoreBoostedEffect | MutationChess/Relic Effects/Acidic Core Boost | extraStacks=1 | 酸核隐藏（拥有Boss粘液腺体后+1层） |
| N17 | ChessMasterEvery6CardsStrengthEffect | MutationChess/Relic Effects/Chess Master 6 Cards | threshold=6, strengthGain=1 | 棋王冠（每6张牌→+1本局力量） |
| N18 | EliteVictoryExtraCardGroupEffect | MutationChess/Relic Effects/Elite Extra Cards | extraGroup=1 | 宝箱（精英战胜利额外一组卡奖励）（BattleManager.cs里也有实现，可以留空） |
| N19 | EternalFlameBattleStartEffect | MutationChess/Relic Effects/Eternal Flame | str=2, dex=2, block=10, energy=2 | 永焰（开局buff一次性） |
| N20 | EnergyCoreBattleStartEffect | MutationChess/Relic Effects/Energy Core | energy=2 | 能核（开局+2能量） |
| N21 | DrawingPadDraw2Effect | MutationChess/Relic Effects/Drawing Pad Draw 2 | draw=2 | 画板（开局抽2） |
| N22 | ShopRestockEffect | MutationChess/Relic Effects/Shop Restock | — | 补货符（商店不会卖空） |
| N23 | GainEnergyNextTurnEffect | MutationChess/Effects/Gain Energy Next Turn | energyGain=1 | 蓄势/凝神（下回合+1能量） |

#### 诅咒卡效果（5个 · Cursed稀有度 · 无法打出 · 被动触发）

| # | .asset文件名 | 菜单路径 | 参数设置 | 用途 |
|---|---|---|---|---|
| C1 | CurseDecayEffect | MutationChess/Curse Effects/Decay | hpLossPerTurn=1 | 诅咒_衰败（手牌中每回合-1HP） |
| C2 | CurseFogEffect | MutationChess/Curse Effects/Fog | handSizeReduction=1 | 诅咒_迷雾（手牌上限-1） |
| C3 | CurseChainsEffect | MutationChess/Curse Effects/Chains | drawReduction=1 | 诅咒_枷锁（每回合抽牌-1） |
| C4 | CurseDevourEffect | MutationChess/Curse Effects/Devour | hpLossPerCard=1 | 诅咒_噬命（打出其他卡时-1HP） |
| C5 | CurseVoidEffect | MutationChess/Curse Effects/Void | — | 诅咒_虚耗（占位，营地可移除） |

#### 系列遗物效果（需补建 12个 · 隐藏效果激活条件系Boss遗物）

| # | .asset文件名 | 菜单路径 | 参数设置 | 对应遗物 |
|---|---|---|---|---|
| 1 | SlimeWeakEffect | MutationChess/Relic Effects/Slime Weak | weakAmount=1 | 粘液手套 |
| 2 | GoldBonusEffect | MutationChess/Relic Effects/Gold Bonus | goldBonusMultiplier=0.2 | 金偶像 |
| 3 | GainStrengthBattleStartEffect | MutationChess/Relic Effects/Gain Strength Battle Start | strengthAmount=2 | 血契 |
| 4 | GainDexterityBattleStartEffect | MutationChess/Relic Effects/Gain Dexterity Battle Start | dexterityAmount=1 | 铜盾 / 霜巨人（开局敏捷） |
| 5 | MaxHealthSmallEffect | MutationChess/Relic Effects/Max Health Small | healthBonus=8 | 血护符（+8最大生命） |
| 6 | BloodCostReductionEffect | MutationChess/Relic Effects/Blood Cost Reduction | costReduction=1 | 血护符隐藏（鲜血脉络Boss激活） |
| 7 | BlockCostReductionEffect | MutationChess/Relic Effects/Block Cost Reduction | costReduction=1 | 冰晶符隐藏（寒霜之心Boss激活） |
| 8 | SlimeExpandEffect | MutationChess/Relic Effects/Slime Expand | stacks=2 | 酸核（旧逻辑备用） |
| 9 | TagComboResonanceEffect | MutationChess/Effects/Tag Combo Resonance | — | 共鸣石 |
| 10 | MaxHealthEffect | MutationChess/Relic Effects/Max Health | healthBonus=20 | 棋王冠（+20生命） |
| 11 | BloodPactStrengthEffect | MutationChess/Relic Effects/Blood Pact Str & HP | extraStr=1, loseMaxHp=5 | 血契隐藏（+3力-5HP，Boss激活） |
| 12 | SlimeEnergyEffect | MutationChess/Relic Effects/Slime Energy | energyGain=1 | 粘液之心（打出粘液牌） |

#### 通用遗物效果（8个）

| # | .asset文件名 | 菜单路径 | 参数设置 | 对应遗物 |
|---|---|---|---|---|
| 1 | PhoenixReviveEffect | MutationChess/Relic Effects/Phoenix Revive | reviveHealthPercent=0.5 | 凤凰羽毛 |
| 2 | TempDexterity3TurnsEffect | MutationChess/Relic Effects/Temp Dexterity 3 Turns | dex=1, turns=3 | 泰坦之心（1敏·续3回合·旧版每回合+2→新版改为一次性） |
| 3 | Gain12GoldOnVictoryEffect | MutationChess/Relic Effects/Gain 12 Gold Victory | gold=12 | 金杯（旧治疗→12金币） |

### 1.3 固有效果资源

保存目录：`Assets/TextMesh Pro/Resources/InherentEffects/`（需新建此文件夹）

| # | .asset文件名 | 菜单路径 | 参数设置 | 用途 |
|---|---|---|---|---|
| 1 | SlimeInherent | MutationChess/Inherent/Slime | — | 粘液字段固有 |
| 2 | ReluctantInherent | MutationChess/Inherent/Reluctant | drawCount=1 | 不舍字段固有 |

---

## 二、卡牌资源 (.asset)

保存目录：`Assets/TextMesh Pro/Resources/Cards/`（按系列子文件夹分类）

### 2.1 已有卡牌（无需创建）

基础/: 攻击, 防御, 痛击, 加固, 后发制人, 暮光仪式, 预知仪式
粘液/: 粘液打击, 粘液防御, 粘液附体

### 2.2 需要新建的卡牌（39张 · **稀有度不含Mythic，Mythic→Legendary）

#### 粘液系列（3张）→ 保存到 `粘液/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | inherentEffectIds |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 粘液喷射 | Attack | Common | 1 | 3 | 0 | 2 | Slime | DealDamageEffect, ApplyWeakEffect | SlimeInherent |
| 2 | 粘液陷阱 | Skill | Uncommon | 0 | 0 | 0 | 5 | Slime | DealDamageNextTurnEffect | SlimeInherent |
| 3 | 粘液分裂 | Skill | Uncommon | 1 | 0 | 0 | 1 | Slime | DrawCardsEffect | SlimeInherent |

#### 不舍系列（4张）→ 新建 `不舍/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | inherentEffectIds |
|---|---|---|---|---|---|---|---|---|---|---|
| 4 | 不舍之盾 | Defense | Common | 1 | 0 | 6 | 0 | Reluctant | ApplyBlockEffect | ReluctantInherent |
| 5 | 回响打击 | Attack | Common | 1 | 7 | 0 | 0 | Reluctant | DealDamageEffect | ReluctantInherent |
| 6 | 执念 | Skill | Uncommon | 1 | 0 | 0 | 2 | Reluctant | ApplyTemporaryStrengthEffect | ReluctantInherent |
| 7 | 轮回 | Skill | Rare | 0 | 0 | 0 | 1 | Reluctant | DrawReluctantFromDiscardEffect | ReluctantInherent |

#### 暗影系列（8张）→ 新建 `暗影/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds |
|---|---|---|---|---|---|---|---|---|---|
| 8 | 暗影突袭 | Attack | Common | 1 | 9 | 0 | 0 | Shadow | DealDamageEffect |
| 9 | 影刃 | Attack | Common | 0 | 4 | 0 | 0 | Shadow | DealDamageEffect |
| 10 | 暗袭 | Attack | Uncommon | 2 | 14 | 0 | 1 | Shadow | DealDamageEffect, DrawCardsEffect |
| 11 | 暗影迷雾 | Skill | Uncommon | 1 | 0 | 8 | 1 | Shadow | ApplyBlockEffect, ApplyDexterityEffect |
| 12 | 幻影 | Skill | Rare | 2 | 0 | 0 | 0 | Shadow | DamageReductionEffect |
| 13 | 暗影蓄力 | Skill | Common | 1 | 0 | 0 | 2 | Shadow | ApplyShadowStrengthEffect |
| 14 | 暗影爆发 | Attack | Uncommon | 1 | 0 | 0 | 2 | Shadow | ShadowBurstEffect |
| 15 | 影舞 | Skill | Rare | 1 | 0 | 0 | 3 | Shadow | ApplyShadowStrengthEffect, ApplyDexterityEffect |

#### 鲜血系列（6张·带恢复）→ 新建 `鲜血/` 文件夹（**CardBalanceConfig已加恢复效果）

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | bloodPerEnergy | tags | effectIds |
|---|---|---|---|---|---|---|---|---|---|---|
| 16 | 血瀑 | Attack | Uncommon | 2 | 18 | 0 | 3 | 3 | Blood | DealDamageEffect, HealPlayerEffect |
| 17 | 嗜血仪式 | Skill | Rare | 1 | 0 | 0 | 2 | 4 | Blood | GainStrengthEffect, HealPlayerEffect |
| 18 | 血池 | Attack | Common | 0 | 8 | 0 | 0 | 5 | Blood | DealDamageEffect, HealPlayerEffect |
| 19 | 鲜血献祭 | Skill | Uncommon | 1 | 0 | 0 | 6 | 3 | Blood | HealPlayerEffect, DrawCardsEffect |
| 20 | 血怒 | Attack | Uncommon | 2 | 12 | 0 | 3 | 3 | Blood | DealDamageEffect, HealPlayerEffect |
| 21 | 血腥撕裂 | Attack | Rare | 3 | 25 | 0 | 0 | 3 | Blood | DealDamageEffect, HealPlayerEffect |

**说明**：鲜血卡同时具备扣血与恢复机制，不再是单向消耗。

#### 寒霜系列（6张）→ 新建 `寒霜/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | blockPerEnergy | tags | effectIds |
|---|---|---|---|---|---|---|---|---|---|---|
| 22 | 寒枪 | Attack | Uncommon | 2 | 12 | 0 | 0 | 5 | Frost | DealDamageEffect |
| 23 | 霜甲 | Defense | Common | 2 | 0 | 15 | 0 | 4 | Frost | ApplyBlockEffect |
| 24 | 寒霜反击 | Defense | Rare | 3 | 0 | 20 | 3 | 6 | Frost | ApplyBlockEffect, ApplyThornsEffect |
| 25 | 冰封 | Defense | Common | 1 | 0 | 10 | 0 | 5 | Frost | ApplyBlockEffect |
| 26 | 寒冰壁垒 | Defense | Uncommon | 2 | 0 | 18 | 0 | 4 | Frost | ApplyBlockEffect |
| 27 | 冰霜之锤 | Attack | Uncommon | 2 | 10 | 8 | 0 | 5 | Frost | DealDamageEffect, ApplyBlockEffect |

#### 腐化系列（5张）→ 新建 `腐化/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | exhaust | effectIds |
|---|---|---|---|---|---|---|---|---|---|---|
| 28 | 腐化 | Attack | Common | 0 | 6 | 0 | 0 | Corrupt | true | DealDamageEffect |
| 29 | 腐蚀打击 | Attack | Uncommon | 1 | 14 | 0 | 2 | Corrupt | true | DealDamageEffect, ApplyVulnerabilityEffect |
| 30 | 腐化释放 | Skill | Rare | 2 | 0 | 0 | 0 | Corrupt | false | CorruptReleaseEffect |
| 31 | 暗影腐化 | Skill | Rare | 1 | 0 | 0 | 2 | Corrupt | false | AddCardToDeckEffect |
| 32 | 腐化吞噬 | Attack | Legendary | 3 | 20 | 0 | 2 | Corrupt | true | DealDamageEffect, DrawCardsEffect |
| **注** | | | 原Mythic→Legendary | | | | | | |

#### 联动卡牌（2张）→ 新建 `联动/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | inherentEffectIds |
|---|---|---|---|---|---|---|---|---|---|---|
| 33 | 粘腻爱意 | Skill | Rare | 2 | 0 | 0 | 3 | Slime, Reluctant | ApplyWeakEffect, ApplyVulnerabilityEffect, ReduceStrengthEffect | SlimeInherent, ReluctantInherent |
| 34 | 霜影斩 | Attack | Rare | 2 | 0 | 0 | 2 | Shadow, Frost | BlockToAttackEffect | — |

#### 通用卡牌（3张）→ 保存到 `基础/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | effectIds |
|---|---|---|---|---|---|---|---|---|
| 35 | 预知 | Skill | Common | 0 | 0 | 0 | 3 | InspectEffect |
| 36 | 探索 | Skill | Uncommon | 1 | 0 | 0 | 3 | DiscoverEffect |
| 37 | 礼物之力 | Attack | Rare | 1 | 10 | 0 | 1 | GiftEffect, DealDamageEffect |

#### 无色卡牌（6张）→ 新建 `无色/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | isColorless | effectIds |
|---|---|---|---|---|---|---|---|---|---|
| 38 | 宝藏 | Skill | Uncommon | 0 | 0 | 0 | 2 | true | TreasureEffect |
| 39 | 冥想 | Skill | Uncommon | 1 | 0 | 0 | 2 | true | GainEnergyEffect, DrawCardsEffect |
| 40 | 神秘卷轴 | Skill | Rare | 1 | 0 | 0 | 3 | true | DiscoverEffect |
| 41 | 古老符文 | Skill | Rare | 0 | 0 | 0 | 2 | true | GainStrengthEffect, ApplyDexterityEffect |
| 42 | 圣物 | Skill | Legendary | 2 | 0 | 6 | 6 | true | HealPlayerEffect, ApplyBlockEffect |
| 43 | 深渊之眼 | Skill | Legendary | 2 | 0 | 0 | 3 | true | DamageReductionEffect, DrawCardsEffect |
| **注** | | | 原Mythic→Legendary | | | | | | |

#### 新增通用卡牌第一弹（6张 · 中国元素）→ 保存到 `基础/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | exhaust | effectIds |
|---|---|---|---|---|---|---|---|---|---|
| 44 | 破阵 | Attack | Common | 1 | 8 | 0 | 0 | false | DealDamageEffect |
| 45 | 回春 | Skill | Uncommon | 0 | 0 | 0 | 4 | false | HealPlayerEffect |
| 46 | 镇魂 | Skill | Common | 1 | 0 | 6 | 1 | false | ApplyBlockEffect, DrawCardsEffect |
| 47 | 蓄势 | Skill | Uncommon | 0 | 0 | 0 | 2 | false | GainEnergyNextTurnEffect |
| 48 | 斩缘 | Attack | Uncommon | 2 | 14 | 0 | 0 | true | DealDamageEffect |
| 49 | 灵动 | Skill | Common | 1 | 0 | 0 | 3 | false | DamageReductionEffect |

#### 新增通用卡牌第二弹（6张 · 中国元素续）→ 保存到 `基础/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | exhaust | effectIds |
|---|---|---|---|---|---|---|---|---|---|
| 50 | 惊雷 | Attack | Uncommon | 2 | 12 | 0 | 0 | true | DealDamageEffect |
| 51 | 守心 | Skill | Common | 1 | 0 | 5 | 1 | false | ApplyBlockEffect, DrawCardsEffect |
| 52 | 破军 | Attack | Rare | 3 | 20 | 0 | 0 | false | DealDamageEffect |
| 53 | 归元 | Skill | Uncommon | 1 | 0 | 5 | 5 | false | HealPlayerEffect, ApplyBlockEffect |
| 54 | 凝神 | Skill | Uncommon | 0 | 0 | 0 | 1 | false | GainEnergyNextTurnEffect, DrawCardsEffect |
| 55 | 玄甲 | Defense | Common | 2 | 0 | 14 | 0 | false | ApplyBlockEffect |

#### 诅咒卡牌（5张 · Cursed稀有度 · 无法打出 · 负面效果）→ 新建 `诅咒/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | tags | effectIds | 说明 |
|---|---|---|---|---|---|---|---|
| 56 | 诅咒_衰败 | Curse | Cursed | 0 | Curse | CurseDecayEffect | 手牌中每回合-1HP |
| 57 | 诅咒_迷雾 | Curse | Cursed | 0 | Curse | CurseFogEffect | 手牌上限-1 |
| 58 | 诅咒_枷锁 | Curse | Cursed | 0 | Curse | CurseChainsEffect | 每回合抽牌-1 |
| 59 | 诅咒_噬命 | Curse | Cursed | 0 | Curse | CurseDevourEffect | 打出其他卡时-1HP |
| 60 | 诅咒_虚耗 | Curse | Cursed | 0 | Curse | CurseVoidEffect | 占位，营地可移除 |

---

## 三、遗物资源 (.asset)

保存目录：`Assets/TextMesh Pro/Resources/Relics/`

**字段规则（从 RelicDataAsset.cs 对应）**：
- `relicId` / `relicName` / `rarity` / `faction`
- `baseEffectIds`（基础效果列表 · 一直显示 & 激活）
- `hiddenActivatorRelicId`（Boss遗物relicId，留空=无隐藏效果，例如Boss_BloodVein）
- `hiddenEffectIds`（拥有Activator才激活，获得Boss前不显示效果 & 不激活）
- `isBossRelic`（Boss专属掉落= true，激活系列隐藏效果）
- `isStartingRelic`（起始遗物=true，如燃烧之心）
- `isShopRelic`（商店出售=true，如能核/画板/宝箱/金偶像）

### 3.1 已有遗物（11个 · 更新稀有度）

铁戒指, 血护符(Common→Rare), 剑碎片, 剑柄碎片, 剑之核心, 粘液腺体, 记忆晶状体, 腐化肝脏, 储蓄罐, 胜利誓约之剑, 鲜血脉络

### 3.2 6个系列 Boss 遗物（**Special稀有度，仅Boss掉落 · isBossRelic=true）

| # | .asset文件名 | relicId | rarity | faction | price | baseEffectIds | 说明 |
|---|---|---|---|---|---|---|---|
| B1 | 鲜血脉络 | Boss_BloodVein | Special | Blood | 350 | BossBloodVeinEffect(BattleStart) | Boss核心：激活全鲜血系列隐藏效果 |
| B2 | 寒霜之心 | Boss_FrostHeart | Special | Frost | 350 | BossFrostHeartEffect(BattleStart) | Boss核心：激活全寒霜系列隐藏效果 |
| B3 | 腐化肝脏 | Boss_CorruptLiver | Special | Corrupt | 350 | BossCorruptLiverEffect(BattleStart) | Boss核心：激活全腐化系列隐藏效果 |
| B4 | 粘液腺体 | Boss_SlimeGland | Special | Slime | 350 | BossSlimeGlandEffect(BattleStart) | Boss核心：激活全粘液系列隐藏效果 |
| B5 | 不舍锁链 | Boss_ReluctantChain | Special | Reluctant | 350 | BossReluctantChainEffect(BattleStart) | Boss核心：激活全不舍系列隐藏效果，替换掉旧的"不舍系列=记忆晶状体" |
| B6 | 记忆晶状体 | Boss_MemoryLens | Special | Shadow | 350 | ShadowCardBonusDamageEffect + ApplyBlockEffect(15, BattleStart) + Every4TurnsLoseDexEffect(TurnEnd) | 暗影Boss核心：**每4回合-1敏捷负面**；激活暗影隐藏；暗影牌+3伤害； |

### 3.3 需要新建的系列遗物（含基础效果 + 隐藏效果）=19个）

| # | .asset文件名 | relicId | rarity | faction | price | baseEffectIds | hiddenActivatorRelicId | hiddenEffectIds |
|---|---|---|---|---|---|---|---|---|
| 1 | 血祭坛 | Blood_CrimsonAltar | Rare | Blood | 285 | BloodAltarEffect(BattleStart) | Boss_BloodVein | BloodAltarBoostedEffect(BattleStart) |
| 2 | 雪符 | Frost_Snowflake | Common | Frost | 150 | FrostSnowflakeEffect(BattleStart) | Boss_FrostHeart | GainDexterityBattleStartEffect(BattleStart, +1敏) |
| 3 | 枯枝 | Corrupt_DeadBranch | Rare | Corrupt | 285 | CorruptAddCardEffect(CardExhausted) | Boss_CorruptLiver | (隐藏：+1牌) |
| 4 | 粘液手套 | Slime_StickyGlove | Common | Slime | 150 | SlimeWeakEffect(CardPlayed) | Boss_SlimeGland | SlimeWeakEffect(CardPlayed, 虚弱+1) |
| 5 | 怀旧链 | Reluctant_Nostalgia | Common | Reluctant | 150 | ReluctantBlockBonusEffect(CardPlayed, 2格挡) | Boss_ReluctantChain | ReluctantBlockBonusEffect(CardPlayed, +2格挡) |
| 6 | 暗影斗篷 | Shadow_Cloak | Common | Shadow | 150 | ApplyBlockEffect(BattleStart, 10格挡) | Boss_MemoryLens | GainDexterityBattleStartEffect(+1敏) |
| 7 | 幻影面具 | Shadow_PhantomMask | Rare | Shadow | 285 | PhantomAfter5CardsEffect(AfterCardsPlayed,5张→-5伤) | Boss_MemoryLens | PhantomAfter5CardsBoostEffect(再+3减伤→共-8) |
| 8 | 深渊凝视 | Shadow_AbyssGaze | Legendary | Shadow | 320 | AbyssEvery4AttackEffect(CalculateAtkDmg,4张攻击翻倍) | Boss_MemoryLens | AbyssReduceThresholdEffect(3张触发) |
| 9 | 吸血獠牙 | Blood_VampireFang | Common（原Rare→调低 | Blood | 150 | AttackHeal1Effect(CardPlayed, 攻击牌+1HP) | Boss_BloodVein | AttackHeal1Effect(CardPlayed, +1HP→共2) |
| 10 | 血契 | Blood_BloodPact | Legendary | Blood | 300 | GainStrengthBattleStartEffect(BattleStart, +2力量) | Boss_BloodVein | BloodPactStrengthEffect(BattleStart) |
| 11 | 永冻土 | Frost_Permafrost | Rare | Frost | 285 | FrostPermafrostEffect(BattleStart,15格挡) | Boss_FrostHeart | ApplyBlockEffect(再+10→25) |
| 12 | 霜巨人 | Frost_FrostGiant | Legendary | Frost | 300 | GainDexterityBattleStartEffect(+1敏, BattleStart) + FrostBonusBlockEffect(寒霜牌+8格挡, CardPlayed) | **加强：无需隐藏 | 无隐藏，直接给全效果 |
| 13 | 暗黑法典 | Corrupt_DarkTome | Common | Corrupt | 150 | ExhaustEnergyEffect(CardExhausted) | Boss_CorruptLiver | (隐藏+1) |
| 14 | 死灵之书 | Corrupt_Necronomicon | Legendary | Corrupt | 320 | ExhaustDrawEffect(CardExhausted) | Boss_CorruptLiver | (隐藏+1抽) |
| 15 | 粘液之心 | Slime_SlimeHeart | Rare | Slime | 285 | SlimeEnergyEffect(CardPlayed, 粘液牌→+1能量) | Boss_SlimeGland | 再+1费 |
| 16 | 酸核 | Slime_AcidicCore | Legendary | Slime | 300 | AcidicCoreDebuff3StacksEffect(BattleStart,3种debuff各3层) | Boss_SlimeGland | AcidicCoreBoostedEffect(各+1层) |
| 17 | 回响戒 | Reluctant_EchoRing | Rare | Reluctant | 285 | ReluctantBonusDrawEffect(CardPlayed, 不舍牌+1抽) | Boss_ReluctantChain | ReluctantBonusDrawEffect(CardPlayed, +1抽→共2) |
| 18 | 共鸣石 | Combo_ResonanceStone | Legendary（原Mythic→Legendary） | None | 350 | TagComboResonanceEffect(CardPlayed) | - | （无隐藏） |
| 19 | 棋王冠 | Combo_ChessMaster | Legendary（原Mythic→Legendary） | None | 350 | MaxHealthEffect(BattleStart, +20生命) + ChessMasterEvery6CardsStrengthEffect(AfterCardsPlayed,6张→+1力量) | - | **联动**：每6张力量+1 |

**稀有度改动**：
- **调高**：血护符(Common→Rare)、能核(Rare→Legendary)、画板(Rare→Legendary)
- **调低**：吸血獠牙(Rare→Common)
- **改名**：吸血鬼之牙→吸血獠牙、棋王之冠→棋王冠、能量核心→能核、绘画板→画板、鲜血护符→血护符

### 3.4 商店遗物（5个 · isShopRelic=true）

| # | .asset文件名 | relicId | rarity | price | baseEffectIds | 说明 |
|---|---|---|---|---|---|---|
| S1 | 能核 | Shop_EnergyCore | Legendary（Rare→调高） | 325 | EnergyCoreBattleStartEffect（开局+2能量） | 战斗开始一次性+2能量 |
| S2 | 画板 | Shop_DrawingPad | Legendary（Rare→调高） | 325 | DrawingPadDraw2Effect（开局抽2） | 调高稀有度 |
| S3 | 宝箱 | Shop_TreasureChest | Rare | 325 | EliteVictoryExtraCardGroupEffect（Victory） | **战胜精英额外一组卡牌奖励** |
| S4 | 金偶像 | Shop_GoldenIdol | Legendary | 350 | GoldBonusEffect(Victory, +20%金币) | — |
| S5 | 补货符 | Shop_RestockTalisman | Rare | 300 | ShopRestockEffect（Passive） | **商店不会卖空**：购买后商品自动补货 |

### 3.5 通用遗物（12个 · 2026改版效果）

| # | .asset文件名 | relicId | rarity | price | baseEffectIds | 改动说明 |
|---|---|---|---|---|---|---|
| U1 | 铜盾 | Generic_BronzeShield | Common | 150 | GainDexterityBattleStartEffect（BattleStart, +1敏） | — |
| U2 | 战士腰带 | Generic_WarriorBelt | Common | 150 | MaxHealthSmallEffect(BattleStart, +15最大生命） | — |
| U3 | 力量坠饰 | Generic_PowerPendant | Rare | 285 | GainStrengthBattleStartEffect(BattleStart, +2力量) | — |
| U4 | 钢铁意志 | Generic_IronWill | Rare | 285 | BlockAndStrengthEffect(BattleStart) | — |
| U5 | 金杯 | Generic_GoldenChalice | Rare | 285 | Gain12GoldOnVictoryEffect(Victory, +12金币) | 原回血→改成胜利获得12金币 |
| U6 | 战旗 | Generic_BattleStandard | Legendary | 320 | AllStatsBuffEffect(BattleStart, 力1敏1格挡10) | — |
| U7 | 凤凰羽毛 | Generic_PhoenixFeather | Legendary | 350 | PhoenixReviveEffect(濒死半血复活) | — |
| U8 | 泰坦之心 | Generic_TitanHeart | Legendary | 320 | TempDexterity3TurnsEffect(1敏捷，持续3回合) | 平衡调整：原每回合+2敏→开局一次性，改为1敏3回合 |
| U9 | 永焰 | Generic_EternalFlame | Legendary（原Mythic→降为Legendary） | 380 | EternalFlameBattleStartEffect(开局：力2敏2格挡10能量2) | — |
| U10 | 吸血獠牙 / 血护符 / 霜巨人 等均在上面的「系列遗物」 |
| U11 | 燃烧之心 | Starter_BurningHeart | Starting（起始=新稀有度！） | 0 | HealOnVictoryEffect(Victory 小恢复) | **起始遗物：isStartingRelic=true；起始稀有度 |
| U12 | 储蓄罐 / 胜利誓约之剑 / 剑碎片系列 | 沿用老配置即可 |  |  |  |

---

## 四、创建步骤

1. 等待 Unity 自动编译所有新增 .cs 脚本
2. 按 1.1 重命名已有 .asset（文件名需与 effectId 一致）
3. 按 1.2 创建效果 .asset 和固有效果 .asset（**包括：6个Boss、1个ChessMasterEvery6Cards、AcidicCoreDebuff3Stacks、吸血獠牙AttackHeal1Effect、泰坦TempDex3Turn、金杯Gain12Gold
4. 按第二节创建卡牌 .asset 到对应系列文件夹 → 注意：腐化吞噬/圣物/深渊之眼 三张原Mythic稀有度要改成Legendary
5. 按第三节创建遗物 .asset（**尤其是6个Boss遗物，标记isBossRelic=true，稀有度Special；燃烧之心标记isStartingRelic=true；稀有度改动：血护符Rare，吸血獠牙Common）
6. Unity 会自动生成 .meta 文件

---

## 五、脚本挂载清单（MainScene 场景）

> **说明**：以下脚本均为 `MonoBehaviour` 单例（通过 `Instance` 访问），需挂载到 `Assets/_Project/Scenes/MainScene.unity` 场景的 GameObject 上。`ScriptableObject` 配置类（如 ShopConfig/RelicBalanceConfig/PotionBalanceConfig/CardBalanceConfig/BackgroundConfig/BossRewardConfig）和 `CardEffect` 派生类**不需要挂载**，通过 `Resources.Load` 加载。

### 5.1 系统管理器 → 挂到 `GameManager` 空对象

在场景中新建空 GameObject 命名为 `GameManager`，挂载以下 7 个脚本：

| # | 脚本 | 路径 | 职责 |
|---|---|---|---|
| 1 | GameManager | `Scripts/GameManager.cs` | 游戏主控制器，管理地图流转、节点触发、战斗入口 |
| 2 | PlayerDataManager | `Scripts/Core/PlayerDataManager.cs` | 玩家数据（HP/金币/牌组）持久化管理 |
| 3 | RelicManager | `Scripts/Core/RelicManager.cs` | 遗物持有、效果注册/注销、隐藏效果激活 |
| 4 | RelicMergeService | `Scripts/Core/RelicMergeService.cs` | 遗物合成（剑碎片+剑柄→剑之核心） |
| 5 | FactionUnlockService | `Scripts/Core/FactionUnlockService.cs` | 阵营解锁状态管理 |
| 6 | PotionDropService | `Scripts/Core/PotionDropService.cs` | 药水掉落概率与稀有度计算 |
| 7 | ShopDataService | `Scripts/Core/ShopDataService.cs` | 商店商品生成与数据服务 |

### 5.2 战斗系统 → 挂到 `BattleSystem` 空对象

在场景中新建空 GameObject 命名为 `BattleSystem`，挂载以下 4 个脚本：

| # | 脚本 | 路径 | 职责 |
|---|---|---|---|
| 1 | BattleManager | `Scripts/Battle/BattleManager.cs` | 战斗主流程、UI 刷新、回合事件触发 |
| 2 | TurnManager | `Scripts/Battle/TurnManager.cs` | 玩家/敌人回合切换 |
| 3 | EffectManager | `Scripts/Core/EffectManager.cs` | 效果事件管线（BattleStart/CardPlayed/TurnEnd 等） |
| 4 | BossRewardService | `Scripts/Battle/BossRewardService.cs` | Boss 战胜利后的奖励生成 |

### 5.3 UI 系统 → 挂到 Canvas 子对象

| # | 脚本 | 路径 | 挂载目标 | 职责 |
|---|---|---|---|---|
| 1 | HandManager | `Scripts/UI/HandManager.cs` | `Canvas/HandPanel` | 手牌/抽牌堆/弃牌堆/消耗堆管理 |
| 2 | UILayoutManager | `Scripts/UI/UILayoutManager.cs` | `Canvas` | UI 布局自适应 |
| 3 | ShopPanel | `Scripts/UI/ShopPanel.cs` | `Canvas/ShopPanel` | 商店界面交互 |

### 5.4 相机 → 挂到 `Main Camera`

| # | 脚本 | 路径 | 挂载目标 | 职责 |
|---|---|---|---|---|
| 1 | CameraController | `Scripts/CameraController.cs` | `Main Camera` | 相机缩放/旋转/拖拽/视角重置 |

### 5.5 无需挂载的脚本类型

| 类型 | 示例脚本 | 使用方式 |
|---|---|---|
| ScriptableObject 配置 | ShopConfig, RelicBalanceConfig, PotionBalanceConfig, CardBalanceConfig, BackgroundConfig, BossRewardConfig | Project 窗口右键 → Create → 对应菜单 → 创建 .asset |
| CardEffect 派生类 | DealDamageEffect, ApplyBlockEffect, AddCardToDeckEffect, GiftEffect 等全部效果 | 同上，创建 .asset 后填入卡牌/遗物的 effectIds |
| 数据资产类 | RelicDataAsset, CardDataAsset, PotionDataAsset | 同上，创建 .asset 配置卡牌/遗物/药水数据 |
| 静态工具类 | GameLogger, CardData, CardName 等 | 代码内直接调用，无需挂载 |

### 5.6 挂载检查清单

挂载完成后，进入 Play 模式前请确认：
1. `GameManager` 对象上 7 个脚本均无 Missing 引用
2. `BattleSystem` 对象上 4 个脚本均无 Missing 引用
3. `BattleManager` 的 Inspector 中 `battlePanel`/`handPanel`/`endTurnButton`/`enemyIntentUI` 等字段已拖入对应 UI 引用
4. `HandManager` 的 Inspector 中卡牌父节点已拖入
5. `ShopPanel` 的 Inspector 中商品列表父节点已拖入
6. `CameraController` 挂在 `Main Camera` 上
7. 所有 ScriptableObject 配置 .asset 已创建并放入 `Resources/` 对应目录
