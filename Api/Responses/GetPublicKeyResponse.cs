﻿using System;
using System.Runtime.Serialization;

namespace Api.Responses
{
    [Serializable]
    public class GetPublicKeyResponse
    {
        [DataMember]
        public string PublicKey { get; private set; }
    }
}