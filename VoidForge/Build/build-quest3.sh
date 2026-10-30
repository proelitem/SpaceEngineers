#!/usr/bin/env bash
# Build de l'APK Quest 3 en ligne de commande (Linux / macOS).
# Prérequis : Unity 6.3 LTS avec le module Android (SDK, NDK, OpenJDK) installé par Unity Hub.
# Usage : UNITY_PATH=/chemin/vers/Unity ./Build/build-quest3.sh [--dev] [sortie.apk]
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_PATH="${UNITY_PATH:-}"
DEV_FLAG=""
OUTPUT="Builds/VoidForge-Quest3.apk"

for arg in "$@"; do
  case "$arg" in
    --dev) DEV_FLAG="-vfDev"; OUTPUT="Builds/VoidForge-Quest3-dev.apk" ;;
    *) OUTPUT="$arg" ;;
  esac
done

if [[ -z "$UNITY_PATH" ]]; then
  echo "Définis UNITY_PATH (ex. ~/Unity/Hub/Editor/6000.3.0f1/Editor/Unity)." >&2
  exit 1
fi

mkdir -p "$PROJECT_DIR/Logs"
"$UNITY_PATH" \
  -batchmode -nographics -quit \
  -projectPath "$PROJECT_DIR" \
  -buildTarget Android \
  -executeMethod VoidForge.Editor.QuestBuildPipeline.BuildBatch \
  -vfOutput "$OUTPUT" $DEV_FLAG \
  -logFile "$PROJECT_DIR/Logs/build-quest3.log"

echo "APK : $PROJECT_DIR/$OUTPUT"
