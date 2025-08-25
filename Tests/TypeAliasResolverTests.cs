#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using xAPI4Unity.Editor.Types;
using UnityEngine;
using xAPI4Unity.Editor;

namespace xAPI4Unity.Tests
{
    [TestFixture]
    public class TypeSystemTests
    {
        // -----------------------------
        // Helpers
        // -----------------------------
        private static Type VTDef(int arity)
        {
            switch (arity)
            {
                case 1: return typeof(ValueTuple<>);
                case 2: return typeof(ValueTuple<,>);
                case 3: return typeof(ValueTuple<,,>);
                case 4: return typeof(ValueTuple<,,,>);
                case 5: return typeof(ValueTuple<,,,,>);
                case 6: return typeof(ValueTuple<,,,,,>);
                case 7: return typeof(ValueTuple<,,,,,,>);
                case 8: return typeof(ValueTuple<,,,,,,,>);
                default: throw new NotSupportedException();
            }
        }

        public static readonly TypeNameResolver TypeNameResolver = new TypeNameResolver(new Aliases());

        private static Type ExpectedTuple(params Type[] elements)
        {
            if (elements.Length <= 7)
                return VTDef(elements.Length).MakeGenericType(elements);

            // Verschachtelung: (T1..T7, ValueTuple(Rest...))
            var head = elements.Take(7).ToArray();
            var tail = elements.Skip(7).ToArray();
            var nested = ExpectedTuple(tail);
            var args = head.Concat(new[] { nested }).ToArray();
            return VTDef(8).MakeGenericType(args);
        }

        private static ResolvedType ResolveExpr(string expr)
        {
            var e = TypeExpression.Parse(expr);
            return e.Resolve(TypeNameResolver);
        }

        private static void AssertOkExpr(string expr, Type expected)
        {
            var t = ResolveExpr(expr);
            Assert.That(t.Type, Is.Not.Null, $"Erwartete erfolgreiche Auflösung für '{expr}'.");
            Assert.That(t.Type, Is.EqualTo(expected));
        }

        private static void AssertNullExpr(string expr)
        {
            var t = ResolveExpr(expr);
            Assert.That(t, Is.Null, $"Erwartete fehlgeschlagene Auflösung für '{expr}'.");
        }

        // -----------------------------
        // TypeNameResolver – Aliases & FQN
        // -----------------------------
        [Test]
        public void Resolver_Resolves_Aliases_CaseInsensitive()
        {
            Assert.That(TypeNameResolver.ResolveType("int").Type, Is.EqualTo(typeof(int)));
            Assert.That(TypeNameResolver.ResolveType("InT").Type, Is.EqualTo(typeof(int)));
            Assert.That(TypeNameResolver.ResolveType("FLOAT").Type, Is.EqualTo(typeof(float)));
            Assert.That(TypeNameResolver.ResolveType("boolean").Type, Is.EqualTo(typeof(bool)));
            Assert.That(TypeNameResolver.ResolveType("u64").Type, Is.EqualTo(typeof(ulong)));
        }

        [Test]
        public void Resolver_Resolves_FullyQualified()
        {
            Assert.That(TypeNameResolver.ResolveType("System.Int32").Type, Is.EqualTo(typeof(int)));
            Assert.That(TypeNameResolver.ResolveType("System.Single").Type, Is.EqualTo(typeof(float)));
        }

        [Test]
        public void Resolver_ReturnsNull_For_Unknown()
        {
            Assert.That(TypeNameResolver.ResolveType("no_such_type_abcdef"), Is.Null);
        }

        // -----------------------------
        // TypeExpression – Primitive, Nullable, Arrays
        // -----------------------------
        [Test]
        public void Expression_Parses_Primitive()
        {
            AssertOkExpr("int", typeof(int));
            AssertOkExpr("string", typeof(string));
            AssertOkExpr("bool", typeof(bool));
        }

        [Test]
        public void Expression_Parses_Regex()
        {
            const string regex = "/^[A-Z]{3}$/";
            AssertOkExpr(regex, typeof(string));
            var resolved = TypeNameResolver.ResolveType(regex);
            Assert.AreEqual(resolved.Definition, regex);
            Assert.IsTrue(resolved.IsRegexExpr);
            var r = new Regex(regex.Trim('/'));
            Assert.IsTrue(r.IsMatch("ABC"));
            Assert.IsFalse(r.IsMatch("ABCXXXX"));
        }

        [Test]
        public void Expression_Parses_Nullable_Value()
        {
            var expected = typeof(Nullable<>).MakeGenericType(typeof(int));
            AssertOkExpr("int?", expected);
        }

        [Test]
        public void Expression_Parses_Arrays_Jagged_And_MultiDimensional()
        {
            AssertOkExpr("float[]", typeof(float).MakeArrayType());           // float[]
            AssertOkExpr("float[,,]", typeof(float).MakeArrayType(3));       // float[,,]

            // int[][, ,]  (erst jagged → int[], dann 3D → (int[])[,,])
            var expected = typeof(int).MakeArrayType().MakeArrayType(3);
            AssertOkExpr("int[][,,]", expected);
        }

        // -----------------------------
        // TypeExpression – Tupel
        // -----------------------------
        [Test]
        public void Tuple_Single_Element()
        {
            AssertOkExpr("(int)", ExpectedTuple(typeof(int)));
        }

        [Test]
        public void Tuple_Three_Floats()
        {
            var expected = ExpectedTuple(typeof(float), typeof(float), typeof(float));
            AssertOkExpr("(float, float, float)", expected);
        }

        [Test]
        public void Tuple_Seven_Elements()
        {
            var elems = Enumerable.Repeat(typeof(int), 7).ToArray();
            AssertOkExpr("(int, int, int, int, int, int, int)", ExpectedTuple(elems));
        }

        [Test]
        public void Tuple_Eight_Elements_Nested()
        {
            var elems = Enumerable.Repeat(typeof(int), 8).ToArray();
            AssertOkExpr("(int, int, int, int, int, int, int, int)", ExpectedTuple(elems));
        }

        [Test]
        public void Tuple_Nine_Elements_Nested()
        {
            var elems = Enumerable.Repeat(typeof(int), 9).ToArray();
            AssertOkExpr("(int, int, int, int, int, int, int, int, int)", ExpectedTuple(elems));
        }

        [Test]
        public void Tuple_Nested_Tuple_Inside()
        {
            var inner = ExpectedTuple(typeof(float), typeof(float));
            var expected = ExpectedTuple(typeof(int), inner);
            AssertOkExpr("(int, (float, float))", expected);
        }

        [Test]
        public void Tuple_With_Whitespace_Variants()
        {
            var expected = ExpectedTuple(typeof(float), typeof(float), typeof(float));
            AssertOkExpr("(  float ,  float,float   )", expected);
        }

        [Test]
        public void Tuple_Array_And_Nullable_Combinations()
        {
            // (int,float)[] → Array von ValueTuple<int,float>
            var expectedArray = ExpectedTuple(typeof(int), typeof(float)).MakeArrayType();
            AssertOkExpr("(int, float)[]", expectedArray);

            // (int,(float,float))? → Nullable<ValueTuple<int,ValueTuple<float,float>>>
            var inner = ExpectedTuple(typeof(float), typeof(float));
            var outer = ExpectedTuple(typeof(int), inner);
            var expectedNullable = typeof(Nullable<>).MakeGenericType(outer);
            AssertOkExpr("(int,(float,float))?", expectedNullable);
        }

        [Test]
        public void Tuple_Empty_Throws()
        {
            // Leeres Tupel "()" führt in Resolve zu ArgumentException (ValueTuple benötigt >=1 Element)
            Assert.Throws<ArgumentException>(() => ResolveExpr("()"));
        }

        [Test]
        public void Tuple_Unbalanced_Parens_ReturnsNull_On_Resolve()
        {
            // Kein gültiges Tupel (fehlende ')') → wird als CoreName behandelt, Auflösung schlägt fehl → null
            AssertNullExpr("(int, string");
        }

        // -----------------------------
        // TypeExpressionSet – Unionen
        // -----------------------------
        [Test]
        public void Union_Int_Or_String()
        {
            var set = TypeExpressionSet.Parse("int | string");
            var types = set.ResolveAll(TypeNameResolver).Select(t => t.Type).ToArray();
            CollectionAssert.Contains(types, typeof(int));
            CollectionAssert.Contains(types, typeof(string));
        }

        [Test]
        public void Union_Tuple_Or_UnityVector()
        {
            var set = TypeExpressionSet.Parse("(int,float) | vec3");
            var types = set.ResolveAll(TypeNameResolver).Select(t => t.Type).ToArray();

            var tuple = ExpectedTuple(typeof(int), typeof(float));
            CollectionAssert.Contains(types, tuple);
            CollectionAssert.Contains(types, typeof(Vector3));
        }

        [Test]
        public void Union_Empty_Part_Throws()
        {
            Assert.Throws<ArgumentException>(() => TypeExpressionSet.Parse("   "));
        }

        // -----------------------------
        // TypeSchema – Parsing & Validation
        // -----------------------------
        [Test]
        public void Schema_Parse_And_GetTypesFromKey()
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "vr/pos3", "(float,float,float)" },
                { "either-int-or-string", "int | string" },
                { "tuple-array", "(int,float)[]" }
            };

            var schema = TypeSchema.Parse(dict, validateResolution: true, TypeNameResolver);

            var posTypes = schema.GetTypesFromKey("vr/pos3");
            Assert.That(posTypes.Length, Is.EqualTo(1));
            Assert.That(posTypes[0].Type, Is.EqualTo(ExpectedTuple(typeof(float), typeof(float), typeof(float))));

            var either = schema.GetTypesFromKey("either-int-or-string").Select(t => t.Type).ToArray();
            CollectionAssert.Contains(either, typeof(int));
            CollectionAssert.Contains(either, typeof(string));

            var tupleArray = schema.GetTypesFromKey("tuple-array");
            Assert.That(tupleArray.Length, Is.EqualTo(1));
            Assert.That(tupleArray[0].Type, Is.EqualTo(ExpectedTuple(typeof(int), typeof(float)).MakeArrayType()));
        }

        [Test]
        public void Schema_TryParse_Collects_Errors()
        {
            var input = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ok1", "int" },
                { "bad1", "unknown_type_xyz" },
                { "ok2", "(float,float)" },
                { "bad2", "()" } // leeres Tupel → Resolve wirft in Schema.Validate
            };

            Dictionary<string, TypeExpressionSet> parsed;
            List<string> errors;

            var ok = TypeSchema.TryParse(input, out parsed, out errors, validateResolution: true);

            Assert.That(ok, Is.False);
            Assert.That(errors, Is.Not.Empty);
            Assert.That(parsed.ContainsKey("ok1"), Is.True);
            Assert.That(parsed.ContainsKey("ok2"), Is.True);
        }

        // -----------------------------
        // Unity-spezifische Aliases
        // -----------------------------
        [Test]
        public void Unity_Aliases_Vector_And_Color()
        {
            AssertOkExpr("vec3", typeof(Vector3));
            AssertOkExpr("vector2", typeof(Vector2));
            AssertOkExpr("color32", typeof(Color32));
        }
        
        [Test]
        public void Regex_Parses_And_Matches_Valid_Values()
        {
            var expr = "/^\\d{4}$/"; // exakt 4 Ziffern
            var resolved = ResolveExpr(expr);
    
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.IsRegexExpr, Is.True);
            Assert.That(resolved.Definition, Is.EqualTo(expr));
            Assert.That(resolved.Type, Is.EqualTo(typeof(string)));

            var regex = new Regex(expr.Trim('/'));
            Assert.That(regex.IsMatch("2025"), Is.True);
            Assert.That(regex.IsMatch("abc"), Is.False);
            Assert.That(regex.IsMatch("123"), Is.False);
        }
        
        [Test]
        public void Range_Parses_InclusiveIntRange()
        {
            var expr = "int[1,10]";
            var resolved = ResolveExpr(expr);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved, Is.InstanceOf<ResolvedRange>());
    
            var range = (ResolvedRange)resolved;
            Assert.That(range.Type, Is.EqualTo(typeof(int)));
            Assert.That(range.MinInclusive, Is.True);
            Assert.That(range.MaxInclusive, Is.True);
            Assert.That(range.RawMin, Is.EqualTo("1"));
            Assert.That(range.RawMax, Is.EqualTo("10"));
        }

        [Test]
        public void Range_Parses_ExclusiveFloatRange()
        {
            var expr = "float(0.0, 1.0)";
            var resolved = ResolveExpr(expr);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved, Is.InstanceOf<ResolvedRange>());
    
            var range = (ResolvedRange)resolved;
            Assert.That(range.Type, Is.EqualTo(typeof(float)));
            Assert.That(range.MinInclusive, Is.False);
            Assert.That(range.MaxInclusive, Is.False);
            Assert.That(range.RawMin, Is.EqualTo("0.0"));
            Assert.That(range.RawMax, Is.EqualTo("1.0"));
        }
    }
}
#endif
