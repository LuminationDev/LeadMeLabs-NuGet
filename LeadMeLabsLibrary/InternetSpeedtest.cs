using System;
using System.Diagnostics;
using System.Net.Http;

namespace LeadMeLabsLibrary
{
    public static class InternetSpeedtest
    {
        public static double GetInternetSpeed()
        {
            if (!IsConnectedToInternet())
            {
                return -1;
            }
            
            var watch = new Stopwatch();
            
            byte[] data;
            using (var client = new System.Net.WebClient())
            {
                watch.Start();
                data = client.DownloadData("https://luminationdev.github.io/lumination-static-file-private.github.io/largestaticfile.txt");
                watch.Stop();
            }
            
            var speed = watch.Elapsed.TotalSeconds; // instead of [Seconds] property

            return ((data.LongLength / speed) / 1000000) * 8;
        }
        
        private static bool IsConnectedToInternet()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var response = httpClient.GetAsync("http://electronlauncher.herokuapp.com/static/electron-launcher/latest.yml").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
