"""High-level script for cleaning asset bundles."""

import argparse
import sys
from utils import assets


def main():
    """Main asset cleanup process."""
    parser = argparse.ArgumentParser(
        description="Clean asset build artifacts. Specify what to clean explicitly."
    )
    parser.add_argument(
        "--bundles",
        action="store_true",
        help="Remove compiled asset bundles.",
    )
    parser.add_argument(
        "--cache",
        action="store_true",
        help="Clean build cache (temporary Unity project).",
    )
    parser.add_argument(
        "--tool", action="store_true", help="Remove locally installed ABB tool."
    )
    parser.add_argument(
        "--manifests",
        action="store_true",
        help="Remove .manifest files from output directory.",
    )
    parser.add_argument(
        "--meta", action="store_true", help="Remove .meta files from Assets directory."
    )
    parser.add_argument(
        "--all",
        action="store_true",
        help="Full cleanup: bundles, cache, tool, manifests and meta files.",
    )
    args = parser.parse_args()

    if not any(
        [args.bundles, args.cache, args.tool, args.manifests, args.meta, args.all]
    ):
        parser.error(
            "No cleanup action specified. Use --bundles, --cache, --tool, --manifests, --meta, or --all."
        )

    if args.all and any(
        [args.bundles, args.cache, args.tool, args.manifests, args.meta]
    ):
        parser.error("--all cannot be combined with other flags.")

    try:
        print("--- Starting Asset Cleanup ---")
        print()

        if args.bundles or args.all:
            assets.clean_built_bundles()
            if not args.all or (args.all and not (args.cache or args.tool)):
                print()
        else:
            print("Bundles: not requested for cleanup")
            print()

        if args.cache or args.all:
            assets.clean_build_cache()
            if not args.all or (args.all and not args.tool):
                print()
        else:
            print("Cache: not requested for cleanup")
            print()

        if args.tool or args.all:
            assets.clean_abb_tool()
            if not args.all or (args.all and not (args.manifests or args.meta)):
                print()
        else:
            print("Tool: not requested for cleanup")
            print()

        if args.manifests or args.all:
            assets.clean_manifests()
            if not args.all or (args.all and not args.meta):
                print()
        else:
            print("Manifests: not requested for cleanup")
            print()

        if args.meta or args.all:
            assets.clean_meta_files()
            print()
        else:
            print("Meta files: not requested for cleanup")
            print()

        print("--- Asset Cleanup Finished Successfully ---")
    except Exception as e:
        print(f"\nError: Asset cleanup failed: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
