# copilot_build_instructions.py
#
# Скрипт собирает единый файл инструкций Copilot в порядке:
#   1) .github/copilot/copilot-instructions.base.md
#   2) остальные .github/copilot/*.md (кроме base), по алфавиту
#   3) заголовок "# Архитектура решения"
#   4) все readme.md из директорий, где есть *.csproj (по алфавиту)
#
# Между каждой частью вставляются две пустые строки.
# Итог сохраняется в .github/copilot-instructions.md (UTF-8 без BOM).
#
# --- Использование ---
# python copilot_build_instructions.py [флаги]
#
# Доступные флаги:
#   --root <path>        Корень репозитория (по умолчанию текущая папка).
#   --base <path>        Путь к базовому файлу (по умолчанию .github/copilot/copilot-instructions.base.md).
#   --copilot-dir <path> Каталог с прочими *.md (по умолчанию .github/copilot).
#   --output <path>      Путь к итоговому файлу (по умолчанию .github/copilot-instructions.md).
#   --encoding <enc>     Кодировка всех файлов (по умолчанию utf-8).
#   --verbose            Подробный вывод.
#   --dry-run            Только показать план (ничего не записывать).
#   --no-wait            Не ждать 5 секунд перед выходом.
#
# Примеры:
#   python copilot_build_instructions.py
#   python copilot_build_instructions.py --verbose --dry-run
#   python copilot_build_instructions.py --encoding cp1251 --no-wait
#
# -----------------------------------------

import sys
import os
import argparse
import logging
import time
from typing import List, Tuple

# --- ЛОГИРОВАНИЕ -------------------------------------------------------------

def setup_logging(verbose: bool) -> None:
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(level=level, format="%(levelname)s: %(message)s")

logger = logging.getLogger("copilot-builder")

# --- ВСПОМОГАТЕЛЬНЫЕ ---------------------------------------------------------

def ensure_dir_for_file(path: str) -> None:
    d = os.path.dirname(path)
    if d and not os.path.exists(d):
        os.makedirs(d, exist_ok=True)

def normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")

def is_github_dir(path: str) -> bool:
    parts = {p.lower() for p in path.split(os.sep)}
    return ".github" in parts

def read_text(path: str, encoding: str) -> Tuple[bool, str]:
    try:
        with open(path, "r", encoding=encoding, newline=None) as f:
            txt = f.read()
        return True, normalize_newlines(txt)
    except Exception as e:
        logger.error("Ошибка чтения %s: %s", path, e)
        return False, ""

def write_text(path: str, text: str, encoding: str) -> bool:
    try:
        ensure_dir_for_file(path)
        with open(path, "w", encoding=encoding, newline="\n") as f:
            f.write(text)
        return True
    except Exception as e:
        logger.error("Ошибка записи %s: %s", path, e)
        return False

# --- ПОИСК ФАЙЛОВ ------------------------------------------------------------

def list_copilot_md(base_dir: str, base_filename: str) -> List[str]:
    results: List[str] = []
    if not os.path.isdir(base_dir):
        return results
    for name in os.listdir(base_dir):
        full = os.path.join(base_dir, name)
        if not os.path.isfile(full):
            continue
        if not name.lower().endswith(".md"):
            continue
        if name == base_filename:
            continue
        results.append(os.path.normpath(full))
    results.sort(key=lambda p: os.path.basename(p).lower())
    return results

def find_project_readmes(root: str, ignore_dirnames: List[str]) -> List[str]:
    results: List[str] = []
    ignore_lc = {d.lower() for d in ignore_dirnames}

    for cur, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d.lower() not in ignore_lc]
        if is_github_dir(cur):
            continue
        has_csproj = any(f.lower().endswith(".csproj") for f in files)
        if not has_csproj:
            continue
        if "readme.md" in (fn.lower() for fn in files):
            results.append(os.path.normpath(os.path.join(cur, "readme.md")))

    results.sort(key=lambda p: os.path.relpath(p, root).lower())
    return results

# --- СБОРКА ------------------------------------------------------------------

def append_piece(pieces: List[str], text: str) -> None:
    pieces.append(text.rstrip())
    pieces.append("")
    pieces.append("")

def build_output(root: str, base_path: str, copilot_dir: str, out_path: str, encoding: str) -> int:
    if not os.path.isfile(base_path):
        logger.error("Базовый файл не найден: %s", base_path)
        return 1

    ok, base_text = read_text(base_path, encoding)
    if not ok:
        return 1

    pieces: List[str] = []
    logger.info("Добавляю базовый файл: %s", base_path)
    append_piece(pieces, base_text)

    base_filename = os.path.basename(base_path)
    other_md = list_copilot_md(copilot_dir, base_filename)
    for i, md in enumerate(other_md, 1):
        ok, txt = read_text(md, encoding)
        if not ok:
            logger.warning("Пропуск (ошибка чтения): %s", md)
            continue
        logger.info("[%d/%d] Добавляю: %s", i, len(other_md), md)
        append_piece(pieces, txt)

    logger.info("Добавляю заголовок раздела архитектуры")
    append_piece(pieces, "# Архитектура решения")

    extras = find_project_readmes(root, ignore_dirnames=[".github"])
    for i, p in enumerate(extras, 1):
        ok, txt = read_text(p, encoding)
        if not ok:
            logger.warning("[%d/%d] Пропуск (ошибка чтения): %s", i, len(extras), p)
            continue
        logger.info("[%d/%d] Добавляю: %s", i, len(extras), p)
        append_piece(pieces, txt)

    joined = "\n".join(pieces).rstrip() + "\n"
    if write_text(out_path, joined, encoding):
        logger.info("Итоговый файл создан: %s (%d байт)", out_path, os.path.getsize(out_path))
        return 0
    return 1

# --- CLI ---------------------------------------------------------------------

def parse_args(argv: List[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Сборщик инструкций Copilot (заданный порядок).")
    parser.add_argument("--root", default=os.getcwd(), help="Корень репозитория.")
    parser.add_argument("--base", default=os.path.join(".github", "copilot", "copilot-instructions.base.md"),
                        help="Путь к базовому файлу.")
    parser.add_argument("--copilot-dir", default=os.path.join(".github", "copilot"),
                        help="Каталог, откуда брать остальные *.md.")
    parser.add_argument("--output", default=os.path.join(".github", "copilot-instructions.md"),
                        help="Путь к итоговому файлу.")
    parser.add_argument("--encoding", default="utf-8", help="Кодировка (по умолчанию utf-8).")
    parser.add_argument("--verbose", action="store_true", help="Подробный вывод.")
    parser.add_argument("--dry-run", action="store_true", help="Показать план без записи.")
    parser.add_argument("--no-wait", action="store_true", help="Не ждать 5 секунд перед выходом.")
    return parser.parse_args(argv)

def main(argv: List[str]) -> int:
    args = parse_args(argv)
    setup_logging(args.verbose)

    root = os.path.normpath(args.root)
    base_path = os.path.normpath(args.base)
    copilot_dir = os.path.normpath(args.copilot_dir)
    out_path = os.path.normpath(args.output)
    encoding = args.encoding

    if args.dry_run:
        print("DRY-RUN:")
        print("Base:", base_path, "OK" if os.path.isfile(base_path) else "нет")
        others = list_copilot_md(copilot_dir, os.path.basename(base_path))
        print("Другие MD:", len(others))
        projects = find_project_readmes(root, ignore_dirnames=[".github"])
        print("README в проектах:", len(projects))
        rc = 0 if os.path.isfile(base_path) else 1
    else:
        rc = build_output(root, base_path, copilot_dir, out_path, encoding)

    if not args.no_wait:
        print("Завершение через 5 секунд...")
        time.sleep(5)

    return rc

if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
