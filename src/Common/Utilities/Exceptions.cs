// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Common.Utilities
{
    [Serializable]
    public class ExtendedException : Exception
    {
        public ExtendedException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        protected ExtendedException()
        {
        }

        protected ExtendedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class UnexpectedException : ExtendedException
    {
        public UnexpectedException(string name, object value)
            : base($"Unexpected value [{name}={value} Type={value.GetType().Name}]")
        {
        }

        private UnexpectedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
