# 《异变棋局》Unity → Unreal Engine 5 移植评估

> 生成日期：2026-08-15 · 范围：卡牌/效果/遗物/敌人数据 + 战斗逻辑 + 元进程系统
> 配套工具：`ue5_datatable_export.py`（数据转换原型，已可运行）

---

## 0. 结论摘要

| 资产 | 可否直接迁移 | 说明 |
|---|---|---|
| 美术资源（卡面/敌人立绘/背景） | ✅ 100% | PNG 原样导入，改引用方式即可 |
| 音频 / 文案 / 数值设计 | ✅ 100% | 数据已全部导出为 CSV（见第 1 节） |
| 数据结构（卡牌/遗物/敌人定义） | ✅ 已完成 | 转换器输出 UE DataTable 可导入的 CSV |
| C# 游戏逻辑 | ❌ 必须重写 | 无自动转换工具；UE 侧用 C++ 重写 |
| UI / 动画（DOTween、手牌布局） | ❌ 必须重写 | 依赖 Unity UI + DOTween，无对应物 |

**一句话结论：数据与设计全部保留，代码整体重写。** 这是所有 Unity→UE 移植的标准路径，也是本项目最合理的路径——本次 Unity 侧刚完成的「效果类合并」（120→81，参数化思路）恰好是 UE 侧数据驱动效果的正确形态，可以直接沿用。

---

## 1. 现状盘点（2026-08-15 实测导出）

| 数据类型 | 数量 | 来源 | UE5 目标 |
|---|---|---|---|
| 卡牌 | 72 张 | `TextMesh Pro/Resources/Cards/**/*.asset` | DataTable `cards` |
| 效果资产 | 99 个 | `TextMesh Pro/Resources/Effects/*.asset` | DataTable `effects` |
| 遗物 | 50 个 | `RelicBalanceConfig.cs`（**纯代码配置**，无资产文件） | DataTable `relics` |
| 敌人 | 9 种 | `Enemy.cs` CreateDefaultEnemies（代码配置） | DataTable `enemies` |
| 地图参数 | 1 个 | `ScriptableObjects/DefaultMapConfig.asset` | 手工转 JSON/DataAsset |
| 效果类代码 | 81 个类 | `_Project/Scripts/Effects/` | UE 侧参数化效果 + 类型枚举 |

代码规模参照：效果管线（EffectManager + 触发枚举 + 值修饰器）、战斗状态机（BattleManager/TurnManager）、UI（HandManager 等 6 个文件用 DOTween、5 个文件用协程）、地图生成（MapGenerator）、存档（PlayerPrefs）。

---

## 2. 数据层转换方案（工具已就绪）

### 2.1 导出

```bash
cd Porting
python ue5_datatable_export.py          # 输出 CSV 到 Porting/DataTables/
python ue5_datatable_export.py --json   # 同时输出 UE DataTable JSON 格式
```

产物（已生成并验证）：
- `cards.csv` — 72 行：Name/CardType/Rarity/Faction/Cost/Damage/Block/MagicNumber/Exhaust/Tags/Description/EffectIds/InherentEffectIds/IsColorless/IsFactionLocked/CardArtPath
- `effects.csv` — 99 行：Name/EffectClass/Description/ParamsJson（各效果类的序列化字段，键值对 JSON）
- `relics.csv` — 50 行：RelicId/RelicName/Rarity/Faction/Price/四类标志/HiddenActivatorRelicId/BaseEffectsJson/HiddenEffectsJson（含 trigger 与 value1/value2）
- `enemies.csv` — 9 行：Name/MaxHealth/AttackDamage/EnemyType/Description

### 2.2 导入 UE5

1. 内容浏览器右键 → **Import** → 选择 CSV → 导入为 **DataTable**
2. 导入对话框指定行名列为第一列（Name/RelicId）
3. 对应结构体（C++ 声明）：

```cpp
USTRUCT(BlueprintType) struct FEffectEntry {   // BaseEffectsJson 的元素
    GENERATED_BODY()
    UPROPERTY() FString EffectId;
    UPROPERTY() FName Trigger;                 // BattleStart/CardPlayed/...
    UPROPERTY() float Value1 = 0.f, Value2 = 0.f;
};
USTRUCT(BlueprintType) struct FRelicRow : public FTableRowBase {
    GENERATED_BODY()
    UPROPERTY() FString RelicName;
    UPROPERTY() FString Rarity, Faction;
    UPROPERTY() int32 Price = 0;
    UPROPERTY() bool IsShopRelic, IsBossRelic, IsStartingRelic, IsSynthesisTarget;
    UPROPERTY() FString HiddenActivatorRelicId;
    UPROPERTY() FString BaseEffectsJson;       // FJsonObjectConverter 反序列化
    UPROPERTY() FString HiddenEffectsJson;
};
```

### 2.3 生产化建议（移植时再做）

1. **主键 ASCII 化**：目前行名是中文（凝神、燃烧之心），UE FName 支持但检索别扭。建议给卡牌加 `CardId`（如 `C_Defend_Frost_01`），Unity 侧 Card 类已有 `cardId` 字段但资产未填——UE 侧直接补齐。
2. **行引用强类型化**：Unity 用 `effectIds: [字符串]` 运行时按名加载；UE 侧改为 `FDataTableRowHandle`，编辑器内下拉选择、编译期查错，杜绝「资产名拼错静默失效」类问题（本项目曾因此类问题修过 10 个错绑资产）。
3. **美术资源软引用**：`CardArtPath` 字符串 → `TSoftObjectPtr<UTexture2D>`，配合 AssetManager 异步加载。
4. **ParamsJson 反序列化**：效果参数用 `FJsonObjectConverter::JsonObjectStringToUStruct` 转入 `FEffectParams` 结构体，或直接做成 **参数化效果 DataTable**（见第 4 节）。

---

## 3. 架构映射表（Unity → UE5）

| Unity 概念 | UE5 对应物 | 备注 |
|---|---|---|
| MonoBehaviour | `AActor`（场景对象）/ `UActorComponent`（逻辑组件） | 按职责拆分 |
| ScriptableObject 数据资产 | `UDataTable` 行 / `UDataAsset` | 卡牌/遗物/敌人→行；全局配置→DataAsset |
| 单例管理器（BattleManager/RelicManager/…） | `UGameInstanceSubsystem`（全局）/ `UWorldSubsystem`（每局） | 不要再造静态单例 |
| EffectManager 事件总线 + Trigger 枚举 | 自定义 `TMulticastDelegate` + `TMap<FName, TArray<FEffectBinding>>` | 保留原「快照迭代 + 值修饰器链」设计 |
| `ConversionModifier` 静态全局状态 | **UWorldSubsystem 实例成员** | 静态状态是本次移植重点清理对象（见风险 R1） |
| Coroutine（5 个文件） | `FTimerHandle` / 蓝图 Latent Action（Delay） | 逻辑简单，逐处替换 |
| DOTween（6 个文件，手牌/战斗演出） | UMG 动画（WidgetAnimation）/ Timeline | 表现层重做 |
| TextMesh Pro（含 SDF 字体资产） | UMG `RichTextBlock` + **复合字体**（Font Fallback） | TMP 字体资产不可直接导入，需重配字体回退链 |
| Sprite / Image | UMG `Image` + `UTexture2D`（或 Paper2D） | 卡牌游戏用 UMG 即可，无需 Paper2D |
| Prefab | Blueprint / Widget Blueprint | 卡牌、敌人、UI 面板 |
| Resources.Load（13 个文件） | `UDataTable` + `TSoftObjectPtr` 异步加载 | 消除字符串加载 |
| PlayerPrefs 存档 | `USaveGame` + `FJsonObjectConverter` | 存档结构可沿用（卡组/遗物/金币/节点进度） |
| Input.GetMouseButtonDown | Enhanced Input | 鼠标拖牌、点击节点 |
| Scene | `ULevel` / World Partition | 单场景 + 子关卡即可 |
| AudioSource | `AudioComponent` / MetaSound | 音频文件直接复用 |
| 战斗日志（BattleLogManager） | UMG ListView + 文本行 | UI 重写，文案复用 |
| 地图生成（MapGenerator） | C++ 节点图算法移植 | 逻辑与 Unity 无关，照搬算法即可 |

**不建议使用 GAS（Gameplay Ability System）**：卡牌游戏的 Buff/效果只有 8 种 BuffType + 触发管线，状态简单；GAS 的 Attribute/GameplayEffect/Ability 体系学习与维护成本远高于收益。自定义轻量 Buff 数组 + 事件总线（即 Unity 侧现有设计）才是正确形态。

---

## 4. 效果系统：Unity → UE 的推荐形态

Unity 侧现状（合并后）：81 个效果类，全部继承 `CardEffect`（ScriptableObject），资产按名引用。

UE 侧推荐（吸取本次合并的经验，一步到位）：

```
┌─ FEffectRow (DataTable effects) ───────────────┐
│  RowName / EffectType(枚举) / ParamsJson        │
└────────────────────────────────────────────────┘
        │ 按 EffectType 分发到
        ▼
┌─ UEffectBase (C++ 抽象) ──────────────────────┐
│  virtual void Execute(FCombatContext&)          │
│  virtual FString GetDescription(const FCardRow&)│
│  virtual void ResetForBattle()                  │
└────────────────────────────────────────────────┘
        ▲ 实现约 15~20 个参数化效果类（对应本次合并后的 6 个方向）
```

- 卡牌行 `EffectIds` 引用 effects 表的行句柄；遗物行 `BaseEffectsJson` 反序列化出 `{EffectId, Trigger, Value1, Value2}`。
- `FCombatContext` = USTRUCT：`BattleManager* / TargetEnemy / TargetPlayer / SourceCard`（与现有 CombatContext 一一对应）。
- 触发管线 = `BattleManager` 持有 `TMap<FName, TArray<FEffectBinding>>`，触发器遍历时快照拷贝（沿用 Unity 侧已修好的快照迭代语义）。
- **value1 覆盖语义要保留**：RelicManager.ApplyConfigValue 的「配置值优先于资产默认值」规则（刚在 Unity 侧修好）在 UE 侧由 `ParamsJson` 覆盖实现。

---

## 5. 分阶段移植路线图

| 阶段 | 内容 | 工作量（单人全职） |
|---|---|---|
| **P0 数据落地** | UE 项目骨架、DataTable 结构体、CSV 导入与读取、主键 ASCII 化 | 1~2 周 |
| **P1 核心战斗循环** | 能量/抽牌/出牌/回合切换/敌人 AI/胜利失败判定 + 效果管线 + 15~20 个参数化效果 | 6~8 周 |
| **P2 元进程** | 地图生成、商店、遗物、药水、Boss 系列隐藏效果、存档 | 4~6 周 |
| **P3 UI 与演出** | 手牌扇形布局、拖牌交互、战斗日志、动画（DOTween 重做）、音效 | 4~6 周 |
| **P4 收尾** | 平衡调整、本地化（中文原文直用）、Steam 集成、QA | 4~8 周 |

合计：**约 5~7 个月**（单人全职，含学习曲线）；若外包 UI/动画，可压缩 1~2 个月。战斗系统是本项目核心复杂度所在（80+ 效果语义、粘液联动、Boss 隐藏机制），P1 应投入最多时间并尽早做自动化测试（效果单测用 UE Automation 框架）。

---

## 6. 风险与注意点

- **R1 静态全局状态**：`ConversionModifier`（战斗计数器、幻影减伤、Boss 标志）与 `SlimeTriggerGuard` 是 `static`，跨战斗污染是 Unity 侧修过一轮的老问题。UE 侧必须做成 `UWorldSubsystem` 实例，随世界创建/销毁清零。
- **R2 字符串效果引用**：Unity 按名加载（已发现过 10 个错绑资产、多个幽灵引用）。UE 侧用 `FDataTableRowHandle` 彻底消灭此类问题。
- **R3 中文文本**：UE 默认字体不含中文字形，需配置复合字体（如思源黑体 Noto Sans CJK）作为回退；TMP 的 SDF 字体资产无法迁移，需用原 TTF 重做。
- **R4 演出动画**：手牌打牌手感（DOTween 缓动）是卡牌游戏体验核心，UE 侧 WidgetAnimation 需要专门打磨，不能简单平移。
- **R5 随机数**：Unity `Random.Range` 与 UE `FMath::RandRange` 序列不同。若未来做种子回放/竞技，统一用 `FRandomStream` 并注入到所有随机点。
- **R6 数据一致性**：两套导出 CSV 需纳入版本管理，改卡牌数值只在一边改（建议 UE 侧 DataTable 为唯一数据源后，Unity 侧停止改动，或反向再导一次）。
- **R7 效果语义回归**：81 个效果类中有大量细节语义（如「magicNumber 覆盖」「第一回合不减持续」这类刚修过的坑）。P1 必须为每个效果写单测 + 对照 Unity 行为逐一验收。

---

## 7. 工作量估算

| 项目 | 估算 |
|---|---|
| 数据转换（已完成原型，生产化补主键） | 1~2 周 |
| 战斗逻辑重写（含全部效果语义） | 2~3 个月 |
| 元进程系统 | 1~1.5 个月 |
| UI/动画/音频集成 | 1~1.5 个月 |
| 打磨/QA/Steam | 1~2 个月 |
| **总计** | **5~7 个月（单人全职）** |

对比在 Unity 内继续完成游戏（现状继续开发至可上架）：**移植会额外增加 5~7 个月成本**。若目标是尽快上架 Steam，Unity 直发更划算（引擎对最终玩家不可见，Unity 个人版免费额度和 Steam 抽成与 UE 完全一致——详见此前会话的定价分析）；只有当团队长期技术栈是 UE、或后续项目都要在 UE 做时，移植才值得。

---

## 8. 建议

1. **数据侧**：本导出器已把 4 类数据全部转出，UE 侧随时可开工；建议现在就把 `Porting/DataTables/` 纳入 git，作为「数据规格文档」。
2. **逻辑侧**：不翻译代码，按第 4 节形态重写效果管线，把 Unity 侧合并经验（参数化、配置值优先、战斗边界重置）直接落实为 UE 侧初始设计。
3. **决策**：若决定移植，从 P0+P1 做 2 个月 PoC（抽牌/出牌/打伤害一条链路 + 5 个效果），PoC 通过再投入完整开发。
