using System.Diagnostics;
using System.Reflection;

namespace L2EliminateUpdater
{
    internal class Program
    {
        static bool isFullCheck = false;
        static bool isEngClient = false;
        static void Main()
        {
            Console.Title = $"{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product} v.{Assembly.GetEntryAssembly().GetName().Version.ToString(2)} by {Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright}";

            Console.Write("Full Check? [yes/no]\n(default: no, only check system): ");
            string fullCheck = Console.ReadLine();
            if(!string.IsNullOrEmpty(fullCheck))
            {
                isFullCheck = fullCheck.ToLower().StartsWith("y");
            }
            Console.WriteLine();
            Console.Write("ENG Client? [yes/no]\n(default: no, start RU client): ");
            string lang = Console.ReadLine();
            if (!string.IsNullOrEmpty(lang))
            {
                isEngClient = lang.ToLower().StartsWith("y");
            }
            Console.WriteLine();
            Update().ContinueWith(t =>
            {
                if(t.Result)
                {
                    RunLineageClient(isEngClient);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("\nUpdate Failed. Not all files updated. Please run again.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Thread.Sleep(Timeout.Infinite);
                }
            }).Wait();
        }

        static async Task<bool> Update()
        {
            Updater updater = new(isEngClient);
            bool isFullCheckSuccess = false;
            bool isSystemCheckSuccess = false;
            if (isFullCheck)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                await Console.Out.WriteLineAsync("=== FULL CHECK ===");
                Console.ForegroundColor = ConsoleColor.White;
                await Check(Updater.ListType.Client, updater, isSuccess => isFullCheckSuccess = isSuccess);
                await Check(Updater.ListType.Patch, updater, isSuccess => isSystemCheckSuccess = isSuccess);
            }
            else
            {
                isFullCheckSuccess = true;
                Console.ForegroundColor = ConsoleColor.Yellow;
                await Console.Out.WriteLineAsync("=== SYSTEM CHECK ===");
                Console.ForegroundColor = ConsoleColor.White;
                await Check(Updater.ListType.Patch, updater, isSuccess => isSystemCheckSuccess = isSuccess);
            }
            Directory.Delete(updater.GetUpdaterDir().FullName, true);
            return isFullCheckSuccess && isSystemCheckSuccess;
        }

        static void RunLineageClient(bool isEngClient)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, isEngClient ? "system-en" : "system-ru");
            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = dir,
                Verb = "runas",
                FileName = Path.Combine(dir, "l2.exe"),
                UseShellExecute = true
            });
        }

        static async Task Check(Updater.ListType checkType, Updater updater, Action<bool> onEndCallback)
        {
            bool isSuccess = true;
            foreach (var fo in await updater.GetFileList(checkType))
            {
                await Console.Out.WriteLineAsync();
                await Console.Out.WriteAsync($"Cheking file: {fo.File.TrimStart('\\')} ... ");
                try
                {
                    if (await updater.DownloadFileAsync(fo, checkType))
                    {
                        updater.UnZip(fo);
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                    isSuccess = false;
                }
            }
            onEndCallback.Invoke(isSuccess);
            await Task.CompletedTask;
        }
    }
}