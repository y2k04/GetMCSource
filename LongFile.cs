using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

public static class LongFile
{
    private const int MAX_PATH = 260;

    public static void Delete(string path)
    {
        if (path.Length < MAX_PATH)
            File.Delete(path);
        else if (!NativeMethods.DeleteFileW(GetWin32LongPath(path)))
            ThrowWin32Exception();
    }

    public static void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
        if (sourceFileName.Length < MAX_PATH && (destFileName.Length < MAX_PATH))
            File.Copy(sourceFileName, destFileName, overwrite);
        else if (!NativeMethods.CopyFileW(GetWin32LongPath(sourceFileName), GetWin32LongPath(destFileName), !overwrite))
            ThrowWin32Exception();
    }

    #region Helper methods
    [DebuggerStepThrough]
    public static void ThrowWin32Exception()
    {
        int code = Marshal.GetLastWin32Error();
        if (code != 0)
            throw new Win32Exception(code);
    }

    public static string GetWin32LongPath(string path) =>
        path.StartsWith(@"\\?\") ? path : (path.StartsWith("\\") ? $@"\\?\UNC\{path.Substring(2)}" : (path.Contains(":") ? $@"\\?\{path}" : $@"\\?\{Combine(Environment.CurrentDirectory, path).Replace("\\.\\", "\\")}").TrimEnd('.'));

    private static string Combine(string path1, string path2) =>
        $@"{path1.TrimEnd('\\')}\{path2.TrimStart('\\').TrimEnd('.')}";
    #endregion
}