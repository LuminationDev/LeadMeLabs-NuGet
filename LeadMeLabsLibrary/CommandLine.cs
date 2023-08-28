using System.Diagnostics;

namespace LeadMeLabsLibrary
{
    public static class CommandLine
    {
        /// <summary>
        /// Collect just the IPv4 address, filter out any virtual boxes that may be on the system.
        /// </summary>
        /// <returns>An IPAddress object of the local IP Address</returns>
        public static string? RunCMDWithOutput(string command)
        {
            Process cmd = BuildCMD();
            cmd.Start();
            cmd.StandardInput.WriteLine(command);
            return Outcome(cmd);
        }

        public static Process BuildCMD()
        {
            Process temp = new();
            temp.StartInfo.FileName = "cmd.exe";
            temp.StartInfo.RedirectStandardInput = true;
            temp.StartInfo.RedirectStandardError = true;
            temp.StartInfo.RedirectStandardOutput = true;
            temp.StartInfo.CreateNoWindow = true;
            temp.StartInfo.UseShellExecute = false;
            return temp;
        }
        
        /// <summary>
        /// Determine the outcome of the command action, by creating event handlers to combine output data as it's 
        /// received. This stops buffers from overflowing which leads to thread hanging. It also determines if an error 
        /// has occurred or the operation ran as expected.
        /// </summary>
        /// <param name="temp">A Process that represents a current command process that has been executed.</param>
        /// <returns>A string representing the output or error from the command prompt.</returns>
        private static string? Outcome(Process temp)
        {
            string output = "";
            string error = "";

            temp.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    output += e.Data + "\n";
                }
            );

            temp.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    error += e.Data + "\n";
                }
            );

            temp.BeginOutputReadLine();
            temp.BeginErrorReadLine();
            temp.StandardInput.Flush();
            temp.StandardInput.Close();
            temp.WaitForExit();

            if (error != null)
            {
                return output;
            }
            else
            {
                return error;
            }
        }
    }
}
