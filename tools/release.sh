#!/usr/bin/env bash
# Release Trampoline everywhere in one go:
#   tools/release.sh --major | --minor | --patch   start a new release
#   tools/release.sh --resume                      finish the last tag's release
#
# 1. Game repo: branch release-X.Y.Z off main, bump the version
#    (export_presets.cfg, flake.nix), write the release notes in $EDITOR
#    (fastlane/.../fr-FR/changelogs/<versionCode>.txt), open the PR, wait
#    for CI, merge it.
# 2. Tag vX.Y.Z on main and wait for release.yml: GitHub Release (signed
#    APK), gh-pages and magamajo.
# 3. nixpkgs (../nixpkgs): new version and source hash, test build, then
#    amend the "init" PR while it isn't merged, or open an update PR.
# 4. fdroiddata (../fdroiddata): while the recipe isn't in F-Droid yet,
#    move its build to the new version and amend the MR branch; once it
#    is, F-Droid's bot adds new versions itself (AutoUpdateMode).
#
# Each step checks whether it's already done, so after an interruption
# (failed CI, closed terminal) running the same command again picks up where
# it stopped. Once the tag is pushed, the next version would be computed
# from it: use --resume then.
set -euo pipefail

github_repo=MaximilienNaveau/trampoline
nixpkgs_file=pkgs/by-name/tr/trampoline/package.nix
fdroid_file=metadata/com.magamajo.trampoline.yml
fdroid_branch=com.magamajo.trampoline

step() { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
die() { printf '\033[31merror:\033[0m %s\n' "$*" >&2; exit 1; }

case "${1:-}" in
  --major | --minor | --patch | --resume) part=${1#--} ;;
  *) sed -n '2,4s/^# \?//p' "$0" >&2; exit 2 ;;
esac

game=$(cd "$(dirname "$0")/.." && pwd)
nixpkgs=${NIXPKGS_DIR:-$game/../nixpkgs}
fdroiddata=${FDROIDDATA_DIR:-$game/../fdroiddata}

for tool in git gh nix jq curl; do
  command -v "$tool" >/dev/null || die "$tool is missing"
done
gh auth status >/dev/null 2>&1 || die "gh isn't logged in: run gh auth login"
[ -d "$nixpkgs/.git" ] || die "no nixpkgs clone at $nixpkgs (set NIXPKGS_DIR)"
[ -d "$fdroiddata/.git" ] || die "no fdroiddata clone at $fdroiddata (set FDROIDDATA_DIR)"

cd "$game"
[ -z "$(git status --porcelain)" ] || die "$game has uncommitted changes"
git switch -q main
git pull -q --ff-only origin main
git fetch -q --tags origin

# The new version, from the last tag.
last=$(git tag --list 'v[0-9]*.[0-9]*.[0-9]*' --sort=-v:refname | head -1)
last=${last#v}
IFS=. read -r major minor patch <<<"$last"
case $part in
  major) version=$((major + 1)).0.0 ;;
  minor) version=$major.$((minor + 1)).0 ;;
  patch) version=$major.$minor.$((patch + 1)) ;;
  resume) version=$last ;;
esac
# Android's versionCode, as for every release so far: 4.1.0 -> 4001000.
IFS=. read -r major minor patch <<<"$version"
code=$((major * 1000000 + minor * 1000 + patch))
branch=release-$version
notes=fastlane/metadata/android/fr-FR/changelogs/$code.txt

read -rp "Release Trampoline $version (versionCode $code)? [y/N] " answer
[ "$answer" = y ] || exit 1

# --- 1. Release branch and PR ------------------------------------------------
pr_state=$(gh pr view "$branch" --repo "$github_repo" --json state --jq .state 2>/dev/null || true)
if ! git rev-parse -q --verify "refs/tags/v$version" >/dev/null && [ "$pr_state" != MERGED ]; then
  step "Branch $branch"
  git switch -q "$branch" 2>/dev/null || git switch -q -c "$branch"

  current=$(sed -n 's/^version\/name="\(.*\)"$/\1/p' export_presets.cfg)
  if [ "$current" != "$version" ]; then
    # Every preset's version (Android's name and code, macOS's and iOS's).
    sed -i -e "s/^version\/code=.*/version\/code=$code/" \
      -e "s/^version\/name=\".*\"/version\/name=\"$version\"/" \
      -e "s/^application\/\(short_\)\?version=\".*\"/application\/\1version=\"$version\"/" \
      export_presets.cfg
    sed -i "s/^\( *version = \)\"${current//./\\.}\";/\1\"$version\";/" flake.nix
    grep -q "version = \"$version\";" flake.nix || die "flake.nix: no version = \"$current\"; to bump"
    git commit -q -am "Bump the version to $version"
  fi

  if [ ! -s "$notes" ]; then
    step "Release notes ($notes)"
    draft=$(mktemp --suffix=.txt)
    {
      echo "# Notes de version $version, en français, 500 caractères au plus"
      echo "# (F-Droid coupe au-delà). Les lignes en # sont ignorées."
      echo "# Commits depuis v$last :"
      git log --no-merges --format='#   %s' "v$last..HEAD"
    } >"$draft"
    "${VISUAL:-${EDITOR:-vi}}" "$draft"
    # Drop the comments and the blank lines before the text.
    { grep -v '^#' "$draft" || true; } | sed '/./,$!d' >"$notes"
    rm -f "$draft"
  fi
  grep -q '[^[:space:]]' "$notes" || { rm -f "$notes"; die "empty release notes"; }
  [ "$(wc -m <"$notes")" -le 500 ] || die "$notes is over 500 characters: shorten it, then run this again"
  if [ -n "$(git status --porcelain -- "$notes")" ]; then
    git add "$notes"
    git commit -q -m "Write the $version release notes"
  fi

  step "Pull request"
  git push -q -u origin "$branch"
  [ -n "$pr_state" ] || gh pr create --repo "$github_repo" --base main --head "$branch" \
    --title "Release $version" --body-file "$notes"
  # CI takes a moment to register its checks on a new PR.
  while [[ $(gh pr checks "$branch" --repo "$github_repo" 2>&1 || true) == *"no checks reported"* ]]; do
    sleep 20
  done
  gh pr checks "$branch" --repo "$github_repo" --watch --fail-fast --interval 60 ||
    die "CI failed: fix it on $branch, then run this again"
  gh pr merge "$branch" --repo "$github_repo" --merge --delete-branch
  git switch -q main
  git pull -q --ff-only origin main
  git branch -q -D "$branch" 2>/dev/null || true
fi

# --- 2. Tag and release workflow ---------------------------------------------
if ! git ls-remote --exit-code --tags origin "refs/tags/v$version" >/dev/null; then
  step "Tag v$version"
  git pull -q --ff-only origin main
  [ "$(sed -n 's/^version\/name="\(.*\)"$/\1/p' export_presets.cfg)" = "$version" ] ||
    die "main's export_presets.cfg isn't at $version"
  git rev-parse -q --verify "refs/tags/v$version" >/dev/null ||
    git tag -a "v$version" -m "Trampoline $version"
  git push -q origin "v$version"
fi

if ! gh release view "v$version" --repo "$github_repo" >/dev/null 2>&1; then
  step "Release workflow"
  run=""
  while [ -z "$run" ]; do
    sleep 10
    run=$(gh run list --repo "$github_repo" --workflow release.yml --branch "v$version" \
      --limit 1 --json databaseId --jq '.[].databaseId')
  done
  gh run watch "$run" --repo "$github_repo" --exit-status --interval 60 >/dev/null ||
    die "the release workflow failed (gh run view $run --repo $github_repo --log-failed): re-run it, then run this with --resume"
  gh release view "v$version" --repo "$github_repo" >/dev/null || die "no GitHub Release v$version"
fi
echo "GitHub Release: https://github.com/$github_repo/releases/tag/v$version"

# Is $2 on the default branch of the public repo $1 (a raw-file URL prefix)?
upstream_has() { [ "$(curl -s -o /dev/null -w '%{http_code}' "$1/$2")" = 200 ]; }

# --- 3. nixpkgs --------------------------------------------------------------
step "nixpkgs"
cd "$nixpkgs"
git diff --quiet -- . ":!$nixpkgs_file" || die "$nixpkgs has unrelated changes"
# fetchFromGitHub's hash: the unpacked tag tarball's, which this prefetches.
hash=$(nix flake prefetch --json "github:$github_repo/v$version" | jq -r .hash)
set_nixpkgs_version() {
  sed -i -e "s|^  version = \".*\";|  version = \"$version\";|" \
    -e "s|^    hash = .*;|    hash = \"$hash\";|" "$nixpkgs_file"
  nix-build -A trampoline --no-out-link >/dev/null
  git add "$nixpkgs_file"
}
if upstream_has https://raw.githubusercontent.com/NixOS/nixpkgs/master "$nixpkgs_file"; then
  # Already in nixpkgs: an update PR (unless r-ryantm's bot was faster).
  git fetch -q upstream master
  old=$(git show "upstream/master:$nixpkgs_file" | sed -n 's/^  version = "\(.*\)";/\1/p')
  if [ "$old" = "$version" ]; then
    echo "nixpkgs already has $version"
  else
    git switch -q -C "trampoline-$version" upstream/master
    set_nixpkgs_version
    git commit -q -m "trampoline: $old -> $version"
    git push -q -f -u origin "trampoline-$version"
    gh pr create --repo NixOS/nixpkgs --base master \
      --head "${github_repo%%/*}:trampoline-$version" --fill
  fi
else
  # The "init" PR isn't merged yet: it packages the new version instead.
  git switch -q trampoline-init
  set_nixpkgs_version
  git commit -q --amend -m "$(git log -1 --format=%B | sed "1s/.*/trampoline: init at $version/")"
  git push -q -f origin trampoline-init
  echo "nixpkgs PR updated: trampoline: init at $version"
fi

# --- 4. fdroiddata -----------------------------------------------------------
step "fdroiddata"
cd "$fdroiddata"
if upstream_has https://gitlab.com/fdroid/fdroiddata/-/raw/master "$fdroid_file"; then
  echo "F-Droid's bot adds $version itself (AutoUpdateMode: Version), within a day or two."
else
  git switch -q "$fdroid_branch"
  git diff --quiet -- . ":!$fdroid_file" || die "$fdroiddata has unrelated changes"
  # The recipe's only build, and the version F-Droid offers.
  sed -i -e "s/^  - versionName: .*/  - versionName: $version/" \
    -e "s/^    versionCode: .*/    versionCode: $code/" \
    -e "s/^    commit: .*/    commit: v$version/" \
    -e "s/^CurrentVersion: .*/CurrentVersion: $version/" \
    -e "s/^CurrentVersionCode: .*/CurrentVersionCode: $code/" "$fdroid_file"
  git add "$fdroid_file"
  git commit -q --amend --no-edit
  git push -q -f origin "$fdroid_branch"
  echo "Merge request: https://gitlab.com/maximilien_naveau/fdroiddata/-/merge_requests/new?merge_request%5Bsource_branch%5D=$fdroid_branch"
fi

step "Trampoline $version is released"
