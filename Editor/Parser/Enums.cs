#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace xAPI4Unity.Editor.Parser
{
    /// <summary>
    /// Access modifiers
    /// </summary>
    internal enum Modifier
    {
        Public,
        Private,
        Protected,
        Static,
        Virtual,
        ReadOnly,
        Sealed
    }

    /// <summary>
    /// Class with enum methods
    /// </summary>
    internal static class EnumMethods
    {
        /// <summary>
        /// Gets the string of a source key
        /// </summary>
        /// <param name="type">Source type</param>
        /// <returns>String of a source type</returns>
        public static string GetSourceKeyString(this SourceType type)
        {
            return type.GetName();
        }

        /// <summary>
        /// Gets a source type from a string
        /// </summary>
        /// <param name="str">Source type string</param>
        /// <returns>Corresponding source type</returns>
        public static SourceType? GetSourceKeyFromString(this string str)
        {
            foreach (SourceType val in Enum.GetValues(typeof(SourceType)))
            {
                if (val.GetName() == str || val.GetSourceKeyString() == str)
                {
                    return val;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the name of an enum
        /// </summary>
        /// <param name="enu">Enum value</param>
        /// <returns>Name of the enum value</returns>
        public static string GetName(this Enum enu)
        {
            return Enum.GetName(enu.GetType(), enu);
        }
    }
}
#endif