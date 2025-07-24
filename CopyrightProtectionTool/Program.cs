using System.Text;
using TestCommon;
namespace CopyrightProtectionTool
{
    internal class Program
    {
        static string Head = "/************************************Copyright*****************************************";
        static string End = "************************************Copyright*****************************************/";

        static void Main(string[] args)
        {
            string codeDir = Path.Combine(FileTool.GetSlnDir(), "AKNet");
            foreach (var v in Directory.GetFiles(codeDir, "*.cs", SearchOption.AllDirectories))
            {
                Do(v);
            }

            Console.WriteLine("Finish !!!");
            while (true) { }
        }

        static string GetCopyrightContent()
        {
            string templateContent = File.ReadAllText("CodeSnippets.template.txt", Encoding.UTF8);
            Dictionary<string, string> mTemplateDic = new Dictionary<string, string>();
            mTemplateDic["$ProjectName$"] = "AKNet";
            mTemplateDic["$Web$"] = "https://github.com/825126369/AKNet";
            mTemplateDic["$Author$"] = "AKe";
            mTemplateDic["$CreateTime$"] = DateTime.Now.ToString();
            mTemplateDic["$Description$"] = "这是一个 C# 的游戏网络库";
            mTemplateDic["$Copyright$"] = "MIT软件许可证";
            mTemplateDic["$HEAD$"] = Head;
            mTemplateDic["$END$"] = End;

            string addContent = templateContent;
            foreach (var v in mTemplateDic)
            {
                addContent = addContent.Replace(v.Key, v.Value);
            }

            return addContent;
        }

        static void Do(string filePath)
        {
            Console.WriteLine(filePath);
            string code = File.ReadAllText(filePath, Encoding.UTF8);
            while (code.StartsWith(Head))
            {
                int nEndIndex = code.IndexOf(End);
                int nRemoveLength = nEndIndex + End.Length;
                code = code.Remove(0, nRemoveLength);
            }

            code.TrimStart();
            if (!code.StartsWith(Environment.NewLine))
            {
                code = Environment.NewLine + code;
            }
            code = GetCopyrightContent() + code;
            File.WriteAllText(filePath, code, Encoding.UTF8);
        }
    }
}
