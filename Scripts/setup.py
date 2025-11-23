import abc
import argparse
import os
import shutil
import stat
import subprocess
import sys
import urllib.request
from pathlib import Path
from typing import Dict, List

import utils
from utils import UI


class PackageManager:

    @staticmethod
    def detect() -> str:

        if shutil.which("apt"):
            return "apt"
        if shutil.which("dnf"):
            return "dnf"
        if shutil.which("brew"):
            return "brew"
        if shutil.which("choco"):
            return "choco"
        if shutil.which("pacman"):
            return "pacman"
        if shutil.which("zypper"):
            return "zypper"
        return "unknown"


class Requirement(abc.ABC):

    @abc.abstractmethod
    def check(self) -> bool:
        pass

    @abc.abstractmethod
    def setup(self) -> None:
        pass

    @abc.abstractmethod
    def clean(self) -> None:
        pass

    @abc.abstractmethod
    def name(self) -> str:
        pass


class VirtualEnvironment(Requirement):

    def name(self) -> str:
        return "Virtual Environment"

    def get_python_exe(self) -> Path:
        if sys.platform == "win32":
            return utils.Paths.VENV / "Scripts" / "python.exe"
        return utils.Paths.VENV / "bin" / "python"

    def check(self) -> bool:
        return utils.Paths.VENV.exists() and self.get_python_exe().exists()

    def setup(self) -> None:
        with UI.spin(f"Creating virtual environment at {utils.Paths.VENV}..."):
            utils.Paths.VENV.parent.mkdir(parents=True, exist_ok=True)
            utils.run([sys.executable, "-m", "venv", str(utils.Paths.VENV)])

    def clean(self) -> None:
        pass


class PipRequirement(Requirement):

    def __init__(self, name: str, packages: List[str]):
        self._name = name
        self.packages = packages

    def name(self) -> str:
        return self._name

    def _get_stamp_path(self) -> Path:
        sanitized_name = self._name.lower().replace(" ", "_")
        return utils.Paths.VENV / f".req.{sanitized_name}.stamp"

    def _get_pip_exe(self) -> Path:
        if sys.platform == "win32":
            return utils.Paths.VENV / "Scripts" / "pip.exe"
        return utils.Paths.VENV / "bin" / "pip"

    def check(self) -> bool:
        return self._get_stamp_path().exists()

    def setup(self) -> None:
        pip_exe = self._get_pip_exe()
        if not pip_exe.exists():
            UI.error(f"pip not found at {pip_exe}")
            sys.exit(1)

        with UI.spin(f"Installing dependencies: {self._name}..."):
            for pkg in self.packages:
                utils.run([str(pip_exe), "install", pkg])

        stamp_path = self._get_stamp_path()
        stamp_path.parent.mkdir(parents=True, exist_ok=True)
        stamp_path.touch()

    def clean(self) -> None:
        pip_exe = self._get_pip_exe()
        if pip_exe.exists():
            for pkg in self.packages:
                package_name = pkg.split(";")[0].strip()
                for op in ["==", ">=", "<=", ">", "<", "~="]:
                    package_name = package_name.split(op)[0].strip()

                utils.run([str(pip_exe), "uninstall", "-y", package_name], check=False)

        stamp_path = self._get_stamp_path()
        if stamp_path.exists():
            stamp_path.unlink()


class SystemBinary(Requirement):

    def __init__(self, binary: str, hints: Dict[str, str]):
        self.binary = binary
        self.hints = hints

    def name(self) -> str:
        return f"System Binary: {self.binary}"

    def check(self) -> bool:
        return shutil.which(self.binary) is not None

    def setup(self) -> None:
        pm = PackageManager.detect()
        pkg_name = self.hints.get(pm)

        hint = None
        if pkg_name:
            if pm == "apt":
                hint = f"Run: sudo apt install {pkg_name}"
            elif pm == "dnf":
                hint = f"Run: sudo dnf install {pkg_name}"
            elif pm == "brew":
                hint = f"Run: brew install {pkg_name}"
            elif pm == "choco":
                hint = f"Run: choco install {pkg_name}"
            elif pm == "pacman":
                hint = f"Run: sudo pacman -S {pkg_name}"
            elif pm == "zypper":
                hint = f"Run: sudo zypper install {pkg_name}"
            else:
                hint = (
                    f"Please install package '{pkg_name}' using your package manager."
                )
        else:
            hint = "Manual installation required."

        UI.error(f"Missing system binary: {self.binary}", hint=hint)
        sys.exit(1)

    def clean(self) -> None:
        pass


class ExternalTool(Requirement):

    def __init__(self, filename: str, url: str):
        self.filename = filename
        self.url = url

    def name(self) -> str:
        return f"External Tool: {self.filename}"

    def check(self) -> bool:
        return (utils.Paths.TOOLS / self.filename).exists()

    def setup(self) -> None:
        dest = utils.Paths.TOOLS / self.filename
        utils.Paths.TOOLS.mkdir(parents=True, exist_ok=True)

        try:
            with UI.spin(f"Downloading {self.filename}..."):
                urllib.request.urlretrieve(self.url, dest)
                st = os.stat(dest)
                os.chmod(dest, st.st_mode | stat.S_IEXEC)
        except Exception as e:
            UI.error(f"Failed to download {self.filename}: {e}")
            sys.exit(1)

    def clean(self) -> None:
        dest = utils.Paths.TOOLS / self.filename
        if dest.exists():
            dest.unlink()


class DotNetToolRequirement(Requirement):
    def __init__(self, tool_id: str, version: str, tool_exe_name: str):
        self.tool_id = tool_id
        self.version = version
        self.tool_exe_name = tool_exe_name

    def name(self) -> str:
        return f".NET Tool: {self.tool_id} ({self.version})"

    def _get_tool_path(self) -> Path:
        return utils.Paths.TOOLS / self.tool_exe_name

    def check(self) -> bool:
        return self._get_tool_path().exists()

    def setup(self) -> None:
        utils.Paths.TOOLS.mkdir(parents=True, exist_ok=True)

        cmd = [
            "dotnet",
            "tool",
            "install",
            self.tool_id,
            "--version",
            self.version,
            "--tool-path",
            str(utils.Paths.TOOLS),
        ]

        with UI.spin(f"Installing {self.tool_id}..."):
            try:
                utils.run(cmd, capture=True)
            except subprocess.CalledProcessError as e:
                UI.error(f"Failed to install {self.tool_id}")
                if e.stderr:
                    UI.print_line(e.stderr)
                sys.exit(1)

    def clean(self) -> None:
        if not self.check():
            return

        cmd = [
            "dotnet",
            "tool",
            "uninstall",
            self.tool_id,
            "--tool-path",
            str(utils.Paths.TOOLS),
        ]

        try:
            utils.run(cmd, capture=True)
        except subprocess.CalledProcessError:
            pass

        # Cleanup empty directory if needed
        if utils.Paths.TOOLS.exists() and not any(utils.Paths.TOOLS.iterdir()):
            try:
                utils.Paths.TOOLS.rmdir()
            except OSError:
                pass


class Runtime:

    @staticmethod
    def enforce_venv() -> None:
        venv = VirtualEnvironment()

        current_exe = Path(sys.executable).absolute()
        venv_exe = venv.get_python_exe().absolute()

        in_venv = current_exe == venv_exe

        if not in_venv:
            if not venv.check():
                venv.setup()

            args = [str(venv_exe), str(Path(sys.argv[0]).absolute())] + sys.argv[1:]

            try:
                if sys.platform == "win32":
                    sys.exit(subprocess.run(args).returncode)
                else:
                    os.execv(str(venv_exe), args)
            except Exception as e:
                UI.error(f"Failed to re-execute in venv: {e}")
                sys.exit(1)


class Component:

    def __init__(self, name: str, requirements: List[Requirement]):
        self.name_str = name
        self.requirements = requirements

    def check(self) -> bool:
        return all(req.check() for req in self.requirements)

    def setup(self) -> None:
        UI.header(f"Component: {self.name_str}")
        for req in self.requirements:
            if req.check():
                UI.success(req.name())
            else:
                req.setup()
                UI.success(
                    f"{req.name()} {utils.Colors.DIM}(Installed){utils.Colors.RESET}"
                )

    def clean(self) -> None:
        UI.header(f"Cleaning: {self.name_str}")
        for req in self.requirements:
            req.clean()
            UI.step(f"Removed {req.name()}")


class Environment:

    def __init__(self, manifest: Dict[str, Component]):
        self.manifest = manifest

    def _resolve_targets(self, targets: List[str]) -> List[str]:
        if "all" in targets:
            return list(self.manifest.keys())
        return list(dict.fromkeys(targets))

    def setup(self, targets: List[str]) -> None:
        resolved = self._resolve_targets(targets)
        UI.header("Environment Setup")
        for name in resolved:
            self.manifest[name].setup()
        UI.success("Setup complete.")

    def check(self, targets: List[str]) -> None:
        resolved = self._resolve_targets(targets)
        UI.header("Environment Check")
        all_ok = True
        for name in resolved:
            if self.manifest[name].check():
                UI.success(name)
            else:
                UI.error(name)
                all_ok = False
        if not all_ok:
            sys.exit(1)

    def clean(self, targets: List[str]) -> None:
        resolved = self._resolve_targets(targets)
        UI.header("Environment Clean")
        for name in resolved:
            self.manifest[name].clean()
        UI.success("Clean complete.")


MANIFEST: Dict[str, Component] = {
    "find": Component(
        "find",
        [
            PipRequirement(
                "find_deps",
                ["pywin32; sys_platform == 'win32'"],
            )
        ],
    ),
    "launch": Component(
        "launch",
        [
            PipRequirement(
                "launch_deps",
                ["psutil", "pywin32; sys_platform == 'win32'"],
            )
        ],
    ),
    "build": Component(
        "build",
        [
            SystemBinary(
                "dotnet",
                {
                    "apt": "dotnet-sdk-8.0",
                    "dnf": "dotnet-sdk-8.0",
                },
            )
        ],
    ),
    "assets": Component(
        "assets",
        [
            DotNetToolRequirement(
                "CryptikLemur.AssetBundleBuilder", "4.0.1", "assetbundlebuilder"
            )
        ],
    ),
}


def parse_args():
    parser = argparse.ArgumentParser(
        description="Manage development environment dependencies"
    )
    subparsers = parser.add_subparsers(dest="command", metavar="")

    setup_parser = subparsers.add_parser("setup", help="Install dependencies")
    setup_parser.add_argument(
        "components",
        nargs="+",
        choices=list(MANIFEST.keys()) + ["all"],
        help="Components to install",
    )

    check_parser = subparsers.add_parser("check", help="Verify dependencies")
    check_parser.add_argument(
        "components",
        nargs="+",
        choices=list(MANIFEST.keys()) + ["all"],
        help="Components to verify",
    )

    clean_parser = subparsers.add_parser("clean", help="Remove dependencies")
    clean_parser.add_argument(
        "components",
        nargs="+",
        choices=list(MANIFEST.keys()) + ["all"],
        help="Components to remove",
    )

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        sys.exit(1)
    return args


def main():
    args = parse_args()

    Runtime.enforce_venv()

    env = Environment(MANIFEST)

    if args.command == "setup":
        env.setup(args.components)
    elif args.command == "check":
        env.check(args.components)
    elif args.command == "clean":
        env.clean(args.components)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        UI.error("Cancelled by user.")
        sys.exit(130)
