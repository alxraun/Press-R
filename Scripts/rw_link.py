import argparse
import sys
from pathlib import Path

import setup
from rw_find import find_installation
from utils import UI, Fs, Paths


def _link_mod(source: Path, target_root: Path) -> None:
    mod_name = source.name
    target_path = target_root / mod_name

    try:
        Fs.create_symlink(source, target_path)
        UI.success(f"Linked {mod_name}")
    except FileExistsError:
        UI.warn(f"Skipping {mod_name}: Target exists and is not a symlink.")
    except OSError as error:
        UI.error(f"Failed to link {mod_name}: {error}")


def _unlink_mod(source: Path, target_root: Path) -> None:
    mod_name = source.name
    target_path = target_root / mod_name

    try:
        Fs.remove_symlink(target_path)
        UI.success(f"Unlinked {mod_name}")
    except FileExistsError:
        UI.warn(f"Skipping {mod_name}: Target exists and is not a symlink.")
    except OSError as error:
        UI.error(f"Failed to unlink {mod_name}: {error}")


def _link_all_mods(source_root: Path, target_root: Path) -> None:
    if not source_root.exists():
        UI.warn(f"No Mods directory found at {source_root}")
        return

    UI.header("Linking Mods")

    for item in source_root.iterdir():
        if item.is_dir():
            _link_mod(item, target_root)

    UI.success("Mod linking completed.")


def _unlink_all_mods(source_root: Path, target_root: Path) -> None:
    if not source_root.exists():
        UI.warn(f"No Mods directory found at {source_root}")
        return

    UI.header("Unlinking Mods")

    for item in source_root.iterdir():
        if item.is_dir():
            _unlink_mod(item, target_root)

    UI.success("Mod unlinking completed.")


def _resolve_target_mods_directory() -> Path:
    installation = find_installation()
    if not installation:
        UI.error("RimWorld installation not found.")
        sys.exit(1)
    return Path(installation["directory"]) / "Mods"


def _ensure_environment() -> None:
    setup.Runtime.enforce_venv()
    env = setup.Environment(setup.MANIFEST)
    if not env.manifest["find"].check():
        UI.error(
            "'find' environment is not configured.",
            hint="Run: python Scripts/setup.py setup find",
        )
        sys.exit(1)


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Manage mod symlinks")
    subparsers = parser.add_subparsers(dest="command", metavar="")

    subparsers.add_parser("link", help="Create symlinks for all mods")
    subparsers.add_parser("unlink", help="Remove symlinks for all mods")

    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        sys.exit(1)

    return parser.parse_args()


def run():
    _ensure_environment()
    args = _parse_args()

    target_mods_path = _resolve_target_mods_directory()
    source_mods_path = Paths.PROJECT / "Mods"

    if args.command == "link":
        _link_all_mods(source_mods_path, target_mods_path)
    elif args.command == "unlink":
        _unlink_all_mods(source_mods_path, target_mods_path)


if __name__ == "__main__":
    try:
        run()
    except KeyboardInterrupt:
        UI.error("Cancelled by user.")
        sys.exit(130)
