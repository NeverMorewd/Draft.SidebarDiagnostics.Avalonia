#!/usr/bin/env bash
set -euo pipefail

publish_directory="$1"
output_directory="$2"
version="${3#v}"
runtime_identifier="$4"

case "$runtime_identifier" in
  linux-x64) architecture="amd64" ;;
  linux-arm64) architecture="arm64" ;;
  *) echo "Unsupported Debian runtime: $runtime_identifier" >&2; exit 1 ;;
esac

mkdir -p "$output_directory"
output_directory="$(realpath "$output_directory")"
if [[ -z "$output_directory" || "$output_directory" == "/" ]]; then
  echo "Refusing to use an unsafe output directory." >&2
  exit 1
fi

package_root="$output_directory/deb-root-$architecture"
package_file="$output_directory/SidebarDiagnostics-$runtime_identifier.deb"
rm -rf "$package_root"
mkdir -p "$package_root/DEBIAN" "$package_root/opt/sidebar-diagnostics" "$package_root/usr/bin"
mkdir -p "$package_root/usr/share/applications" "$package_root/usr/share/icons/hicolor/scalable/apps"
mkdir -p "$package_root/usr/share/doc/sidebar-diagnostics"
cp -a "$publish_directory/." "$package_root/opt/sidebar-diagnostics/"
cp packaging/linux/net.sidebardiagnostics.app.desktop "$package_root/usr/share/applications/"
cp packaging/linux/net.sidebardiagnostics.app.svg "$package_root/usr/share/icons/hicolor/scalable/apps/"
cp LICENSE.md "$package_root/usr/share/doc/sidebar-diagnostics/copyright"
cp NOTICE.md THIRD-PARTY-NOTICES.md "$package_root/usr/share/doc/sidebar-diagnostics/"

cat > "$package_root/usr/bin/sidebar-diagnostics" <<'EOF'
#!/usr/bin/env sh
exec /opt/sidebar-diagnostics/SidebarDiagnostics.App "$@"
EOF

installed_size="$(du -sk "$package_root" | cut -f1)"
cat > "$package_root/DEBIAN/control" <<EOF
Package: sidebar-diagnostics
Version: $version
Section: utils
Priority: optional
Architecture: $architecture
Installed-Size: $installed_size
Maintainer: Sidebar Diagnostics contributors <noreply@github.com>
Depends: libfontconfig1, libice6, libsm6, libx11-6
Suggests: gnome-shell-extension-appindicator
Homepage: https://github.com/NeverMorewd/SidebarDiagnostics.Avalonia
Description: Cross-platform system monitor built with Avalonia
 Sidebar Diagnostics presents CPU, memory, storage, network, GPU, and
 available hardware sensor information in a compact desktop sidebar.
EOF

chmod 0755 "$package_root/usr/bin/sidebar-diagnostics" "$package_root/opt/sidebar-diagnostics/SidebarDiagnostics.App"
find "$package_root" -type d -exec chmod 0755 {} +
dpkg-deb --build --root-owner-group "$package_root" "$package_file"
(cd "$output_directory" && sha256sum "$(basename "$package_file")") > "$package_file.sha256"
dpkg-deb --info "$package_file"
dpkg-deb --contents "$package_file"
