import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import List, Dict, Optional

import setup
import utils
from utils import UI


SOLUTION_FILE = utils.Paths.PROJECT / "Microtools.sln"


def get_solution_projects(sln_path: Path) -> Dict[str, Path]:
    projects = {}
    if not sln_path.exists():
        UI.error(f"Solution file not found: {sln_path}")
        sys.exit(1)

    project_pattern = re.compile(
        r'Project\("{[^}]+}"\)\s*=\s*"([^"]+)",\s*"([^"]+)",\s*"{[^}]+}"'
    )

    with open(sln_path, "r", encoding="utf-8-sig") as f:
        for line in f:
            match = project_pattern.search(line)
            if match:
                name, path_str = match.groups()
                if path_str.endswith(".csproj"):
                    path = Path(path_str.replace("\\", "/"))
                    projects[name] = (sln_path.parent / path).resolve()

    return projects


def _parse_dotnet_errors(output: str) -> List[str]:
    if not output:
        return []
    return [
        line
        for line in output.splitlines()
        if "error" in line.lower() or "warning" in line.lower()
    ]


def run_dotnet_clean(target: Path) -> None:
    cmd = ["dotnet", "clean", str(target)]
    with UI.spin(f"Cleaning {target.name}..."):
        utils.run(cmd, capture=True)


def run_dotnet_build(target: Path, config: str) -> None:
    cmd = [
        "dotnet",
        "build",
        str(target),
        "-c",
        config,
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary;ForceNoAlign",
    ]
    with UI.spin(f"Building {target.name} ({config})..."):
        try:
            utils.run(cmd, capture=True)
        except subprocess.CalledProcessError as e:
            error_lines = []
            if e.stdout:
                error_lines.extend(_parse_dotnet_errors(e.stdout))
            if e.stderr:
                error_lines.extend(_parse_dotnet_errors(e.stderr))

            for line in error_lines:
                UI.print_line(f"{utils.Colors.RED}{line.strip()}{utils.Colors.RESET}")
            raise e


def parse_arguments(projects: Dict[str, Path]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build FlexVerse projects.")

    parser.add_argument(
        "targets",
        nargs="+",
        choices=["all"] + list(projects.keys()),
        help="Target(s) to build. Use 'all' to build the entire solution, or specify one or more project names.",
    )

    parser.add_argument(
        "--config",
        "-c",
        choices=["Debug", "Release"],
        default="Debug",
        help="Build configuration (default: Debug)",
    )

    parser.add_argument(
        "--clean",
        action="store_true",
        help="Clean output directory before build",
    )

    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        sys.exit(1)

    return parser.parse_args()


def run_build(
    targets: List[str],
    config: str,
    should_clean: bool,
    projects: Dict[str, Path],
    sln_path: Path,
) -> None:
    setup.Runtime.enforce_venv()

    build_targets: List[Path] = []

    if "all" in targets:
        build_targets.append(sln_path)
    else:
        for t in targets:
            if t in projects:
                build_targets.append(projects[t])
            else:
                UI.error(f"Unknown target: {t}")
                sys.exit(1)

    if should_clean:
        for target in build_targets:
            run_dotnet_clean(target)
    for target in build_targets:
        run_dotnet_build(target, config)

    UI.success(f"Build completed successfully ({config}).")


def main():
    projects = get_solution_projects(SOLUTION_FILE)

    args = parse_arguments(projects)

    try:
        run_build(args.targets, args.config, args.clean, projects, SOLUTION_FILE)
    except Exception:
        sys.exit(1)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        UI.error("Cancelled by user.")
        sys.exit(130)
