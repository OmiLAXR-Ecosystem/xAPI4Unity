# xAPI4Unity

## Getting started
1. Install xAPI4Unity package (see below).
2. Choose one of the three options to in `xAPI4Unity > Open Fetcher Window (Settings)` in Unity menu to get xAPI Registry folder copy in your project.
3. Simply edit your JSON files in `Asset/xapi`. Each time you safe a JSON file in that location, the transformation is triggered (if watcher is set to `Yes`). Of course, you can use `Update xAPI.Registry` button whenever you wish. But this button is needed in case you turned off the watcher.
4. On problems or if you disabled the watcher, you can use `xAPI4Unity > Update xAPI.Registry (JSON => C#)` in Unity menu to trigger transformation manually.
5. In `xAPI4Unity / Open registry page` you can open `https://xapi.elearn.rwth-aachen.de`.

## Install package
### Adding scoped registry by using manifest.json (quick way)
1. Go to you project root and then open `Packages/manifest.json`.
2. Ensure following entries in your file: `"scopedRegistries": [
   {
   "name": "npmjs",
   "url": "http://registry.npmjs.com/",
   "scopes": [
   "com.rwth.unity.omilaxr.xapi4unity"
   ]
   }]`.
3. If you already have this registry, just add the scope `com.rwth.unity.omilaxr.xapi4unity`.
4. Go to the Unity Package Manager to `My registries` and install the package `com.rwth.unity.omilaxr.xapi4unity`.

## Install by using scoped registry
1. Ensure in "Project settings" > "Package Manager" that you have the scoped registry with following settings:
   - Name: npmjs
   - URL: http://registry.npmjs.com
   - Scope(s): `com.rwth.unity.omilaxr.xapi4unity`
2. Go to Package Manager.
3. Click on the (+) button.
4. Select 'Add package by name'.
5. Place in 'Name' field: `com.rwth.unity.omilaxr.xapi4unity`.

## (Alternative) Install by using Git url 
1. Go to Package Manager.
2. Click on the (+) button.
3. Select 'Add package from git URL'.
4. Paste `https://gitlab.com/learntech-rwth/omilaxr-ecosystem/xapi-4-unity.git` and confirm.

### Setup JSON Definitions Location
For transformation you need a source for the xAPI Registry definitions. You have two options: `GitLab` or `Local`.
In case of GitLab you will again, fetch data from published GitLab repository.
By choosing Local, you have to define the path for the clone of this repository. The selected path shall have `package.json` and `definitions` folder in root.
If you choose a path (e.g. `./Assets/xapi`) and click on `Clone repository`, the tool will automatically create this kind of source by using git clone. To do it, git cli must be installed.

### Fetcher configuration
The only option you need to do here, is to select a path where you want to place your generated C# files of the xAPI Registry. You can place it for example at `./Assets`.

### JSON Watcher 
In case you are using the fetcher type `Unity` it will transform the JSON files to C# directly inside of Unity Editor. This can be done by using the transform button or the watcher (turned on by default and can be disabled).