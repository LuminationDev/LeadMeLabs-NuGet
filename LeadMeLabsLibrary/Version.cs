using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LeadMeLabsLibrary
{
    public static class Version
    {
        /// <summary>
        /// Print out the currently running software version to a text file at 'programLocation\_logs\version.txt'. 
        /// If the version number or program directory cannot be found the function returns false, bailing out before
        /// writing the version. 
        /// </summary>
        public static bool GenerateVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string? version = fileVersionInfo.ProductVersion;
            string? programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if(version == null || programLocation == null)
            {
                return false;
            }

            File.WriteAllText($"{programLocation}\\_logs\\version.txt", version);

            return version != null;
        }
    }
}
