using System;

public static class SystemUtil
{
    public static string GetSysMemory()
    {
        Environment.WorkingSet
        //PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        return ConvertBytes((long)memoryCounter.NextValue() * 1024 * 1024);
    }

    public static string GetUnUsedMemory()
    {
        //PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        return ConvertBytes((long)memoryCounter.NextValue() * 1024 * 1024);
    }

    public static string GetUsedMemory()
    {
        //PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Committed Bytes");
        return ConvertBytes((long)memoryCounter.NextValue());
    }

    public static string ConvertBytes(long len)
    {
        double dlen = len;
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (dlen >= 1024 && order + 1 < sizes.Length)
        {
            order++;
            dlen = dlen / 1024;
        }
        return String.Format("{0:0.##} {1}", dlen, sizes[order]);
    }
}