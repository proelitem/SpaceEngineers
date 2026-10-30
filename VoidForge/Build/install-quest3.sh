#!/usr/bin/env bash
# Installe l'APK sur le Quest 3 branché en USB (mode développeur activé) et le lance.
set -euo pipefail
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
APK="${1:-$PROJECT_DIR/Builds/VoidForge-Quest3.apk}"
adb devices
adb install -r "$APK"
adb shell am start -n com.proelitem.voidforge/com.unity3d.player.UnityPlayerGameActivity 2>/dev/null \
  || adb shell monkey -p com.proelitem.voidforge -c android.intent.category.LAUNCHER 1
echo "Journal en direct : adb logcat -s Unity"
