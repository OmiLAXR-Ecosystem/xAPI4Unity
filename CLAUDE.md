# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Working rules (versioning, CHANGELOG, `.meta` files, licenses, compatibility) are in @AGENTS.md. Follow them.

## What this is

`com.rwth.unity.omilaxr.xapi4unity`: Editor-only Unity package that turns xAPI JSON definitions (the workspace `xapi` repo, or local `Assets/xapi`) into C# under `Assets/xAPI.Registry`.

## Commands

There is no command-line build or linter: the package compiles inside a Unity project. Paths below are relative to the workspace root (`..`).

- **Develop:** open `OmiLAXR_Dev/` in Unity 6000.3.13f1. Its `Packages/manifest.json` links `com.rwth.unity.omilaxr.xapi4unity` from disk (`file:`), so edits recompile live.
- **Tests** (`Tests/`, assembly `xAPI4Unity.Tests`): an editor-only asmdef. Run them in the Unity Test Runner (EditMode tab).
  The tests only show up if `com.rwth.unity.omilaxr.xapi4unity` is listed under `"testables"` in `OmiLAXR_Dev/Packages/manifest.json`. It currently is not.
- **CLI test run.** Batch mode fails while the editor has the project open. In that case, run against an APFS copy (`cp -c -R OmiLAXR_Dev <scratch>/Dev`).

  ```bash
  /Applications/Unity/Hub/Editor/6000.3.13f1/Unity.app/Contents/MacOS/Unity -batchmode \
    -projectPath OmiLAXR_Dev -runTests -testPlatform EditMode \
    -assemblyNames xAPI4Unity.Tests -testResults results.xml
  # single test: replace -assemblyNames … with -testFilter xAPI4Unity.Tests.<Class>.<Method>
  ```

## Architecture

The whole package is the editor assembly `xAPI4Unity` (`includePlatforms: Editor`). Flow:

1. **Source.** `FetcherWindow` (`xAPI4Unity / Setup xAPI definitions`) clones the `xapi` registry with the git CLI (`GitHelper`, `ProcessHelper`) or points to a local folder with a `definitions/` root. Settings are in `Editor/Settings`: `MainSettings`, `FetcherSettings` and `LocalSourceSettings`, with defaults in `DefaultValues`.
2. **Parse.** `Editor/Parser/Definitions` models the JSON (`xAPIDefinition`, verbs, activities, extensions, contexts). `Editor/Parser/IO` reads files and tracks file meta.
3. **Types.** Optional `{def_folder}.config.json` (`ConfigFile`, `Editor/Types`) types the extensions. It supports aliases (`str`, `vec3`, …), `?` nullables, ranges, tuples, regex patterns, variables, and any C# type from loaded assemblies.
4. **Generate.** `Parser/Generator.Generate` (internal) builds a syntax tree (`Parser/Code/SyntaxTree`) and serializes it to C# (`Parser/Code/CSharp`). `CoreFiles` emits the static registry core.
5. **Watch.** `DefinitionAssetPostProcessor` regenerates when JSON under `Assets/xapi` changes, if the watcher is enabled. `Startup` runs the initial checks.

Other tools: `XapiDefinitionsEditorWindow` edits definitions, `XapiMergeWindow`/`MergeReport` merge upstream patches into local definitions, and `UnityPackageExporter` exports `xapi`/`xAPI.Registry` as a `.unitypackage`.
