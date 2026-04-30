#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$repo_root/PromptManager"
dotnet run "PromptManager.csproj" -f net10.0-windows10.0.19041.0 "$@"
