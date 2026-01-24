using System;

namespace AKNet.Common
{
    public static partial class VersionPublishConfig
    {
        public static readonly DateTime m_BuildTime;
        public static readonly System.Version m_Version;

        private static string m_BuildTimeStr = null;

        public static string GetBuildTimeStr()
        {
            if (m_BuildTimeStr == null)
            {
                m_BuildTimeStr = m_BuildTime.ToString();
            }

            return m_BuildTimeStr;
        }
    }
}