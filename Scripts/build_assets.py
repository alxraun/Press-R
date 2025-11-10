"""High-level script for building all asset bundles."""

import sys
from utils import assets


def main():
    """Main asset build process."""
    try:
        print("--- Starting Asset Bundle Build Process ---")

        assets.ensure_abb()

        bundle_names = assets.get_bundle_names()

        for bundle_name in bundle_names:
            assets.build_bundle(bundle_name)

        print("\n--- Asset Bundle Build Process Finished Successfully ---")
    except Exception as e:
        print(f"\nError: Asset bundle build failed: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
