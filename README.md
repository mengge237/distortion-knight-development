# Distortion Knight（异变棋局）

> 基于 Unity 的类《杀戮尖塔》卡牌 Roguelike 游戏。在程序化生成的棋局地图中探索、与变异敌人战斗，构筑卡组与遗物组合，挑战最终 Boss。

## 核心玩法

- **棋局地图**：多层程序化节点地图（起点 → 普通怪/精英怪/商店/宝箱/事件/休息 → Boss）
- **回合制战斗**：能量 + 抽牌 + 出牌 + 格挡 + 敌人意图预告
- **六大阵营**：鲜血 / 寒霜 / 暗影 / 粘液 / 腐化 / 不舍，各具专属卡牌与遗物
- **卡组构筑**：战斗奖励、商店购买、遗物合成（剑刃碎片 + 剑柄碎片 → 剑之核心）
- **遗物系统**：50 种遗物（普通/稀有/传说/Boss/阵营），阵营遗物由 Boss 激活器解锁隐藏效果
- **药水系统**：战斗掉落 + 商店购买，楼层越深稀有度越高
- **商店**：模仿杀戮尖塔布局（遗物顶部、药水右上、卡牌居中、移除服务左下），含打折、补货符、卡牌移除服务

## 技术栈

| 项目 | 详情 |
|------|------|
| 引擎 | Unity 2022.3.62f3c1 |
| 渲染管线 | Universal Render Pipeline (URP) |
| UI | UGUI + TextMesh Pro + DOTween |
| 数据驱动 | ScriptableObject（卡牌 / 效果 / 遗物 / 药水 / 节点蓝图 / 配置） |
| 命名空间 | `MutationChess` |

## 快速开始

1. Unity Hub 安装 **2022.3.62f3c1**（含 URP、Windows Build Support）
2. 克隆仓库：`git clone https://gitee.com/mengge237/distortion-knight-development.git`
3. Unity Hub → Add project from disk → 选择仓库根目录
4. 打开场景 `Assets/_Project/Scenes/MainScene.unity`

## AI/MCP 集成（Claude Code 操作 Unity）

项目内置 [MCP for Unity](https://github.com/CoplayDev/unity-mcp)，Claude Code 可经由 MCP 直接操控 Unity 编辑器（改场景/资产、执行菜单命令、运行 C# 等）：

1. 前置依赖：Python 3.11+ 与 [uv](https://docs.astral.sh/uv/)（首次导入时向导会引导安装）
2. Unity 打开项目 → 自动解析 `Packages/manifest.json` 中的 `com.coplaydev.unity-mcp` → 弹设置向导确认 Python/uv → Done
3. 日常使用：`Window → MCP for Unity → Start Server`（状态面板显示 Connected，监听 `http://localhost:8080/mcp`）
4. Claude Code 通过根目录 `.mcp.json` 连接；**重启会话或 `/mcp` 重连**后 MCP 工具生效
5. Unity 未启动时 MCP 工具不可用，Claude 会退回直接修改文件的方式

## 调试台

- 按键 `~` 或 `F1` 开关，含卡牌 / 遗物 / 战斗 / 玩家 / 地图 / 日志六个页面，全中文界面
- 阵营遗物按阵营分区显示，添加时自动附带对应 Boss 激活器遗物
- 战斗页含**无敌开关**（敌人攻击不掉血）
- **文件开关**（正式包）：仅当 exe 同级目录或 StreamingAssets 下存在 `debug_enable` 或 `debug_enable.txt` 标记文件时才启用；开发构建与编辑器内始终可用

## 文档

| 文档 | 说明 |
|------|------|
| [docs/Wiki](./docs/Wiki/01-首页.md) | 技术文档：架构设计、代码约定、已知局限 |
| [docs/知识库](./docs/知识库/01-项目概览.md) | 项目概览、开发进度、开发日志 |

## 里程碑

| 日期 | 内容 |
|------|------|
| 2026-07-08 | 项目初始化 |
| 2026-07-10 | 手牌扇形布局 + 战斗日志 UI 框架 |
| 2026-07-28 | 卡牌资产化 + 遗物系统 + 图标资源 |
| 2026-07-31 | 卡牌描述系统：全项目零 GBK，44 个效果动态生成中文说明 |
| 2026-08-15 | 效果类合并（21→6 参数化）、Assets 目录商业级重构、调试台重设计（阵营分区 + 中国特色配色） |
| 2026-08-15 | 全项目编码统一 UTF-8 无 BOM、硬编码清理（GameIds 统一 ID/路径常量）、GameConfig/ShopConfig 配置资产化 |
| 2026-08-15 | 调试台文件开关化 + 无敌功能；商店改造为杀戮尖塔式布局（真实卡牌移除弹窗、购买反馈） |

## 仓库

- **Gitee**：https://gitee.com/mengge237/distortion-knight-development
- **分支约定**：`master` 主分支；开发分支按日期命名（如 `8.15.1`）
