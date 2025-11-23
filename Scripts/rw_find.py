import configparser
import json
import os
import re
import sys
from pathlib import Path
from typing import Optional

import utils
from utils import UI

winreg: Optional[object] = None
string: Optional[object] = None

try:
    import winreg
except ImportError:
    pass

try:
    import string
except ImportError:
    pass


def find_installation() -> dict[str, str] | None:
    cache_file = utils.Paths.BUILD / "cache" / "rw_path.json"

    cached_path = _try_read_from_cache(cache_file)
    if cached_path:
        return cached_path

    search_strategy = _get_platform_search_strategy()
    result = None
    for finder in search_strategy:
        result = finder()
        if result:
            break

    if result:
        _write_to_cache(cache_file, result)

    return result


def validate_path(path) -> dict[str, str] | None:
    path = Path(path)
    if not path.is_dir():
        return None

    if sys.platform == "win32":
        executable = path / "RimWorldWin64.exe"
        if executable.exists():
            return {"directory": str(path), "executable": "RimWorldWin64.exe"}
    elif sys.platform == "linux":
        executable = path / "RimWorldLinux"
        if executable.exists():
            return {"directory": str(path), "executable": "RimWorldLinux"}
    elif sys.platform == "darwin":
        app_path = path if str(path).endswith(".app") else path / "RimWorldMac.app"
        if app_path.is_dir():
            return {
                "directory": str(app_path / "Contents" / "MacOS"),
                "executable": "RimWorldMac",
            }
    return None


def _try_read_from_cache(cache_file: Path) -> dict[str, str] | None:
    if not cache_file.exists():
        return None

    try:
        with open(cache_file, "r", encoding="utf-8") as f:
            cached_data = json.load(f)
            path_to_validate = cached_data.get("directory")

            if path_to_validate and sys.platform == "darwin":
                path_to_validate = str(Path(path_to_validate).parent.parent)

            if path_to_validate and validate_path(path_to_validate):
                return cached_data
    except (json.JSONDecodeError, TypeError, KeyError):
        pass

    try:
        cache_file.unlink()
    except OSError:
        pass

    return None


def _write_to_cache(cache_file: Path, data: dict):
    cache_file.parent.mkdir(parents=True, exist_ok=True)
    with open(cache_file, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def _get_platform_search_strategy() -> list:
    if sys.platform == "win32":
        return [
            _find_in_steam,
            _find_in_registry_windows,
            _find_with_full_scan,
        ]
    elif sys.platform == "linux":
        return [
            _find_in_steam,
            _find_in_desktop_files_linux,
            _find_with_full_scan,
        ]
    elif sys.platform == "darwin":
        return [
            _find_in_steam,
            _find_in_applications_macos,
            _find_with_full_scan,
        ]
    else:
        UI.error(f"Unsupported platform: {sys.platform}")
        sys.exit(1)


def _find_in_steam() -> dict[str, str] | None:
    if sys.platform == "win32":
        steam_paths = [
            Path(os.environ.get("PROGRAMFILES(X86)", "C:\\Program Files (x86)"))
            / "Steam"
            / "steamapps"
            / "common"
            / "RimWorld",
            Path(os.environ.get("PROGRAMFILES", "C:\\Program Files"))
            / "Steam"
            / "steamapps"
            / "common"
            / "RimWorld",
        ]
        for path in steam_paths:
            if path.exists():
                return {"directory": str(path), "executable": "RimWorldWin64.exe"}

    elif sys.platform == "linux":
        steam_path = (
            Path.home()
            / ".local"
            / "share"
            / "Steam"
            / "steamapps"
            / "common"
            / "RimWorld"
        )
        if steam_path.exists():
            return {"directory": str(steam_path), "executable": "RimWorldLinux"}

    elif sys.platform == "darwin":
        steam_path = (
            Path.home()
            / "Library"
            / "Application Support"
            / "Steam"
            / "steamapps"
            / "common"
            / "RimWorld"
        )
        if steam_path.exists():
            return {
                "directory": str(steam_path / "RimWorldMac.app" / "Contents" / "MacOS"),
                "executable": "RimWorldMac",
            }

    return None


def _find_in_registry_windows() -> dict[str, str] | None:
    if not winreg:
        return None

    uninstall_paths = [
        r"Software\Microsoft\Windows\CurrentVersion\Uninstall",
        r"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    ]
    for path in uninstall_paths:
        try:
            with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, path) as key:
                for i in range(winreg.QueryInfoKey(key)[0]):
                    subkey_name = winreg.EnumKey(key, i)
                    with winreg.OpenKey(key, subkey_name) as subkey:
                        try:
                            display_name, _ = winreg.QueryValueEx(subkey, "DisplayName")
                            if "rimworld" in display_name.lower():
                                install_loc, _ = winreg.QueryValueEx(
                                    subkey, "InstallLocation"
                                )
                                result = validate_path(install_loc)
                                if result:
                                    return result
                        except OSError:
                            continue
        except OSError:
            continue
    return None


def _find_in_desktop_files_linux() -> dict[str, str] | None:
    desktop_paths = [
        Path.home() / ".local" / "share" / "applications",
        Path("/usr/share/applications"),
        Path("/usr/local/share/applications"),
    ]
    parser = configparser.ConfigParser()

    for path in desktop_paths:
        if not path.is_dir():
            continue
        for desktop_file in path.rglob("*.desktop"):
            try:
                parser.read(desktop_file)
                if "Desktop Entry" in parser:
                    entry = parser["Desktop Entry"]
                    if "rimworld" in entry.get("Name", "").lower():
                        exec_path = entry.get("Path") or entry.get("Exec")
                        if exec_path:
                            install_dir = Path(re.split(r" %| '", exec_path)[0]).parent
                            result = validate_path(install_dir)
                            if result:
                                return result
            except (configparser.Error, FileNotFoundError):
                continue
    return None


def _find_in_applications_macos() -> dict[str, str] | None:
    app_paths = [
        Path("/Applications/RimWorldMac.app"),
        Path.home() / "Applications" / "RimWorldMac.app",
    ]
    for path in app_paths:
        result = validate_path(path)
        if result:
            return result
    return None


def _find_with_full_scan() -> dict[str, str] | None:
    UI.warn("RimWorld not found in common locations. Starting full filesystem scan...")
    UI.info("This might take a while...")

    search_roots = []
    exclude_dirs = set()

    if sys.platform == "win32" and string:
        for drive in string.ascii_uppercase:
            path = Path(f"{drive}:/")
            if path.exists():
                search_roots.append(path)
        windir = os.environ.get("WINDIR", "C:\\Windows")
        exclude_dirs.update(
            [
                Path(windir),
                Path(os.environ.get("PROGRAMFILES", "C:\\Program Files")),
                Path(os.environ.get("PROGRAMFILES(X86)", "C:\\Program Files (x86)")),
                Path.home() / "AppData",
                Path("$RECYCLE.BIN"),
            ]
        )

    elif sys.platform in ["linux", "darwin"]:
        search_roots.append(Path.home())
        for mount in ["/mnt", "/media", "/run/media"]:
            if Path(mount).exists():
                search_roots.append(Path(mount))
        exclude_dirs.update(
            [
                "/dev",
                "/proc",
                "/sys",
                "/tmp",
                "/var/tmp",
                "/lost+found",
                Path.home() / ".cache",
                Path.home() / ".config",
                Path.home() / ".local" / "share" / "Steam",
            ]
        )

    for root in search_roots:
        try:
            for dirpath, dirnames, _ in os.walk(root):
                current_path = Path(dirpath)
                is_excluded = False
                for excluded in exclude_dirs:
                    try:
                        if current_path == excluded or excluded in current_path.parents:
                            is_excluded = True
                            break
                    except TypeError:
                        if str(current_path).startswith(str(excluded)):
                            is_excluded = True
                            break
                if is_excluded:
                    dirnames[:] = []
                    continue

                if "RimWorld" in dirnames:
                    found_path = Path(dirpath) / "RimWorld"
                    UI.step(f"Scanning: {found_path}")
                    result = validate_path(found_path)
                    if result:
                        UI.success("Found installation.")
                        return result
                    dirnames.remove("RimWorld")
        except PermissionError:
            continue

    return None


if __name__ == "__main__":
    try:
        import json

        import setup
        import utils

        setup.Runtime.enforce_venv()

        env = setup.Environment(setup.MANIFEST)
        if not env.manifest["find"].check():
            UI.error(
                "'find' environment is not configured.",
                hint="Run: python Scripts/setup.py setup find",
            )
            sys.exit(1)

        result = find_installation()
        if result:
            print(json.dumps(result, indent=2))
            sys.exit(0)
        else:
            UI.error("RimWorld installation not found.", hint="Try running with --scan")
            sys.exit(1)
    except KeyboardInterrupt:
        UI.error("Cancelled by user.")
        sys.exit(130)
