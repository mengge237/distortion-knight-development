#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
全项目编码统一（维护工具）

把仓库内所有文本类文件统一为 UTF-8 无 BOM：
  - GBK / GB18030 编码文件 → 转码为 UTF-8（Unity/C# 只认 UTF-8，GBK 会导致
    中文注释乱码或编译错误）；
  - 带 UTF-8 BOM 的文件 → 去掉 BOM；
  - 二进制文件（含 \\x00 字节）自动跳过；
  - Library/Temp/Obj 等生成目录自动跳过。

用法：
  python normalize_encoding.py            # 实际执行转码，输出报告
  python normalize_encoding.py --check    # 仅检查不写文件

退出码 0 = 无问题（或已全部修复）；1 = 存在无法识别的编码文件，需人工处理。
"""

import os
import re
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)

TEXT_EXT = {
    ".cs", ".asset", ".meta", ".json", ".md", ".txt", ".py", ".prefab",
    ".unity", ".mat", ".shader", ".asmdef", ".csv", ".xml", ".yaml", ".yml",
    ".bat", ".sh", ".html", ".css", ".js", ".rsp",
}
SKIP_DIRS = {
    ".git", "Library", "Temp", "Obj", "obj", "bin", "Logs", "Build",
    "Builds", "UserSettings", ".vs", ".idea", "node_modules",
    "MemoryCaptures", "Recordings", "CrashReports",
}

report = []
undecodable = []


def normalize(path):
    with open(path, "rb") as f:
        raw = f.read()
    if b"\x00" in raw[:4096]:
        return  # 二进制
    bom = raw.startswith(b"\xef\xbb\xbf")
    body = raw[3:] if bom else raw
    try:
        text = body.decode("utf-8")
        if not bom:
            return  # 已符合
        status = "utf8-bom"
    except UnicodeDecodeError:
        for enc in ("gb18030", "gbk", "big5"):
            try:
                text = body.decode(enc)
                status = enc
                break
            except UnicodeDecodeError:
                continue
        else:
            undecodable.append(path)
            return
    if "--check" in sys.argv:
        report.append((os.path.relpath(path, PROJECT_ROOT), status))
        return
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    report.append((os.path.relpath(path, PROJECT_ROOT), status))


def main():
    for root, dirs, files in os.walk(PROJECT_ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for fn in files:
            ext = os.path.splitext(fn)[1].lower()
            if ext not in TEXT_EXT:
                continue
            normalize(os.path.join(root, fn))

    if report:
        mode = "发现（未修改）" if "--check" in sys.argv else "已统一为 UTF-8 无 BOM"
        print(f"{mode} {len(report)} 个文件：")
        for rel, enc in report:
            print(f"  [{enc}] {rel}")
    else:
        print("全部文本文件已是 UTF-8 无 BOM。")

    if undecodable:
        print(f"\n警告：{len(undecodable)} 个文件无法按 UTF-8/GBK/GB18030/Big5 解码，需人工处理：")
        for p in undecodable:
            print("  " + os.path.relpath(p, PROJECT_ROOT))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
