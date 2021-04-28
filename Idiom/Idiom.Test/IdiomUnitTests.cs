using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Idiom.Test.CSharpCodeFixVerifier<
    Idiom.IdiomAnalyzer,
    Idiom.IdiomCodeFixProvider>;

namespace Idiom.Test
{
    [TestClass]
    public class IdiomUnitTest
    {
        [TestMethod]
        public async Task Empty_NoDiagnostics()
        {
            var test = @"";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NoQualifiedNames_NoDiagnostics()
        {
            var test = @"
                using System;
                namespace App
                {
                    static class Program
                    {
                        static void Main(string[] args)
                        {
                            Console.WriteLine(""hello, world"");
                        }
                    }
                }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task QualifiedNames_RaisesDiagnostics()
        {
            var test = @"
                using System;
                namespace Foo.Bar
                {
                    public class Baz
                    {
                        public class Hello
                        {
                            public static void World()
                            {
                                Console.WriteLine(""hello, world"");
                            }
                        }
                    }
                }
                namespace App
                {
                    static class Program
                    {
                        static void Main(string[] args)
                        {
                            {|#0:Foo.Bar.Baz|}.Hello.World();
                        }
                    }
                }";

            var expected = VerifyCS.Diagnostic(IdiomAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("Foo.Bar.Baz");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        //Diagnostic and CodeFix both triggered and checked for
        // [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("Idiom").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
