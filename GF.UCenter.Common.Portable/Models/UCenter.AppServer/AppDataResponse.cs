﻿using System;
using System.Runtime.Serialization;

namespace GF.UCenter.Common.Portable
{
    [Serializable]
    [DataContract]
    public class AppDataResponse
    {
        [DataMember]
        public string AppId;
        [DataMember]
        public string AccountId;
        [DataMember]
        public string Data;
    }
}
