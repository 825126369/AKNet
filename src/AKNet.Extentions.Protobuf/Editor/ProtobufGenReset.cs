/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:55
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AKNet.Extentions.Protobuf.Editor
{
    internal static class ProtobufGenReset
    {
        const string ResetFuncName = "Reset";
        public static void GenerateProtoResetCSFile(string ProtoOutPath, string ProtoNameSpaceRootName, string ProtoDLLFilePath)
        {
            Dictionary<string, List<Type>> mNameSpaceTypeDic = new Dictionary<string, List<Type>>();
            Type[] mType = Assembly.LoadFile(Path.GetFullPath(ProtoDLLFilePath)).GetTypes();
            foreach (var v in mType)
            {
                bool bOnlyProtobufResetCSFile = true;
                foreach (var v2 in v.GetProperties())
                {
                    if (v2.Name.Contains("Parser") || v2.Name.Contains("Descriptor"))
                    {
                        bOnlyProtobufResetCSFile = false;
                        break;
                    }
                }

                if (!bOnlyProtobufResetCSFile && v.FullName.StartsWith(ProtoNameSpaceRootName) && v.Name != "<>c" && !v.Name.EndsWith("Reflection"))
                {
                    List<Type> mListTypes = null;
                    if (!mNameSpaceTypeDic.TryGetValue(v.Namespace, out mListTypes))
                    {
                        mListTypes = new List<Type>();
                        mNameSpaceTypeDic[v.Namespace] = mListTypes;
                    }
                    mListTypes.Add(v);
                }
            }

            string mContent = string.Empty;
            mContent += "using AKNet.Extentions.Protobuf;\n";
            mContent += "using Google.Protobuf;\n";
            foreach (var v in mNameSpaceTypeDic)
            {
                mContent += GenerateProtoResetCSFile(v.Key, v.Value);
            }

            string filePath = Path.Combine(ProtoOutPath, "IProtobufMessageReset.cs");
            File.WriteAllText(filePath, mContent);
        }

        public static string GenerateProtoResetCSFile(string mNameSpace, List<Type> mClassList)
        {
            string mNameSpaceStr = string.Empty;
            mNameSpaceStr += $"namespace {mNameSpace}\n";
            mNameSpaceStr += $"{{\n";

            foreach (var v in mClassList)
            {
                NetLog.Log("��ǰ����: " + v.Namespace + " | " + v.Name);
                string mClassStr = string.Empty;
                mClassStr += $"\tpublic sealed partial class {v.Name} : IProtobufResetInterface\n";
                mClassStr += $"\t{{\n";

                string mStaticFunc = string.Empty;
                mStaticFunc += $"\t\tpublic void {ResetFuncName}()\n";
                mStaticFunc += $"\t\t{{\n";

                foreach (var v2 in v.GetProperties())
                {
                    if (!v2.Name.Contains("Parser") && !v2.Name.Contains("Descriptor"))
                    {
                        if (v2.PropertyType.IsValueType)
                        {
                            mStaticFunc += $"\t\t\t{v2.Name} = default;\n";
                        }
                        else if (v2.PropertyType == typeof(string))
                        {
                            mStaticFunc += $"\t\t\t{v2.Name} = string.Empty;\n";
                        }
                        else if (v2.PropertyType == typeof(ByteString))
                        {
                            mStaticFunc += $"\t\t\t{v2.Name} = ByteString.Empty;\n";
                        }
                        else if (v2.PropertyType.Name.Contains("RepeatedField"))
                        {
                            if (v2.PropertyType.GenericTypeArguments[0].IsClass && v2.PropertyType.GenericTypeArguments[0] != typeof(string))
                            {
                                mStaticFunc += $"\t\t\tforeach(var v in {v2.Name})\n";
                                mStaticFunc += $"\t\t\t{{\n";
                                mStaticFunc += $"\t\t\t\tIMessagePool<{GetClassFullName(v2.PropertyType.GenericTypeArguments[0])}>.recycle(v);\n";
                                mStaticFunc += "\t\t\t}\n";
                            }

                            mStaticFunc += $"\t\t\t{v2.Name}.Clear();\n";
                        }
                        else if (v2.PropertyType.IsClass && !v2.PropertyType.IsGenericType) //��
                        {
                            mStaticFunc += $"\t\t\tIMessagePool<{GetClassFullName(v2.PropertyType)}>.recycle({v2.Name});\n";
                            mStaticFunc += $"\t\t\t{v2.Name} = null;\n";
                        }
                        else
                        {
                            NetLog.LogError($"��֧�ֵ����ͣ�{v2.PropertyType.Name} : {v2.Name}");
                        }
                    }
                }

                mStaticFunc += "\t\t}\n";
                mClassStr += mStaticFunc + "\t}\n";
                mNameSpaceStr += mClassStr;
            }
            mNameSpaceStr += "}\n";
            return mNameSpaceStr;
        }

        private static string GetClassFullName(Type v)
        {
            string className = $"{v.Namespace}.{v.Name}";
            return className;
        }
    }
}