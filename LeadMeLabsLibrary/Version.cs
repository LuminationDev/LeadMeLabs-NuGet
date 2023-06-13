using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LeadMeLabsDLL
{
    static class Version
    {
        /// <summary>
        /// Print out the currently running software version to a text file.
        /// </summary>
        public static bool GenerateVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;
            string programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            File.WriteAllText($"{programLocation}\\_logs\\version.txt", version);

            return version != null;
        }
    }
}
