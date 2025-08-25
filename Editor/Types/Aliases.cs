using System;
using System.Collections.Generic;
using UnityEngine;

namespace xAPI4Unity.Editor.Types
{
    /// <summary>
    /// Represents a collection of default type aliases for primitive and Unity-specific types.
    /// </summary>
    public class Aliases : Dictionary<string, Type>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Aliases"/> class with default mappings.
        /// </summary>
        public Aliases() : base()
        {
            // Primitive types
            Add("string", typeof(string)); Add("str", typeof(string)); Add("char", typeof(char));
            Add("bool", typeof(bool)); Add("boolean", typeof(bool));

            Add("byte", typeof(byte)); Add("uint8", typeof(byte)); Add("u8", typeof(byte));
            Add("sbyte", typeof(sbyte)); Add("int8", typeof(sbyte)); Add("i8", typeof(sbyte));

            Add("short", typeof(short)); Add("int16", typeof(short)); Add("i16", typeof(short));
            Add("ushort", typeof(ushort)); Add("uint16", typeof(ushort)); Add("u16", typeof(ushort));

            Add("int", typeof(int)); Add("int32", typeof(int)); Add("i32", typeof(int)); Add("integer", typeof(int));
            Add("uint", typeof(uint)); Add("uint32", typeof(uint)); Add("u32", typeof(uint));

            Add("long", typeof(long)); Add("int64", typeof(long)); Add("i64", typeof(long));
            Add("ulong", typeof(ulong)); Add("uint64", typeof(ulong)); Add("u64", typeof(ulong));

            Add("single", typeof(float)); Add("float", typeof(float)); Add("real", typeof(float));
            Add("double", typeof(double)); Add("number", typeof(double));
            Add("decimal", typeof(decimal));

            Add("void", typeof(void));

            // Unity-specific types
            Add("vec2", typeof(Vector2)); Add("vector2", typeof(Vector2));
            Add("vec2int", typeof(Vector2Int)); Add("vector2int", typeof(Vector2Int)); Add("vec2i", typeof(Vector2Int));

            Add("vec3", typeof(Vector3)); Add("vector3", typeof(Vector3));
            Add("vec3i", typeof(Vector3Int)); Add("vec3int", typeof(Vector3Int)); Add("vector3int", typeof(Vector3Int));

            Add("vec4", typeof(Vector4)); Add("vector4", typeof(Vector4));

            Add("mat4", typeof(Matrix4x4)); Add("mat4x4", typeof(Matrix4x4)); Add("matrix4x4", typeof(Matrix4x4)); Add("matrix", typeof(Matrix4x4));

            Add("quat", typeof(Quaternion)); Add("quaternion", typeof(Quaternion)); Add("quatf", typeof(Quaternion));

            Add("rect", typeof(Rect)); Add("rectint", typeof(RectInt)); Add("recti", typeof(RectInt));

            Add("bounds", typeof(Bounds)); Add("boundsint", typeof(BoundsInt));

            Add("plane", typeof(Plane));

            Add("ray", typeof(Ray)); Add("ray2d", typeof(Ray2D));

            Add("color", typeof(Color)); Add("color32", typeof(Color32)); Add("gradient", typeof(Gradient));
        }
    }
}