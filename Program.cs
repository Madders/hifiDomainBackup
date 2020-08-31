using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace hifiDomainBackup
{
    class Program
    {
        static string backupFilePath = "/home/madders/backup-madders-place-20200831-2020-08-31_15-24-09.content.zip";
        static string destinationFilePath = "/home/madders/worlds/madders-place/hifiassets/";
        static string destinationHostingRoot = "https://files.tivolicloud.com/madders/MaddersPlace/HiFiAssets/";


        static void Main(string[] args)
        {
            string modelsJson = string.Empty;

            Console.WriteLine("Loading and extracting models.json from archive");

            using (FileStream zipToOpen = new FileStream(backupFilePath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    var modelArchive = archive.GetEntry("models.json.gz");
                    using (Stream entryStream = modelArchive.Open())
                    {
                        using (GZipStream decompressionStream = new GZipStream(entryStream, CompressionMode.Decompress))
                        {
                            const int size = 4096;
                            byte[] buffer = new byte[size];
                            using (MemoryStream memory = new MemoryStream())
                            {
                                int count = 0;
                                do
                                {
                                    count = decompressionStream.Read(buffer, 0, size);
                                    if (count > 0)
                                    {
                                        memory.Write(buffer, 0, count);
                                    }
                                }
                                while (count > 0);

                                modelsJson = System.Text.ASCIIEncoding.ASCII.GetString(memory.ToArray());
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Parsing models.json");

            List<AssetLink> assetLinks = new List<AssetLink>();

            ParseAssetLinks(modelsJson, "\\\\\"(http://hifi-content.s3.amazonaws.com.*?)\\\\\"", assetLinks);
            ParseAssetLinks(modelsJson, "\\\\\"(http://content.highfidelity.com.*?)\\\\\"", assetLinks);

            ParseAssetLinks(modelsJson, "\"(http://hifi-content.s3.amazonaws.com.*?)\"", assetLinks);
            ParseAssetLinks(modelsJson, "\"(http://content.highfidelity.com.*?)\"", assetLinks);

            Console.WriteLine("{0} HiFi Assets found to download and replace.", assetLinks.Count);

            foreach(AssetLink assetLink in assetLinks)
            {
                Console.WriteLine(assetLink.DownloadPath);
            }

            Console.WriteLine("Done");
        }

        static void ParseAssetLinks(string json, string pattern, List<AssetLink> assetLinks)
        {
            foreach(Match match in Regex.Matches(json, pattern))
            {
                if(!match.Groups[1].ToString().EndsWith("\\") && !assetLinks.Exists(al => al.MatchString == match.ToString()))
                {
                    assetLinks.Add(new AssetLink{
                        MatchString = match.ToString(),
                        DownloadPath = match.Groups[1].ToString()
                    });
                }
            }
        }
    }

    public class AssetLink
    {
        public string MatchString { get; set; }
        public string DownloadPath { get; set; }
        public string LocalPath { get; set; }
    }
}
