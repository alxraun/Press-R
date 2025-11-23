import sys
import argparse
import shutil
import subprocess
import os
import re
from pathlib import Path
from typing import List, Optional

import setup

setup.Runtime.enforce_venv()

try:
    import tomllib
except ImportError:
    sys.stderr.write("Error: 'tomllib' module not found. Python 3.11+ is required.\n")
    sys.exit(1)

import tempfile
import utils
from utils import UI


class AssetConfig:
    CONFIG_PATH = utils.Paths.PROJECT / "assetbundler.toml"

    @classmethod
    def load(cls) -> dict:
        if not cls.CONFIG_PATH.exists():
            UI.error(f"Configuration file not found at '{cls.CONFIG_PATH}'.")
            sys.exit(1)

        try:
            with open(cls.CONFIG_PATH, "rb") as f:
                return tomllib.load(f)
        except (OSError, tomllib.TOMLDecodeError) as e:
            UI.error(f"Failed to read configuration file: {e}")
            sys.exit(1)

    @classmethod
    def get_output_directory(cls) -> Path:
        config = cls.load()
        output_dir = config.get("global", {}).get("output_directory", "")
        if not output_dir:
            UI.error("'output_directory' not specified in config.")
            sys.exit(1)
        return utils.Paths.PROJECT / output_dir

    @classmethod
    def get_bundle_names(cls) -> List[str]:
        config = cls.load()
        bundles = config.get("bundles", {})
        if not bundles:
            UI.error("No bundles defined in config.")
            sys.exit(1)
        return list(bundles.keys())

    @classmethod
    def create_temp_config_with_absolute_paths(cls) -> Optional[str]:
        config = cls.load()
        temp_path_str = config.get("global", {}).get("temp_project_path")

        if not temp_path_str or os.path.isabs(temp_path_str):
            return None

        absolute_temp_path = (utils.Paths.PROJECT / temp_path_str).resolve()

        with open(cls.CONFIG_PATH, "r", encoding="utf-8") as f:
            toml_content = f.read()

        pattern = r'(temp_project_path\s*=\s*)"[^"]*"'
        replacement = rf'\1"{absolute_temp_path.as_posix()}"'
        modified_toml_content = re.sub(pattern, replacement, toml_content)

        with tempfile.NamedTemporaryFile(
            mode="w+", delete=False, suffix=".toml", encoding="utf-8"
        ) as temp_config_file:
            temp_config_file.write(modified_toml_content)
            return temp_config_file.name


class AbbTool:
    TOOL_ID = "CryptikLemur.AssetBundleBuilder"
    TOOL_VERSION = "4.0.1"
    ABB_PATH = utils.Paths.TOOLS / "assetbundlebuilder"

    @classmethod
    def build(cls, bundle_name: str):
        if not cls.ABB_PATH.exists():
            UI.error(
                "ABB tool not found.", hint="Run 'python Scripts/setup.py setup assets'"
            )
            sys.exit(1)

        temp_config = AssetConfig.create_temp_config_with_absolute_paths()
        config_path = temp_config if temp_config else str(AssetConfig.CONFIG_PATH)

        UI.step(f"Building bundle '{bundle_name}'...")

        cmd = [
            str(cls.ABB_PATH),
            bundle_name,
            "--config",
            config_path,
            "--non-interactive",
            "--ci",
            "-vv",
        ]

        try:
            utils.run(cmd, cwd=utils.Paths.PROJECT, capture=True)
            UI.success(f"Successfully built '{bundle_name}'.")
        except subprocess.CalledProcessError as e:
            UI.error(f"Build failed for '{bundle_name}'.")
            if e.stdout:
                UI.print_line(e.stdout)
            if e.stderr:
                UI.print_line(e.stderr)
            sys.exit(1)
        finally:
            if temp_config and os.path.exists(temp_config):
                os.remove(temp_config)


class AssetCleaner:
    @staticmethod
    def _is_unity_bundle(file_path: Path) -> bool:
        try:
            with open(file_path, "rb") as f:
                header = f.read(8)
                return header.startswith(b"UnityFS") or header.startswith(b"UnityWeb")
        except OSError:
            return False

    @classmethod
    def clean_bundles(cls):
        output_dir = AssetConfig.get_output_directory()
        if not output_dir.exists():
            UI.info("Output directory does not exist. Nothing to clean.")
            return

        with UI.spin(f"Cleaning bundles in '{output_dir}'..."):
            count = 0
            for f in output_dir.iterdir():
                if f.is_file() and f.suffix != ".manifest" and cls._is_unity_bundle(f):
                    f.unlink()
                    count += 1
        UI.success(f"Removed {count} bundle file(s).")

    @classmethod
    def clean_cache(cls):
        cache_dir = utils.Paths.BUILD / "cache" / "unity-project"
        if cache_dir.exists():
            with UI.spin(f"Cleaning build cache at '{cache_dir}'..."):
                shutil.rmtree(cache_dir)
            UI.success("Build cache removed.")
        else:
            UI.info("Build cache not found.")

    @classmethod
    def clean_manifests(cls):
        output_dir = AssetConfig.get_output_directory()
        if not output_dir.exists():
            return

        with UI.spin(f"Cleaning manifests in '{output_dir}'..."):
            count = 0
            for f in output_dir.glob("*.manifest"):
                f.unlink()
                count += 1
        UI.success(f"Removed {count} manifest file(s).")

    @classmethod
    def clean_meta(cls):
        assets_dir = utils.Paths.PROJECT / "Assets"
        if not assets_dir.exists():
            return

        with UI.spin(f"Cleaning .meta files in '{assets_dir}'..."):
            count = 0
            for f in assets_dir.glob("**/*.meta"):
                f.unlink()
                count += 1
        UI.success(f"Removed {count} meta file(s).")


def run_build(args):
    bundles = AssetConfig.get_bundle_names()
    UI.header("Starting Asset Bundle Build")

    for bundle in bundles:
        AbbTool.build(bundle)

    UI.success("All bundles built successfully.")


def run_clean(args):
    if not any([args.bundles, args.cache, args.manifests, args.meta, args.all]):
        UI.error(
            "No cleanup action specified.",
            hint="Use --all or specific flags like --bundles",
        )
        sys.exit(1)

    UI.header("Starting Asset Cleanup")

    if args.bundles or args.all:
        AssetCleaner.clean_bundles()

    if args.cache or args.all:
        AssetCleaner.clean_cache()

    if args.manifests or args.all:
        AssetCleaner.clean_manifests()

    if args.meta or args.all:
        AssetCleaner.clean_meta()

    UI.success("Cleanup finished.")


def main():
    setup.Runtime.enforce_venv()

    parser = argparse.ArgumentParser(description="Manage Microtools assets")
    subparsers = parser.add_subparsers(dest="command", metavar="", required=True)

    build_parser = subparsers.add_parser("build", help="Build asset bundles")
    build_parser.set_defaults(func=run_build)

    clean_parser = subparsers.add_parser("clean", help="Clean asset artifacts")
    clean_parser.add_argument(
        "--bundles", action="store_true", help="Remove compiled bundles"
    )
    clean_parser.add_argument("--cache", action="store_true", help="Remove build cache")
    clean_parser.add_argument(
        "--manifests", action="store_true", help="Remove .manifest files"
    )
    clean_parser.add_argument("--meta", action="store_true", help="Remove .meta files")
    clean_parser.add_argument("--all", action="store_true", help="Clean everything")
    clean_parser.set_defaults(func=run_clean)

    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        sys.exit(1)

    args = parser.parse_args()

    try:
        args.func(args)
    except KeyboardInterrupt:
        UI.error("Cancelled by user.")
        sys.exit(130)


if __name__ == "__main__":
    main()
