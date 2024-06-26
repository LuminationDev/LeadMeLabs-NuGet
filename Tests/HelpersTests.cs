using LeadMeLabsLibrary;

namespace Tests;

public class HelpersTests
{
    [Fact]
    public void TestGetPasswordValidityMessagesGetsAllMessages()
    {
        // Test TooShort
        List<string> resultTooShort = Helpers.GetPasswordValidityMessages("aBC45fhijklmn");
        Assert.Collection(resultTooShort, e => Assert.Equal(PasswordMessages.TooShort, e));
       
        // Test Uppercase
        List<string> resultUppercase = Helpers.GetPasswordValidityMessages("abc45fhijklmnO");
        Assert.Collection(resultUppercase, e => Assert.Equal(PasswordMessages.Uppercase, e));
        
        // Test Lowercase
        List<string> resultLowercase = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLMNo");
        Assert.Collection(resultLowercase, e => Assert.Equal(PasswordMessages.Lowercase, e));
        
        // Test Numbers
        List<string> resultNumbers = Helpers.GetPasswordValidityMessages("ABCD5FGHIJKLMno");
        Assert.Collection(resultNumbers, e => Assert.Equal(PasswordMessages.Numbers, e));
       
        // Test Years
        List<string> resultYears = Helpers.GetPasswordValidityMessages("aBC45FGHIJKLMNo1990");
        Assert.Collection(resultYears, e => Assert.Equal(PasswordMessages.Years, e));
       
        // Test Triples
        List<string> resultTriples = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLooo");
        Assert.Collection(resultTriples, e => Assert.Equal(PasswordMessages.Triples, e));
        
        // Test SerialNo
        List<string> resultSerialNo = Helpers.GetPasswordValidityMessages("S\\NABC45FGHIJKLMno");
        Assert.Collection(resultSerialNo, e => Assert.Equal(PasswordMessages.SerialNo, e));
        
        // Test LabLocation
        Environment.SetEnvironmentVariable("LabLocation", "MyComputer", EnvironmentVariableTarget.Process);
        List<string> resultLabLocation = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLoMyComputer");
        Assert.Collection(resultLabLocation, e => Assert.Equal(PasswordMessages.LabLocation, e));
        Environment.SetEnvironmentVariable("LabLocation", "AComputer", EnvironmentVariableTarget.Process);
        List<string> resultLabLocation2 = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLoMyComputer");
        Assert.Empty(resultLabLocation2);
        
        // Test DateSlashes
        List<string> resultDateSlashes = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmno" + DateTime.Now.ToString("dd/MM/yyyy"));
        Assert.Collection(resultDateSlashes, 
            e => Assert.Equal(PasswordMessages.Years, e),
            e => Assert.Equal(PasswordMessages.DateSlashes, e)
        );
       
        // Test DateShort
        List<string> resultDateShort = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmno" + DateTime.Now.ToString("ddMMyyyy"));
        Assert.Collection(resultDateShort, 
            e => Assert.Equal(PasswordMessages.Years, e),
            e => Assert.Equal(PasswordMessages.DateShort, e)
            );
        
        // Test DateSlashesShortYear
        List<string> resultDateSlashesShortYear = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmno" + DateTime.Now.ToString("dd/MM/yy"));
        Assert.Collection(resultDateSlashesShortYear, e => Assert.Equal(PasswordMessages.DateSlashesShortYear, e));
        
        // Test DateShortYear
        List<string> resultDateShortYear = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmno" + DateTime.Now.ToString("ddMMyy"));
        Assert.Collection(resultDateShortYear, e => Assert.Equal(PasswordMessages.DateShortYear, e));
        
        // Test Acronym
        Environment.SetEnvironmentVariable("LabLocation", "MyComputer", EnvironmentVariableTarget.Process);
        List<string> resultAcronym = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnoMC");
        Assert.Collection(resultAcronym, e => Assert.Equal(PasswordMessages.Acronym, e));
        Environment.SetEnvironmentVariable("LabLocation", "AComputer", EnvironmentVariableTarget.Process);
        List<string> resultAcronym2 = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnoMC");
        Assert.Empty(resultAcronym2);
        
        // Test Lumination
        List<string> resultLumination = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnolum");
        Assert.Collection(resultLumination, e => Assert.Equal(PasswordMessages.Lumination, e));
        List<string> resultLumination2 = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnolumi");
        Assert.Collection(resultLumination2, e => Assert.Equal(PasswordMessages.Lumination, e));
        List<string> resultLumination3 = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnolumination");
        Assert.Collection(resultLumination3, e => Assert.Equal(PasswordMessages.Lumination, e));
        
        // Test Streets
        List<string> resultStreets = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnostirling");
        Assert.Collection(resultStreets, e => Assert.Equal(PasswordMessages.Streets, e));
        List<string> resultStreets2 = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnokingwilliam");
        Assert.Collection(resultStreets2, e => Assert.Equal(PasswordMessages.Streets, e));
        
        // Test Password
        List<string> resultPassword = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnopword");
        Assert.Collection(resultPassword, e => Assert.Equal(PasswordMessages.Password, e));
        List<string> resultPassword2 = Helpers.GetPasswordValidityMessages("ABC45FGHIJKLmnopassword");
        Assert.Collection(resultPassword2, e => Assert.Equal(PasswordMessages.Password, e));
    }
}