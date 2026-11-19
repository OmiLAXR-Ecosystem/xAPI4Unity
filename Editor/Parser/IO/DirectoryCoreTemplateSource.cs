/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;

namespace xAPI4Unity.Editor.Parser.IO
{
    /// <summary>
    /// Reads core templates (<c>&lt;file name&gt;.tmpl</c>) from a folder.
    /// </summary>
    internal sealed class DirectoryCoreTemplateSource : ICoreTemplateSource
    {
        /// <summary>
        /// Templates of this package, relative to the Unity project.
        /// </summary>
        private const string PackageTemplatePath = "Packages/com.rwth.unity.omilaxr.xapi4unity/Editor/Assets/Core";

        /// <summary>
        /// Creates a source that reads the templates from a folder.
        /// </summary>
        /// <param name="directory">Folder that holds the <c>.tmpl</c> files.</param>
        public DirectoryCoreTemplateSource(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("The template folder must not be empty.", nameof(directory));
            Folder = directory;
        }

        /// <summary>
        /// Folder that holds the <c>.tmpl</c> files.
        /// </summary>
        public string Folder { get; }

        /// <summary>
        /// The templates shipped in this package (<c>Editor/Assets/Core</c>).
        /// </summary>
        public static DirectoryCoreTemplateSource ForPackage() =>
            new DirectoryCoreTemplateSource(Path.GetFullPath(PackageTemplatePath));

        /// <inheritdoc/>
        /// <exception cref="FileNotFoundException">The folder has no template for this file.</exception>
        public string Read(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName + ".tmpl"));
    }
}
#endif
