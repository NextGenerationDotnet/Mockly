﻿namespace Mockly.Generator;

public class CodeSnippets
{
    public static string MocklyFileHeader => $@"// <auto-generated/>
using global::System;
using global::System.Linq;
using global::Microsoft.Extensions.DependencyInjection;

#nullable enable

";
}
