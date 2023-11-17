using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System;

public class LongDirectory
{
    public static void Delete(string path, bool recursive)
    {
        if (path.Length < 260 && !recursive)
            Directory.Delete(path, false);
        else
        {
            if (!recursive)
                if (!NativeMethods.RemoveDirectory(GetWin32LongPath(path)))
                    ThrowWin32Exception();
            else
                DeleteDirectories(new string[] { GetWin32LongPath(path) });
        }
    }

    private static void DeleteDirectories(string[] directories)
    {
        foreach (string directory in directories)
        {
            foreach (string file in GetFiles(directory, null, SearchOption.TopDirectoryOnly))
                LongFile.Delete(file);
            directories = GetDirectories(directory, null, SearchOption.TopDirectoryOnly);
            DeleteDirectories(directories);
            if (!NativeMethods.RemoveDirectory(GetWin32LongPath(directory)))
                ThrowWin32Exception();
        }
    }

    public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
    {
        var dirs = new List<string>();
        InternalGetDirectories(path, searchPattern ?? "*", searchOption, ref dirs);
        return dirs.ToArray();
    }

    private static void InternalGetDirectories(string path, string searchPattern, SearchOption searchOption, ref List<string> dirs)
    {
        IntPtr findHandle = NativeMethods.FindFirstFile(Path.Combine(GetWin32LongPath(path), searchPattern), out NativeMethods.WIN32_FIND_DATA findData);
        try
        {
            if (findHandle != new IntPtr(-1))
            {
                do
                {
                    if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            string subdirectory = Path.Combine(path, findData.cFileName);
                            dirs.Add(GetCleanPath(subdirectory));
                            if (searchOption == SearchOption.AllDirectories)
                                InternalGetDirectories(subdirectory, searchPattern, searchOption, ref dirs);
                        }
                    }
                } while (NativeMethods.FindNextFile(findHandle, out findData));
                NativeMethods.FindClose(findHandle);
            } else
                ThrowWin32Exception();
        }
        catch
        {
            NativeMethods.FindClose(findHandle);
            throw;
        }
    }

    public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        var files = new List<string>();
        var dirs = new List<string> { path };

        if (searchOption == SearchOption.AllDirectories)
            dirs.AddRange(GetDirectories(path, null, SearchOption.AllDirectories)); //Add all the subpaths

        foreach (var dir in dirs)
        {
            IntPtr findHandle = NativeMethods.FindFirstFile(Path.Combine(GetWin32LongPath(dir), searchPattern ?? "*"), out NativeMethods.WIN32_FIND_DATA findData);
            try
            {
                if (findHandle != new IntPtr(-1))
                {
                    do
                    {
                        if ((findData.dwFileAttributes & FileAttributes.Directory) == 0)
                            files.Add(GetCleanPath(Path.Combine(dir, findData.cFileName)));
                    } while (NativeMethods.FindNextFile(findHandle, out findData));
                    NativeMethods.FindClose(findHandle);
                }
            }
            catch
            {
                NativeMethods.FindClose(findHandle);
                throw;
            }
        }
        return files.ToArray();
    }

    #region Helper methods
    [DebuggerStepThrough]
    public static void ThrowWin32Exception()
    {
        int code = Marshal.GetLastWin32Error();
        if (code != 0)
            throw new System.ComponentModel.Win32Exception(code);
    }

    public static string GetWin32LongPath(string path) =>
        path.StartsWith(@"\\?\") ? path : (path.StartsWith("\\") ? $@"\\?\UNC\{path.Substring(2)}" : (path.Contains(":") ? $@"\\?\{path}" : $@"\\?\{Combine(Environment.CurrentDirectory, path).Replace("\\.\\", "\\")}").TrimEnd('.'));

    private static string GetCleanPath(string path) =>
        path.StartsWith(@"\\?\UNC\") ? $@"\\{path.Substring(8)}" : (path.StartsWith(@"\\?\") ? path.Substring(4) : path);

    private static string Combine(string path1, string path2) =>
        $@"{path1.TrimEnd('\\')}\{path2.TrimStart('\\').TrimEnd('.')}";
    #endregion
}