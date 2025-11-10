"""Utility functions for locating RimWorld installation."""

import configparser
import os
import re
import sys
from pathlib import Path
from typing import Optional


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


def validate_rimworld_path(path):
    """Check if path contains valid RimWorld installation."""
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


def find_in_steam():
    """Find RimWorld in Steam library locations."""
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


def find_in_registry_windows():
    """Find RimWorld via Windows Registry."""
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
                                result = validate_rimworld_path(install_loc)
                                if result:
                                    return result
                        except OSError:
                            continue
        except OSError:
            continue
    return None


def find_in_desktop_files_linux():
    """Find RimWorld by parsing Linux .desktop files."""
    desktop_paths = [
        Path.home() / ".local" / "share" / "applications",
        "/usr/share/applications",
        "/usr/local/share/applications",
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
                            result = validate_rimworld_path(install_dir)
                            if result:
                                return result
            except (configparser.Error, FileNotFoundError):
                continue
    return None


def find_in_applications_macos():
    """Find RimWorld in macOS Applications folders."""
    app_paths = [
        Path("/Applications/RimWorldMac.app"),
        Path.home() / "Applications" / "RimWorldMac.app",
    ]
    for path in app_paths:
        result = validate_rimworld_path(path)
        if result:
            return result
    return None


def find_with_full_scan():
    """Perform comprehensive filesystem scan."""
    print(
        "Warning: RimWorld not found in common locations. Starting full filesystem scan...",
        file=sys.stderr,
    )
    print("This might take a while...", file=sys.stderr)

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
                    print(f"Checking potential path: {found_path}", file=sys.stderr)
                    result = validate_rimworld_path(found_path)
                    if result:
                        print("Found!", file=sys.stderr)
                        return result
                    dirnames.remove("RimWorld")
        except PermissionError:
            continue

    return None
