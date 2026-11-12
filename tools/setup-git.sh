#!/usr/bin/env sh
# Sets up a clone of this repository for development:
#   1. Git LFS (hooks for this clone and download of LFS files)
#   2. UnityYAMLMerge registered as the git mergetool "unityyamlmerge" (repository-local config)
#
# Run from Git Bash (Windows) or a terminal (macOS):
#   sh tools/setup-git.sh
#
# UnityYAMLMerge is looked up in the Unity Hub install folders for the editor version in
# Basis/ProjectSettings/ProjectVersion.txt. Pass its path explicitly when Unity is installed elsewhere:
#   sh tools/setup-git.sh --yaml-merge "/path/to/UnityYAMLMerge"

set -eu

yaml_merge=""
while [ $# -gt 0 ]; do
    case "$1" in
        --yaml-merge)
            [ $# -ge 2 ] || { echo "error: --yaml-merge needs a path" >&2; exit 2; }
            yaml_merge="$2"
            shift 2
            ;;
        -h|--help)
            sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        *)
            echo "error: unknown option: $1" >&2
            exit 2
            ;;
    esac
done

repo_root=$(git rev-parse --show-toplevel)
cd "$repo_root"

# --- Git LFS -----------------------------------------------------------------

if ! git lfs version >/dev/null 2>&1; then
    echo "error: Git LFS is not installed. Install it from https://git-lfs.com and run this script again." >&2
    exit 1
fi
git lfs install
git lfs pull
echo "Git LFS: ready"

# --- UnityYAMLMerge ----------------------------------------------------------

unity_version=$(sed -n 's/^m_EditorVersion: *//p' Basis/ProjectSettings/ProjectVersion.txt | tr -d '\r')
if [ -z "$unity_version" ]; then
    echo "error: could not read the Unity version from Basis/ProjectSettings/ProjectVersion.txt" >&2
    exit 1
fi

# Prints the Unity Hub "secondary install path" (the custom editor folder set in Hub preferences), if any.
hub_secondary_path() {
    file="$1"
    [ -f "$file" ] || return 0
    # The file holds a single JSON string, e.g. "F:\\Unity". Strip the quotes and unescape backslashes.
    sed -e 's/^[[:space:]]*"//' -e 's/"[[:space:]]*$//' -e 's/\\\\/\\/g' "$file" | tr -d '\r'
}

find_yaml_merge() {
    case "$(uname -s)" in
        Darwin)
            secondary=$(hub_secondary_path "$HOME/Library/Application Support/UnityHub/secondaryInstallPath.json")
            for base in "$secondary" "/Applications/Unity/Hub/Editor"; do
                [ -n "$base" ] || continue
                candidate="$base/$unity_version/Unity.app/Contents/Tools/UnityYAMLMerge"
                [ -x "$candidate" ] && { echo "$candidate"; return 0; }
            done
            ;;
        MINGW*|MSYS*|CYGWIN*)
            secondary=$(hub_secondary_path "${APPDATA:-}/UnityHub/secondaryInstallPath.json")
            for base in "$secondary" "${PROGRAMFILES:-C:/Program Files}/Unity/Hub/Editor"; do
                [ -n "$base" ] || continue
                candidate="$base/$unity_version/Editor/Data/Tools/UnityYAMLMerge.exe"
                [ -f "$candidate" ] && { echo "$candidate"; return 0; }
            done
            ;;
        *)
            secondary=$(hub_secondary_path "$HOME/.config/UnityHub/secondaryInstallPath.json")
            for base in "$secondary" "$HOME/Unity/Hub/Editor"; do
                [ -n "$base" ] || continue
                candidate="$base/$unity_version/Editor/Data/Tools/UnityYAMLMerge"
                [ -x "$candidate" ] && { echo "$candidate"; return 0; }
            done
            ;;
    esac
    return 1
}

if [ -z "$yaml_merge" ]; then
    if ! yaml_merge=$(find_yaml_merge); then
        echo "error: UnityYAMLMerge for Unity $unity_version was not found in the Unity Hub install folders." >&2
        echo "       Install Unity $unity_version from Unity Hub, or pass the path with --yaml-merge." >&2
        exit 1
    fi
fi
if [ ! -f "$yaml_merge" ]; then
    echo "error: $yaml_merge does not exist" >&2
    exit 1
fi

# Forward slashes keep the path intact when git runs the command through sh.
yaml_merge=$(printf '%s' "$yaml_merge" | sed 's#\\#/#g')

git config --local mergetool.unityyamlmerge.cmd "'$yaml_merge' merge -p \"\$BASE\" \"\$REMOTE\" \"\$LOCAL\" \"\$MERGED\""
git config --local mergetool.unityyamlmerge.trustExitCode false
# A *.orig backup left under Assets would be imported by Unity and get a .meta of its own.
git config --local mergetool.keepBackup false
echo "UnityYAMLMerge: registered as mergetool 'unityyamlmerge' ($yaml_merge)"
echo
echo "Resolve conflicts in scenes and prefabs with:"
echo "  git mergetool --tool=unityyamlmerge -- <file>"
