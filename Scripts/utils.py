import sys
import os
import time
import threading
import shutil
import subprocess
import contextlib
import itertools
import platform
import shlex
from pathlib import Path
from typing import Optional, Union, List, Tuple

PLATFORM_ID = f"{sys.platform}-{platform.machine().lower()}"


class Paths:
    PROJECT = Path(__file__).resolve().parent.parent
    BUILD = Path(PROJECT / ".build")

    VENV = BUILD / ".venv" / PLATFORM_ID
    TOOLS = BUILD / "tools"


class Colors:
    RESET = "\033[0m"
    BOLD = "\033[1m"
    DIM = "\033[2m"
    RED = "\033[91m"
    GREEN = "\033[92m"
    YELLOW = "\033[93m"
    BLUE = "\033[94m"
    MAGENTA = "\033[95m"
    CYAN = "\033[96m"
    WHITE = "\033[97m"

    @classmethod
    def _init(cls):
        if not sys.stderr.isatty() or "NO_COLOR" in os.environ:
            for attr in dir(cls):
                if not attr.startswith("_") and isinstance(getattr(cls, attr), str):
                    setattr(cls, attr, "")


Colors._init()


class UI:
    _scope_stack: List[Tuple[str, str]] = []
    _lock = threading.Lock()
    _active_spinner: Optional["UI.Spinner"] = None

    class Spinner(threading.Thread):
        def __init__(self, message: str):
            super().__init__(daemon=True)
            self.message = message
            self.frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
            self._stop_event = threading.Event()

        def run(self):
            if not sys.stderr.isatty():
                return

            cycle = itertools.cycle(self.frames)
            while not self._stop_event.is_set():
                with UI._lock:
                    sys.stderr.write(f"\r{next(cycle)} {self.message}")
                    sys.stderr.flush()
                time.sleep(0.1)

        def start(self):
            if sys.stderr.isatty():
                super().start()
            else:
                sys.stderr.write(f"[START] {self.message}\n")

        def clear_line(self):
            if sys.stderr.isatty():
                sys.stderr.write("\r" + " " * (len(self.message) + 2) + "\r")
                sys.stderr.flush()

        def stop(self, success: bool):
            self._stop_event.set()
            if sys.stderr.isatty():
                if self.is_alive():
                    self.join()
                self.clear_line()

    @staticmethod
    def _clear_spinner():
        if UI._active_spinner:
            UI._active_spinner.clear_line()

    @staticmethod
    def header(text: str):
        with UI._lock:
            UI._clear_spinner()
            sys.stderr.write(f"{Colors.BOLD}{Colors.WHITE}{text}{Colors.RESET}\n")
            sys.stderr.write("─" * len(text) + "\n")

    @staticmethod
    def step(text: str):
        with UI._lock:
            UI._clear_spinner()
            sys.stderr.write(f"{Colors.CYAN}➜ {Colors.RESET}{text}\n")

    @staticmethod
    def success(text: str):
        with UI._lock:
            UI._clear_spinner()
            sys.stderr.write(f"{Colors.GREEN}✓ {Colors.RESET}{text}\n")

    @staticmethod
    def warn(text: str, hint: Optional[str] = None):
        with UI._lock:
            UI._clear_spinner()
            sys.stderr.write(f"{Colors.YELLOW}! {text}{Colors.RESET}\n")
            if hint:
                sys.stderr.write(f"  {Colors.DIM}Hint: {hint}{Colors.RESET}\n")

    @staticmethod
    def error(text: str, hint: Optional[str] = None):
        with UI._lock:
            UI._clear_spinner()
            sys.stderr.write(f"{Colors.RED}✗ {text}{Colors.RESET}\n")
            if hint:
                sys.stderr.write(f"  {Colors.DIM}Hint: {hint}{Colors.RESET}\n")

    @staticmethod
    def info(text: str):
        with UI._lock:
            UI._clear_spinner()
            sys.stderr.write(f"{Colors.DIM}• {Colors.RESET}{text}\n")

    @staticmethod
    @contextlib.contextmanager
    def scope(tag: str, color: str = Colors.CYAN):
        UI._scope_stack.append((tag, color))
        try:
            yield
        finally:
            UI._scope_stack.pop()

    @staticmethod
    @contextlib.contextmanager
    def spin(text: str):
        spinner = UI.Spinner(text)
        UI._active_spinner = spinner
        spinner.start()
        try:
            yield
            UI._active_spinner = None
            spinner.stop(success=True)
            UI.success(text)
        except Exception:
            UI._active_spinner = None
            spinner.stop(success=False)
            UI.error(text)
            raise

    @staticmethod
    def print_line(text: str):
        with UI._lock:
            UI._clear_spinner()
            prefix = "".join(
                f"[{color}{tag}{Colors.RESET}]" for tag, color in UI._scope_stack
            )
            if prefix:
                prefix += " "
            sys.stderr.write(f"{prefix}{text}\n")


def require(binary: str):
    if shutil.which(binary) is None:
        UI.error(f"Missing required binary: {binary}")
        sys.exit(1)


class Fs:
    @staticmethod
    def ensure_dir(path: Path):
        path.mkdir(parents=True, exist_ok=True)

    @staticmethod
    def clean_dir(path: Path):
        if path.exists():
            shutil.rmtree(path)

    @staticmethod
    def create_symlink(source: Path, target: Path):
        if target.is_symlink():
            target.unlink()

        if target.exists():
            raise FileExistsError(
                f"Target {target} already exists and is not a symlink."
            )

        target.parent.mkdir(parents=True, exist_ok=True)
        target.symlink_to(source)

    @staticmethod
    def remove_symlink(target: Path):
        if target.is_symlink():
            target.unlink()
        elif target.exists():
            raise FileExistsError(
                f"Target {target} exists and is not a symlink. Refusing to delete."
            )


def run(
    cmd: Union[str, List[str]],
    *,
    input: Optional[str] = None,
    cwd: Optional[Path] = None,
    env: Optional[dict[str, str]] = None,
    timeout: int = 300,
    check: bool = True,
    capture: bool = False,
    stream_output: bool = False,
    detach: bool = False,
) -> subprocess.CompletedProcess:
    if isinstance(cmd, str):
        cmd_list = shlex.split(cmd)
    else:
        cmd_list = cmd

    run_env = os.environ.copy()
    if env:
        run_env.update(env)

    if detach:
        subprocess.Popen(
            cmd_list,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            stdin=subprocess.DEVNULL,
            cwd=cwd,
            env=run_env,
        )
        return subprocess.CompletedProcess(args=cmd_list, returncode=0)

    if UI._active_spinner and not capture and not stream_output:
        capture = True

    if stream_output:
        try:
            process = subprocess.Popen(
                cmd_list,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                stdin=subprocess.PIPE if input else None,
                cwd=cwd,
                env=run_env,
                text=True,
                encoding="utf-8",
                errors="replace",
            )

            if input and process.stdin:
                process.stdin.write(input)
                process.stdin.close()

            if process.stdout:
                for line in process.stdout:
                    UI.print_line(line.strip())

            process.wait(timeout=timeout)

            retcode = process.returncode
            if check and retcode != 0:
                raise subprocess.CalledProcessError(retcode, cmd_list)

            return subprocess.CompletedProcess(
                args=cmd_list, returncode=retcode, stdout=None, stderr=None
            )
        except subprocess.CalledProcessError as e:
            UI.error(f"Command failed: {e}")
            raise e
    else:
        try:
            return subprocess.run(
                cmd_list,
                check=check,
                text=True,
                capture_output=capture,
                input=input,
                timeout=timeout,
                cwd=cwd,
                env=run_env,
                encoding="utf-8" if capture else None,
            )
        except subprocess.CalledProcessError as e:
            if check:
                UI.error(f"Command failed: {e}")
                if e.stdout:
                    sys.stderr.write(f"stdout: {e.stdout}\n")
                if e.stderr:
                    sys.stderr.write(f"stderr: {e.stderr}\n")
                sys.exit(1)
            raise e
