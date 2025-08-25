# Changelog

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

## [2.1.8 - 2.1.9] - 2025-06-06

### Added
- Introduced the **"Edit xAPI Definitions"** window.
- Automatic detection of `xAPI.Registry` and setting/unsetting the `XAPI_REGISTRY_EXISTS` scripting define symbol accordingly.

### Changed
- Improved UI/UX for the editor tools.
- Added error handling: invalid xAPI registry extension files are now caught and ignored.

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

