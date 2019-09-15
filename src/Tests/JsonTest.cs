using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SchemaGenerator.Core.Extensions;
using SchemaGenerator.Json;
using System.Reflection;
using SchemaGenerator.Core.Utilities;
using Xunit;

namespace SchemaGenerator.Tests
{
    public class JsonTest
    {
        [Fact]
        public void BasicTest()
        {
            var expectedSchema =
                new JObject
                {
                    {
                        "types",
                        new JArray
                        {
                            new JObject
                            {
                                {"name", typeof(BasicTestA).GetDisplayName(true)},
                                {"baseType", typeof(object).GetDisplayName(true)},
                                {
                                    "properties",
                                    new JArray
                                    {
                                        new JObject
                                        {
                                            {"name", nameof(BasicTestA.A)},
                                            {"type", typeof(int).GetDisplayName(true)}
                                        }
                                    }
                                }
                            }
                        }
                    },
                    {"enums", new JArray()}
                }.ToString();

            var schemaGenerator =
                new JsonSchemaGenerator(
                    new[] {typeof(BasicTestA)},
                    assemblyName =>
                        assemblyName.Name ==
                        $"{nameof(SchemaGenerator)}.{nameof(Tests)}",
                    memberInfo =>
                    {
                        switch (memberInfo)
                        {
                            case FieldInfo fieldInfo:
                                return fieldInfo.IsPublic;
                            case PropertyInfo propertyInfo:
                                return propertyInfo.GetMethod?.IsPublic == true && propertyInfo.SetMethod != null;
                            default:
                                throw new UnexpectedException(nameof(MemberInfo), memberInfo.GetType().Name);
                        }
                    });

            var schema = schemaGenerator.Generate();

            schema.Should().Be(expectedSchema);
        }

        [Fact]
        public void InheritanceTest()
        {
            var expectedSerializableTypes =
                new[]
                {
                    typeof(InheritanceTestA),
                    typeof(InheritanceTestB1),
                    typeof(InheritanceTestC1)
                };

            var schemaGenerator =
                new JsonSchemaGenerator(
                    new[] {typeof(InheritanceTestB1)},
                    assemblyName =>
                        assemblyName.Name ==
                        $"{nameof(SchemaGenerator)}.{nameof(Tests)}",
                    memberInfo => ((FieldInfo) memberInfo).IsPublic);

            var serializableTypes = schemaGenerator.SerializableTypes;

            serializableTypes.Should().BeEquivalentTo(expectedSerializableTypes);
        }
    }

    public class BasicTestA
    {
        public int A;
    }

    public class InheritanceTestA
    {
    }

    public class InheritanceTestB1 : InheritanceTestA
    {
    }

    [UsedImplicitly]
    public class InheritanceTestB2 : InheritanceTestA
    {
    }

    public class InheritanceTestC1 : InheritanceTestB1
    {
    }
}