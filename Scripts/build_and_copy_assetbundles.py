import argparse
import subprocess
import shutil
import os
import sys
import logging
import glob
from typing import Optional, Tuple, Dict, Any, List

# --- Constants ---
UNITY_ENV_VAR = "UNITY_EDITOR_PATH"

# --- Logging Setup ---
# Configure logging to be less verbose by default
logging.basicConfig(
    level=logging.INFO,  # Keep INFO level for key messages
    format="%(message)s",  # Simplified format
)
log = logging.getLogger(__name__)

# =============================================================================
# Helper Functions
# =============================================================================


def _resolve_single_path(
    cli_path: Optional[str],
    env_var_name: Optional[str],
    base_dir: str,
    description: str,
    is_file: bool = False,
    is_required: bool = True,
) -> Optional[str]:
    """Resolves a single path based on priority: CLI > Env Var."""
    path_source = "Not specified"
    resolved_path: Optional[str] = None

    # 1. Priority: CLI Argument
    if cli_path:
        if not os.path.isabs(cli_path):
            resolved_path = os.path.abspath(os.path.join(base_dir, cli_path))
        else:
            resolved_path = os.path.abspath(cli_path)
        path_source = f"CLI argument (--{description.lower().replace(' ', '-')})"
    # 2. Priority: Environment Variable
    elif env_var_name and (env_path := os.environ.get(env_var_name)):
        if not os.path.isabs(env_path):
            resolved_path = os.path.abspath(os.path.join(base_dir, env_path))
        else:
            resolved_path = os.path.abspath(env_path)
        path_source = f"Environment variable ({env_var_name})"

    if not resolved_path:
        if is_required:
            env_msg = (
                f" or environment variable '{env_var_name}'" if env_var_name else ""
            )
            log.error(
                f"ERROR: Could not determine required {description}: No value provided via CLI{env_msg}."
            )
        return None

    # 4. Validation
    if not os.path.exists(resolved_path):
        log.error(f"ERROR: Resolved {description} path does not exist: {resolved_path}")
        return None
    elif is_file and not os.path.isfile(resolved_path):
        log.error(
            f"ERROR: Resolved {description} path exists but is not a file: {resolved_path}"
        )
        return None
    elif not is_file and not os.path.isdir(resolved_path):
        log.error(
            f"ERROR: Resolved {description} path exists but is not a directory: {resolved_path}"
        )
        return None

    return resolved_path


def _execute_unity_build(unity_path: str, project_path: str, build_method: str) -> bool:
    """Executes the Unity build process in batch mode."""
    log.info(f"Starting Unity build...")
    unity_args = [
        unity_path,
        "-batchmode",
        "-quit",
        "-projectPath",
        project_path,
        "-executeMethod",
        build_method,
        # Add -logFile argument to redirect Unity logs
        "-logFile",
        "-",  # Log to stdout/stderr which subprocess captures
    ]

    try:
        process = subprocess.run(
            unity_args,
            check=True,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",  # Keep replacing errors, common with Unity logs
        )
        log.info("Unity build process finished successfully.")
        if process.stdout:  # Log stdout as well, as Unity might log info there
            log.info("Unity Build Output (stdout):\\n%s", process.stdout.strip())
        if process.stderr:
            log.warning("Unity Build Output (stderr):\\n%s", process.stderr.strip())
        return True
    except FileNotFoundError:
        log.error(
            f"ERROR: Failed to start Unity. Executable not found at: {unity_path}"
        )
        return False
    except subprocess.CalledProcessError as e:
        log.error(f"ERROR: Unity build failed with exit code {e.returncode}.")
        stdout_content = e.stdout.strip() if e.stdout else "(empty)"
        stderr_content = e.stderr.strip() if e.stderr else "(empty)"
        log.error("Unity Output (stdout):\\n%s", stdout_content)
        log.error("Unity Error Output (stderr):\\n%s", stderr_content)
        if not stdout_content and not stderr_content:
            log.warning(
                "Unity provided no output to stdout or stderr. Check Unity Editor log file for details."
            )
            # You might need to locate the Unity log file path based on OS and Unity version
            # Example for Windows: %USERPROFILE%\\AppData\\Local\\Unity\\Editor\\Editor.log
            # Example for macOS: ~/Library/Logs/Unity/Editor.log
            # Example for Linux: ~/.config/unity3d/Editor.log
            log.warning("Common Unity log locations:")
            log.warning(
                "  Windows: %USERPROFILE%\\\\AppData\\\\Local\\\\Unity\\\\Editor\\\\Editor.log"
            )
            log.warning("  macOS: ~/Library/Logs/Unity/Editor.log")
            log.warning("  Linux: ~/.config/unity3d/Editor.log")
        return False
    except KeyboardInterrupt:
        log.warning("KeyboardInterrupt detected. Aborting Unity build process.")
        # Optionally, try to terminate the Unity process if needed, though subprocess.run should handle it
        return False
    except Exception:
        log.exception("ERROR: An unexpected error occurred during Unity execution.")
        return False


def _ensure_directory_exists(dir_path: str) -> bool:
    """Checks if a directory exists, creates it if not."""
    if os.path.isdir(dir_path):
        return True
    elif os.path.exists(dir_path):
        log.error(f"ERROR: Path exists but is not a directory: {dir_path}")
        return False
    else:
        # log.info(f"Creating required directory: {dir_path}") # Create silently
        try:
            os.makedirs(dir_path)
            return True
        except OSError:
            log.exception(f"ERROR: Could not create directory '{dir_path}'.")
            return False


def _determine_pattern_base_dir(source_pattern_abs: str, base_dir: str) -> str:
    """Determines the effective base directory for a source pattern."""
    if os.path.isfile(source_pattern_abs):
        return os.path.dirname(source_pattern_abs)
    if os.path.isdir(source_pattern_abs):
        return source_pattern_abs
    if any(wild in source_pattern_abs for wild in ["*", "?", "["]):
        base_pattern_part = source_pattern_abs
        for wild in ["*", "?", "["]:
            if wild in base_pattern_part:
                base_pattern_part = base_pattern_part.split(wild, 1)[0]
        if base_pattern_part.endswith(os.sep) or base_pattern_part == "":
            potential_base = os.path.normpath(os.path.join(base_dir, base_pattern_part))
        else:
            potential_base = os.path.dirname(
                os.path.normpath(os.path.join(base_dir, base_pattern_part))
            )
        while not os.path.isdir(potential_base) and potential_base != base_dir:
            potential_base = os.path.dirname(potential_base)
        return potential_base if os.path.isdir(potential_base) else base_dir
    return base_dir


def _copy_item(item_abs_path: str, final_target_path: str, mapping: str) -> bool:
    """Unified copy of file or directory."""
    try:
        if os.path.isdir(item_abs_path):
            shutil.copytree(item_abs_path, final_target_path, dirs_exist_ok=True)
        elif os.path.isfile(item_abs_path):
            shutil.copy2(item_abs_path, final_target_path)
        else:
            log.warning(
                f"Skipping copy: Source item is neither file nor directory: {item_abs_path}"
            )
            return True
        return True
    except Exception:
        log.exception(
            f"ERROR: Failed to copy item '{item_abs_path}' for mapping '{mapping}' to '{final_target_path}'."
        )
        return False


def _process_mapping(
    mapping: str, source_base: str, target_base: str, mapping_type: str
) -> Tuple[bool, int]:
    """
    Unified handling of asset or output mapping.
    mapping_type: 'asset' or 'output'.
    Returns (success, items_copied).
    """
    overall_success = True
    items_copied = 0
    try:
        src_rel, tgt_rel = mapping.split(":", 1)
    except ValueError:
        log.error(
            f"ERROR: Skipping invalid {mapping_type} mapping format: '{mapping}'."
        )
        return False, 0

    src_abs = os.path.normpath(os.path.join(source_base, src_rel))
    tgt_abs = os.path.normpath(os.path.join(target_base, tgt_rel))
    pattern_base = _determine_pattern_base_dir(src_abs, source_base)
    includes_glob = any(ch in src_rel for ch in ["*", "?"])
    recursive = "**" in src_rel

    # Determine strategy
    strategy = "preserve_structure"
    if not includes_glob:
        strategy = (
            "copy_dir_as_subdir" if os.path.isdir(src_abs) else "copy_single_file"
        )
    elif recursive:
        parts = src_rel.split("**/")
        if len(parts) > 1:
            last = parts[-1]
            if ("*" in last or "?" in last) and "." in last:
                strategy = "flatten"
            elif "/" not in last and not any(w in last for w in ["*", "?"]):
                strategy = "flatten"

    # Ensure target exists
    ensure_dir = tgt_abs
    if not _ensure_directory_exists(ensure_dir):
        log.error(
            f"ERROR: Cannot create target '{ensure_dir}' for mapping '{mapping}'."
        )
        return False, 0

    # Find items
    found = glob.glob(src_abs, recursive=recursive)
    if not found and not includes_glob and os.path.exists(src_abs):
        found = [src_abs]
    if not found:
        return True, 0

    for item in found:
        final_tgt = None
        if strategy == "copy_dir_as_subdir":
            if item != src_abs:
                continue
            final_tgt = os.path.join(tgt_abs, os.path.basename(item))
        elif strategy == "copy_single_file":
            if item != src_abs:
                continue
            final_tgt = os.path.join(tgt_abs, os.path.basename(item))
        elif strategy == "flatten":
            if os.path.isdir(item):
                continue
            final_tgt = os.path.join(tgt_abs, os.path.basename(item))
        else:  # preserve_structure
            if item == pattern_base and includes_glob:
                continue
            rel = os.path.relpath(item, pattern_base)
            final_tgt = os.path.join(tgt_abs, rel)
            parent = os.path.dirname(final_tgt)
            if not _ensure_directory_exists(parent):
                log.error(f"ERROR: Cannot create '{parent}' for item '{item}'.")
                overall_success = False
                continue

        rel_src = os.path.relpath(item, source_base)
        rel_tgt = os.path.relpath(final_tgt, target_base)
        if _copy_item(item, final_tgt, mapping):
            if rel_src != rel_tgt or includes_glob:
                log.info(f"  - {rel_src} -> {rel_tgt} (Strategy: {strategy})")
            items_copied += 1
        else:
            overall_success = False

    return overall_success, items_copied


def _copy_source_assets(
    asset_mappings: List[str],
    target_mod_dir: str,
    unity_project_path: str,
) -> bool:  # Changed return type
    """
    Copies source assets based on mapping patterns.
    - 'file:target': Copies file.
    - 'dir:target': Copies 'dir' into 'target/dir'.
    - 'dir/*:target', 'dir/**:target': Copies contents of 'dir' into 'target', preserving structure.
    - 'dir/**/*.ext:target': Copies all matching files recursively into 'target' directly (flattened).
    Logs operations with relative paths.
    """
    if not asset_mappings:
        return True

    unity_project_root_abs = unity_project_path
    overall_success = True
    items_copied_count = 0

    for mapping in asset_mappings:
        success, count = _process_mapping(
            mapping, target_mod_dir, unity_project_path, "asset"
        )
        overall_success &= success
        items_copied_count += count

    if items_copied_count == 0 and asset_mappings:
        log.info("  (No items were copied based on the provided asset mappings)")
    elif not overall_success:
        log.error("Source asset copy finished with errors.")

    # Simple finish log moved to orchestrator
    return overall_success


def _copy_mapped_outputs(
    output_mappings: List[str],
    unity_project_path: str,
    target_mod_dir: str,
) -> bool:  # Changed return type
    """Copies build outputs and logs operations with relative paths."""
    if not output_mappings:
        return True

    overall_success = True
    items_copied_count = 0

    for mapping in output_mappings:
        success, count = _process_mapping(
            mapping, unity_project_path, target_mod_dir, "output"
        )
        overall_success &= success
        items_copied_count += count

    if items_copied_count == 0 and output_mappings:
        # Log only if mappings were provided but nothing matched (suppress if no mappings)
        log.info("  (No items matched the provided output mappings)")
    elif not overall_success:
        log.error("Build output copy finished with errors.")

    # Simple finish log moved to orchestrator
    return overall_success


# =============================================================================
# Pre-Check Helper
# =============================================================================


def _pre_check_single_mapping(
    mapping: str,
    source_base: str,
    target_base: str,
    mapping_type: str,
) -> bool:
    """
    Pre-checks a single mapping for format and target directory existence.
    mapping_type: 'asset' or 'output'.
    """
    try:
        src_rel, tgt_rel = mapping.split(":", 1)
        if not src_rel or not tgt_rel:
            raise ValueError()
    except ValueError:
        log.error(
            f"ERROR: Invalid {mapping_type} mapping format: '{mapping}'. Expected 'SRC:DEST'."
        )
        return False

    tgt_abs = os.path.normpath(os.path.join(target_base, tgt_rel))
    # Determine directory to check
    # If copying a directory as subdir or flattening, ensure base exists
    # Simplify: always require target directory exists or can be created
    if not _ensure_directory_exists(tgt_abs):
        log.error(
            f"ERROR: Target directory '{tgt_abs}' does not exist for {mapping_type} mapping '{mapping}'."
        )
        return False
    return True


def _pre_check_all_mappings(
    asset_mappings: Optional[List[str]],
    output_mappings: Optional[List[str]],
    target_mod_dir: str,
    unity_project_path: str,
) -> bool:
    """
    Performs comprehensive pre-checks for all asset and output mappings.
    Checks format, source existence (optional for assets), and target path validity.
    Aborts script by returning False on the first error found. Logs only errors.
    """
    # Simplified pre-check, focusing on target path existence based roughly on strategy

    # --- Unified Pre-Check for Asset Mappings ---
    if asset_mappings:
        for mapping in asset_mappings:
            if not _pre_check_single_mapping(
                mapping, target_mod_dir, unity_project_path, "asset"
            ):
                return False

    # --- Unified Pre-Check for Output Mappings ---
    if output_mappings:
        for mapping in output_mappings:
            if not _pre_check_single_mapping(
                mapping, unity_project_path, target_mod_dir, "output"
            ):
                return False

    return True


# =============================================================================
# Orchestration and Main Execution
# =============================================================================


def orchestrate_build_and_copy(
    target_mod_dir: str,
    unity_path: Optional[str],
    unity_project_path: str,
    asset_mappings: Optional[List[str]],
    output_mappings: Optional[List[str]],
    build_method: Optional[str],
) -> int:  # Return exit code
    """Orchestrates the AssetBundle build and copy process."""

    is_manual_mode = unity_path is None or build_method is None
    if is_manual_mode:
        log.info("Mode: Manual Build")
    else:
        log.info(f"Mode: Automatic Build")

    # 1. Copy Source Assets
    copy_assets_success = True
    if asset_mappings:
        log.info("Step 1: Copying source assets...")
        copy_assets_success = _copy_source_assets(
            asset_mappings, target_mod_dir, unity_project_path
        )
        if copy_assets_success:
            log.info("Step 1: Finished copying source assets.")  # Simple finish log
        if not copy_assets_success:
            return 1
    else:
        log.info("Step 1: Skipping source asset copy (no mappings provided).")

    # 2. Execute Build
    build_success = True
    if not is_manual_mode:
        log.info("Step 2: Executing automatic Unity build...")
        if not _execute_unity_build(unity_path, unity_project_path, build_method):
            return 1
        # Success message logged in _execute_unity_build
    else:
        log.info("Step 2: Manual build required.")
        print("\n" + "-" * 60)  # Fixed newline
        print(f"ACTION REQUIRED:")
        print(f"1. Open the Unity project: {unity_project_path}")
        print(f"2. Manually trigger the AssetBundle build process in the Unity Editor.")
        print(f"   (Ensure output matches expected location for Step 3, if applicable)")
        print(f"3. Wait for the Unity build to complete.")
        print("-" * 60)
        try:
            input(
                "--> Press Enter here once the manual Unity build is finished, or Ctrl+C to cancel... "
            )
            log.info("Resuming script after manual build confirmation.")
        except KeyboardInterrupt:
            log.info(
                "Script interrupted by user during manual build pause. Exiting gracefully."
            )
            return 0

    # 3. Copy Mapped Build Outputs
    copy_outputs_success = True
    if output_mappings:
        log.info("Step 3: Copying build outputs...")
        copy_outputs_success = _copy_mapped_outputs(
            output_mappings, unity_project_path, target_mod_dir
        )
        if copy_outputs_success:
            log.info("Step 3: Finished copying build outputs.")  # Simple finish log
        if not copy_outputs_success:
            return 1
    else:
        log.info("Step 3: Skipping build output copy (no mappings provided).")

    # Final result depends on all steps succeeding
    overall_success = copy_assets_success and build_success and copy_outputs_success

    if overall_success:
        log.info("----------------------------------------")
        log.info("Build and copy process finished successfully.")
        log.info("----------------------------------------")
        return 0
    else:
        # Specific errors logged previously
        log.error("----------------------------------------")
        log.error("Build and copy process finished with errors.")
        log.error("----------------------------------------")
        return 1


def _parse_arguments() -> argparse.Namespace:
    """Parses command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Builds Unity AssetBundles and copies assets/outputs.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )

    # --- Base Paths ---
    parser.add_argument(
        "--unity-project-path",
        required=True,
        help="Path to the Unity project root directory.",
    )
    parser.add_argument(
        "--target-mod-dir", required=True, help="Path to the target mod root directory."
    )

    # --- Unity Build Parameters ---
    parser.add_argument(
        "--unity-path",
        default=None,
        help=f"Path to Unity Editor executable. Overrides {UNITY_ENV_VAR}. Needed for automatic build.",
    )
    parser.add_argument(
        "--build-method",
        default=None,
        help="Static C# method for Unity build (e.g., 'Class.Method'). If omitted, uses manual build mode.",
    )

    # --- Asset & Output Mappings ---
    parser.add_argument(
        "--asset-mapping",
        action="append",
        default=[],
        metavar="SRC_MOD:DEST_UNITY",
        help="Copy source assets. Format: 'source:target'. Source relative to mod dir (file/dir/glob), target relative to Unity project (dir). Examples: 'MyFolder:Target' copies to 'Target/MyFolder'; 'Source/**/*:Target' copies contents preserving structure; 'Source/**/*.png:Target' copies all pngs flatly.",
    )
    parser.add_argument(
        "--output-mapping",
        action="append",
        default=[],
        metavar="SRC_UNITY:DEST_MOD",
        help="Copy build outputs in the same way as source assets: 'source:target'. Source relative to Unity project (file/dir/glob), target relative to mod dir. Supports glob patterns, recursive '**', and preserves structure similarly.",
    )

    return parser.parse_args()


def _resolve_paths(
    args: argparse.Namespace, workspace_root: str
) -> Dict[str, Optional[str]]:
    """Resolves all necessary paths using the priority logic."""
    paths = {}
    paths["unity"] = _resolve_single_path(
        args.unity_path,
        UNITY_ENV_VAR,
        workspace_root,
        "Unity Path",
        is_file=True,
        is_required=False,
    )
    paths["project"] = _resolve_single_path(
        args.unity_project_path,
        None,
        workspace_root,
        "Unity Project Path",
        is_file=False,
        is_required=True,
    )
    paths["target_mod"] = _resolve_single_path(
        args.target_mod_dir,
        None,
        workspace_root,
        "Target Mod Directory",
        is_file=False,
        is_required=True,
    )
    return paths


def _validate_required_paths(
    resolved_paths: Dict[str, Optional[str]],
    args: argparse.Namespace,
    workspace_root: str,
) -> bool:
    """Validates that all essential paths were successfully resolved or obtained."""

    # --- Validate Base Paths ---
    required_cli = ["project", "target_mod"]
    missing_or_invalid_base = [
        name for name in required_cli if resolved_paths.get(name) is None
    ]
    if missing_or_invalid_base:
        return False

    # --- Validate Unity Path (Only if build_method is specified) ---
    if args.build_method:
        if resolved_paths.get("unity") is None:
            validated_unity_path = _resolve_single_path(
                args.unity_path,
                UNITY_ENV_VAR,
                workspace_root,
                "Unity Path",
                is_file=True,
                is_required=True,
            )
            if validated_unity_path is None:
                log.error(
                    f"ERROR: Unity Path is required for automatic build mode (--build-method specified). Provide --unity-path or set {UNITY_ENV_VAR}."
                )
                return False
            resolved_paths["unity"] = validated_unity_path
        elif not os.path.isfile(resolved_paths["unity"]):
            log.error(
                f"ERROR: Resolved Unity Path is not a valid file: {resolved_paths['unity']}"
            )
            return False

    # --- Validate asset_mappings Format & Target Scope ---
    if args.asset_mapping:
        unity_project_dir_abs = resolved_paths["project"]
        if not unity_project_dir_abs:
            return False  # Should be caught by base check

        for mapping in args.asset_mapping:
            if (
                ":" not in mapping
                or len(mapping.split(":", 1)) != 2
                or not mapping.split(":", 1)[0]
                or not mapping.split(":", 1)[1]
            ):
                log.error(
                    f"ERROR: Invalid format for --asset-mapping: '{mapping}'. Expected 'SRC_MOD:DEST_UNITY'."
                )
                return False
            _, target_part = mapping.split(":", 1)
            abs_target_path = os.path.abspath(
                os.path.join(unity_project_dir_abs, target_part)
            )
            project_prefix = os.path.join(unity_project_dir_abs, "")
            if not abs_target_path.startswith(project_prefix):
                log.error(
                    f"ERROR: Invalid target path in --asset-mapping: '{target_part}'. Must resolve inside Unity project '{unity_project_dir_abs}'. Resolved: '{abs_target_path}'"
                )
                return False

    # --- Validate output_mappings Format & Target Scope ---
    if args.output_mapping:
        target_mod_dir_abs = resolved_paths["target_mod"]
        if not target_mod_dir_abs:
            return False  # Should be caught by base check

        for mapping in args.output_mapping:
            if (
                ":" not in mapping
                or len(mapping.split(":", 1)) != 2
                or not mapping.split(":", 1)[0]
                or not mapping.split(":", 1)[1]
            ):
                log.error(
                    f"ERROR: Invalid format for --output-mapping: '{mapping}'. Expected 'SRC_UNITY:DEST_MOD'."
                )
                return False
            _, target_part = mapping.split(":", 1)
            abs_target_path = os.path.abspath(
                os.path.join(target_mod_dir_abs, target_part)
            )
            mod_prefix = os.path.join(target_mod_dir_abs, "")
            if not abs_target_path.startswith(mod_prefix):
                log.error(
                    f"ERROR: Invalid target path in --output-mapping: '{target_part}'. Must resolve inside target mod dir '{target_mod_dir_abs}'. Resolved: '{abs_target_path}'"
                )
                return False

    return True


def main():
    """Main entry point for the script."""
    args = _parse_arguments()
    workspace_root = os.getcwd()

    resolved_paths = _resolve_paths(args, workspace_root)

    # Validate base paths and unity path (if needed) first
    if not _validate_required_paths(resolved_paths, args, workspace_root):
        log.critical("Aborting due to invalid configuration or missing required paths.")
        sys.exit(1)

    # Perform comprehensive pre-checks on mappings *before* orchestration
    if not _pre_check_all_mappings(
        asset_mappings=args.asset_mapping,
        output_mappings=args.output_mapping,
        target_mod_dir=resolved_paths["target_mod"],
        unity_project_path=resolved_paths["project"],
    ):
        log.critical("Aborting due to failed pre-checks.")
        sys.exit(1)

    # Proceed with orchestration only if paths and mappings are valid
    exit_code = orchestrate_build_and_copy(
        target_mod_dir=resolved_paths["target_mod"],
        unity_path=resolved_paths.get("unity"),
        unity_project_path=resolved_paths["project"],
        asset_mappings=args.asset_mapping,
        output_mappings=args.output_mapping,
        build_method=args.build_method,
    )

    sys.exit(exit_code)


if __name__ == "__main__":
    main()
