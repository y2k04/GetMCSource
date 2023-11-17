using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace GenerateMCSource
{
    class Program
    {
        static readonly HttpClient c = new HttpClient();
        static readonly string tempPath = $@"{Path.GetTempPath()}{Path.GetRandomFileName()}";

        static void Main()
        {
            if (Directory.Exists($@"{Environment.CurrentDirectory}\mcsource")) Directory.Delete($@"{Environment.CurrentDirectory}\mcsource", true);
            Directory.CreateDirectory($@"{Environment.CurrentDirectory}\mcsource");
            Directory.CreateDirectory(tempPath);

            byte[] archive = c.GetByteArrayAsync("https://github.com/FabricMC/fabric-example-mod/archive/refs/heads/1.20.zip").Result;
            File.WriteAllBytes($@"{tempPath}\temp.zip", archive);
            ZipFile.ExtractToDirectory($@"{tempPath}\temp.zip", tempPath);
            File.Delete($@"{tempPath}\temp.zip");

            var cd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = $@"{tempPath}\fabric-example-mod-1.20";
            Process.Start("cmd", $@"/c gradlew.bat gensources").WaitForExit();
            Environment.CurrentDirectory = cd;

            var sourceDirs = Directory.GetDirectories($@"{tempPath}\fabric-example-mod-1.20\.gradle\loom-cache\minecraftMaven\net\minecraft", "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < 2; i++)
            {
                var temp = Directory.GetFiles(Directory.GetDirectories(sourceDirs[i], "*", SearchOption.TopDirectoryOnly)[0], "*sources.jar", SearchOption.TopDirectoryOnly)[0];
                LongFile.Copy(temp, $@"{tempPath}\{temp.Substring(temp.LastIndexOf('\\') + 1)}", true);
                try { ZipFile.ExtractToDirectory($@"{tempPath}\{temp.Substring(temp.LastIndexOf('\\') + 1)}", $@"{tempPath}\mcsource{i}"); } catch { }
                Directory.Delete($@"{tempPath}\mcsource{i}\META-INF", true);

                foreach (var directory in Directory.GetDirectories($@"{tempPath}\mcsource{i}", "*", SearchOption.TopDirectoryOnly))
                    CopyFolder(directory, $@"{Environment.CurrentDirectory}\mcsource\{directory.Substring(directory.LastIndexOf('\\') + 1)}");
            }

            LongDirectory.Delete(tempPath, true);
        }

        static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            foreach (string file in Directory.GetFiles(sourceFolder))
                File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)));
            foreach (string folder in Directory.GetDirectories(sourceFolder))
                CopyFolder(folder, Path.Combine(destFolder, Path.GetFileName(folder)));
        }
    }
}
