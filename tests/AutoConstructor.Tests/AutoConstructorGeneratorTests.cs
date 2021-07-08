﻿using System.Text;
using System.Threading.Tasks;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests
{
    public class AutoConstructorGeneratorTests
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
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(AutoConstructorGenerator.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(AutoConstructorGenerator.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(AutoConstructorGenerator.InjectAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "Test.Test.g.cs", SourceText.From(generated, Encoding.UTF8)),
                    }
                }
            }.RunAsync();
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

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(AutoConstructorGenerator.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(AutoConstructorGenerator.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(AutoConstructorGenerator.InjectAttributeText, Encoding.UTF8)),
                    }
                }
            }.RunAsync();
        }
    }
}
