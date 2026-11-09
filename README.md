# xAPI4Unity

xAPI4Unity is a Unity package designed to bridge xAPI functionalities into Unity projects conveniently. It enables seamless integration of xAPI JSON definitions and transforms them into C# scripts you can use in your applications. This tool provides support for xAPI registry handling, editor tools, and watcher-based transformations.

---

## Key Features
- Transform xAPI JSON definitions into C# scripts directly in Unity.
- Built-in JSON watcher to auto-reflect changes (optional).
- Fetch and manage xAPI definitions from local or Git repositories.
- Easy-to-use Unity editor interface for managing definitions.
- Fully compatible with Unity LTS versions.

---

## Tested Unity Versions
- [ ] Unity 2019.4.40f1 (LTS)
- [X] Unity 2020.3.48f1 (LTS)
- [X] Unity 2021.3.45f1 (LTS)
- [X] Unity 2022.3.52f1 (LTS)
- [ ] Unity 2023 (no LTS)
- [X] Unity 6 (LTS)

---

## Getting Started

### Step-by-Step Guide
1. **Install the xAPI4Unity package**: Follow the installation instructions below.
2. **Set up xAPI Registry**: Go to `Unity Menu > xAPI4Unity > Open Wizard`. The wizard opens by itself on editor start while the definitions or the registry are missing. In the "Source" step, choose how you want to fetch the xAPI registry:
   - Patch the definitions into `Assets/xapi`.
   - Download a zip and use the extracted directory.
   - Clone the Git repository (as a submodule if your project uses git).
3. **Edit and transform JSON files**: Use the `Assets/xapi` folder for your xAPI JSON files. Changes are automatically transformed into C# if "Regenerate on changes" is on. Alternatively, use `Update xAPI.Registry` in the wizard's "Generate" step for manual transformation.
4. **Manual Transformation**: Use the Unity menu `xAPI4Unity > Update xAPI.Registry (JSON => C#)` to trigger transformation manually if required.
5. **View Documentation**: Open `https://omilaxr-ecosystem.github.io/xapi` via `xAPI4Unity > Open registry page` in the Unity menu for more information.

---

## Installation

### Option 1: Add Scoped Registry via manifest.json
1. Open `Packages/manifest.json` in your Unity project root.
2. Add the following scoped registry configuration:
   ```json
   "scopedRegistries": [
       {
         "name": "npmjs",
         "url": "http://registry.npmjs.com/",
         "scopes": [
           "com.rwth.unity.omilaxr.xapi4unity"
         ]
       }
   ]
   ```
3. Add the `com.rwth.unity.omilaxr.xapi4unity` scope if another registry is already defined.
4. Open Unity's Package Manager, go to `My Registries`, and install `com.rwth.unity.omilaxr.xapi4unity`.

### Option 2: Using Scoped Registry from Project Settings
1. Navigate to `Project Settings > Package Manager`.
2. Add the following scoped registry:
   - **Name**: npmjs
   - **URL**: `http://registry.npmjs.com`
   - **Scope(s)**: `com.rwth.unity.omilaxr.xapi4unity`
3. Open Package Manager, select `Add package by name`, and input `com.rwth.unity.omilaxr.xapi4unity`.

### Option 3: Using Git URL
1. Go to Package Manager.
2. Select `Add package from git URL`.
3. Paste `https://gitlab.com/learntech-rwth/omilaxr-ecosystem/xapi-4-unity.git` and click `Add`.

---

## Setup Guide

### JSON Definitions Location
You need a source for xAPI Registry definitions for transformation:
- **Git Repository**: Fetch definitions directly from a published Git repository (default: [OmiLAXR-Ecosystem/xapi](https://github.com/OmiLAXR-Ecosystem/xapi)) (requires git CLI installed).
- **Local Source**: Select a local directory with the `definitions` folder at its root.

### Fetcher Configuration
- Configure where the generated C# files from xAPI definitions will be saved. For example, you can set this to `./Assets`.

### JSON Watcher
- When enabled, xAPI4Unity automatically detects changes in JSON files in the `Assets/xapi` folder and triggers transformation into C#. The watcher is enabled by default but can be disabled via settings.

---

## Developer Notes
### Contributing
Feel free to create pull requests or raise issues to help improve xAPI4Unity. Contributions are always welcome.

### License
This project is licensed under the AGPL-3.0-or-later. See the `LICENSE` file for details.

### Support
For further assistance, please visit [xAPI Registry Documentation](https://omilaxr-ecosystem.github.io/xapi) or raise an issue in the repository.