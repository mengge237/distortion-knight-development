#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
资产符合性校验（维护工具）

校验四类引用与结构，发现「幽灵引用 / 错绑资产 / 过期字段」并给出清单：
  1. Resources/Effects 与 Resources/InherentEffects 下每个 .asset 的 m_Script guid
     必须对应现存的效果类 .cs；
  2. 每个效果资产的序列化字段必须属于其目标类（含基类 CardEffect 的公共字段）；
  3. 卡牌资产（Cards/**）的 effectIds / inherentEffectIds 每个条目必须解析到
     现存效果资产文件（按资产名，不含 .asset 后缀）；
  4. RelicBalanceConfig.cs 的 effectId 引用必须解析到现存效果资产；
     hiddenActivatorRelicId 必须指向配置内现存的 relicId。

用法：
  python check_asset_conformance.py            # 报告问题，退出码非 0 表示有问题
  python check_asset_conformance.py --fix      # （保留参数）当前仅报告，修复需人工/专用脚本
"""

import os
import re
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
FX_SRC = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts", "Effects")
FX_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Resources", "Effects")
INH_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Resources", "InherentEffects")
CARDS_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Resources", "Cards")
CARD_ASSET_CLS = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts",
                              "Core", "CardDataAsset.cs")
RELIC_CONFIG = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Scripts",
                            "Config", "RelicBalanceConfig.cs")


def read_text(path):
    return open(path, encoding="utf-8-sig", errors="replace").read()


# ---------------------------------------------------------------- 1. guid -> 类名
def build_guid_map():
    guid_map = {}
    if not os.path.isdir(FX_SRC):
        return guid_map
    for fn in os.listdir(FX_SRC):
        if not fn.endswith(".cs.meta"):
            continue
        m = re.search(r"guid:\s*([0-9a-f]{32})",
                      read_text(os.path.join(FX_SRC, fn)))
        if m:
            guid_map[m.group(1)] = fn[: -len(".cs.meta")]
    return guid_map


# ---------------------------------------------------------------- 2. 效果资产校验
def collect_class_fields(cls_path):
    """目标类 + 基类 CardEffect 的可序列化字段名。
    Unity 序列化规则：public 非 static 字段；private/protected 字段仅当带 [SerializeField]。"""
    fields = set()
    text = read_text(cls_path)
    lines = text.splitlines()
    for i, line in enumerate(lines):
        m = re.match(
            r"\s*(?:\[SerializeField\]\s*)?(public|private|protected|internal)\s+"
            r"(?:static\s+)?(?:readonly\s+)?[\w<>,\.\[\]\s]+\s+(\w+)\s*[;={\[]", line)
        if not m:
            continue
        vis, name = m.group(1), m.group(2)
        if vis == "public" and "static" in line:
            continue  # static 不序列化
        if vis in ("private", "protected", "internal"):
            # 需带 [SerializeField]（同行，或上一非空行）
            if "[SerializeField]" not in line:
                prev = ""
                for j in range(i - 1, -1, -1):
                    if lines[j].strip():
                        prev = lines[j].strip()
                        break
                if not prev.startswith("[SerializeField]"):
                    continue
        fields.add(name)
    return fields


def collect_with_base(cls_name):
    fields = collect_class_fields(os.path.join(FX_SRC, cls_name + ".cs"))
    base_path = os.path.join(FX_SRC, "CardEffect.cs")
    if os.path.isfile(base_path):
        fields |= collect_class_fields(base_path)
    return fields


def check_effect_assets(guid_map, problems):
    names = set()
    for d in (FX_DIR, INH_DIR):
        if not os.path.isdir(d):
            continue
        for fn in sorted(os.listdir(d)):
            if not fn.endswith(".asset"):
                continue
            rel = os.path.relpath(os.path.join(d, fn), PROJECT_ROOT)
            names.add(fn[:-6])
            text = read_text(os.path.join(d, fn))
            m = re.search(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", text)
            if not m:
                problems.append(f"[{rel}] 找不到 m_Script 引用")
                continue
            cls = guid_map.get(m.group(1))
            if cls is None:
                problems.append(f"[{rel}] m_Script guid {m.group(1)} 无对应效果类（类已删除？）")
                continue
            asset_fields = set(re.findall(r"^  ([A-Za-z][A-Za-z0-9]*):", text, re.M))
            class_fields = collect_with_base(cls)
            unknown = asset_fields - class_fields
            if unknown:
                problems.append(f"[{rel}] 类 {cls} 不认识的字段: {sorted(unknown)}")
    return names


# ---------------------------------------------------------------- 3. 卡牌资产校验
def check_card_assets(guid_map, problems):
    """每张卡牌资产的 m_Script 必须指向 CardDataAsset，且序列化字段 ⊆ 类字段。"""
    cls_guid = None
    meta_path = CARD_ASSET_CLS + ".meta"
    if os.path.isfile(meta_path):
        m = re.search(r"guid:\s*([0-9a-f]{32})", read_text(meta_path))
        cls_guid = m.group(1) if m else None
    cls_fields = collect_class_fields(CARD_ASSET_CLS) if os.path.isfile(CARD_ASSET_CLS) else set()
    if cls_guid is None or not cls_fields:
        problems.append("[CardDataAsset] 找不到类定义或其 meta guid")
        return
    if not os.path.isdir(CARDS_DIR):
        return
    for root, _, files in os.walk(CARDS_DIR):
        for fn in files:
            if not fn.endswith(".asset"):
                continue
            p = os.path.join(root, fn)
            rel = os.path.relpath(p, PROJECT_ROOT)
            text = read_text(p)
            m = re.search(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", text)
            if not m:
                problems.append(f"[{rel}] 找不到 m_Script 引用")
                continue
            if m.group(1) != cls_guid:
                bound = guid_map.get(m.group(1), m.group(1))
                problems.append(f"[{rel}] m_Script 不是 CardDataAsset（绑定到 {bound}）")
                continue
            asset_fields = set(re.findall(r"^  ([A-Za-z][A-Za-z0-9]*):", text, re.M))
            unknown = asset_fields - cls_fields
            if unknown:
                problems.append(f"[{rel}] CardDataAsset 不认识的字段: {sorted(unknown)}")


# ---------------------------------------------------------------- 4. 卡牌引用校验
def parse_list_blocks(text):
    """解析 `  key:` 后跟 `  - item` 的列表块，返回 {key: [items]}。"""
    result = {}
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        m = re.match(r"^  ([A-Za-z][A-Za-z0-9]*):\s*$", lines[i])
        if m:
            key = m.group(1)
            items = []
            j = i + 1
            while j < len(lines) and re.match(r"^  - (.+)$", lines[j]):
                item = re.match(r"^  - (.+)$", lines[j]).group(1)
                # 还原 \uXXXX 转义
                item = re.sub(r"\\u([0-9a-fA-F]{4})",
                              lambda mm: chr(int(mm.group(1), 16)), item)
                if item.startswith('"') and item.endswith('"'):
                    item = item[1:-1]
                items.append(item)
                j += 1
            if items:
                result[key] = items
            i = j
        else:
            i += 1
    return result


def check_card_refs(valid_names, problems):
    if not os.path.isdir(CARDS_DIR):
        return
    for root, _, files in os.walk(CARDS_DIR):
        for fn in files:
            if not fn.endswith(".asset"):
                continue
            p = os.path.join(root, fn)
            rel = os.path.relpath(p, PROJECT_ROOT)
            blocks = parse_list_blocks(read_text(p))
            for key in ("effectIds", "inherentEffectIds"):
                for ref in blocks.get(key, []):
                    if ref not in valid_names:
                        problems.append(f"[{rel}] {key} 引用不存在的效果资产: {ref}")


# ---------------------------------------------------------------- 5. 遗物配置引用校验
def check_relic_config(valid_names, problems):
    if not os.path.isfile(RELIC_CONFIG):
        return
    text = read_text(RELIC_CONFIG)
    relic_ids = set()
    for m in re.finditer(r"relicId\s*=\s*\"([^\"]*)\"", text):
        relic_ids.add(m.group(1))
    for m in re.finditer(r"effectId\s*=\s*\"([^\"]*)\"", text):
        if m.group(1) not in valid_names:
            problems.append(f"[RelicBalanceConfig.cs] effectId 引用不存在的效果资产: {m.group(1)}")
    for m in re.finditer(r"hiddenActivatorRelicId\s*=\s*\"([^\"]*)\"", text):
        rid = m.group(1)
        if rid and rid not in relic_ids:
            problems.append(f"[RelicBalanceConfig.cs] hiddenActivatorRelicId 指向不存在的遗物: {rid}")


def main():
    guid_map = build_guid_map()
    print(f"效果类（含 .meta guid）: {len(guid_map)} 个")
    problems = []
    valid_names = check_effect_assets(guid_map, problems)
    print(f"效果资产（Effects + InherentEffects）: {len(valid_names)} 个")
    check_card_assets(guid_map, problems)
    check_card_refs(valid_names, problems)
    check_relic_config(valid_names, problems)

    if problems:
        print(f"\n发现 {len(problems)} 个问题：")
        for p in problems:
            print("  " + p)
        return 1
    print("\n全部通过：资产类绑定、字段、卡牌引用、遗物引用均有效。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
