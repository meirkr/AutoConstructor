﻿namespace AutoConstructor.Generator;

public static class Source
{
    internal const string AttributeName = "AutoConstructor";

    internal const string AttributeFullName = $"{AttributeName}Attribute";

    internal const string AttributeText = $@"// <auto-generated />

[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class {AttributeFullName} : System.Attribute
{{
}}
";

    internal const string IgnoreAttributeName = "AutoConstructorIgnore";

    internal const string IgnoreAttributeFullName = $"{IgnoreAttributeName}Attribute";

    internal const string IgnoreAttributeText = $@"// <auto-generated />

[System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class {IgnoreAttributeFullName} : System.Attribute
{{
}}
";

    internal const string InjectAttributeName = "AutoConstructorInject";

    internal const string InjectAttributeFullName = $"{InjectAttributeName}Attribute";

    internal const string InjectAttributeText = $@"// <auto-generated />

[System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class {InjectAttributeFullName} : System.Attribute
{{
    public {InjectAttributeFullName}(string initializer, string parameterName, System.Type injectedType)
    {{
        Initializer = initializer;
        ParameterName = parameterName;
        InjectedType = injectedType;
    }}

    public string Initializer {{ get; }}

    public string ParameterName {{ get; }}

    public System.Type InjectedType {{ get; }}
}}
";
}
