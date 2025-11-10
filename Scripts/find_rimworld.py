"""Script to find RimWorld installation path."""

import argparse
import json
import sys
from pathlib import Path


try:
    from utils import finders
except ImportError:
    sys.path.append(str(Path(__file__).parent))
    from utils import finders


def run_search_strategy(strategy):
    """Execute finder functions until one returns result."""
    for finder_func in strategy:
        result = finder_func()
        if result:
            return result
    return None


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="Find RimWorld installation")
    parser.add_argument(
        "--directory-only", action="store_true", help="Print only directory path"
    )
    parser.add_argument(
        "--executable-only", action="store_true", help="Print only executable name"
    )
    args = parser.parse_args()

    project_root = Path(__file__).parent.parent
    cache_dir = project_root / ".build" / "cache"
    cache_file = cache_dir / "rimworld_path.json"

    if cache_file.exists():
        with open(cache_file, "r", encoding="utf-8") as f:
            try:
                cached_data = json.load(f)
                path_to_validate = cached_data.get("directory")
                if path_to_validate and sys.platform == "darwin":
                    path_to_validate = str(Path(path_to_validate).parent.parent)

                if path_to_validate and finders.validate_rimworld_path(
                    path_to_validate
                ):
                    if args.directory_only:
                        print(cached_data["directory"])
                    elif args.executable_only:
                        print(cached_data["executable"])
                    else:
                        print(json.dumps(cached_data, indent=2))
                    return
            except (json.JSONDecodeError, TypeError, KeyError):
                pass
        try:
            cache_file.unlink()
        except OSError:
            pass

    search_strategy = []
    if sys.platform == "win32":
        search_strategy = [
            finders.find_in_steam,
            finders.find_in_registry_windows,
            finders.find_with_full_scan,
        ]
    elif sys.platform == "linux":
        search_strategy = [
            finders.find_in_steam,
            finders.find_in_desktop_files_linux,
            finders.find_with_full_scan,
        ]
    elif sys.platform == "darwin":
        search_strategy = [
            finders.find_in_steam,
            finders.find_in_applications_macos,
            finders.find_with_full_scan,
        ]
    else:
        print(f"Unsupported platform: {sys.platform}", file=sys.stderr)
        sys.exit(1)

    result = run_search_strategy(search_strategy)

    if not result:
        print(
            "RimWorld installation not found even after a full scan.",
            file=sys.stderr,
        )
        print("Please ensure the game is installed correctly.", file=sys.stderr)
        sys.exit(1)

    cache_dir.mkdir(parents=True, exist_ok=True)
    with open(cache_file, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)

    if args.directory_only:
        print(result["directory"])
    elif args.executable_only:
        print(result["executable"])
    else:
        print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
