#!/usr/bin/env bash
# Laver en Unity-pakke af RedMoon.Core i ./unity-export/com.redmoon.core
# Brug i Unity: Window → Package Manager → "+" → "Add package from disk..." → vælg package.json.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/unity-export/com.redmoon.core"

rm -rf "$OUT"
mkdir -p "$OUT/Runtime" "$OUT/Samples~/Bootstrap"

# Kildekoden (uden bin/obj) – Unity kompilerer selv .cs-filerne.
(cd "$ROOT/src/RedMoon.Core" && find . -name "*.cs" -not -path "./bin/*" -not -path "./obj/*" -print0 \
  | while IFS= read -r -d '' file; do
      mkdir -p "$OUT/Runtime/$(dirname "$file")"
      cp "$file" "$OUT/Runtime/$file"
    done)

cp "$ROOT/unity/package.json" "$OUT/package.json"
cp "$ROOT/unity/RedMoon.Core.asmdef" "$OUT/Runtime/RedMoon.Core.asmdef"
cp "$ROOT/unity/csc.rsp" "$OUT/Runtime/csc.rsp"
cp "$ROOT/unity/Samples/Bootstrap/"* "$OUT/Samples~/Bootstrap/"

echo "Unity-pakke klar: $OUT"
