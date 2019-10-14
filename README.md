# SchemaGenerator

A generic tool for automatic schema generation for a set of C# classes.

https://www.nuget.org/packages/SchemaGenerator.Core/

https://www.nuget.org/packages/SchemaGenerator.Json/

## Example Uses

* Integration between C# code and TypeScript UI.
* Source controlled database schema, automatically generated at compile-time, for understanding necessary database migration steps.
* Creating Protobuf serializers more easily. 

## Getting Started

Consider the following class:
```csharp
namespace Sample
{
    public class Apple
    {
        [Serialize]
        public int SeedCount { get; set; }
    }
}
```

You can create a JSON schema like that:
```csharp
var schemaGenerator =
    new JsonSchemaGenerator(
        new[] { typeof(Apple) },
        assemblyName => assemblyName.Name == typeof(Apple).Assembly.GetName(),
        memberInfo => memberInfo.HasAttribute<SerializeAttribute>());
schemaGenerator.Validate();
var schema = schemaGenerator.Generate();
```

This will generate the following schema:
```json
{
  "types": [
    {
      "name": "Sample.Apple",
      "baseType": "System.Object",
      "properties": [
        {
          "name": "SeedCount",
          "type": "System.Int32"
        }
      ]
    }
  ],
  "enums": []
}
```

If you want to create another type of schema, create a custom generator by extending Core.SchemaGenerator.

## Algorithm and Usage

Extended with a set of root types, the generator scans relevant assemblies to understand what types are serializable:
* Serializable fields and properties of other serializable types.
* Base classes and derived classes of serializable types.

It can be extended to support any serialization logic that can be inferred by the MemberInfo itself, for example:
* An attribute on the member, e.g. SerializableAttribute.
* A set of rules on the access modifiers of the field or property, e.g. IsPublic.

Extended with a Generate method, it can output any desired schema format: Json, Protobuf, TypeScript modules, Java classes, etc.

## Roadmap

* A Nuget package for a TypeScript implementation
* More tests
* Better documentation

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security
Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should
receive a response within 24 hours. If for some reason you do not, please follow up via
email to ensure we received your original message. Further information, including the
[MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in
the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).
