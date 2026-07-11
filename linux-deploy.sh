#!/usr/bin/env bash

set -Eeuo pipefail

readonly SCRIPT_NAME="${0##*/}"

die() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

prompt_default() {
    local prompt="$1"
    local default_value="$2"
    local value

    read -r -p "$prompt [$default_value]: " value
    printf '%s' "${value:-$default_value}"
}

confirm() {
    local prompt="$1"
    local default_answer="${2:-n}"
    local answer
    local suffix='[y/N]'

    if [[ "$default_answer" == "y" ]]; then
        suffix='[Y/n]'
    fi

    read -r -p "$prompt $suffix " answer
    answer="${answer:-$default_answer}"
    [[ "$answer" =~ ^[Yy]$ ]]
}

if [[ ! -f PromptManager.slnx || ! -f PromptManager.UI/PromptManager.UI.csproj ]]; then
    die "$SCRIPT_NAME must be run from the Prompt Manager repository root."
fi

required_commands=(dotnet dpkg-deb lintian sha256sum install find du cut)
for command_name in "${required_commands[@]}"; do
    command -v "$command_name" >/dev/null 2>&1 ||
        die "Required command '$command_name' was not found. See docs/LINUX_DEPLOYMENT.md for host setup."
done

if command -v magick >/dev/null 2>&1; then
    image_command=(magick)
elif command -v convert >/dev/null 2>&1; then
    image_command=(convert)
else
    die "ImageMagick is required (the 'magick' or 'convert' command was not found)."
fi

detected_arch="$(dpkg --print-architecture 2>/dev/null || true)"
case "$detected_arch" in
    amd64|arm64) ;;
    *) detected_arch=amd64 ;;
esac

printf 'Prompt Manager local Linux package builder\n\n'
app_version="$(prompt_default 'Application version (SemVer, without a leading v)' '1.0.0')"
[[ "$app_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.+~-]+)*$ ]] ||
    die "Application version '$app_version' is not a supported SemVer value."

deb_version="$(prompt_default 'Debian package version (without a leading v)' "${app_version}-1")"
[[ "$deb_version" =~ ^[0-9][0-9A-Za-z.+:~-]*$ ]] ||
    die "Debian version '$deb_version' contains unsupported characters."

deb_arch="$(prompt_default 'Debian architecture (amd64 or arm64)' "$detected_arch")"
case "$deb_arch" in
    amd64) runtime=linux-x64 ;;
    arm64) runtime=linux-arm64 ;;
    *) die "Unsupported architecture '$deb_arch'. Choose amd64 or arm64." ;;
esac

maintainer="$(prompt_default 'Package maintainer (Name <email>)' 'Prompt Manager <promptmanager@localhost>')"
[[ "$maintainer" =~ ^.+\ \<[^\<\>[:space:]]+@[^\<\>[:space:]]+\>$ ]] ||
    die "Maintainer must use the format 'Name <email>'."

printf '\nBuild settings:\n'
printf '  Application version: %s\n' "$app_version"
printf '  Debian version:      %s\n' "$deb_version"
printf '  Architecture:        %s (%s)\n' "$deb_arch" "$runtime"
printf '  Maintainer:          %s\n\n' "$maintainer"
confirm 'Continue with restore, tests, build, and packaging?' y || die 'Cancelled by user.'

publish_root='artifacts/publish'
package_base='artifacts/package'
package_root="$package_base/promptmanager_${deb_version}_${deb_arch}"
deb_dir='artifacts/deb'
deb_file="$deb_dir/promptmanager_${deb_version}_${deb_arch}.deb"
checksum_file="${deb_file}.sha256"

rm -rf -- "$publish_root" "$package_base"

dotnet restore PromptManager.slnx
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj \
    --no-restore --configuration Release
dotnet build PromptManager.slnx --no-restore --configuration Release
dotnet publish PromptManager.UI/PromptManager.UI.csproj \
    --configuration Release \
    --runtime "$runtime" \
    --self-contained true \
    --output "$publish_root" \
    -p:Version="$app_version" \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:PublishSingleFile=false

if confirm 'Launch the published application for a GUI smoke test now?'; then
    printf 'Close Prompt Manager to continue packaging.\n'
    "$publish_root/PromptManager.UI"
fi

install -d \
    "$package_root/DEBIAN" \
    "$package_root/opt/promptmanager" \
    "$package_root/usr/bin" \
    "$package_root/usr/share/applications" \
    "$package_root/usr/share/pixmaps" \
    "$package_root/usr/share/doc/promptmanager"

cp -a "$publish_root/." "$package_root/opt/promptmanager/"
chmod 0755 "$package_root/opt/promptmanager/PromptManager.UI"
ln -s /opt/promptmanager/PromptManager.UI "$package_root/usr/bin/promptmanager"
"${image_command[@]}" 'PromptManager.UI/Assets/icon.ico[0]' \
    "$package_root/usr/share/pixmaps/promptmanager.png"
chmod 0644 "$package_root/usr/share/pixmaps/promptmanager.png"
install -m 0644 LICENSE.md "$package_root/usr/share/doc/promptmanager/copyright"

printf '%s\n' \
    '[Desktop Entry]' \
    'Type=Application' \
    'Name=Prompt Manager' \
    'Comment=Organize and reuse prompts' \
    'Exec=promptmanager' \
    'Icon=promptmanager' \
    'Terminal=false' \
    'Categories=Utility;' \
    'StartupNotify=true' \
    > "$package_root/usr/share/applications/promptmanager.desktop"

printf '%s\n' \
    'Package: promptmanager' \
    "Version: $deb_version" \
    'Section: utils' \
    'Priority: optional' \
    "Architecture: $deb_arch" \
    "Maintainer: $maintainer" \
    'Depends: libx11-6, libice6, libsm6, libfontconfig1' \
    'Homepage: https://github.com/StraykerPL/PromptManager' \
    'Description: Desktop application for organizing reusable prompts' \
    ' Prompt Manager stores, searches, rates, and organizes a local library of' \
    ' reusable prompts using folders, tags, and model metadata.' \
    > "$package_root/DEBIAN/control"

find "$package_root" -type d -exec chmod 0755 {} +
find "$package_root" -type f -exec chmod 0644 {} +
chmod 0755 "$package_root/opt/promptmanager/PromptManager.UI"
installed_size="$(du -sk "$package_root" | cut -f1)"
printf 'Installed-Size: %s\n' "$installed_size" >> "$package_root/DEBIAN/control"

mkdir -p "$deb_dir"
rm -f -- "$deb_file" "$checksum_file"
dpkg-deb --build --root-owner-group "$package_root" "$deb_file"
dpkg-deb --info "$deb_file"
dpkg-deb --contents "$deb_file"

if ! lintian "$deb_file"; then
    printf '\nLintian reported findings. Review them before distributing the package.\n' >&2
    confirm 'Continue and create the checksum?' || die 'Stopped after Lintian findings.'
fi

sha256sum "$deb_file" > "$checksum_file"
sha256sum --check "$checksum_file"

printf '\nPackage created successfully:\n  %s\n  %s\n' "$deb_file" "$checksum_file"

if confirm 'Install this package locally with APT now?'; then
    command -v sudo >/dev/null 2>&1 || die "The 'sudo' command is required for installation."
    sudo apt install "./$deb_file"
    printf 'Installed. Start the application from the desktop menu or run: promptmanager\n'
fi
