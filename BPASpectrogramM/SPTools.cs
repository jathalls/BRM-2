using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BPASpectrogramM
{
    public static class SPTools
    {
        /// <summary>
        /// Copies a file from the application's package to the application's data directory.
        /// </summary>
        /// <remarks>This method reads the specified file from the application's package and writes it to
        /// the application's data directory. If a file with the same name already exists in the target directory
        /// it will not be overwritten. The method returns the full path of the copied file.
        /// </remarks>
        /// <param name="filename">The name of the file to copy. This must be the name of a file located in the application's package.</param>
        /// <returns>The full path to the file in AppDataDirectory, or null if the file doesn't exist.</returns>
        public static async Task<string> CopyFileToAppDataDirectory(string filename)
        {
            // Create an output filename
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, filename);
            if (File.Exists(targetFile))
            {
                return targetFile;
            }

            try
            {
                // Try to get the embedded resource from BPASpectrogramM assembly
                var assembly = typeof(SPTools).Assembly;
                string resourceName = $"BPASpectrogramM.Resources.Raw.{filename}";
                
                using Stream inputStream = assembly.GetManifestResourceStream(resourceName);
                if (inputStream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SPTools] Embedded resource not found: {resourceName}");
                    return null;
                }

                // Copy the file to the AppDataDirectory
                using FileStream outputStream = File.Create(targetFile);
                await inputStream.CopyToAsync(outputStream);
                return targetFile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SPTools] Error copying file: {ex.Message}");
                return null;
            }
        }
    }
}
