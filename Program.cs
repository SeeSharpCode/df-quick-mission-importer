using DFQuickMissionImporter.PFF;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using DFQuickMissionImporter;
using DFQuickMissionImporter.RTXT;

if (!File.Exists("df2.pff"))
{
    WriteError("Error: df2.pff not found, please place this program in your DF2 folder");
    return;
}

if (args.Length == 0)
{
    WriteError("Error: please supply a mission name");
    return;
}

Console.WriteLine("Backing up df2.pff to df2.pff.backup...");
File.Copy("df2.pff", "df2.pff.backup", true);

Console.WriteLine("Reading df2.pff...");
var pff = new PFFArchive("df2.pff");

var missionName = args[0].ToUpper();
var bmsFileName = $"{missionName}.BMS";
if (!File.Exists(bmsFileName))
{
    WriteError($"Error: {bmsFileName} does not exist");
    return;
}
var bms = File.ReadAllBytes(bmsFileName);
pff.Entries[bmsFileName] = new PFFEntry(bmsFileName, bms.Length, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), bms);

var metadataFileName = $"{missionName}.yaml";
if (!File.Exists(metadataFileName))
{
    WriteError($"Error: {metadataFileName} does not exist");
    return;
}

var metadataYaml = File.ReadAllText(metadataFileName);

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var metadata = deserializer.Deserialize<MissionMetadata>(metadataYaml);

Console.WriteLine("Reading df2brief.bin...");
var df2briefContents = pff.Entries["DF2BRIEF.BIN"].Contents;
var rtxt = new RTXTFile(df2briefContents);

Console.WriteLine("Adding briefings...");
rtxt.Entries["TITLE"][missionName] = new RTXTFile.StringValue(metadata.Title.ReplaceLineEndings());
rtxt.Entries["QMBRIEF"][missionName] = new RTXTFile.StringValue(metadata.QuickBriefing.ReplaceLineEndings());
rtxt.Entries["MBBRIEF"][missionName] = new RTXTFile.StringValue(metadata.LongBriefing.ReplaceLineEndings());

pff.Entries["DF2BRIEF.BIN"].Contents = rtxt.ToBytes();

var tgaFileName = $"{missionName}.TGA";
if (File.Exists(tgaFileName))
{
    Console.WriteLine("Packing TGA image...");
    var image = File.ReadAllBytes(tgaFileName);
    pff.Entries[tgaFileName] = new PFFEntry(tgaFileName, image.Length, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), image);
}
else
{
    Console.WriteLine($"{tgaFileName} not found, skipping image packing");
}

Console.WriteLine("Saving df2.pff...");
File.WriteAllBytes("df2.pff", pff.ToBytes());

Console.WriteLine($"Renaming {bmsFileName} to {bmsFileName}.packed since it is now packed in df2.pff...");
File.Move(bmsFileName, $"{bmsFileName}.packed");

Console.WriteLine("Successfully imported mission!");

static void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
}
