#!/usr/bin/env bash
# Samler Unity-udgaven af Rød Måne i ./unity-export/RedMoon (+ RedMoon-Unity.zip).
# Brug i Unity: træk mappen "RedMoon" ind i dit projekts Assets-mappe, og tryk Play. Se unity/LAES-MIG.md.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXPORT="$ROOT/unity-export"
OUT="$EXPORT/RedMoon"

rm -rf "$OUT" "$EXPORT/RedMoon-Unity.zip"
mkdir -p "$OUT/Core" "$OUT/App" "$OUT/Plugins"

# 1) Forretningslogikken (samme kildekode som mobilappen).
(cd "$ROOT/src/RedMoon.Core" && find . -name "*.cs" -not -path "./bin/*" -not -path "./obj/*" -print0 \
  | while IFS= read -r -d '' file; do
      mkdir -p "$OUT/Core/$(dirname "$file")"
      cp "$file" "$OUT/Core/$file"
    done)
cp "$ROOT/unity/RedMoon.Core.asmdef" "$OUT/Core/RedMoon.Core.asmdef"
cp "$ROOT/unity/csc.rsp" "$OUT/Core/csc.rsp"

# 2) Unity-brugerfladen, stilark og native plugins (Keychain/Keystore).
cp -R "$ROOT/unity/App/." "$OUT/App/"
cp -R "$ROOT/unity/Plugins/." "$OUT/Plugins/"
cp "$ROOT/unity/LAES-MIG.md" "$OUT/LAES-MIG.md"

(cd "$EXPORT" && zip -qr RedMoon-Unity.zip RedMoon)
echo "Unity-udgave klar: $OUT"
echo "Zip: $EXPORT/RedMoon-Unity.zip"
