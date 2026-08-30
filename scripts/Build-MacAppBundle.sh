#!/usr/bin/env bash
set -euo pipefail

publish_directory="$1"
app="$2"
package_version="${3#v}"
bundle_version="${package_version%%-*}"

if [[ ! "$bundle_version" =~ ^[0-9]+(\.[0-9]+){0,2}$ ]]; then
  echo "Invalid macOS bundle version: $package_version" >&2
  exit 1
fi

if [[ ! -f "$publish_directory/SidebarDiagnostics.App" ]]; then
  echo "The macOS executable is missing from $publish_directory." >&2
  exit 1
fi

if [[ -e "$app" ]]; then
  echo "The output application bundle already exists: $app" >&2
  exit 1
fi

mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp -R "$publish_directory/." "$app/Contents/MacOS/"
cp src/Assets/sidebar.ico "$app/Contents/Resources/sidebar.ico"
sed -e "s/__SHORT_VERSION__/${bundle_version}/g" \
    -e "s/__BUNDLE_VERSION__/${bundle_version}/g" \
    packaging/macos/Info.plist > "$app/Contents/Info.plist"
chmod +x "$app/Contents/MacOS/SidebarDiagnostics.App"

plutil -lint "$app/Contents/Info.plist"
test "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$app/Contents/Info.plist")" = "$bundle_version"
test "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$app/Contents/Info.plist")" = "$bundle_version"
test "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Contents/Info.plist")" = "SidebarDiagnostics.App"
