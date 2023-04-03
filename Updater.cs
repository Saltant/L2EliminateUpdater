using L2EliminateUpdater.Models;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace L2EliminateUpdater
{
    public class Updater
    {
        const string UPDATE_CLIENT_FILES_URL = "https://expanseupdasdhhrwcg.super-guard.cloud/uppdr1/client/files.xml";
        const string UPDATE_PATCH_FILES_URL = "https://expanseupdasdhhrwcg.super-guard.cloud/uppdr1/patch/files.xml";
        const string UPDATE_FULL_URL = "https://expanseupdasdhhrwcg.super-guard.cloud/uppdr1/client";
        const string UPDATE_PATCH_URL = "https://expanseupdasdhhrwcg.super-guard.cloud/uppdr1/patch";
        const string USER_AGENT = "expupdtrv1";

        readonly DirectoryInfo updaterDir = Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdaterTmp"));
        readonly HttpClient httpClient;
        readonly bool isEngClient;
        public DirectoryInfo GetUpdaterDir() => updaterDir;

        public Updater(bool isEngClient)
        {
            this.isEngClient = isEngClient;
            httpClient = new();
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        }

        public enum ListType
        {
            Client,
            Patch
        }

        public async Task<List<Set>> GetFileList(ListType listType)
        {
            var response = await httpClient.GetAsync(listType == ListType.Client ? UPDATE_CLIENT_FILES_URL : UPDATE_PATCH_FILES_URL);
            response.EnsureSuccessStatusCode();
            FileList fl = null;
            try
            {
                XmlSerializer serializer = new(typeof(FileList));
                fl = (FileList)serializer.Deserialize(response.Content.ReadAsStream());
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
            return isEngClient ? fl?.Set.Where(x => x.File.StartsWith("\\system-en")).ToList() : fl?.Set.Where(x => x.File.StartsWith("\\system-ru")).ToList();
        }

        public async Task<bool> DownloadFileAsync(Set fo, ListType listType)
        {
            bool result = false;
            bool isNeedDownload = false;
            string pathToLocalFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fo.File.TrimStart('\\'));
            if (File.Exists(pathToLocalFile))
            {
                if (!fo.Hash.SequenceEqual(GetMd5HashFromFile(pathToLocalFile)))
                {
                    isNeedDownload = true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("GOOD");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            else isNeedDownload = true;

            if (isNeedDownload)
            {
                var response = await httpClient.GetAsync(new Uri(Path.Combine(listType == ListType.Client ? UPDATE_FULL_URL : UPDATE_PATCH_URL, fo.File.TrimStart('\\') + ".zip")));
                response.EnsureSuccessStatusCode();

                var dir = Directory.CreateDirectory(Path.Combine(updaterDir.FullName, fo.File[..fo.File.LastIndexOf('\\')].TrimStart('\\')));
                using var fs = new FileStream(Path.Combine(dir.FullName, fo.File[fo.File.LastIndexOf('\\')..].TrimStart('\\') + ".zip"), FileMode.Create);
                await response.Content.CopyToAsync(fs);
                result = true;
            }
            return result;
        }

        public void UnZip(Set fo)
        {
            var dir = Path.Combine(updaterDir.FullName, fo.File[..fo.File.LastIndexOf('\\')].TrimStart('\\'));
            var archiveFile = Path.Combine(dir, fo.File[fo.File.LastIndexOf('\\')..].TrimStart('\\') + ".zip");
            ZipFile.ExtractToDirectory(archiveFile, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fo.File[..fo.File.LastIndexOf('\\')].TrimStart('\\')), true);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Updated");
            Console.ForegroundColor = ConsoleColor.White;
            File.Delete(archiveFile);
        }

        static string GetMd5HashFromFile(string fileName)
        {
            using FileStream fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fileStream != null)
            {
                MD5 mD = MD5.Create();
                byte[] array = mD.ComputeHash(fileStream);
                fileStream.Close();
                StringBuilder stringBuilder = new();
                byte[] array2 = array;
                foreach (byte b in array2)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
                return stringBuilder.ToString();
            }
            return "";
        }
    }
}
