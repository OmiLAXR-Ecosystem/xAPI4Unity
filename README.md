# xAPI4Unity

## Getting started
1. Install xAPI4Unity package (e.g. as package, see [here](https://learntech-rwth.gitlab.io/omilaxr-ecosystem/omilaxr-documentation/getting-started/index.html))
2. Go the the top bar of Unity and click `OmiLAXR > xAPI > Open Fetcher Window`
3. Use the tool to transform xAPI Jsons to C# by following next steps here. These steps have to be done once. Later you can use quick options to fetch the definitions.

### Select your tool
Here you can decide between `Web service` and `Command Line`. The web service will use the deployed xAPI registry from GitLab.
For developers we strongly recommend to choose `Command Line`. 
In case of Command line, you have to download the CLI Tool by pressing `Download Executable`.

### Setup JSON Definitions Location
For transformation you need a source for the xAPI Registry definitions. You have two options: `GitLab` or `Local`.
In case of GitLab you will again, fetch data from published GitLab repository.
By choosing Local, you have to define the path for the clone of this repository. The selected path shall have `package.json` and `definitions` folder in root.
If you choose a path (e.g. `./Assets/xapi`) and click on `Clone repository`, the tool will automatically create this kind of source by using git clone. To do it, git cli must be installed.

### Fetcher configuration
The only option you need to do here, is to select a path where you want to place your generated C# files of the xAPI Registry. You can place it for example at `./Assets`.

### Check your settings
Here you can review your setup. For Unity, it is strongly recommended to generated .asmdef file.
The `version` field is only important if you want to flag your generated files with a specific version. Otherwise you can keep default `1.0.0`.

If you want to create an own shell or bash script for generating the files, you can click `Copy to clipboard`. This way the parameters for the CLI will be copied into your clipboard.
These parameters can be passed to `xAPI_Definitions_Fetcher.CLI.exe`. That's overall what this Unity editor window is doing as well.

### Do the transformation
Finally, you can press on `Transform JSON => C#` to generate the C# files.

If you are doing changes at definitions in `./Assets/xapi` you have to update the C# files.
This can be done by clicking this button again, using CLI command or clicking inside of Unity on `OmiLAXR > xAPI > Quick transform JSON => C#`.