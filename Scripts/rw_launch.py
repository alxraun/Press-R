import argparse
import sys
import time
import os
import webbrowser
from pathlib import Path
import setup

setup.Runtime.enforce_venv()

import rw_find
import utils
from utils import UI


RIMWORLD_APP_ID = "294100"
TIMEOUT = 30


def find_and_kill(executable_name: str):
    import psutil

    process_name_with_ext = executable_name
    process_name_without_ext = os.path.splitext(executable_name)[0]

    for proc in psutil.process_iter(["name", "pid"]):
        try:
            proc_name = proc.info["name"]
            if proc_name.lower() in [
                process_name_with_ext.lower(),
                process_name_without_ext.lower(),
            ]:
                UI.step(f"Terminating process {proc_name} (PID: {proc.info['pid']})...")
                p = psutil.Process(proc.info["pid"])
                p.kill()
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            pass
        except (psutil.Error, OSError) as e:
            UI.warn(
                f"Could not terminate process {proc.info.get('name', '')} "
                f"(PID: {proc.info.get('pid', '')}): {e}"
            )


def launch(directory: Path, executable: str):
    import subprocess

    exe_path = directory / executable
    if not exe_path.is_file():
        raise FileNotFoundError(f"Executable not found at '{exe_path}'")

    if sys.platform == "linux":
        try:
            os.chmod(exe_path, 0o755)
        except OSError as e:
            UI.warn(f"Could not set executable permission on '{exe_path}': {e}")

    process = subprocess.Popen(
        [str(exe_path)],
        cwd=str(directory),
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        start_new_session=True,
    )
    return process


def launch_steam_game(app_id: str):
    steam_url = f"steam://rungameid/{app_id}"

    UI.step(f"Launching Steam AppID {app_id}...")
    try:
        if sys.platform == "linux":
            utils.run(["xdg-open", steam_url], detach=True)
        elif sys.platform == "darwin":
            utils.run(["open", steam_url], detach=True)
        elif sys.platform == "win32":
            utils.run(["cmd", "/c", "start", "", steam_url], detach=True)
        else:
            webbrowser.open(steam_url)
        UI.success("Launch command sent to Steam.")
    except Exception as e:
        UI.error(f"Error launching Steam: {e}")
        sys.exit(1)


def position_on_monitor(process, monitor_index: int, maximize: bool):
    if sys.platform == "win32":
        _position_window_windows(process, monitor_index, maximize)
    elif sys.platform == "linux":
        _position_window_linux(process, monitor_index, maximize)
    else:
        UI.warn(f"Window management not implemented for {sys.platform}")


if sys.platform == "win32":

    def _find_window_for_pid_win(pid):
        import win32gui
        import win32process

        start_time = time.time()
        while time.time() - start_time < TIMEOUT:

            def callback(hwnd, hwnds):
                if win32gui.IsWindowVisible(hwnd) and win32gui.IsWindowEnabled(hwnd):
                    _, found_pid = win32process.GetWindowThreadProcessId(hwnd)
                    if found_pid == pid and win32gui.GetParent(hwnd) == 0:
                        hwnds.append(hwnd)
                return True

            hwnds = []
            win32gui.EnumWindows(callback, hwnds)
            if hwnds:
                return hwnds[0]
            time.sleep(0.5)
        return None

    def _position_window_windows(process, monitor_index: int, maximize: bool):
        import win32api
        import win32con
        import win32gui

        with UI.spin("Waiting for window creation..."):
            hwnd = _find_window_for_pid_win(process.pid)

        if not hwnd:
            raise RuntimeError("Timed out waiting for the process window.")

        monitors = win32api.EnumDisplayMonitors()
        if monitor_index >= len(monitors) or monitor_index < 0:
            UI.warn(
                f"Monitor index {monitor_index} is out of range. "
                "Falling back to primary monitor (0)."
            )
            monitor_index = 0

        monitor_handle = monitors[monitor_index][0]
        monitor_info = win32api.GetMonitorInfo(monitor_handle)  # type: ignore[arg-type]
        work_area = monitor_info["Work"]
        left, top = work_area[0], work_area[1]

        win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
        time.sleep(0.25)
        win32gui.SetWindowPos(
            hwnd,
            win32con.HWND_TOP,
            left,
            top,
            0,
            0,
            win32con.SWP_NOSIZE | win32con.SWP_NOZORDER | win32con.SWP_SHOWWINDOW,
        )
        time.sleep(0.25)
        if maximize:
            win32gui.ShowWindow(hwnd, win32con.SW_MAXIMIZE)


if sys.platform == "linux":

    def _check_command_exists_linux(cmd: str) -> bool:
        try:
            utils.run(["which", cmd], capture=True)
            return True
        except Exception:
            return False

    def _find_window_id_for_pid_linux(pid: int) -> str | None:
        start_time = time.time()
        while time.time() - start_time < TIMEOUT:
            try:
                result = utils.run(["wmctrl", "-lp"], capture=True)
                for line in result.stdout.strip().split("\n"):
                    parts = line.split()
                    if len(parts) >= 3 and int(parts[2]) == pid:
                        return parts[0]
            except (Exception, ValueError):
                pass
            time.sleep(0.5)
        return None

    def _get_linux_monitor_geometry(monitor_index: int) -> tuple[int, int]:
        try:
            result = utils.run(["xrandr", "--query"], capture=True)

            import re

            monitors = re.findall(
                r"(\S+) connected (?:primary )?(\d+)x(\d+)\+(\d+)\+(\d+)",
                result.stdout,
            )

            if not monitors:
                raise RuntimeError("Could not parse monitor geometry from xrandr.")

            if monitor_index >= len(monitors) or monitor_index < 0:
                UI.warn(
                    f"Monitor index {monitor_index} is out of range. "
                    "Falling back to primary monitor (0)."
                )
                monitor_index = 0

            target_monitor = monitors[monitor_index]
            return int(target_monitor[3]), int(target_monitor[4])
        except Exception as e:
            raise RuntimeError(
                "'xrandr' is required on Linux to get monitor geometry."
            ) from e

    def _position_window_linux(process, monitor_index: int, maximize: bool):
        if not _check_command_exists_linux("wmctrl"):
            raise RuntimeError(
                "`wmctrl` is not installed. Please install it (e.g., 'sudo apt-get install wmctrl')."
            )

        with UI.spin("Waiting for window creation..."):
            win_id = _find_window_id_for_pid_linux(process.pid)

        if not win_id:
            raise RuntimeError("Timed out waiting for the process window.")

        x, y = _get_linux_monitor_geometry(monitor_index)

        utils.run(
            ["wmctrl", "-ir", win_id, "-b", "remove,maximized_vert,maximized_horz"],
            capture=True,
        )
        time.sleep(0.25)
        utils.run(["wmctrl", "-ir", win_id, "-e", f"0,{x},{y},-1,-1"], capture=True)
        time.sleep(0.25)

        if maximize:
            utils.run(
                ["wmctrl", "-ir", win_id, "-b", "add,maximized_vert,maximized_horz"],
                capture=True,
            )


def launch_direct(args):
    UI.step("Locating RimWorld installation...")
    info = rw_find.find_installation()
    if not info:
        raise RuntimeError("RimWorld installation not found.")

    UI.info(f"Location: {info['directory']}")

    if args.clear_launch:
        find_and_kill(info["executable"])
        time.sleep(0.5)

    UI.step(f"Launching executable...")
    rw_proc = launch(Path(info["directory"]), info["executable"])
    UI.success(f"Process started (PID: {rw_proc.pid}).")

    if args.no_manage_window:
        UI.info("Window management disabled.")
        return

    if not args.no_manage_window:
        with UI.spin(f"Positioning window on monitor {args.monitor}..."):
            position_on_monitor(rw_proc, args.monitor, not args.no_maximize)

    UI.success("Window positioned.")
    UI.success("RimWorld is running.")


def launch_steam(args):
    if args.clear_launch:
        UI.step("Attempting to terminate existing RimWorld processes...")
        try:
            info = rw_find.find_installation()
            if info:
                find_and_kill(info["executable"])
                time.sleep(0.5)
        except RuntimeError:
            UI.warn("Could not find RimWorld executable to terminate.")

    launch_steam_game(RIMWORLD_APP_ID)


def main():
    env = setup.Environment(setup.MANIFEST)
    if not env.manifest["launch"].check():
        UI.error(
            "'launch' environment is not configured.",
            hint="Run: python Scripts/setup.py setup launch",
        )
        sys.exit(1)

    parser = argparse.ArgumentParser(description="Launch RimWorld")
    parser.add_argument(
        "--method",
        choices=["direct", "steam"],
        default="steam",
        help="Launch method: direct (executable) or steam (Steam client)",
    )
    parser.add_argument(
        "-c",
        "--clear-launch",
        action="store_true",
        help="Kill existing RimWorld instances before launch",
    )
    parser.add_argument(
        "-m",
        "--monitor",
        type=int,
        default=0,
        help="Monitor index for window placement (direct only)",
    )
    parser.add_argument(
        "--no-manage-window",
        action="store_true",
        help="Skip window positioning (direct only)",
    )

    parser.add_argument(
        "--no-maximize",
        action="store_true",
        help="Position window without maximizing (direct only)",
    )

    args = parser.parse_args()

    try:
        if args.method == "direct":
            launch_direct(args)
        elif args.method == "steam":
            launch_steam(args)
    except (RuntimeError, FileNotFoundError) as e:
        UI.error(f"Fatal error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        UI.error("Cancelled by user.")
        sys.exit(130)
