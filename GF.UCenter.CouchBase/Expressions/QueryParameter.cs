﻿using System;

namespace GF.UCenter.CouchBase
{
    internal class QueryParameter
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public Type DataType { get; set; }

        public TypeCode TypeCode { get; set; }

    }
}
