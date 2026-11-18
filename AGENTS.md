# AGENTS.md

How to work in `com.rwth.unity.omilaxr.xapi4unity`. Architecture and commands are in `CLAUDE.md`. Workspace-wide context is in `../CLAUDE.md`.

## Repository and pull requests

- This folder is its own git repo inside the `omilaxr-ecosystem` workspace. Run git commands here.
- `main` is protected. Every change goes through a pull request from a branch named `<type>/<topic>`, such as `feat/wizard-search` or `fix/download-url`. Do not name branches like versions (`vX.Y.Z`): tags have those names.
- Merge method:
  - **Squash** by default. The PR title and description become the single commit on `main`, so the PR title is what release-please reads.
  - **Rebase and merge** when the branch is a series of clean Conventional Commits that should each land on `main` and show up in the CHANGELOG. Then every commit message counts, not the PR title.
- PR titles and commit messages follow Conventional Commits: `type(scope): summary`, where type is `feat`, `fix`, `perf`, `revert`, `refactor`, `docs`, `test`, `build`, `ci` or `chore`. Do not commit unrelated working-tree changes that you did not make.
- The `Checks` workflow must pass before a merge: the PR title format, and `.github/scripts/check_package.py` (`.meta` files, guids, SPDX headers, versions). Run the script locally before opening a PR: `python3 .github/scripts/check_package.py`.

## Releases

- Releases are automated with release-please (`.github/workflows/publish.yml`). Every push to `main` updates an open release PR with the next version, the CHANGELOG and `CITATION.cff`.
- Merging the release PR creates the tag `vX.Y.Z` and the GitHub release, signs the package with Unity (`upm pack`), publishes the signed tarball to npm and attaches it to the GitHub release.
- Never tag, bump versions or write version headings by hand.

## Version

- release-please derives the version from the PR titles since the last release:
  - `fix:`, `perf:`, `revert:` give a patch.
  - `feat:` gives a minor.
  - `!` after the type or scope (`feat(api)!: …`) or a `BREAKING CHANGE: <what users must change>` footer in the PR description gives a major.
  - Other types give no release.
- To force a version, add the footer `Release-As: X.Y.Z` to the PR description.
- Breaking in this package: removing or renaming public types, members, menu items or settings; renaming serialized fields without `[FormerlySerializedAs]`; changing `.meta` guids; changing generated registry code in a way that breaks `OmiLAXR.xAPI`; raising the minimum Unity version. Deprecate with `[Obsolete]` in a minor, remove in the next major.
- release-please updates `package.json`, `.github/.release-please-manifest.json` and the `CITATION.cff` lines marked `# x-release-please-version`. The workflow sets `date-released`. A new line that holds the version needs the same marker.

## CHANGELOG

- release-please writes `CHANGELOG.md` from the PR titles: `feat` goes under `Added`, `fix` under `Fixed`, `perf` and `revert` under `Changed`, breaking changes under `⚠ BREAKING CHANGES`. Other types are hidden. Do not edit `CHANGELOG.md` in feature PRs.
- So the PR title is the changelog line. Write it for users, in English, and name types and members in backticks.
- To add detail, edit the release PR right before merging it. release-please rebuilds it on every push to `main`.
- Entries up to 2.3.0 were written by hand and keep their format.

## Unity package rules

- Every new file and folder needs a committed `.meta`. Let Unity create it (open `OmiLAXR_Dev`), or copy a neighbor's `.meta` with a fresh random 32-hex `guid`. Never delete or regenerate existing `.meta` files: prefabs, scenes and asmdefs reference their GUIDs. Folders ending in `~` (`Samples~`) have no `.meta`.
- The minimum Unity version is `2020.3` (`package.json` `unity`), but development happens on Unity 6. Put version-specific APIs behind `#if UNITY_2023_1_OR_NEWER` (or similar).
- Serialized field names are part of the scene and prefab format. Rename them only with `[FormerlySerializedAs]`.
- New components get `[AddComponentMenu("OmiLAXR / …")]`. Copy the path from a component of the same kind.
- License: Source headers and README say AGPL-3.0-or-later, but `LICENSE` contains the GPL-3.0 text and `package.json` has no `license` field. Do not change either without asking the user.
- Add or adjust tests in `Tests/` with every behaviour change. Name test files `<Subject>_Tests.cs`, name methods `Subject_Does_Something`, and use `// Arrange / Act / Assert` comments.

## xAPI4Unity-specific rules

- Write CHANGELOG entries in English. The 2.2.3 entry in German is an exception, not the rule.
- The generated C# is consumed by `OmiLAXR.xAPI` (namespace `xAPI.Registry`). After changing the generator, regenerate a registry and check that `OmiLAXR.xAPI` still compiles with `XAPI_REGISTRY_EXISTS` set.
