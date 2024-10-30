/************************************XKNet Copyright*****************************************

*************************************XKNet Copyright*****************************************/

namespace CopyrightProtectionTool
{
    internal class Program
    {
        static string templateContent = string.Empty;

        static string Head = "/************************************XKNet Copyright*****************************************";
        static string End = "************************************XKNet Copyright*****************************************/";

        static void Main(string[] args)
        {
            templateContent = File.ReadAllText("CodeSnippets.template.txt");
            string codeDir = Path.Combine(FileTool.GetSlnDir(), "XKNet");
            foreach (var v in Directory.GetFiles(codeDir, "*.cs", SearchOption.AllDirectories))
            {
                Do(v);
            }

            while (true) { }
        }

        static void Do(string filePath)
        {
            Console.WriteLine(filePath);
            string code = File.ReadAllText(filePath);
            while (code.StartsWith(Head))
            {
                int nEndIndex = code.IndexOf(End);
                int nRemoveLength = nEndIndex + End.Length;
                code = code.Remove(0, nRemoveLength);
            }

            Dictionary<string, string> mTemplateDic = new Dictionary<string, string>();
            mTemplateDic["$ProjectName$"] = "XKNet";
            mTemplateDic["$Web$"] = "https://github.com/825126369/XKNet";
            mTemplateDic["$Author$"] = "阿珂";
            mTemplateDic["$CreateTime$"] = DateTime.Now.ToString();
            mTemplateDic["$Description$"] = "XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1";
            mTemplateDic["$Copyright$"] = "国际MIT软件分发协议";
            mTemplateDic["$HEAD$"] = Head;
            mTemplateDic["$END$"] = End;

            string addContent = templateContent;
            foreach (var v in mTemplateDic)
            {
                addContent = addContent.Replace(v.Key, v.Value);
            }

            if (!code.StartsWith("\n"))
            {
                code += "\n";
            }
            code = addContent + code;
            File.WriteAllText(filePath, code);
        }
    }
}
