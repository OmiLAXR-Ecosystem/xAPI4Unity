# Changelog

## [2.2.3] - 2025-10-13

### Added
- Unterstützung für Arrays als Werte in `xAPI.Extensions`.

### Changed
- Kompatibilität mit Unity 2020 hergestellt.

### Fixed
- Removed nullable reference annotations outside a `#nullable` context in `TypeSchema` and the `xAPI_Extensions` template, which caused warning CS8632 in the editor and in every generated registry.

---

## [2.2.2] - 2025-08-25

### Added
- Support for variables in `xapi.config.json` to allow reuse of patterns or type definitions across multiple entries.

### Changed
- Cached and reused pre-compiled regular expressions instead of instantiating identical patterns multiple times to improve performance.

---

## [2.2.1] - 2025-08-25

### Added
- Support for strict typing of xAPI extensions via `{def_folder}.config.json` placed alongside the definition folder.
- Support for custom types based on any available Unity C# assembly, including user-defined serializable types.
- Support for predefined type aliases such as `str` for `string`, `vec3` for `UnityEngine.Vector3`, and more.
- Support for nullable types using the `?` suffix (e.g., `float?`).
- Pattern matching using regular expressions (e.g., for ISO 8601 duration strings like `blinkDuration`).
- Range validation for numeric types using syntax like `int[0, 3]` or `float(0, 5]`.
- Support for tuples (e.g., `(vec3, vec3, vec3)` for a triangle) and jagged arrays (e.g., `int[][]`).
- Transformation configuration option `ignoredContexts` to exclude specific contexts from xAPI processing.

### Changed
- `fixationDuration` and `blinkDuration` are now mapped to `OmiLAXR.Types.Duration` and validated using ISO 8601 regex patterns.
- If no explicit type is defined for an extension path, it now defaults to `object`.

### Fixed
- Improved general user experience (UX).
- Added code documentation, citation metadata, and structural improvements.

---

## [2.2.0] - 2025-06-06

### Added
- The xAPI Definitions Editor window now opens automatically at startup if no xAPI Registry is set.
- Export your xAPI Registry as a `.unitypackage` via the new menu entry: `xAPI4Unity / Export .unitypackage`.
- Added “Create folder” option in the xAPI Definitions Editor.
- Unsaved files in the xAPI Definitions Editor are now marked with an asterisk `*`.

### Changed
- Improved UI for better usability.
- Removed obsolete warnings.
- Resolved minor compatibility issues.

### Fixed
- Various small fixes and stability improvements.

---

## [2.1.10] - 2025-05-22

### Changed
- Increased the default height of the **xAPI Definitions** window.
- Improved compatibility with older Unity versions.
- Triggered QuickFetch automatically when clicking "Save" in the xAPI Definitions Editor.

### Fixed
- Fixed issues with detection of the default `Assets` folder.
- Removed use of a shared `SettingsKey` that previously caused multiple projects to share the same settings folder.

---

## [2.1.8 - 2.1.9] - 2025-05-21

### Added
- Introduced the **"Edit xAPI Definitions"** window.
- Automatically detects the presence of `xAPI.Registry` and sets/unsets the `XAPI_REGISTRY_EXISTS` scripting define symbol accordingly.

### Changed
- Improved the UI/UX of the editor tooling.
- Invalid xAPI registry extension files are now caught and ignored without breaking the system.

### Fixed
- Restored compatibility with Unity 2020.

---

## [2.1.7] - 2025-01-17

### Changed
- Settings now save automatically.
- Removed the "Save" button from the settings UI.
- Option 1 now automatically sets the path to the `Assets` folder in the project.

---

## [2.1.6] - 2024-11-10

### Added
- Added Option 1: download and extract xAPI Registry from GitLab directly into the Assets folder.
- Ensured compatibility with Unity 2020, 2021, 2022, and Unity 6.

### Changed
- Renamed existing Options: previous Option 1 is now Option 2, previous Option 2 is now Option 3.
- Updated README documentation.
- Renamed menu items.
- Performed smaller internal code optimizations.

### Fixed
- No bug fixes in this release.

---

## [2.1.5] - 2024-11-08

### Changed
- Compatible with Unity 6.

---

## [2.1.4] - 2024-10-25

### Added
- Error message in the editor window when the local source has no `definitions` folder.

### Removed
- Dependency on `com.unity.editorcoroutines`.

### Changed
- Raised the `com.unity.nuget.newtonsoft-json` dependency to 3.1.1.

---

## [2.1.3] - 2024-10-11

### Changed
- Improved the generated string output of `xAPI_Extensions`.
- Removed unused code (HTTP helper, tree view).

---

## [2.1.2] - 2024-08-16

### Fixed
- A missing namespace in the generated code.

---

## [2.1.1] - 2024-08-15

### Added
- Methods to manage entries in the generated `xAPI_Definition` and `xAPI_Extensions` classes.

---

## [2.1.0] - 2024-08-15

### Changed
- Updated the editor window UI.

### Removed
- Web service and CLI support, and the GitLab view. Definitions come from a local source or a cloned repository.

---

## [2.0.6] - 2024-08-15

### Fixed
- Context names that start with a character other than a letter are now valid.

---

## [2.0.5] - 2024-08-15

### Added
- Generation of the `xAPI_Contexts.cs` file.
- Detection of unsaved settings in the editor window.
- **Clone as submodule** and **Open folder** buttons for the local source.

### Fixed
- The "Clone repository" button on macOS.

---

## [2.0.2] - 2024-08-14

### Changed
- Improved the editor window experience.

---

## [2.0.1] - 2024-02-26

### Added
- The registry is regenerated when a definition JSON file changes, including sub definitions.

### Changed
- Generator classes are `internal` and wrapped in `#if UNITY_EDITOR`.
- Updated the fetcher window and README.

### Fixed
- A button in the editor window.

---

## [2.0.0] - 2024-02-24

### Added
- Parser and code generator that turn the xAPI JSON definitions into C# (`xAPI.Registry`).

---

## [1.0.4] - 2024-02-21

### Changed
- Renamed EduXR to OmiLAXR.

### Fixed
- Build errors caused by editor references. Editor code is wrapped in `#if UNITY_EDITOR`.

---

## [1.0.3] - 2023-04-23

### Changed
- Settings are saved in the project instead of the editor.
- Changed the namespace.
- The settings window asks to save before it closes and has a scroll view.
- Improved the fetcher service and updated its API base URL.

### Fixed
- A crash when fetching from GitLab.

---

## [1.0.2] - 2023-04-14

### Added
- Browse folder button.

### Changed
- The CLI is downloaded on demand instead of being part of the package.
- Generated files are cleared before they are created again, and get a `.meta` file.
- Improved the editor UX.

---

## [1.0.1] - 2023-04-11

### Changed
- Updated the bundled CLI.

---

## [1.0.0] - 2023-03-30

First release of xAPI4Unity.

### Added
- Editor window that downloads the xAPI registry definitions from GitLab or a local source and generates the registry with the CLI.
- Settings that are saved between sessions.
