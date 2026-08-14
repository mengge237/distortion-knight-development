#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UE5 数据表导出原型（Plan C 数据转换部分）

把《异变棋局》的全部游戏数据从 Unity 侧导出为 UE5 DataTable 可导入的 CSV：
  1. cards.csv    —— 卡牌（Assets/TextMesh Pro/Resources/Cards/**/*.asset）
  2. effects.csv  —— 效果（Assets/TextMesh Pro/Resources/Effects/*.asset）
  3. relics.csv   —— 遗物（RelicBalanceConfig.cs 代码配置，正则解析）
  4. enemies.csv  —— 敌人（Enemy.cs CreateDefaultEnemies 代码配置，正则解析）

用法：
  python ue5_datatable_export.py            # 输出到 Porting/DataTables/
  python ue5_datatable_export.py --json     # 同时输出 UE DataTable JSON 格式

导入 UE5 步骤（详见 UE5_移植评估.md 第 3 节）：
  CSV：Content 右键 → Import → DataTable，行名列选第一列（Name），
       结构体字段需与 FCardRow/FEffectRow/... 一一对应。
  说明：嵌套结构（效果列表、参数）以 JSON 字符串形式存放在单列，
       UE 侧用 FJsonObjectConverter 反序列化。

依赖：仅标准库（Python 3.7+）。在仓库根目录或任意位置运行均可。
"""

import argparse
import csv
import json
import os
import re
import sys

# ---------------------------------------------------------------- 路径解析
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
CARDS_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Resources", "Cards")
FX_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Resources", "Effects")
OUT_DIR = os.path.join(SCRIPT_DIR, "DataTables")


# ---------------------------------------------------------------- YAML 工具
def unescape_unicode(s):
    """Unity 把非 ASCII 字符写成 \\uXXXX 转义，还原为真实 UTF-8。"""
    def repl(m):
        return chr(int(m.group(1), 16))
    return re.sub(r"\\u([0-9a-fA-F]{4})", repl, s)


def clean_value(raw):
    """去掉 YAML 双引号并还原 Unity 的 \\uXXXX / \\" / \\\\ 转义。"""
    raw = raw.strip()
    if raw.startswith('"') and raw.endswith('"'):
        raw = raw[1:-1]
    raw = raw.replace(r'\"', '"').replace(r'\\', '\\')
    return unescape_unicode(raw)


def parse_asset(path):
    """极简 YAML 解析：只取 MonoBehaviour 块中 `key: value` 行与列表块。
    返回 {key: str}。列表（effectIds: 下的 `- Xxx`）存为逗号分隔字符串。"""
    text = open(path, encoding="utf-8-sig", errors="replace").read()
    result = {}
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        s = lines[i]
        m = re.match(r"^  ([A-Za-z][A-Za-z0-9]*):\s?(.*)$", s)
        if m and not m.group(1).startswith("m_"):
            key, val = m.group(1), m.group(2)
            if val.strip() in ("", "[]"):
                # 列表块：读取后续 `- 项` 行
                items = []
                j = i + 1
                while j < len(lines) and re.match(r"^  - (.+)$", lines[j]):
                    items.append(clean_value(re.match(r"^  - (.+)$", lines[j]).group(1)))
                    j += 1
                result[key] = ",".join(items)
                i = j
                continue
            result[key] = clean_value(val)
        i += 1
    result["__asset_name__"] = clean_value(result.get("m_Name", os.path.basename(path)[:-6]))
    return result


# ---------------------------------------------------------------- 1. 卡牌
CARD_TYPES = {0: "Attack", 1: "Skill", 2: "Power", 3: "Curse", 4: "Status"}
CARD_RARITIES = {0: "Basic", 1: "Common", 2: "Uncommon", 3: "Rare"}
CARD_FACTIONS = {0: "None", 1: "Blood", 2: "Frost", 3: "Shadow", 4: "Slime",
                 5: "Corruption", 6: "Curse", 7: "Reluctant"}


def export_cards():
    rows = []
    for root, dirs, files in os.walk(CARDS_DIR):
        for fn in sorted(files):
            if not fn.endswith(".asset"):
                continue
            d = parse_asset(os.path.join(root, fn))
            rows.append({
                "Name": d.get("cardName", d["__asset_name__"]),
                "CardType": CARD_TYPES.get(int(d.get("cardType", 0)), "Unknown"),
                "Rarity": CARD_RARITIES.get(int(d.get("rarity", 1)), "Unknown"),
                "Faction": CARD_FACTIONS.get(int(d.get("faction", 0)), "None"),
                "Cost": int(d.get("cost", 1)),
                "Damage": int(d.get("damage", 0)),
                "Block": int(d.get("block", 0)),
                "MagicNumber": int(d.get("magicNumber", 0)),
                "Exhaust": int(d.get("exhaust", 0)),
                "Tags": d.get("tags", ""),
                "Description": d.get("description", ""),
                "EffectIds": d.get("effectIds", ""),
                "InherentEffectIds": d.get("inherentEffectIds", ""),
                "IsColorless": int(d.get("isColorless", 0)),
                "IsFactionLocked": int(d.get("isFactionLocked", 0)),
                "CardArtPath": d.get("cardArtPath", ""),
            })
    return rows


# ---------------------------------------------------------------- 2. 效果
def build_guid_class_map():
    """Effects/*.cs.meta 的 guid -> 类名 映射（用于解析资产脚本类型）。"""
    guid_map = {}
    if os.path.isdir(os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts", "Effects")):
        fx_src = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts", "Effects")
        for fn in os.listdir(fx_src):
            if not fn.endswith(".cs.meta"):
                continue
            meta = open(os.path.join(fx_src, fn), encoding="utf-8-sig").read()
            m = re.search(r"guid:\s*([0-9a-f]{32})", meta)
            if m:
                guid_map[m.group(1)] = fn[:-len(".cs.meta")]
    return guid_map


def export_effects(guid_map):
    rows = []
    if not os.path.isdir(FX_DIR):
        return rows
    for fn in sorted(os.listdir(FX_DIR)):
        if not fn.endswith(".asset"):
            continue
        path = os.path.join(FX_DIR, fn)
        text = open(path, encoding="utf-8-sig", errors="replace").read()
        m = re.search(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", text)
        cls = guid_map.get(m.group(1), "Unknown") if m else "Unknown"
        d = parse_asset(path)
        params = {k: v for k, v in d.items() if not k.startswith("__") and k != "effectDescription"}
        rows.append({
            "Name": d["__asset_name__"],
            "EffectClass": cls,
            "Description": d.get("effectDescription", ""),
            "ParamsJson": json.dumps(params, ensure_ascii=False),
        })
    return rows


# ---------------------------------------------------------------- 3. 遗物
def parse_relic_config():
    """正则解析 RelicBalanceConfig.cs 的 CreateDefaultConfig() 代码配置。
    格式固定（代码生成），解析失败率低；若未来条目格式变化，改为手工维护 CSV。"""
    path = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts",
                        "Config", "RelicBalanceConfig.cs")
    text = open(path, encoding="utf-8-sig", errors="replace").read()

    rows = []
    for entry in re.finditer(
            r"config\.entries\.Add\(new RelicBalanceEntry\s*\{(.*?)\}\);", text, re.S):
        body = entry.group(1)

        def get_str(key):
            m = re.search(rf'{key}\s*=\s*"([^"]*)"', body)
            return m.group(1) if m else ""

        def get_enum(key):
            m = re.search(rf'{key}\s*=\s*(\w+)\.(\w+)', body)
            return m.group(2) if m else ""

        def get_int(key):
            m = re.search(rf'{key}\s*=\s*(\d+)', body)
            return m.group(1) if m else "0"

        def get_bool(key):
            m = re.search(rf'{key}\s*=\s*(true|false)', body)
            return "1" if m and m.group(1) == "true" else "0"

        def get_effects(key):
            m = re.search(rf'{key}\s*=\s*new List<RelicEffectEntry>\s*\{{(.*?)\}}', body, re.S)
            if not m:
                return ""
            effects = []
            for e in re.finditer(r'new RelicEffectEntry\s*\{([^}]*)\}', m.group(1)):
                eb = e.group(1)
                em = re.search(r'effectId\s*=\s*"([^"]*)"', eb)
                tm = re.search(r'trigger\s*=\s*EffectTrigger\.(\w+)', eb)
                v1 = re.search(r'value1\s*=\s*([\d.]+)f?', eb)
                v2 = re.search(r'value2\s*=\s*([\d.]+)f?', eb)
                effects.append({
                    "effectId": em.group(1) if em else "",
                    "trigger": tm.group(1) if tm else "",
                    "value1": float(v1.group(1)) if v1 else 0.0,
                    "value2": float(v2.group(1)) if v2 else 0.0,
                })
            return json.dumps(effects, ensure_ascii=False)

        rows.append({
            "RelicId": get_str("relicId"),
            "RelicName": get_str("relicName"),
            "Rarity": get_enum("rarity"),
            "Faction": get_enum("faction"),
            "Price": get_int("price"),
            "IsShopRelic": get_bool("isShopRelic"),
            "IsBossRelic": get_bool("isBossRelic"),
            "IsStartingRelic": get_bool("isStartingRelic"),
            "IsSynthesisTarget": get_bool("isSynthesisTarget"),
            "HiddenActivatorRelicId": get_str("hiddenActivatorRelicId"),
            "BaseEffectsJson": get_effects("baseEffectIds"),
            "HiddenEffectsJson": get_effects("hiddenEffectIds"),
        })
    return rows


# ---------------------------------------------------------------- 4. 敌人
ENEMY_TYPES = {"Normal": 0, "Elite": 1, "Boss": 2, "Event": 3}


def parse_enemies():
    """解析 Enemy.cs 中 CreateDefaultEnemies 的 new EnemyData(...) 调用。"""
    path = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts",
                        "Battle", "Enemy.cs")
    text = open(path, encoding="utf-8-sig", errors="replace").read()
    rows = []
    for i, m in enumerate(re.finditer(
            r'new EnemyData\("([^"]+)",\s*(\d+),\s*(\d+),\s*EnemyType\.(\w+)(?:,\s*"([^"]*)")?\)',
            text)):
        rows.append({
            "Name": m.group(1),
            "MaxHealth": int(m.group(2)),
            "AttackDamage": int(m.group(3)),
            "EnemyType": ENEMY_TYPES.get(m.group(4), 0),
            "Description": m.group(5) or "",
        })
    return rows


# ---------------------------------------------------------------- 输出
def write_csv(filename, rows):
    if not rows:
        print(f"  [跳过] {filename}: 无数据")
        return
    path = os.path.join(OUT_DIR, filename)
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    print(f"  [CSV] {filename}: {len(rows)} 行")


def write_json(filename, rows):
    if not rows:
        return
    path = os.path.join(OUT_DIR, filename)
    with open(path, "w", encoding="utf-8-sig") as f:
        json.dump(rows, f, ensure_ascii=False, indent=1)
    print(f"  [JSON] {filename}: {len(rows)} 行")


def main():
    parser = argparse.ArgumentParser(description="UE5 DataTable 导出原型")
    parser.add_argument("--json", action="store_true", help="同时输出 UE DataTable JSON 格式")
    args = parser.parse_args()

    os.makedirs(OUT_DIR, exist_ok=True)
    print(f"项目根目录: {PROJECT_ROOT}")
    print(f"输出目录: {OUT_DIR}\n")

    guid_map = build_guid_class_map()
    cards = export_cards()
    effects = export_effects(guid_map)
    relics = parse_relic_config()
    enemies = parse_enemies()

    print(f"数据盘点: 卡牌 {len(cards)} / 效果 {len(effects)} / 遗物 {len(relics)} / 敌人 {len(enemies)}\n")

    write_csv("cards.csv", cards)
    write_csv("effects.csv", effects)
    write_csv("relics.csv", relics)
    write_csv("enemies.csv", enemies)
    if args.json:
        write_json("cards.json", cards)
        write_json("effects.json", effects)
        write_json("relics.json", relics)
        write_json("enemies.json", enemies)

    print("\n完成。导入 UE5 的步骤见 UE5_移植评估.md。")


if __name__ == "__main__":
    main()
