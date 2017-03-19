﻿using System;
using System.Runtime.Serialization;

namespace Api.Exceptions
{
    [Serializable]
    public class UnauthorizedException : ApiException
    {
        public UnauthorizedException()
        {
        }

        public UnauthorizedException(string message) : base(message)
        {
        }

        public UnauthorizedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UnauthorizedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}