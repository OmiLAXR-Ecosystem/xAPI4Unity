/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Builds the static core files of a registry and its assembly definition.
    /// Pure functions: the templates come from an <see cref="ICoreTemplateSource"/>.
    /// </summary>
    internal static class CoreFiles
    {
        /// <summary>
        /// Names of the core files. Each has a template <c>&lt;name&gt;.tmpl</c>.
        /// </summary>
        internal static readonly string[] Names =
        {
            "xAPI_Activities.cs", "xAPI_Activity.cs", "xAPI_Actor.cs", "xAPI_Body.cs",
            "xAPI_Context.cs", "xAPI_Definition.cs", "xAPI_Extension.cs", "xAPI_Extensions.cs",
            "xAPI_Verb.cs", "xAPI_Verbs.cs"
        };

        /// <summary>
        /// Namespace the templates are written in. It is replaced by the target namespace.
        /// </summary>
        internal const string TemplateNamespace = "xAPI.Registry";

        /// <summary>
        /// Creates the core files with the target namespace and LF line breaks, even if the
        /// templates were checked out with CRLF.
        /// </summary>
        /// <param name="namespace">The namespace that replaces <see cref="TemplateNamespace"/>.</param>
        /// <param name="templates">Source of the template texts.</param>
        public static IEnumerable<FileMeta> Create(string @namespace, ICoreTemplateSource templates)
        {
            if (templates == null) throw new ArgumentNullException(nameof(templates));
            return Names
                .Select(name => new FileMeta(name, null, Code.CodeGenerator.NormalizeLineEndings(templates.Read(name)).Replace(TemplateNamespace, @namespace)))
                .ToList();
        }

        /// <summary>
        /// Creates the content of the registry's Assembly Definition (.asmdef) file.
        /// </summary>
        /// <param name="namespace">Name and root namespace of the assembly.</param>
        /// <param name="references">Assembly references, may be null.</param>
        public static string CreateAsmDef(string @namespace, IEnumerable<string> references) =>
            AsmDefFile(@namespace, @namespace, (references ?? Array.Empty<string>()).ToArray());

        /// <summary>
        /// Builds the JSON of an .asmdef file with the given name, root namespace and references.
        /// </summary>
        private static string AsmDefFile(string name, string rootNamespace, string[] references) =>
            "{\n  \"name\": \""+ name +"\",\n" +
                          "  \"rootNamespace\": \""+ rootNamespace + "\",\n" +
                          "  \"references\": [\n"+ string.Join(",\n", references.Select(r => "\""+ r + "\"")) +"],\n" +
                          "  \"includePlatforms\": [],\n" +
                          "  \"excludePlatforms\": [],\n" +
                          "  \"allowUnsafeCode\": false,\n" +
                          "  \"overrideReferences\": false,\n" +
                          "  \"precompiledReferences\": [],\n" +
                          "  \"autoReferenced\": true,\n" +
                          "  \"defineConstraints\": [],\n" +
                          "  \"versionDefines\": [],\n" +
                          "  \"noEngineReferences\": false\n}"
            ;

        /// <summary>
        /// Content of the .asmdef.meta file. The guid is fixed, so references to the
        /// registry assembly survive a regeneration.
        /// </summary>
        internal const string AsmDefMeta = "fileFormatVersion: 2\nguid: 5b80f9e8c84ce594bbd1284cff4292dd\nAssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n\n";
    }
}
#endif
