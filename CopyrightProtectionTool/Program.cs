using System.Text;
namespace CopyrightProtectionTool
{
    internal class Program
    {
        static string Head = "/************************************Copyright*****************************************";
        static string End = "************************************Copyright*****************************************/";

        static void Main(string[] args)
        {
            string codeDir = Path.Combine(FileTool.GetSlnDir(), "XKNet");
            foreach (var v in Directory.GetFiles(codeDir, "*.cs", SearchOption.AllDirectories))
            {
                Do(v);
            }

            while (true) { }
        }

        static string GetCopyrightContent()
        {
            string templateContent = File.ReadAllText("CodeSnippets.template.txt", Encoding.UTF8);
            Dictionary<string, string> mTemplateDic = new Dictionary<string, string>();
            mTemplateDic["$ProjectName$"] = "XKNet";
            mTemplateDic["$Web$"] = "https://github.com/825126369/XKNet";
            mTemplateDic["$Author$"] = "阿珂";
            mTemplateDic["$CreateTime$"] = DateTime.Now.ToString();
            mTemplateDic["$Description$"] = "XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1";
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
                code += Environment.NewLine;
            }
            code.TrimStart();
            code = GetCopyrightContent() + code;
            File.WriteAllText(filePath, code, Encoding.UTF8);
        }
    }
}
