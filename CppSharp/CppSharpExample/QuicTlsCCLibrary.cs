using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;

namespace CppSharpExample
{
    //https://github.com/aidin36/cli-openssl-wrapper
    class QuicTlsCCLibrary : ILibrary
    {
        public void Setup(Driver driver)
        {
            driver.Options.CheckSymbols = true;
            driver.Options.GeneratorKind = GeneratorKind.CSharp;
            driver.ParserOptions.LanguageVersion = LanguageVersion.CPP20;

            var module = driver.Options.AddModule("QuicTlsCCLibrary");
            module.IncludeDirs.Add(@"D:\Me\MyProject\AKNet\QuicTlsCC");
            module.IncludeDirs.Add(@"D:\Me\OpenSource\boringssl\include");
            module.Headers.Add("boringssl_wrapper.h");
            module.LibraryDirs.Add(@"D:\Me\MyProject\AKNet\QuicTlsCC\x64");
            module.Libraries.Add("QuicTlsCC.lib");
            module.OutputNamespace = "AKNet.BoringSSL";
        }

        public void SetupPasses(Driver driver) 
        {
           
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            //throw new NotImplementedException();
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
            //throw new NotImplementedException();
        }
    }

    class QuicTlsCCLibraryExample
    {
        public static void Do()
        {
            ConsoleDriver.Run(new QuicTlsCCLibrary());
        }
    }
}
