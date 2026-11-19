/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Text.RegularExpressions;

namespace xAPI4Unity.Editor.Releases
{
    /// <summary>
    /// A semantic version such as <c>2.1.0</c> or <c>2.1.0-beta.1</c>, parsed from a release tag like <c>v2.1.0</c>.
    /// </summary>
    internal readonly struct ReleaseVersion : IComparable<ReleaseVersion>, IEquatable<ReleaseVersion>
    {
        /// <summary>
        /// Optional <c>v</c>, two or three numbers, an optional pre-release after <c>-</c> and optional build metadata after <c>+</c>.
        /// </summary>
        private static readonly Regex Pattern = new Regex(
            @"^\s*[vV]?(\d+)\.(\d+)(?:\.(\d+))?(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?\s*$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Major version.
        /// </summary>
        public readonly int Major;
        /// <summary>
        /// Minor version.
        /// </summary>
        public readonly int Minor;
        /// <summary>
        /// Patch version, 0 if the tag has only two numbers.
        /// </summary>
        public readonly int Patch;
        /// <summary>
        /// Pre-release label such as <c>beta.1</c>, empty for a stable release.
        /// </summary>
        public readonly string PreRelease;

        /// <summary>
        /// Creates a version from its parts.
        /// </summary>
        public ReleaseVersion(int major, int minor, int patch, string preRelease = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = preRelease ?? "";
        }

        /// <summary>
        /// True if the version has a pre-release label.
        /// </summary>
        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

        /// <summary>
        /// Parses a version or tag such as <c>v2.1.0</c>, <c>2.1</c> or <c>2.1.0-beta.1</c>.
        /// </summary>
        /// <returns>False if the text is not a version.</returns>
        public static bool TryParse(string text, out ReleaseVersion version)
        {
            version = default;
            if (string.IsNullOrEmpty(text))
                return false;
            var match = Pattern.Match(text);
            if (!match.Success)
                return false;
            if (!int.TryParse(match.Groups[1].Value, out var major) || !int.TryParse(match.Groups[2].Value, out var minor))
                return false;
            var patch = 0;
            if (match.Groups[3].Success && !int.TryParse(match.Groups[3].Value, out patch))
                return false;
            version = new ReleaseVersion(major, minor, patch, match.Groups[4].Success ? match.Groups[4].Value : "");
            return true;
        }

        /// <summary>
        /// Orders by the numbers first. With equal numbers, a stable release is newer than a pre-release,
        /// and pre-releases are ordered by their dot-separated identifiers.
        /// </summary>
        public int CompareTo(ReleaseVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            if (!IsPreRelease || !other.IsPreRelease)
                return IsPreRelease == other.IsPreRelease ? 0 : IsPreRelease ? -1 : 1;
            return ComparePreRelease(PreRelease, other.PreRelease);
        }

        /// <summary>
        /// Compares two pre-release labels identifier by identifier: numbers numerically and below text, text ordinally.
        /// </summary>
        private static int ComparePreRelease(string a, string b)
        {
            var left = a.Split('.');
            var right = b.Split('.');
            for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
            {
                var leftIsNumber = int.TryParse(left[i], out var leftNumber);
                var rightIsNumber = int.TryParse(right[i], out var rightNumber);
                int result;
                if (leftIsNumber && rightIsNumber)
                    result = leftNumber.CompareTo(rightNumber);
                else if (leftIsNumber != rightIsNumber)
                    result = leftIsNumber ? -1 : 1;
                else
                    result = string.CompareOrdinal(left[i], right[i]);
                if (result != 0)
                    return result;
            }
            return left.Length.CompareTo(right.Length);
        }

        /// <inheritdoc />
        public bool Equals(ReleaseVersion other) => CompareTo(other) == 0;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ReleaseVersion other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Major;
                hash = hash * 397 ^ Minor;
                hash = hash * 397 ^ Patch;
                return hash * 397 ^ (PreRelease ?? "").GetHashCode();
            }
        }

        /// <summary>
        /// The version without <c>v</c>, such as <c>2.1.0</c> or <c>2.1.0-beta.1</c>.
        /// </summary>
        public override string ToString() => IsPreRelease ? $"{Major}.{Minor}.{Patch}-{PreRelease}" : $"{Major}.{Minor}.{Patch}";
    }
}
