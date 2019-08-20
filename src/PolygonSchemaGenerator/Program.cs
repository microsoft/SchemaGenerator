using Common.Extensions;
using Shape;
using System;
using System.IO;
using Utilities;

namespace PolygonSchemaGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                const string path = "Schemas";
                Directory.CreateDirectory(path);

                var schemaGenerator =
                    new JsonSchemaGenerator(
                        new[] { typeof(Polygon) },
                        _ => _.Name.StartsWith(nameof(Shape)),
                        _ => _.HasAttribute<SerializeAttribute>());
                schemaGenerator.Validate();
                var schema = schemaGenerator.Generate();

                File.WriteAllText(
                    Path.Combine(path, "PolygonSchema.json"),
                    schema);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"Error {nameof(PolygonSchemaGenerator)}: " +
                    $"{exception.ToString().Replace(Environment.NewLine, " | ")}");
                throw;
            }
        }
    }
}
