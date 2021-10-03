using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifySourceGenerator = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests;

public class GeneratorTests
{
    [Fact]
    public async Task Run_WithAttributeAndPartial_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;
    }
}";
        const string generated = @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", typeof(System.Guid))]
        private readonly string _guidString;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(injectedType: typeof(System.Guid), parameterName: ""guid"", initializer: ""guid.ToString()"")]
        private readonly string _guidString;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(null, ""guid"", typeof(string))]
        private readonly string _guidString;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(string guid)
        {
            this._guidString = guid ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly System.Guid _guid;
        [AutoConstructorInject(""guid.ToString()"", ""guid"", null)]
        private readonly string _guidString;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guid = guid;
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", null)]
        private readonly string _guidString;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(string guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(initializer: ""guid.ToString()"", injectedType: typeof(System.Guid))]
        private readonly string _guid;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guid = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(parameterName: ""guid"")]
        private readonly string _guidString;
    }
}", @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(string guid)
        {
            this._guidString = guid ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    public async Task Run_WithInjectAttribute_ShouldGenerateClass(string code, string generated)
    {
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorIgnore]
        private readonly int _ignore;
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _ignore = 0;
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private int _ignore;
    }
}")]
    public async Task Run_NoFieldsToInject_ShouldNotGenerateClass(string code)
    {
        await VerifySourceGenerator.RunAsync(code);
    }

    [Fact]
    public async Task Run_WithAttributeAndWithoutPartial_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;
    }
}";

        await VerifySourceGenerator.RunAsync(code);
    }

    [Fact]
    public async Task Run_ClassWithoutNamespace_ShouldGenerateClass()
    {
        const string code = @"
[AutoConstructor]
internal partial class Test
{
    private readonly int _t;
}";
        const string generated = @"// <auto-generated />
partial class Test
{
    public Test(int t)
    {
        this._t = t;
    }
}
";

        await VerifySourceGenerator.RunAsync(code, generated, generatedName: "Test.g.cs");
    }

    [Theory]
    [InlineData("t")]
    [InlineData("_t")]
    [InlineData("__t")]
    public async Task Run_IdentifierWithOrWithoutUnderscore_ShouldGenerateSameClass(string identifier)
    {
        string code = $@"
namespace Test
{{
    [AutoConstructor]
    internal partial class Test
    {{
        private readonly int {identifier};
    }}
}}";
        string generated = $@"// <auto-generated />
namespace Test
{{
    partial class Test
    {{
        public Test(int t)
        {{
            this.{identifier} = t;
        }}
    }}
}}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithMsbuildConfig_ShouldGenerateClass(bool disableNullChecks)
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string _t;
    }
}";
        string generated = $@"// <auto-generated />
namespace Test
{{
    partial class Test
    {{
        public Test(string t)
        {{
            this._t = t{(!disableNullChecks ? " ?? throw new System.ArgumentNullException(nameof(t))" : "")};
        }}
    }}
}}
";

        (string, SourceText) configFile = ("/.editorconfig", SourceText.From($@"
is_global=true
build_property.AutoConstructor_DisableNullChecking = {disableNullChecks}
"));

        await VerifySourceGenerator.RunAsync(code, generated, configFiles: new[] { configFile });
    }

    [Fact]
    public async Task Run_WithMismatchingTypes_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", typeof(System.Guid))]
        private readonly string _i;
        private readonly string _guid;
    }
}";

        DiagnosticResult diagnosticResult = new DiagnosticResult(AutoConstructorGenerator.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 10, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResult });
    }

    [Fact]
    public async Task Run_WithMismatchingFallbackTypes_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(null, ""guid"", null)]
        private readonly string _i;

        [AutoConstructorInject(null, ""guid"", null)]
        private readonly System.Guid _guid;
    }
}";

        DiagnosticResult diagnosticResult = new DiagnosticResult(AutoConstructorGenerator.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 12, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResult });
    }

    [Fact]
    public async Task Run_WithAliasForAttribute_ShouldGenerateClass()
    {
        const string code = @"using Alias = AutoConstructorAttribute;
namespace Test
{
    [Alias]
    internal partial class Test
    {
        private readonly int _t;
    }
}";
        const string generated = @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithNullableReferenceType_ShouldGenerateClass()
    {
        const string code = @"namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string? _t;
    }
}";
        const string generated = @"// <auto-generated />
#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(string? t)
        {
            this._t = t ?? throw new System.ArgumentNullException(nameof(t));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated, nullable: true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithOrWithoutNullableReferenceType_ShouldGenerateClassWithOrWithoutNullCheck(bool enableBoolean)
    {
        const string code = @"namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string _t;
    }
}";
        string generated = $@"// <auto-generated />
namespace Test
{{
    partial class Test
    {{
        public Test(string t)
        {{
            this._t = t{(!enableBoolean ? " ?? throw new System.ArgumentNullException(nameof(t))" : "")};
        }}
    }}
}}
";
        await VerifySourceGenerator.RunAsync(code, generated, nullable: enableBoolean);
    }
}
