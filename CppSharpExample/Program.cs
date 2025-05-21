using CppSharp.Generators;
using CppSharp;
using CppSharp.AST;

namespace CppSharpExample
{
    class MyLibrary : ILibrary
    {
        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            var module = options.AddModule("OpenSSL_Wrapper");
            module.IncludeDirs.Add(@"D:\Me\OpenSource\openssl\include");
            //module.Headers.Add("openssl/macros.h");
            module.Headers.Add("openssl/ssl.h");
            module.Headers.Add("openssl/ssl2.h");
            module.Headers.Add("openssl/ssl3.h");

            //foreach(var v in Directory.GetFiles("D:\\Me\\OpenSource\\openssl\\include\\openssl", "*.h", SearchOption.TopDirectoryOnly))
            //{
            //    module.Headers.Add($"openssl/{Path.GetFileName(v)}");
            //}


            module.LibraryDirs.Add(@"D:\Me\OpenSource\openssl");
            module.Libraries.Add("libssl.lib");
            module.OutputNamespace = "AKNet.Udp5MSQuic.Common";
        }

        public void SetupPasses(Driver driver) { }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            //throw new NotImplementedException();
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
            //throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ConsoleDriver.Run(new MyLibrary());
        }
    }
}
