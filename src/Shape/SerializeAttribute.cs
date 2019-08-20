using System;
using JetBrains.Annotations;

namespace Shape
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializeAttribute : Attribute
    {
    }
}
