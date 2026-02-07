using System.Collections.Generic;

namespace AKNet.Common
{
    //不一定有用，先放这吧
    public static class AKNetConfigMgr
    {
        private static bool bInit = false;
        private static readonly Dictionary<string, ConfigInstance> mConfigDic = new Dictionary<string, ConfigInstance>();
        private static TextParser mTextParser = null;

        public static void Init(string path = "AKNet.Config.text")
        {
            if (bInit) return;
            bInit = true;
            mTextParser = new TextParser(path);
        }

        internal static ConfigInstance FindConfig(string configId)
        {
            if (!mConfigDic.TryGetValue(configId, out ConfigInstance mInstance))
            {
                mInstance = new ConfigInstance();
                mInstance.Init(mTextParser, configId);
                mConfigDic[configId] = mInstance;
            }
            return mInstance;
        }
    }

    internal class ConfigInstance
    {
        public bool bAutoReConnect = false;

        public void Init(TextParser mTextParser, string configId)
        {
            bAutoReConnect = mTextParser.ReadBoolean(configId, "bAutoReConnect", false);
        }
    }
}
