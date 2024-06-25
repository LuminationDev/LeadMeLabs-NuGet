using System.Text;
using LeadMeLabsLibrary;
using LeadMeLabsLibrary.Station;

namespace Tests.Station;

public class SteamAcfReaderTests
{
    [Fact]
    public void TestCanProcessSteamAcfFile()
    {
        string path = "./SteamAcfReaderTest.txt";
        File.WriteAllText(path, "\"AppState\"\n{\n\t\"appid\"\t\t\"250820\"\n\t\"universe\"\t\t\"1\"\n\t\"LauncherPath\"\t\t\"C:\\\\Program Files (x86)\\\\Steam\\\\steam.exe\"\n\t\"name\"\t\t\"SteamVR\"\n\t\"StateFlags\"\t\t\"4\"\n\t\"installdir\"\t\t\"SteamVR\"\n\t\"LastUpdated\"\t\t\"1718233530\"\n\t\"LastPlayed\"\t\t\"1719277995\"\n\t\"SizeOnDisk\"\t\t\"5465093268\"\n\t\"StagingSize\"\t\t\"0\"\n\t\"buildid\"\t\t\"14523237\"\n\t\"LastOwner\"\t\t\"76561199248976819\"\n\t\"UpdateResult\"\t\t\"0\"\n\t\"BytesToDownload\"\t\t\"1729760\"\n\t\"BytesDownloaded\"\t\t\"1729760\"\n\t\"BytesToStage\"\t\t\"81473531\"\n\t\"BytesStaged\"\t\t\"81473531\"\n\t\"TargetBuildID\"\t\t\"14523237\"\n\t\"AutoUpdateBehavior\"\t\t\"2\"\n\t\"AllowOtherDownloadsWhileRunning\"\t\t\"0\"\n\t\"ScheduledAutoUpdate\"\t\t\"0\"\n\t\"InstalledDepots\"\n\t{\n\t\t\"250821\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"7471168474304749345\"\n\t\t\t\"size\"\t\t\"972245754\"\n\t\t}\n\t\t\"250824\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"3080746044983302260\"\n\t\t\t\"size\"\t\t\"581519582\"\n\t\t}\n\t\t\"250825\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"6878307259591782433\"\n\t\t\t\"size\"\t\t\"0\"\n\t\t}\n\t\t\"250827\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"2950592113021695594\"\n\t\t\t\"size\"\t\t\"4820078\"\n\t\t}\n\t\t\"250830\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"7391958373704501609\"\n\t\t\t\"size\"\t\t\"3356665389\"\n\t\t}\n\t\t\"250831\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"4610675545635483043\"\n\t\t\t\"size\"\t\t\"342878509\"\n\t\t}\n\t\t\"250833\"\n\t\t{\n\t\t\t\"manifest\"\t\t\"8084172187417437723\"\n\t\t\t\"size\"\t\t\"206963956\"\n\t\t}\n\t}\n\t\"InstallScripts\"\n\t{\n\t\t\"250821\"\t\t\"installscript_817940.vdf\"\n\t\t\"250831\"\t\t\"tools\\\\steamvr_environments\\\\game\\\\steamtours\\\\install.vdf\"\n\t}\n\t\"SharedDepots\"\n\t{\n\t\t\"228983\"\t\t\"228980\"\n\t\t\"228985\"\t\t\"228980\"\n\t\t\"228986\"\t\t\"228980\"\n\t\t\"228987\"\t\t\"228980\"\n\t\t\"228990\"\t\t\"228980\"\n\t}\n\t\"UserConfig\"\n\t{\n\t\t\"language\"\t\t\"english\"\n\t}\n\t\"MountedConfig\"\n\t{\n\t\t\"language\"\t\t\"english\"\n\t}\n}\n");

        AcfReader acfReader = new AcfReader(path, true);
        acfReader.ACFFileToStruct();
        Assert.Equal("SteamVR", acfReader.gameName);
        Assert.Equal("250820", acfReader.appId);
    }
    
    [Fact]
    public void TestInvalidFileCausesException()
    {
        string path = "";

        Assert.Throws<FileNotFoundException>(() =>
        {
            AcfReader acfReader = new AcfReader("./FakeSteamAcfReaderTest.txt", true);
        });
    }
}