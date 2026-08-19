using System.IO;
using System.Text.Json;
using System.Windows.Media.Media3D;
using MyOrganizer.Wpf.Controls;

var repo = FindRepo() ?? throw new DirectoryNotFoundException("MyOrganizer repo not found.");
var map16 = Path.Combine(repo, "MyOrganizer.Wpf", "Assets", "Teeth", "FDI16SurfaceMap.json");
var map36 = Path.Combine(repo, "MyOrganizer.Wpf", "Assets", "Teeth", "FDI36SurfaceMap.json");
var obj16 = Path.Combine(repo, "MyOrganizer.Wpf", "Assets", "Teeth", "Source", "FDI16_High.obj");
var obj36 = Path.Combine(repo, "MyOrganizer.Wpf", "Assets", "Teeth", "Source", "FDI36_High.obj");

DumpStored("16", obj16, map16, new MeshLoadOptions { MirrorX = true, OrientFdi16 = true }, "golden-asset");
DumpStored("36", obj36, map36, new MeshLoadOptions
{
    MirrorX = false,
    OrientFdi16 = false,
    OrientationProfile = "MandibularFirstMolar"
}, "before-topology");

var path = Fdi36SurfaceMapStore.GenerateDefault();
Console.WriteLine("wrote " + path);

using (var fs = File.OpenRead(obj36))
{
    var parts = StlToothLoader.LoadAlignedParts(fs, out _, new MeshLoadOptions
    {
        MirrorX = false,
        OrientFdi16 = false,
        OrientationProfile = "MandibularFirstMolar"
    });
    var labels = LoadLabels(map36);
    ToothSurfaceLayoutStats.Log("A", "36", "normalized-topology", parts.Crown, labels);
    Console.WriteLine("new 36 " + ToothSurfaceLayoutStats.Json("36", "normalized-topology", parts.Crown, labels));
    Console.WriteLine("new 36 topology " + ToothSurfaceTopology.AnalyzeJson(parts.Crown, labels));
    var own = ToothSurfaceTopology.ValidateOwnership(labels);
    Console.WriteLine("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
}

static void DumpStored(string fdi, string obj, string json, MeshLoadOptions opt, string pipeline)
{
    using var fs = File.OpenRead(obj);
    var parts = StlToothLoader.LoadAlignedParts(fs, out _, opt);
    var labels = LoadLabels(json);
    if (labels.Length != parts.Crown.TriangleIndices.Count / 3)
        throw new InvalidDataException(fdi + " label count mismatch");
    ToothSurfaceLayoutStats.Log(fdi == "16" ? "C" : "A", fdi, pipeline, parts.Crown, labels);
    Console.WriteLine(fdi + " " + pipeline + " " + ToothSurfaceLayoutStats.Json(fdi, pipeline, parts.Crown, labels));
    Console.WriteLine(fdi + " topology " + ToothSurfaceTopology.AnalyzeJson(parts.Crown, labels));
}

static ClinicalSurface[] LoadLabels(string jsonPath)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
    var text = doc.RootElement.GetProperty("labels").GetString()
               ?? throw new InvalidDataException(jsonPath);
    var labels = new ClinicalSurface[text.Length];
    for (var i = 0; i < text.Length; i++)
        labels[i] = (ClinicalSurface)(text[i] - '0');
    return labels;
}

static string? FindRepo()
{
    var dir = AppContext.BaseDirectory;
    for (var i = 0; i < 12; i++)
    {
        if (File.Exists(Path.Combine(dir, "MyOrganizer.Wpf", "Assets", "Teeth", "FDI16SurfaceMap.json")))
            return dir;
        var parent = Directory.GetParent(dir);
        if (parent is null) break;
        dir = parent.FullName;
    }
    return null;
}
