using SchemaGenerator.Common;
using SchemaGenerator.Samples.Shape;
using System;
using System.Reflection;

namespace SchemaGenerator.Samples.RulesBasedTypeScriptSchemaGenerator
{
    public class Program
    {
        public static void Main()
        {
            var schemaGenerator =
                new TypeScriptSchemaGenerator(
                    new[] { typeof(Polygon) },
                    _ => _.Name.StartsWith("SchemaGenerator.Samples"),
                    _ =>
                    {
                        switch (_)
                        {
                            case FieldInfo fieldInfo:
                                return fieldInfo.IsPublic;
                            case PropertyInfo propertyInfo:
                                return propertyInfo.GetMethod != null && propertyInfo.SetMethod != null;
                            default:
                                throw new UnexpectedException(nameof(MemberInfo), _.GetType().Name);
                        }
                    });
            schemaGenerator.Validate();
            var schema = schemaGenerator.Generate();

            Console.WriteLine(schema);
        }
    }
}
