"""Low-level utilities for managing asset bundles."""

import subprocess
import sys
import shutil
import tomllib
import tempfile
import os
import re
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent.parent
CONFIG_PATH = PROJECT_ROOT / ".assetbundler.toml"
TEMP_UNITY_PROJECT = PROJECT_ROOT / ".build" / "cache" / "unity-project"

TOOL_ID = "CryptikLemur.AssetBundleBuilder"
TOOL_VERSION = "4.0.1"
TOOLS_DIR = PROJECT_ROOT / ".build" / "tools"
ABB_PATH = TOOLS_DIR / "assetbundlebuilder"


def _load_config():
    """Load and parse configuration from .assetbundler.toml."""
    if not CONFIG_PATH.exists():
        print(
            f"Error: Configuration file not found at '{CONFIG_PATH}'.", file=sys.stderr
        )
        sys.exit(1)

    try:
        with open(CONFIG_PATH, "rb") as f:
            return tomllib.load(f)
    except OSError as e:
        print(f"Error: Failed to read configuration file: {e}", file=sys.stderr)
        sys.exit(1)
    except tomllib.TOMLDecodeError as e:
        print("Error: Invalid syntax in .assetbundler.toml.", file=sys.stderr)
        print(f"Details: {e}", file=sys.stderr)
        sys.exit(1)


def _get_output_directory():
    """Get output directory from configuration."""
    config = _load_config()
    output_dir = config.get("global", {}).get("output_directory", "")
    if not output_dir:
        print("Error: 'output_directory' not specified in config.", file=sys.stderr)
        sys.exit(1)
    return PROJECT_ROOT / output_dir


def _get_bundle_names():
    """Get list of bundle names from configuration."""
    config = _load_config()
    bundles = config.get("bundles", {})
    if not bundles:
        print("Error: No bundles defined in config.", file=sys.stderr)
        sys.exit(1)
    return list(bundles.keys())


def _get_bundle_filename_pattern(bundle_name: str):
    """Get filename pattern for specified bundle."""
    config = _load_config()
    bundle_config = config.get("bundles", {}).get(bundle_name, {})
    return bundle_config.get("filename", bundle_name + "_[target]")


def get_bundle_names():
    """Get list of bundle names (public function)."""
    return _get_bundle_names()


def ensure_abb():
    """Ensure ABB of required version is installed in .build/tools."""
    if ABB_PATH.exists():
        print(f"ABB found at '{ABB_PATH}'.")
        return

    print(f"ABB not found. Installing version {TOOL_VERSION} into '{TOOLS_DIR}'...")
    TOOLS_DIR.mkdir(parents=True, exist_ok=True)

    try:
        command = [
            "dotnet",
            "tool",
            "install",
            TOOL_ID,
            "--version",
            TOOL_VERSION,
            "--tool-path",
            str(TOOLS_DIR),
        ]
        subprocess.run(command, check=True, text=True, capture_output=True)
        print("ABB installed successfully.")
    except FileNotFoundError:
        print("Error: .NET SDK not found. Please install .NET SDK.", file=sys.stderr)
        sys.exit(1)
    except subprocess.CalledProcessError as e:
        print("Error: Failed to install ABB.", file=sys.stderr)
        if "Unable to load the service index" in e.stderr or "NU1301" in e.stderr:
            print(
                "Network error: Failed to download tool. Please check your internet connection.",
                file=sys.stderr,
            )
        print(e.stderr, file=sys.stderr)
        sys.exit(1)


def build_bundle(bundle_name: str):
    """Build single bundle for all platforms."""
    print(f"Building '{bundle_name}' for all default targets...")

    config = _load_config()

    temp_path_str = config.get("global", {}).get("temp_project_path")
    if temp_path_str and not os.path.isabs(temp_path_str):
        absolute_temp_path = (PROJECT_ROOT / temp_path_str).resolve()

        with open(CONFIG_PATH, "r", encoding="utf-8") as f:
            toml_content = f.read()

        pattern = r'(temp_project_path\s*=\s*)"[^"]*"'
        replacement = rf'\1"{absolute_temp_path.as_posix()}"'
        modified_toml_content = re.sub(pattern, replacement, toml_content)

        if modified_toml_content == toml_content:
            print(
                "Warning: Could not find temp_project_path in config, using original",
                file=sys.stderr,
            )

        with tempfile.NamedTemporaryFile(
            mode="w+", delete=False, suffix=".toml", encoding="utf-8"
        ) as temp_config_file:
            temp_config_file.write(modified_toml_content)
            temp_config_path = temp_config_file.name

        effective_config_path = temp_config_path
        print(f"Using temporary config with absolute path: {effective_config_path}")
    else:
        effective_config_path = str(CONFIG_PATH)
        temp_config_path = None

    try:
        command = [
            str(ABB_PATH),
            bundle_name,
            "--config",
            effective_config_path,
            "--non-interactive",
            "--ci",
            "-vv",
        ]

        result = subprocess.run(
            command, check=True, text=True, cwd=PROJECT_ROOT, capture_output=True
        )
        print(result.stdout)
        if result.stderr:
            print("--- ABB Standard Error ---", file=sys.stderr)
            print(result.stderr, file=sys.stderr)

        print(f"Successfully built '{bundle_name}'.")
    except PermissionError as e:
        print(
            f"Error: Permission denied accessing build files for '{bundle_name}': {e}",
            file=sys.stderr,
        )
        raise
    except subprocess.CalledProcessError as e:
        print(
            f"Error: Build failed for '{bundle_name}'. ABB returned non-zero exit code.",
            file=sys.stderr,
        )
        print("\n--- ABB Standard Output ---", file=sys.stderr)
        print(e.stdout or " (empty)", file=sys.stderr)
        print("\n--- ABB Standard Error ---", file=sys.stderr)
        print(e.stderr or " (empty)", file=sys.stderr)
        raise
    finally:
        if temp_config_path and os.path.exists(temp_config_path):
            os.remove(temp_config_path)
            print(f"Removed temporary config: {temp_config_path}")


def clean_built_bundles():
    """Remove all generated asset bundles by checking file signatures."""
    try:
        output_dir = _get_output_directory()
    except Exception as e:
        print(f"Error: Failed to get output directory: {e}", file=sys.stderr)
        raise

    print(f"Cleaning asset bundles in '{output_dir}'...")

    if not output_dir.exists():
        print("Output directory does not exist. Nothing to clean.")
        return

    cleaned_count = 0

    try:

        for file_path in output_dir.iterdir():
            if not file_path.is_file():
                continue

            if file_path.suffix == ".manifest":
                continue

            if _is_unity_asset_bundle(file_path):
                try:
                    file_path.unlink()
                    cleaned_count += 1
                    print(f"Removed bundle: {file_path.name}")
                except OSError as e:
                    print(
                        f"Error: Failed to remove bundle '{file_path.name}': {e}",
                        file=sys.stderr,
                    )
                    raise

    except Exception as e:
        print(f"Error: Failed during bundle cleanup: {e}", file=sys.stderr)
        raise

    print(f"Removed {cleaned_count} bundle file(s).")


def _is_unity_asset_bundle(file_path: Path) -> bool:
    """Check if file is a Unity asset bundle by reading its signature."""
    try:
        with open(file_path, "rb") as f:

            signature = f.read(8)

            return signature.startswith(b"UnityFS") or signature.startswith(b"UnityWeb")
    except (OSError, IOError):

        return False


def clean_build_cache():
    """Remove ABB build cache (Unity project)."""
    print(f"Cleaning ABB build cache at '{TEMP_UNITY_PROJECT}'...")
    try:
        if TEMP_UNITY_PROJECT.exists():
            shutil.rmtree(TEMP_UNITY_PROJECT)
            print("Build cache removed.")
        else:
            print("Build cache does not exist. Nothing to clean.")
    except OSError as e:
        print(f"Error: Failed to remove build cache: {e}", file=sys.stderr)
        raise


def clean_manifests():
    """Remove .manifest files from the output directory."""
    try:
        output_dir = _get_output_directory()
        if not output_dir.exists():
            print("Output directory does not exist. Nothing to clean.")
            return

        print(f"Cleaning .manifest files in '{output_dir}'...")

        cleaned_count = 0
        for f in output_dir.glob("*.manifest"):
            if f.is_file():
                f.unlink()
                cleaned_count += 1

        print(f"Removed {cleaned_count} manifest file(s).")
    except Exception as e:
        print(f"Error: Failed to clean up manifest files: {e}", file=sys.stderr)
        raise


def clean_meta_files():
    """Remove .meta files from the Assets directory."""
    assets_dir = PROJECT_ROOT / "Assets"
    if not assets_dir.exists():
        print("Assets directory does not exist. Nothing to clean.")
        return

    print(f"Cleaning .meta files in '{assets_dir}'...")

    cleaned_count = 0
    try:
        for f in assets_dir.glob("**/*.meta"):
            if f.is_file():
                f.unlink()
                cleaned_count += 1

        print(f"Removed {cleaned_count} meta file(s).")
    except Exception as e:
        print(f"Error: Failed to clean up meta files: {e}", file=sys.stderr)
        raise


def clean_abb_tool():
    """Remove locally installed ABB tool."""
    if not ABB_PATH.exists():
        print("ABB tool does not exist. Nothing to clean.")
        return

    print(f"Cleaning ABB tool from '{TOOLS_DIR}'...")

    try:
        command = [
            "dotnet",
            "tool",
            "uninstall",
            TOOL_ID,
            "--tool-path",
            str(TOOLS_DIR),
        ]
        subprocess.run(command, check=True, text=True, capture_output=True)
        print("ABB tool removed.")

        if TOOLS_DIR.exists() and not any(TOOLS_DIR.iterdir()):
            try:
                TOOLS_DIR.rmdir()
                print(f"Removed empty directory '{TOOLS_DIR}'.")
            except OSError as e:
                print(f"Error: Failed to remove tools directory: {e}", file=sys.stderr)
                raise

    except subprocess.CalledProcessError as e:
        print("Error: Failed to uninstall ABB.", file=sys.stderr)
        print(e.stderr, file=sys.stderr)
        sys.exit(1)
