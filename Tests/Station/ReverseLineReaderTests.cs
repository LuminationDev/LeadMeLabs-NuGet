using System.Text;
using LeadMeLabsLibrary;
using LeadMeLabsLibrary.Station;

namespace Tests.Station;

public class ReverseLineReaderTests
{
    [Fact]
    public void TestCanReadFileInReverse()
    {
        string path = "./ReverseLineReaderTest.txt";
        File.WriteAllText(path, "Line1\nLine2\nLine3");

        ReverseLineReader reverseLineReader = new ReverseLineReader(path, Encoding.UTF8);
        using (IEnumerator<string?> enumerator = reverseLineReader.GetEnumerator())
        {
            enumerator.MoveNext();
            string? current = enumerator.Current;
            Assert.Equal("Line3", current);
            enumerator.MoveNext();
            current = enumerator.Current;
            Assert.Equal("Line2", current);
            enumerator.MoveNext();
            current = enumerator.Current;
            Assert.Equal("Line1", current);
            Assert.False(enumerator.MoveNext());
        }
    }
}