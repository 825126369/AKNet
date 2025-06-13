using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharpExample
{
    class MSQuicLibrary : ILibrary
    {
        public void Postprocess(Driver driver, ASTContext ctx)
        {
           
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.CheckSymbols = true;
            options.GeneratorKind = GeneratorKind.CSharp;
            var module = options.AddModule("MSQuicSSL_Wrapper");
            module.IncludeDirs.Add(@"C:\Users\14261\.nuget\packages\microsoft.native.quic.msquic.openssl\2.4.10\build\native\include");
            module.Headers.Add("msquic.hpp");
            //module.Headers.Add("msquic_winuser.h");
            module.LibraryDirs.Add(@"C:\Users\14261\.nuget\packages\microsoft.native.quic.msquic.openssl\2.4.10\build\native\lib\x64");
            module.Libraries.Add("msquic");
            module.OutputNamespace = "AKNet.MSQuicWrapper";
            module.Defines.Add("");
        }

        public void SetupPasses(Driver driver)
        {
            
        }    
    }

    class MSQuicLibraryExample
    {
        public static void Do()
        {
            ConsoleDriver.Run(new MSQuicLibrary());
        }
    }
}
