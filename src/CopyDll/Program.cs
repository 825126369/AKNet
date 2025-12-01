using TestCommon;

namespace CopyDll
{
    internal class Program
    {
        static readonly string[] projectDirList = {
                "AKNet.Common",
                "AKNet",
                "AKNet.Extentions.Protobuf",
                "AKNet.MSQuic",
                "AKNet.WebSocket",
                "AKNet.Platform",
                "AKNet.LinuxTcp"
            };

        static void Main(string[] args)
        {
            CopyDLL();
            Console.WriteLine("Finish !");
        }

        static void CopyDLL()
        {
            List<string> outDllDirList = new List<string>();
            outDllDirList.Add(Path.Combine("Debug", "net10.0"));
            outDllDirList.Add(Path.Combine("Debug", "netstandard2.1"));
            outDllDirList.Add(Path.Combine("Release", "net10.0"));
            outDllDirList.Add(Path.Combine("Release", "netstandard2.1"));

            foreach (string dirKey in outDllDirList) 
            {
                string OutDllDir = Path.Combine(FileTool.GetParentDir(FileTool.GetSlnDir()), "DLL_OUT", dirKey);
                Console.WriteLine($"OutDllDir : {OutDllDir}");

                if (!Directory.Exists(OutDllDir))
                {
                    Directory.CreateDirectory(OutDllDir);
                }

                foreach (string dirName in projectDirList)
                {
                    string codeDir = Path.Combine(FileTool.GetSlnDir(), dirName);
                    if (Directory.Exists(codeDir))
                    {
                        foreach (var v in Directory.GetFiles(codeDir, "*.dll", SearchOption.AllDirectories))
                        {
                            if (v.Contains(Path.Combine("bin", dirKey)) && v.Contains("AKNet"))
                            {
                                string fileName = Path.GetFileName(v);
                                string outFilePath = Path.Combine(OutDllDir, fileName);
                                File.Copy(v, outFilePath, true);
                                Console.WriteLine($"Copy DLL: {fileName} {v} => {outFilePath}");
                            }
                        }
                    }
                }
            }
        }
    }
}
