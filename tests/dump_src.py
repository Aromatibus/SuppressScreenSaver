from pathlib import Path
from collections import defaultdict

# ==========================================================
# 設定
# ==========================================================

SCRIPT_DIR = Path(__file__).resolve().parent

BASE_DIR = SCRIPT_DIR.parent / "src"

DUMP_FILE = (
    SCRIPT_DIR
    / f"dump_{BASE_DIR.name}.md"
)



EXCLUDE_FILES = {
    ".gitignore",
    "package-lock.json",
}

EXCLUDE_DIRS = {
    ".git",
    ".idea",
    ".vscode",
    "__pycache__",
    "node_modules",
    "venv",
}

ENCODINGS = (
    "utf-8",
    "utf-8-sig",
    "cp932",
    "shift_jis",
)


# ==========================================================
# 判定
# ==========================================================

def is_excluded(path: Path) -> bool:
    relative = path.relative_to(BASE_DIR)

    # フォルダ除外
    if any(part in EXCLUDE_DIRS for part in relative.parts[:-1]):
        return True

    # ファイル除外
    if path.name in EXCLUDE_FILES:
        return True

    return False


def read_text(path: Path) -> str | None:
    """テキストなら内容を返す"""

    for enc in ENCODINGS:
        try:
            return path.read_text(encoding=enc)
        except (UnicodeDecodeError, OSError):
            pass

    return None


# ==========================================================
# Tree生成
# ==========================================================

def make_tree(paths: list[Path]) -> str:
    """
    paths は BASE_DIR からの相対Path
    """

    tree = defaultdict(set)

    for p in paths:
        parent = Path()

        for part in p.parts[:-1]:
            tree[parent].add((part, True))
            parent = parent / part

        tree[parent].add((p.name, False))

    lines = [BASE_DIR.name + "/"]

    def walk(parent: Path, prefix: str):

        items = sorted(
            tree[parent],
            key=lambda x: (not x[1], x[0].lower())  # フォルダ→ファイル
        )

        for i, (name, is_dir) in enumerate(items):

            last = i == len(items) - 1

            branch = "└─ " if last else "├─ "

            lines.append(prefix + branch + (name + "/" if is_dir else name))

            if is_dir:
                walk(
                    parent / name,
                    prefix + ("   " if last else "│  ")
                )

    walk(Path(), "")

    return "\n".join(lines)


# ==========================================================
# メイン
# ==========================================================

def main():

    print("走査中...")

    targets: list[tuple[Path, str]] = []

    for path in sorted(BASE_DIR.rglob("*")):

        if not path.is_file():
            continue

        if is_excluded(path):
            continue

        text = read_text(path)

        if text is None:
            continue

        relative = path.relative_to(BASE_DIR)

        targets.append((relative, text))

    print(f"対象ファイル数 : {len(targets)}")

    tree = make_tree([p for p, _ in targets])

    print("出力中...")

    with DUMP_FILE.open("w", encoding="utf-8", newline="\n") as f:

        f.write("# Tree\n\n")
        f.write("```text\n")
        f.write(tree)
        f.write("\n```\n\n")

        f.write("# text\n\n")

        for relative, text in targets:

            f.write(f"{relative.as_posix()}\n\n")
            f.write("```text\n")
            f.write(text)

            if not text.endswith("\n"):
                f.write("\n")

            f.write("```\n\n")

    print(f"完了 : {DUMP_FILE}")


if __name__ == "__main__":
    main()
