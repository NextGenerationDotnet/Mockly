using System;
using System.Collections.Generic;
using System.Text;

namespace Mockly.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MocklifyAttribute : Attribute
{
}
