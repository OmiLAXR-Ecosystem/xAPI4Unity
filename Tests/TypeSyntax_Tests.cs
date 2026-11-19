/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Tests
{
    /// <summary>
    /// Tests that the type syntax written into generated code compiles, and that the type system works with an injected resolver.
    /// </summary>
    [TestFixture]
    public class TypeSyntax_Tests
    {
        /// <summary>
        /// Resolver with the built-in aliases.
        /// </summary>
        private static readonly TypeNameResolver Resolver = new TypeNameResolver(new Aliases());

        /// <summary>
        /// Resolves only the names it was given, and records every name it was asked for.
        /// </summary>
        private sealed class StubResolver : ITypeNameResolver
        {
            /// <summary>
            /// Types by name.
            /// </summary>
            private readonly Dictionary<string, Type> _types;
            /// <summary>
            /// Every name the resolver was asked for, in order.
            /// </summary>
            public readonly List<string> Requested = new List<string>();

            /// <summary>
            /// Creates a resolver for the given names.
            /// </summary>
            public StubResolver(Dictionary<string, Type> types) => _types = types;

            /// <summary>
            /// Records the name and returns its type, or null if it is unknown.
            /// </summary>
            public ResolvedType ResolveType(string name)
            {
                Requested.Add(name);
                return _types.TryGetValue(name, out var t) ? new ResolvedType(t, name) : null;
            }
        }

        /// <summary>
        /// Parses, resolves and returns the C# syntax of an expression.
        /// </summary>
        private static string Syntax(string expression) =>
            TypeExpression.Parse(expression).Resolve(Resolver).GetTypeSyntax();

        /// <summary>
        /// Arrays of tuples, nullable tuples, nullable arrays, multidimensional and nested arrays get syntax that compiles.
        /// </summary>
        [TestCase("(int,float)[]", "(System.Int32,System.Single)[]")]
        [TestCase("(int,(float,float))?", "(System.Int32,(System.Single,System.Single))?")]
        [TestCase("int?[]", "System.Int32?[]")]
        [TestCase("float[,]", "System.Single[,]")]
        [TestCase("int[][,,]", "System.Int32[][,,]")]
        [TestCase("vec3[]", "UnityEngine.Vector3[]")]
        public void TypeExpression_Builds_Compilable_Syntax(string expression, string expected)
        {
            // Act
            var syntax = Syntax(expression);

            // Assert
            Assert.AreEqual(expected, syntax);
        }

        /// <summary>
        /// An array of tuples has a ValueTuple array type.
        /// </summary>
        [Test]
        public void TypeExpression_Array_Of_Tuples_Has_A_ValueTuple_Array_Type()
        {
            // Act
            var resolved = TypeExpression.Parse("(int,float)[]").Resolve(Resolver);

            // Assert
            Assert.That(resolved, Is.InstanceOf<ResolvedArray>());
            Assert.AreEqual(typeof(ValueTuple<int, float>[]), resolved.Type);
        }

        /// <summary>
        /// A union of two different tuples keeps both.
        /// </summary>
        [Test]
        public void TypeExpressionSet_Keeps_Two_Different_Tuples()
        {
            // Arrange
            var set = TypeExpressionSet.Parse("(int,int) | (string,string)");

            // Act
            var syntaxes = set.ResolveAll(Resolver).Select(t => t.GetTypeSyntax()).ToArray();

            // Assert
            CollectionAssert.AreEquivalent(
                new[] { "(System.Int32,System.Int32)", "(System.String,System.String)" }, syntaxes);
        }

        /// <summary>
        /// Unknown types inside ?, a range or a tuple give null.
        /// </summary>
        [TestCase("nope?")]
        [TestCase("nope[1,2]")]
        [TestCase("(int,nope)")]
        public void TypeNameResolver_Returns_Null_For_Unknown_Inner_Types(string name)
        {
            // Act
            var resolved = Resolver.ResolveType(name);

            // Assert
            Assert.IsNull(resolved);
        }

        /// <summary>
        /// A range of an unknown type gives null.
        /// </summary>
        [Test]
        public void TypeExpression_Returns_Null_For_A_Range_Of_An_Unknown_Type()
        {
            // Act
            var resolved = TypeExpression.Parse("nope[1,10]").Resolve(Resolver);

            // Assert
            Assert.IsNull(resolved);
        }

        /// <summary>
        /// Aliases resolve in any case.
        /// </summary>
        [TestCase("Vector3", typeof(Vector3))]
        [TestCase("STRING", typeof(string))]
        [TestCase("Int", typeof(int))]
        public void Aliases_Ignore_Case(string name, Type expected)
        {
            // Act
            var resolved = Resolver.ResolveType(name);

            // Assert
            Assert.IsNotNull(resolved);
            Assert.AreEqual(expected, resolved.Type);
        }

        /// <summary>
        /// TypeExpression asks the injected resolver once per tuple element.
        /// </summary>
        [Test]
        public void TypeExpression_Asks_The_Injected_Resolver_For_Each_Tuple_Element()
        {
            // Arrange
            var stub = new StubResolver(new Dictionary<string, Type> { { "id", typeof(Guid) }, { "n", typeof(int) } });

            // Act
            var resolved = TypeExpression.Parse("(id, n)").Resolve(stub);

            // Assert
            CollectionAssert.AreEqual(new[] { "id", "n" }, stub.Requested);
            Assert.AreEqual("(System.Guid,System.Int32)", resolved.GetTypeSyntax());
        }

        /// <summary>
        /// TypeSchema uses the built-in aliases without a resolver.
        /// </summary>
        [Test]
        public void TypeSchema_Uses_The_Builtin_Aliases_Without_A_Resolver()
        {
            // Arrange
            var input = new Dictionary<string, string> { { "ctx/extensions/result/pos", "vec3" } };

            // Act
            var schema = TypeSchema.Parse(input);
            var types = schema.GetTypesFromKey("ctx/extensions/result/pos");

            // Assert
            Assert.AreEqual(typeof(Vector3), types.Single().Type);
        }
    }
}
#endif
