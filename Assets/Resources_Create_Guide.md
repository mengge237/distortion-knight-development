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

#### 基础系列（7张）

| 卡牌名 | cardType | rarity | cost | effectIds | 描述 |
|---|---|---|---|---|---|
| 攻击 | Attack | Common | 1 | DealDamageEffect | 造成6点伤害 |
| 防御 | Defense | Common | 1 | ApplyBlockEffect | 获得5点格挡 |
| 痛击 | Attack | Common | 1 | DealDamageEffect, ApplyVulnerabilityEffect | 造成8点伤害，施加1层易伤 |
| 加固 | Skill | Common | 1 | ApplyDexterityEffect | 获得3点敏捷 |
| 后发制人 | Skill | Uncommon | 0 | InspectEffect | 检视牌堆顶3张牌，可按优先级排序 |
| 暮光仪式 | Attack | Uncommon | 2 | DealDamageEffect | 造成8点伤害（手牌数≤3时伤害翻倍） |
| 预知仪式 | Skill | Uncommon | 0 | DrawCardsEffect | 抽2张牌 |

#### 粘液系列已有卡牌（3张）

| 卡牌名 | cardType | rarity | cost | tags | effectIds | inherentEffectIds | 描述 |
|---|---|---|---|---|---|---|---|
| 粘液打击 | Attack | Common | 1 | Slime | DealDamageEffect | SlimeInherent | 造成5点伤害。固有：粘液 |
| 粘液防御 | Defense | Common | 1 | Slime | ApplyBlockEffect, ApplyWeakEffect | SlimeInherent | 获得4点格挡，施加1层虚弱。固有：粘液 |
| 粘液附体 | Attack | Uncommon | 1 | Slime | DealDamageEffect | SlimeInherent | 造成3点伤害（AoE，对全体敌人）。固有：粘液 |

### 2.2 需要新建的卡牌（39张 · **稀有度不含Mythic，Mythic→Legendary）

#### 粘液系列（3张）→ 保存到 `粘液/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | inherentEffectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 粘液喷射 | Attack | Common | 1 | 3 | 0 | 2 | Slime | DealDamageEffect, ApplyWeakEffect | SlimeInherent | 造成3点伤害，施加2层虚弱。固有：粘液 |
| 2 | 粘液陷阱 | Skill | Uncommon | 0 | 0 | 0 | 5 | Slime | DealDamageNextTurnEffect | SlimeInherent | 下回合对敌人造成5点伤害。固有：粘液 |
| 3 | 粘液分裂 | Skill | Uncommon | 1 | 0 | 0 | 1 | Slime | DrawCardsEffect | SlimeInherent | 抽1张牌。固有：粘液 |

#### 不舍系列（4张）→ 新建 `不舍/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | inherentEffectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 4 | 不舍之盾 | Defense | Common | 1 | 0 | 6 | 0 | Reluctant | ApplyBlockEffect | ReluctantInherent | 获得6点格挡。固有：不舍 |
| 5 | 回响打击 | Attack | Common | 1 | 7 | 0 | 0 | Reluctant | DealDamageEffect | ReluctantInherent | 造成7点伤害。固有：不舍 |
| 6 | 执念 | Skill | Uncommon | 1 | 0 | 0 | 2 | Reluctant | ApplyTemporaryStrengthEffect | ReluctantInherent | 获得2点临时力量。固有：不舍 |
| 7 | 轮回 | Skill | Rare | 0 | 0 | 0 | 1 | Reluctant | DrawReluctantFromDiscardEffect | ReluctantInherent | 从弃牌堆回收1张不舍牌。固有：不舍 |

#### 暗影系列（8张）→ 新建 `暗影/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|
| 8 | 暗影突袭 | Attack | Common | 1 | 9 | 0 | 0 | Shadow | DealDamageEffect | 造成9点伤害 |
| 9 | 影刃 | Attack | Common | 0 | 4 | 0 | 0 | Shadow | DealDamageEffect | 造成4点伤害 |
| 10 | 暗袭 | Attack | Uncommon | 2 | 14 | 0 | 1 | Shadow | DealDamageEffect, DrawCardsEffect | 造成14点伤害，抽1张牌 |
| 11 | 暗影迷雾 | Skill | Uncommon | 1 | 0 | 8 | 1 | Shadow | ApplyBlockEffect, ApplyDexterityEffect | 获得8点格挡，获得1点敏捷 |
| 12 | 幻影 | Skill | Rare | 2 | 0 | 0 | 0 | Shadow | DamageReductionEffect | 本回合受到伤害减少（减伤效果） |
| 13 | 暗影蓄力 | Skill | Common | 1 | 0 | 0 | 2 | Shadow | ApplyShadowStrengthEffect | 获得2点暗影力量 |
| 14 | 暗影爆发 | Attack | Uncommon | 1 | 0 | 0 | 2 | Shadow | ShadowBurstEffect | 造成2倍暗影力量值的伤害 |
| 15 | 影舞 | Skill | Rare | 1 | 0 | 0 | 3 | Shadow | ApplyShadowStrengthEffect, ApplyDexterityEffect | 获得3点暗影力量，获得1点敏捷 |

#### 鲜血系列（6张·带恢复）→ 新建 `鲜血/` 文件夹（**CardBalanceConfig已加恢复效果）

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | bloodPerEnergy | tags | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 16 | 血瀑 | Attack | Uncommon | 2 | 18 | 0 | 3 | 3 | Blood | DealDamageEffect, HealPlayerEffect | 造成18点伤害，恢复3点HP。血转：每1能量=3HP |
| 17 | 嗜血仪式 | Skill | Rare | 1 | 0 | 0 | 2 | 4 | Blood | GainStrengthEffect, HealPlayerEffect | 获得2点力量，恢复2点HP。血转：每1能量=4HP |
| 18 | 血池 | Attack | Common | 0 | 8 | 0 | 0 | 5 | Blood | DealDamageEffect, HealPlayerEffect | 造成8点伤害，恢复HP。血转：每1能量=5HP |
| 19 | 鲜血献祭 | Skill | Uncommon | 1 | 0 | 0 | 6 | 3 | Blood | HealPlayerEffect, DrawCardsEffect | 恢复6点HP，抽3张牌。血转：每1能量=3HP |
| 20 | 血怒 | Attack | Uncommon | 2 | 12 | 0 | 3 | 3 | Blood | DealDamageEffect, HealPlayerEffect | 造成12点伤害，恢复3点HP。血转：每1能量=3HP |
| 21 | 血腥撕裂 | Attack | Rare | 3 | 25 | 0 | 0 | 3 | Blood | DealDamageEffect, HealPlayerEffect | 造成25点伤害，恢复HP。血转：每1能量=3HP |

**说明**：鲜血卡同时具备扣血与恢复机制，不再是单向消耗。

#### 寒霜系列（6张）→ 新建 `寒霜/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | blockPerEnergy | tags | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 22 | 寒枪 | Attack | Uncommon | 2 | 12 | 0 | 0 | 5 | Frost | DealDamageEffect | 造成12点伤害。挡转：每1能量=5HP |
| 23 | 霜甲 | Defense | Common | 2 | 0 | 15 | 0 | 4 | Frost | ApplyBlockEffect | 获得15点格挡。挡转：每1能量=4HP |
| 24 | 寒霜反击 | Defense | Rare | 3 | 0 | 20 | 3 | 6 | Frost | ApplyBlockEffect, ApplyThornsEffect | 获得20点格挡，获得3点荆棘。挡转：每1能量=6HP |
| 25 | 冰封 | Defense | Common | 1 | 0 | 10 | 0 | 5 | Frost | ApplyBlockEffect | 获得10点格挡。挡转：每1能量=5HP |
| 26 | 寒冰壁垒 | Defense | Uncommon | 2 | 0 | 18 | 0 | 4 | Frost | ApplyBlockEffect | 获得18点格挡。挡转：每1能量=4HP |
| 27 | 冰霜之锤 | Attack | Uncommon | 2 | 10 | 8 | 0 | 5 | Frost | DealDamageEffect, ApplyBlockEffect | 造成10点伤害，获得8点格挡。挡转：每1能量=5HP |

#### 腐化系列（5张）→ 新建 `腐化/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | exhaust | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 28 | 腐化 | Attack | Common | 0 | 6 | 0 | 0 | Corrupt | true | DealDamageEffect | 造成6点伤害。消耗 |
| 29 | 腐蚀打击 | Attack | Uncommon | 1 | 14 | 0 | 2 | Corrupt | true | DealDamageEffect, ApplyVulnerabilityEffect | 造成14点伤害，施加2层易伤。消耗 |
| 30 | 腐化释放 | Skill | Rare | 2 | 0 | 0 | 0 | Corrupt | false | CorruptReleaseEffect | 消耗所有腐化卡，每张触发效果 |
| 31 | 暗影腐化 | Skill | Rare | 1 | 0 | 0 | 2 | Corrupt | false | AddCardToDeckEffect | 向牌组添加2张腐化卡 |
| 32 | 腐化吞噬 | Attack | Legendary | 3 | 20 | 0 | 2 | Corrupt | true | DealDamageEffect, DrawCardsEffect | 造成20点伤害，抽2张牌。消耗。Legendary |
| **注** | | | 原Mythic→Legendary | | | | | | | |

#### 联动卡牌（2张）→ 新建 `联动/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | tags | effectIds | inherentEffectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 33 | 粘腻爱意 | Skill | Rare | 2 | 0 | 0 | 3 | Slime, Reluctant | ApplyWeakEffect, ApplyVulnerabilityEffect, ReduceStrengthEffect | SlimeInherent, ReluctantInherent | 施加1层虚弱+1层易伤，减少3点力量。双重标签：粘液+不舍 |
| 34 | 霜影斩 | Attack | Rare | 2 | 0 | 0 | 2 | Shadow, Frost | BlockToAttackEffect | — | 将格挡转化为攻击伤害，倍率2。双重标签：暗影+寒霜 |

#### 通用卡牌（3张）→ 保存到 `基础/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|
| 35 | 预知 | Skill | Common | 0 | 0 | 0 | 3 | InspectEffect | 检视牌堆顶3张牌 |
| 36 | 探索 | Skill | Uncommon | 1 | 0 | 0 | 3 | DiscoverEffect | 发现3张卡牌中选1张 |
| 37 | 礼物之力 | Attack | Rare | 1 | 10 | 0 | 1 | GiftEffect, DealDamageEffect | 造成10点伤害，若牌堆顶为礼物牌则触发额外效果 |

#### 无色卡牌（6张）→ 新建 `无色/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | isColorless | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|
| 38 | 宝藏 | Skill | Uncommon | 0 | 0 | 0 | 2 | true | TreasureEffect | 获得2张随机卡牌 |
| 39 | 冥想 | Skill | Uncommon | 1 | 0 | 0 | 2 | true | GainEnergyEffect, DrawCardsEffect | 获得1点能量，抽2张牌 |
| 40 | 神秘卷轴 | Skill | Rare | 1 | 0 | 0 | 3 | true | DiscoverEffect | 发现3张卡牌中选1张 |
| 41 | 古老符文 | Skill | Rare | 0 | 0 | 0 | 2 | true | GainStrengthEffect, ApplyDexterityEffect | 获得2点力量，获得2点敏捷 |
| 42 | 圣物 | Skill | Legendary | 2 | 0 | 6 | 6 | true | HealPlayerEffect, ApplyBlockEffect | 恢复6点HP，获得6点格挡。Legendary |
| 43 | 深渊之眼 | Skill | Legendary | 2 | 0 | 0 | 3 | true | DamageReductionEffect, DrawCardsEffect | 减伤3点，抽3张牌。Legendary |
| **注** | | | 原Mythic→Legendary | | | | | | | |

#### 新增通用卡牌第一弹（6张 · 中国元素）→ 保存到 `基础/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | exhaust | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|
| 44 | 破阵 | Attack | Common | 1 | 8 | 0 | 0 | false | DealDamageEffect | 造成8点伤害 |
| 45 | 回春 | Skill | Uncommon | 0 | 0 | 0 | 4 | false | HealPlayerEffect | 恢复4点HP |
| 46 | 镇魂 | Skill | Common | 1 | 0 | 6 | 1 | false | ApplyBlockEffect, DrawCardsEffect | 获得6点格挡，抽1张牌 |
| 47 | 蓄势 | Skill | Uncommon | 0 | 0 | 0 | 2 | false | GainEnergyNextTurnEffect | 下回合获得2点能量 |
| 48 | 斩缘 | Attack | Uncommon | 2 | 14 | 0 | 0 | true | DealDamageEffect | 造成14点伤害。消耗 |
| 49 | 灵动 | Skill | Common | 1 | 0 | 0 | 3 | false | DamageReductionEffect | 本回合减伤3点 |

#### 新增通用卡牌第二弹（6张 · 中国元素续）→ 保存到 `基础/`

| # | .asset文件名 | cardType | rarity | cost | damage | block | magic | exhaust | effectIds | 描述 |
|---|---|---|---|---|---|---|---|---|---|---|
| 50 | 惊雷 | Attack | Uncommon | 2 | 12 | 0 | 0 | true | DealDamageEffect | 造成12点伤害。消耗 |
| 51 | 守心 | Skill | Common | 1 | 0 | 5 | 1 | false | ApplyBlockEffect, DrawCardsEffect | 获得5点格挡，抽1张牌 |
| 52 | 破军 | Attack | Rare | 3 | 20 | 0 | 0 | false | DealDamageEffect | 造成20点伤害 |
| 53 | 归元 | Skill | Uncommon | 1 | 0 | 5 | 5 | false | HealPlayerEffect, ApplyBlockEffect | 恢复5点HP，获得5点格挡 |
| 54 | 凝神 | Skill | Uncommon | 0 | 0 | 0 | 1 | false | GainEnergyNextTurnEffect, DrawCardsEffect | 下回合获得1点能量，抽1张牌 |
| 55 | 玄甲 | Defense | Common | 2 | 0 | 14 | 0 | false | ApplyBlockEffect | 获得14点格挡 |

#### 诅咒卡牌（5张 · Cursed稀有度 · 无法打出 · 负面效果）→ 新建 `诅咒/` 文件夹

| # | .asset文件名 | cardType | rarity | cost | tags | effectIds | 描述 |
|---|---|---|---|---|---|---|---|
| 56 | 诅咒_衰败 | Curse | Cursed | 0 | Curse | CurseDecayEffect | 每回合结束损失1HP |
| 57 | 诅咒_迷雾 | Curse | Cursed | 0 | Curse | CurseFogEffect | 手牌上限-1 |
| 58 | 诅咒_枷锁 | Curse | Cursed | 0 | Curse | CurseChainsEffect | 每回合抽牌-1 |
| 59 | 诅咒_噬命 | Curse | Cursed | 0 | Curse | CurseDevourEffect | 打出其他卡时损失1HP |
| 60 | 诅咒_虚耗 | Curse | Cursed | 0 | Curse | CurseVoidEffect | 占位无效果，可在营地移除 |

---

## 三、遗物资源 (.asset)

保存目录：`Assets/TextMesh Pro/Resources/Relics/`

**遗物的完整数据（relicId、价格、效果、隐藏效果、视觉描述等）请参考 [遗物素材清单.txt](遗物素材清单.txt)**

### 字段规则（从 RelicDataAsset.cs 对应）

| 字段 | 说明 |
|---|---|
| relicId | 遗物唯一标识，格式：`分类_名称`（如 Boss_BloodVein、Blood_BloodAmulet） |
| relicName | 遗物显示名称 |
| rarity | 稀有度：Starting / Common / Rare / Legendary / Special |
| faction | 所属派系：Blood / Frost / Corrupt / Slime / Reluctant / Shadow / None |
| baseEffectIds | 基础效果列表，一直显示且激活 |
| hiddenActivatorRelicId | Boss遗物relicId，留空=无隐藏效果 |
| hiddenEffectIds | 拥有Activator后才激活的效果 |
| isBossRelic | Boss专属掉落=true，激活系列隐藏效果 |
| isStartingRelic | 起始遗物=true |
| isShopRelic | 商店出售=true |

### 创建注意事项

1. **6个Boss遗物**：标记 `isBossRelic=true`，稀有度 `Special`，仅Boss掉落
2. **燃烧之心**：标记 `isStartingRelic=true`，稀有度 `Starting`
3. **5个商店遗物**：标记 `isShopRelic=true`（能核/画板/宝箱/金偶像/补货符）
4. **稀有度调整**：血护符(Rare)、吸血獠牙(Common)、能核(Legendary)、画板(Legendary)
5. **冰晶符**使用"符箓"描述，不用"吊坠"
6. **霜巨人**无隐藏效果，直接给全效果
7. 遗物素材清单中所有 `沿用老配置` 的已有遗物，无需重新创建

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
