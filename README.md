# xAPI4Unity

## Getting started (new - recommended)
1. Install xAPI4Unity package (see below).
2. Make sure you have a local copy of https://gitlab.com/learntech-rwth/xapi repository as `Assets/xapi` folder. You can create the copy on your own or go to `xAPI4Unity > Settings` in Unity menu and click on `Clone repository` button. We personally recommend to fork the repository and clone your fork.
3. (Optional) If you wish another `xapi` folder location, then go the the top bar of Unity and click `xAPI4Unity > Settings` and set the new location.
4. Use the tool by simply editing your JSON files in `Asset/xapi`. Each time you safe a JSON file in that location, the transformation is triggered (if watcher is set to `Yes`). Of course, you can use `Update xAPI.Registry` button whenever you wish. But this button is needed in case you turned off the watcher.

## Install by using scoped registry (recommended)
1. Ensure in "Project settings" > "Package Manager" that you have the scoped registry with following settings:
   - Name: npmjs
   - URL: http://registry.npmjs.com
   - Scope(s): `com.rwth.unity.omilaxr.xapi4unity`
2. Go to Package Manager.
3. Click on the (+) button.
4. Select 'Add package by name'.
5. Place in 'Name' field: `com.rwth.unity.omilaxr.xapi4unity`.

### Adding scoped registry by using manifest.json (also recommended - quick way)
1. Alternatively, instead of adding the scoped registry inside Unity editor you can do it by using `manifest.json` file.
2. Go to you project root and then open `Packages/manifest.json`.
3. Ensure following entries in your file: `"scopedRegistries": [
   {
   "name": "npmjs",
   "url": "http://registry.npmjs.com/",
   "scopes": [
   "com.rwth.unity.omilaxr.xapi4unity"
   ]
   }]`.
4. By the way, you can also add here this package by adding `"com.rwth.unity.omilaxr.xapi4unity": "2.0.0"` to the dependencies (attention you can change the version).

## Install by using Git url
1. Go to Package Manager.
2. Click on the (+) button.
3. Select 'Add package from git URL'.
4. Paste `https://gitlab.com/learntech-rwth/omilaxr-ecosystem/xapi-4-unity.git` and confirm.

### Select your tool (obsolete)
Here you can decide between `Unity`, `Web service` and `Command Line`. The web service will use the deployed xAPI registry from GitLab.
For developers we strongly recommend to choose `Unity`. 
In case of Command line, you have to download the CLI Tool by pressing `Download Executable`.

### Setup JSON Definitions Location
For transformation you need a source for the xAPI Registry definitions. You have two options: `GitLab` or `Local`.
In case of GitLab you will again, fetch data from published GitLab repository.
By choosing Local, you have to define the path for the clone of this repository. The selected path shall have `package.json` and `definitions` folder in root.
If you choose a path (e.g. `./Assets/xapi`) and click on `Clone repository`, the tool will automatically create this kind of source by using git clone. To do it, git cli must be installed.

### Fetcher configuration
The only option you need to do here, is to select a path where you want to place your generated C# files of the xAPI Registry. You can place it for example at `./Assets`.

### JSON Watcher 
In case you are using the fetcher type `Unity` it will transform the JSON files to C# directly inside of Unity Editor. This can be done by using the transform button or the watcher (turned on by default and can be disabled).

### Check your settings
In case of using `Unity` fetcher type, everything is pre-configured by default.

Here you can review your setup. For Unity, it is strongly recommended to generated .asmdef file.
The `version` field is only important if you want to flag your generated files with a specific version. Otherwise you can keep default `1.0.0`.

(Obselete)
If you want to create an own shell or bash script for generating the files, you can click `Copy to clipboard`. This way the parameters for the CLI will be copied into your clipboard.
These parameters can be passed to `xAPI_Definitions_Fetcher.CLI.exe`. That's overall what this Unity editor window is doing as well.

### Do the transformation
Finally, you can press on `Update xAPI.Registry (JSON => C#)` to generate the C# files.

If you are using fetcher type `Unity` and the watcher is enabled, you can simply edit JSON files inside your Project and it will transform them automatically when you save the file.

If you are doing changes at definitions in `./Assets/xapi` you have to update the C# files.
This can be done by clicking this button again, using CLI command or clicking inside of Unity on `OmiLAXR > xAPI > Quick transform JSON => C#`.