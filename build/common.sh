# Sourced by package.sh and publish.sh.

# version_number from thunderstore/manifest.json under the given repo root.
manifest_version() {
    python3 - "$1" <<'PY'
import json, os, sys
print(json.load(open(os.path.join(sys.argv[1], "thunderstore/manifest.json")))["version_number"])
PY
}
