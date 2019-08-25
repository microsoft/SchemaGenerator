using JetBrains.Annotations;
using System;

namespace SchemaGenerator.Samples.Shape
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializeAttribute : Attribute
    {
    }
}
