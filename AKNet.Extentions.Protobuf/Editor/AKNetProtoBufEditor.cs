/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;
using System.IO;

namespace AKNet.Extentions.Protobuf.Editor
{
    public static class AKNetProtoBufEditor
    {
        public static void DoProtoResetCSFile(string ProtoOutPath, string ProtoNameSpaceRootName, string ProtoDLLFilePath)
        {
            EditorLog.Log("ProtoOutPath: " + ProtoOutPath);
            EditorLog.Log("ProtoNameSpaceRootName: " + ProtoNameSpaceRootName);
            EditorLog.Log("ProtoDLLFilePath: " + ProtoDLLFilePath);

            ProtobufGenReset.GenerateProtoResetCSFile(ProtoOutPath, ProtoNameSpaceRootName, ProtoDLLFilePath);
        }

        public static void DoPublicCSFile(string ProtocExePath, string ProtoOutPath, string ProtoPath)
        {
            EditorLog.Log("ProtocExePath: " + ProtocExePath);
            EditorLog.Log("ProtoOutPath: " + ProtoOutPath);
            EditorLog.Log("ProtoPath: " + ProtoPath);

            if (!Directory.Exists(ProtoPath))
            {
                Directory.CreateDirectory(ProtoPath);
            }

            if (!Directory.Exists(ProtoOutPath))
            {
                Directory.CreateDirectory(ProtoOutPath);
            }

            string arg = $"--csharp_out={Path.GetRelativePath(ProtoPath, ProtoOutPath)} ";
            foreach (string v in Directory.GetFiles(ProtoPath, "*.proto", SearchOption.TopDirectoryOnly))
            {
                arg += " " + Path.GetFileName(v);
            }

            RunCmd(Path.GetFullPath(ProtocExePath), Path.GetFullPath(ProtoPath), arg);
        }
        
        public static void DoInternalCSFile(string ProtocExePath, string ProtoOutPath, string ProtoPath)
        {
            EditorLog.Log("ProtocExePath: " + ProtocExePath);
            EditorLog.Log("ProtoOutPath: " + ProtoOutPath);
            EditorLog.Log("ProtoPath: " + ProtoPath);

            if (!Directory.Exists(ProtoPath))
            {
                Directory.CreateDirectory(ProtoPath);
            }
            if (!Directory.Exists(ProtoOutPath))
            {
                Directory.CreateDirectory(ProtoOutPath);
            }

            string arg = $"--csharp_out={Path.GetRelativePath(ProtoPath, ProtoOutPath)} ";
            arg += " --csharp_opt=internal_access";
            foreach (string v in Directory.GetFiles(ProtoPath, "*.proto", SearchOption.TopDirectoryOnly))
            {
                arg += " " + Path.GetFileName(v);
            }
            RunCmd(Path.GetFullPath(ProtocExePath), Path.GetFullPath(ProtoPath), arg);
        }

        public static void DoPublicCSFile(string ProtocExePath, string ProtoOutPath, params string[] ProtoPath)
        {
            EditorLog.Log("ProtocExePath: " + ProtocExePath);
            EditorLog.Log("ProtoOutPath: " + ProtoOutPath);
            for (int i = 0; i < ProtoPath.Length; i++)
            {
                EditorLog.Log("ProtoPath: " + ProtoPath);
            }

            if (!Directory.Exists(ProtoOutPath))
            {
                Directory.CreateDirectory(ProtoOutPath);
            }

            string arg = $"--csharp_out={Path.GetFullPath(ProtoOutPath)}";
            for (int i = 0; i < ProtoPath.Length; i++)
            {
                arg += " --proto_path=" + Path.GetFullPath(ProtoPath[i]);
            }

            for (int i = 0; i < ProtoPath.Length; i++)
            {
                foreach (string v in Directory.GetFiles(ProtoPath[i], "*.proto", SearchOption.TopDirectoryOnly))
                {
                    arg += " " + Path.GetFileName(v);
                }
            }

            RunCmd(Path.GetFullPath(ProtocExePath), null, arg);
        }

        private static void RunCmd(string exePath, string workPath, string arg)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = arg;
            info.WorkingDirectory = workPath;
            info.FileName = exePath;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
            info.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;

            EditorLog.Log("RunCmd: " + info.Arguments);

            Process process = Process.Start(info);
            string strOutput = process.StandardOutput.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(strOutput))
            {
                EditorLog.Log(strOutput);
            }
            strOutput = process.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(strOutput))
            {
                EditorLog.LogError(strOutput);
            }
            process.WaitForExit();
            process.Close();
        }
    }
}