using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharpExample
{
    //https://github.com/aidin36/cli-openssl-wrapper
    class OpenSSLLibrary : ILibrary
    {
        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.CheckSymbols = true;
            options.GeneratorKind = GeneratorKind.CSharp;
            var module = options.AddModule("OpenSSL_Wrapper");
            module.IncludeDirs.Add(@"C:\Program Files\OpenSSL-Win64\include");
            module.Headers.Add("openssl/quic.h");
            module.LibraryDirs.Add(@"C:\Program Files\OpenSSL-Win64\lib\VC\x64\MT");
            module.Libraries.Add("libcrypto_static.lib");
            module.Libraries.Add("libssl_static.lib");
            module.OutputNamespace = "AKNet.Udp5MSQuic.Common";
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

    class OpenSSLLibraryExample
    {
        public static void Do()
        {
            ConsoleDriver.Run(new MSQuicLibrary());
        }
    }
}
