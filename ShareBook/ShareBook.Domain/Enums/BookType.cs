﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace ShareBook.Domain.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BookType
    {
        [Description("Impresso")]
        Printed,
        [Description("Eletrônico")]
        Eletronic
    }
}
