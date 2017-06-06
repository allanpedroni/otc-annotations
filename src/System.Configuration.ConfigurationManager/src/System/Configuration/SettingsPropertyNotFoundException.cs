﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Configuration
{
    public class SettingsPropertyNotFoundException : Exception
    {
        public SettingsPropertyNotFoundException(String message)
            : base(message)
        {
        }

        public SettingsPropertyNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SettingsPropertyNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public SettingsPropertyNotFoundException()
        { }
    }

}