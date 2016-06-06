using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LibUsbWin32.NuGetGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            MainInner().Wait();
        }

        static async Task MainInner()
        {
            var versions = new string[]
            {
                "1.2.1.0",
                "1.2.2.0",
                "1.2.3.0",
                "1.2.4.0",
                "1.2.5.0",
                "1.2.6.0"
            };

            foreach (var version in versions)
            {
                await MainInner(version);
            }
        }

        static async Task MainInner(string version)
        {
            Console.WriteLine($"Creating package for version {version}");

            // Clean up any previous download
            string packageDirectory = $"package-{version}";
            string sourceDirectory = $"libusb-win32-bin-{version}";
            string packagePath = $"libusbwin32-driver-{version}.nupkg";

            if (Directory.Exists(packageDirectory))
            {
                Directory.Delete(packageDirectory, true);
            }

            if (Directory.Exists(sourceDirectory))
            {
                Directory.Delete(sourceDirectory, true);
            }

            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }

            // Download and extract the latest version
            var client = new System.Net.Http.HttpClient();

            for (int i = 0; i < 3; i++)
            {
                // Not all SF mirrors may still hold the older versions
                try
                {
                    using (Stream stream = await client.GetStreamAsync($"http://downloads.sourceforge.net/project/libusb-win32/libusb-win32-releases/{version}/libusb-win32-bin-{version}.zip"))
                    using (ZipArchive archive = new ZipArchive(stream))
                    {
                        archive.ExtractToDirectory(".");
                    }
                }
                catch (Exception ex)
                {

                }

                break;
            }

            // Create the package folder
            Directory.CreateDirectory(packageDirectory);
            Directory.CreateDirectory($@"{packageDirectory}\amd64");
            Directory.CreateDirectory($@"{packageDirectory}\x86");

            // Copy the files
            File.Copy($@"{sourceDirectory}\bin\amd64\libusb0.dll", $@"{packageDirectory}\amd64\libusb0.dll");
            File.Copy($@"{sourceDirectory}\bin\amd64\libusb0.sys", $@"{packageDirectory}\amd64\libusb0.sys");

            File.Copy($@"{sourceDirectory}\bin\x86\libusb0_x86.dll", $@"{packageDirectory}\x86\libusb0_x86.dll");
            File.Copy($@"{sourceDirectory}\bin\x86\libusb0.sys", $@"{packageDirectory}\x86\libusb0.sys");

            // Generate the .nuspec file
            string packageTemplate = File.ReadAllText("libusbwin32-driver.nuspec");
            string nugetPackage = packageTemplate.Replace("{Version}", version);
            nugetPackage = nugetPackage.Replace("{Dir}", packageDirectory);

            File.WriteAllText(packagePath, nugetPackage);

            PackageBuilder builder = new PackageBuilder(packagePath, null, false);

            using (Stream stream = File.Open(packagePath, FileMode.Create, FileAccess.ReadWrite))
            {
                builder.Save(stream);
            }
        }
    }
}
