using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace LeadMeLabsLibrary
{
    public static class PasswordMessages
    {
        public const string TooShort = "Password is shorter than 14 characters";
        public const string Uppercase = "Password has less than 2 uppercase letters";
        public const string Lowercase = "Password has less than 2 lowercase letters";
        public const string Numbers = "Password has less than 2 numbers";
        public const string Years = "Password cannot contain numbers that would be a year between 1800 and 2199";
        public const string Triples = "Password cannot contain three of the same character in a row";
        public const string SerialNo = "Password cannot start with S\\N";
        public const string LabLocation = "Password cannot contain the first 4 letters of the lab location";
        public const string DateSlashes = "Password cannot contain todays date in dd/MM/YYYY format";
        public const string DateShort = "Password cannot contain todays date in ddMMYYYY format";
        public const string DateSlashesShortYear = "Password cannot contain todays date in dd/MM/YY format";
        public const string DateShortYear = "Password cannot contain todays date in ddMMYY format";
        public const string Acronym = "Password cannot contain the acryonym of the lab location";
        public const string Lumination = "Password cannot contain strings related to Lumination such as 'lum', 'lumi', 'lumin', 'lumination'";
        public const string Streets = "Password cannot contain Lumination address street names";
        public const string Password = "Password cannot contain 'pword' or 'password'";
    }
    
    public static class Helpers
    {
        public static List<string> GetPasswordValidityMessages(string password)
        {
            List<string> messages = new List<string>();
            if (password.Length < 14)
            {
                messages.Add(PasswordMessages.TooShort);
            }

            if (new Regex("[A-Z]").Matches(password).Count < 2)
            {
                messages.Add(PasswordMessages.Uppercase);
            }
            
            if (new Regex("[a-z]").Matches(password).Count < 2)
            {
                messages.Add(PasswordMessages.Lowercase);
            }
            
            if (new Regex("[0-9]").Matches(password).Count < 2)
            {
                messages.Add(PasswordMessages.Numbers);
            }
            
            if (new Regex("(?:(?:18|19|20|21)[0-9]{2})").IsMatch(password))
            {
                messages.Add(PasswordMessages.Years);
            }
            
            if (!new Regex("^(?:(.)(?!\\1{2}))+$").IsMatch(password))
            {
                messages.Add(PasswordMessages.Triples);
            }

            if (password.StartsWith("S\\N"))
            {
                messages.Add(PasswordMessages.SerialNo);
            }

            string labLocation =
                Environment.GetEnvironmentVariable("LabLocation", EnvironmentVariableTarget.Process) ?? "Unknown";
            if (password.ToLower().Contains(labLocation
                    .Substring(0, labLocation.Length > 4 ? 4 : labLocation.Length).ToLower()))
            {
                messages.Add(PasswordMessages.LabLocation);
            }

            if (password.Contains(DateTime.Now.ToString("dd/MM/yyyy")))
            {
                messages.Add(PasswordMessages.DateSlashes);
            }
            
            if (password.Contains(DateTime.Now.ToString("ddMMyyyy")))
            {
                messages.Add(PasswordMessages.DateShort);
            }
            
            if (password.Contains(DateTime.Now.ToString("dd/MM/yy")))
            {
                messages.Add(PasswordMessages.DateSlashesShortYear);
            }
            
            if (password.Contains(DateTime.Now.ToString("ddMMyy")))
            {
                messages.Add(PasswordMessages.DateShortYear);
            }
            
            var pattern = @"((?<=^|\s)(\w{1})|([A-Z]))";
            if (password.Contains(string.Join(string.Empty, Regex.Matches((Environment.GetEnvironmentVariable("LabLocation", EnvironmentVariableTarget.Process) ?? "Unknown"), pattern).OfType<Match>().Select(x => x.Value.ToUpper()))))
            {
                messages.Add(PasswordMessages.Acronym);
            }
            
            if (password.ToLower().Contains("lum") || password.ToLower().Contains("lumi") || password.ToLower().Contains("lumin") || password.ToLower().Contains("lumination"))
            {
                messages.Add(PasswordMessages.Lumination);
            }
            
            if (password.ToLower().Contains("kingwilliam") || password.ToLower().Contains("stirling"))
            {
                messages.Add(PasswordMessages.Streets);
            }
            
            if (password.ToLower().Contains("pword") || password.ToLower().Contains("password"))
            {
                messages.Add(PasswordMessages.Password);
            }

            return messages;
        }

        public static bool DoesPasswordMeetRequirements(string password)
        {
            return GetPasswordValidityMessages(password).Count == 0;
        }
    }
}
