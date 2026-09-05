namespace inFAMOUSReborn.Services;
using System;
using System.IO;

public static class PathHelper
{
    public static string GetMissionsDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        
        if (baseDir.Contains("/bin/Debug") || baseDir.Contains("/bin/Release"))
        {
            int binIndex = baseDir.IndexOf("/bin/");
            return Path.Combine(baseDir.Substring(0, binIndex), "Missions");
        }
        
        if (baseDir.Contains(".app/Contents/"))
        {
            int appIndex = baseDir.IndexOf(".app");
            string appBundlePath = baseDir.Substring(0, appIndex + 4);
            string parentDir = Directory.GetParent(appBundlePath)?.FullName ?? baseDir;
            return Path.Combine(parentDir, "Missions");
        }
        
        return Path.Combine(baseDir, "Missions");
    }
}