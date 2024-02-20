using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LeadMeLabsLibrary
{
    public static class Helpers
    {
        public static List<string> GetPasswordValidityMessages(string password)
        {
            List<string> messages = new List<string>();
            if (password.Length < 14)
            {
                messages.Add("Password is shorter than 14 characters");
            }

            if (new Regex("[A-Z]").Matches(password).Count < 2)
            {
                messages.Add("Password has less than 2 uppercase letters");
            }
            
            if (new Regex("[a-z]").Matches(password).Count < 2)
            {
                messages.Add("Password has less than 2 lowercase letters");
            }
            
            if (new Regex("[0-9]").Matches(password).Count < 2)
            {
                messages.Add("Password has less than 2 numbers");
            }
            
            if (new Regex("(?:(?:18|19|20|21)[0-9]{2})").IsMatch(password))
            {
                messages.Add("Password cannot contain numbers that would be a year between 1800 and 2199");
            }
            
            if (!new Regex("^(?:(.)(?!\\1{2}))+$").IsMatch(password))
            {
                messages.Add("Password cannot contain three of the same character in a row");
            }

            if (password.StartsWith("S\\N"))
            {
                messages.Add("Password cannot start with S\\N");
            }

            string labLocation =
                Environment.GetEnvironmentVariable("LabLocation", EnvironmentVariableTarget.Process) ?? "Unknown";
            if (password.ToLower().Contains(labLocation
                    .Substring(0, labLocation.Length > 4 ? 4 : labLocation.Length).ToLower()))
            {
                messages.Add("Password cannot contain the first 4 letters of the lab location");
            }

            if (password.Contains(DateTime.Now.ToString("dd/MM/yyyy")))
            {
                messages.Add("Password cannot contain todays date in dd/MM/YYYY format");
            }
            
            if (password.Contains(DateTime.Now.ToString("ddMMyyyy")))
            {
                messages.Add("Password cannot contain todays date in ddMMYYYY format");
            }
            
            if (password.Contains(DateTime.Now.ToString("dd/MM/yy")))
            {
                messages.Add("Password cannot contain todays date in dd/MM/YY format");
            }
            
            if (password.Contains(DateTime.Now.ToString("ddMMyy")))
            {
                messages.Add("Password cannot contain todays date in ddMMYY format");
            }
            
            var pattern = @"((?<=^|\s)(\w{1})|([A-Z]))";
            if (password.Contains(string.Join(string.Empty, Regex.Matches((Environment.GetEnvironmentVariable("LabLocation", EnvironmentVariableTarget.Process) ?? "Unknown"), pattern).OfType<Match>().Select(x => x.Value.ToUpper()))))
            {
                messages.Add("Password cannot contain the acryonym of the lab location");
            }
            
            if (password.ToLower().Contains("lum") || password.ToLower().Contains("lumi") || password.ToLower().Contains("lumin") || password.ToLower().Contains("lumination"))
            {
                messages.Add("Password cannot contain strings related to Lumination such as 'lum', 'lumi', 'lumin', 'lumination'");
            }
            
            if (password.ToLower().Contains("kingwilliam") || password.ToLower().Contains("stirling"))
            {
                messages.Add("Password cannot contain Lumination address street names");
            }
            
            if (password.ToLower().Contains("pword") || password.ToLower().Contains("password"))
            {
                messages.Add("Password cannot contain 'pword' or 'password'");
            }

            return messages;
        }

        public static bool DoesPasswordMeetRequirements(string password)
        {
            return GetPasswordValidityMessages(password).Count == 0;
        }
    }
}
