using ClangSharp;
using ClangSharp.Interop;
using System.Diagnostics;

public class  ClangSharpExample
{
    //ClangSharpPInvokeGenerator “@ClangSharp_MSQuic.rsp”
    public static void Do()
    {
        string sourceFileName = "C:\\Users\\14261\\.nuget\\packages\\microsoft.native.quic.msquic.openssl\\2.4.10\\build\\native\\include\\msquic.h";

        PInvokeGeneratorConfiguration mConfig = new PInvokeGeneratorConfiguration(
            "", "", "AKNet.MSQuicWrapper", "D:\\Temp", null,
            PInvokeGeneratorOutputMode.CSharp, 
            PInvokeGeneratorConfigurationOptions.GenerateMultipleFiles |
              PInvokeGeneratorConfigurationOptions.LogVisitedFiles |
            PInvokeGeneratorConfigurationOptions.GenerateCompatibleCode);

        //mConfig.LibraryPath = ("C:\\Users\\14261\\.nuget\\packages\\microsoft.native.quic.msquic.openssl\\2.4.10\\build\\native\\lib\\x64");
        PInvokeGenerator mm = new PInvokeGenerator(mConfig, null);

        var index = CXIndex.Create();
        CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index, sourceFileName, null, new CXUnsavedFile[0]);
        var diagnostics = translationUnit.GetDiagnostic(1);
        Console.WriteLine(diagnostics.Spelling);

        Debug.Assert(translationUnit != null, "translationUnit == null");
        TranslationUnit A = TranslationUnit.GetOrCreate(translationUnit);
        Debug.Assert(A != null, "A == null");
        
        mm.GenerateBindings(A, "A.cs", null, CXTranslationUnit_Flags.CXTranslationUnit_None);
        Console.WriteLine("Ok");
    }

}