using System.Text;
using LeadMeLabsLibrary;
using LeadMeLabsLibrary.Station;

namespace Tests;

public class EncryptionHelperTests
{
    [Fact]
    public void TestCanDetectFileEncryption()
    {
        string utf8Text = EncryptionHelper.Utf8EncryptNode("this is an important secret");
        
        string path = "./EncryptionTest.txt";
        File.WriteAllText(path, utf8Text);

        string result = EncryptionHelper.DetectFileEncryption(path);
        
        Assert.Equal("this is an important secret", result);
    }
    
    [Fact]
    public void TestCanEncryptAndDecryptUtf8Node()
    {
        string text = EncryptionHelper.Utf8EncryptNode("this is an important secret");

        string result = EncryptionHelper.Utf8DecryptNode(text);
        
        Assert.Equal("this is an important secret", result);
    }
    
    [Fact]
    public void TestCanEncryptAndDecryptUnicodeNode()
    {
        string text = EncryptionHelper.UnicodeEncryptNode("this is an important secret");
        
        string result = EncryptionHelper.UnicodeDecryptNode(text);
        
        Assert.Equal("this is an important secret", result);
    }
    
    [Fact]
    public void TestCanEncryptAndDecryptUnicodeMessage()
    {
        string text = EncryptionHelper.UnicodeEncrypt("this is an important secret", "secret");
        
        string result = EncryptionHelper.UnicodeDecrypt(text, "secret");
        
        Assert.Equal("this is an important secret", result);
    }
    
    [Fact]
    public void TestCanEncryptAndDecrypUtf8Message()
    {
        string text = EncryptionHelper.Encrypt("this is an important secret", "secret");
        
        string result = EncryptionHelper.Decrypt(text, "secret");
        
        Assert.Equal("this is an important secret", result);
    }
}