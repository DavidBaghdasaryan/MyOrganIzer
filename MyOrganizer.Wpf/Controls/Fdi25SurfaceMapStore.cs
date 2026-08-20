using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 25 crown map generated from MaxillarySecondPremolarTemplate
/// + left laterality. Runtime loads the packed JSON. Does not copy FDI 15 indices.
/// </summary>
internal static class Fdi25SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI25SurfaceMap.json";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static ClinicalSurfaceMap? TryLoad(MeshGeometry3D crown)
    {
        try
        {
            var stream = Application.GetResourceStream(new Uri(PackUri, UriKind.Absolute))?.Stream;
            if (stream is null) return null;
            using (stream)
                return Read(crown, stream);
        }
        catch
        {
            return null;
        }
    }

    public static ClinicalSurfaceMap Build(MeshGeometry3D crown) =>
        MaxillarySecondPremolarTemplate.Generate(crown, ToothSide.Left);

    public static string GenerateDefault()
    {
        var obj25 = FindObj("FDI25_High.obj") ?? throw new FileNotFoundException("FDI25_High.obj not found.");
        var obj15 = FindObj("FDI15_High.obj") ?? throw new FileNotFoundException("FDI15_High.obj not found.");
        var json25 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj25)!, "..", "FDI25SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts15;
        StlMeshStats stats15;
        using (var fs = File.OpenRead(obj15))
            parts15 = StlToothLoader.LoadAlignedParts(
                fs, out stats15, MaxillarySecondPremolarTemplate.LoadOptions(ToothSide.Right));
        var frozen15 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json25)!, "FDI15SurfaceMap.json"),
            parts15.Crown.TriangleIndices.Count / 3);
        if (frozen15 is null)
            throw new InvalidDataException("frozen FDI15SurfaceMap.json could not be read for laterality compare.");
        var layout15 = ToothSurfaceLayoutStats.Json("15", "frozen-readonly", parts15.Crown, frozen15);

        ToothMeshParts parts25;
        StlMeshStats stats25;
        using (var fs = File.OpenRead(obj25))
            parts25 = StlToothLoader.LoadAlignedParts(
                fs, out stats25, MaxillarySecondPremolarTemplate.LoadOptions(ToothSide.Left));
        var map = Build(parts25.Crown);
        DumpMeans(parts25.Crown, map);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts25.Crown, map.TriangleSurface);
        var layout25 = ToothSurfaceLayoutStats.Json("25", "template-left", parts25.Crown, map.TriangleSurface);
        LogLaterality(layout15, layout25, stats15, stats25, own, red, parts25.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 15)
            throw new InvalidDataException(
                "FDI25 color 0 is not the cervical neck meanZ01=" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        if (stats25.CrownMeanZ < stats25.RootMeanZ)
            throw new InvalidDataException("FDI25 crown/root Z inverted.");
        Save(map, json25);
        var after = LogFrozenHashes("post");
        if (after.Map15 != frozen.Map15 || after.Obj15 != frozen.Obj15 ||
            after.Map14 != frozen.Map14 || after.Map24 != frozen.Map24 ||
            after.Map34 != frozen.Map34 || after.Map44 != frozen.Map44 ||
            after.Map16 != frozen.Map16 || after.Map26 != frozen.Map26 ||
            after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj14 != frozen.Obj14 || after.Obj24 != frozen.Obj24 ||
            after.Obj34 != frozen.Obj34 || after.Obj44 != frozen.Obj44 ||
            after.Obj16 != frozen.Obj16 || after.Obj26 != frozen.Obj26 ||
            after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved 14/15/24/34/44/16/26/36/46 assets were modified while generating FDI 25.");
        return json25;
    }

    private readonly record struct FrozenHashes(
        string Map14, string Map15, string Map24, string Map34, string Map44,
        string Map16, string Map26, string Map36, string Map46,
        string Obj14, string Obj15, string Obj24, string Obj34, string Obj44,
        string Obj16, string Obj26, string Obj36, string Obj46);

    private static FrozenHashes LogFrozenHashes(string when)
    {
        string HashOf(string relative)
        {
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 10; i++)
            {
                var a = Path.Combine(dir, "MyOrganizer.Wpf", relative);
                var b = Path.Combine(dir, relative);
                var path = File.Exists(a) ? a : File.Exists(b) ? b : null;
                if (path is not null)
                {
                    using var sha = SHA256.Create();
                    using var fs = File.OpenRead(path);
                    return Convert.ToHexString(sha.ComputeHash(fs));
                }
                var parent = Directory.GetParent(dir);
                if (parent is null) break;
                dir = parent.FullName;
            }
            return "missing";
        }

        var hashes = new FrozenHashes(
            HashOf("Assets/Teeth/FDI14SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI15SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI24SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI34SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI44SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI16SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI26SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI36SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI46SurfaceMap.json"),
            HashOf("Assets/Teeth/Source/FDI14_High.obj"),
            HashOf("Assets/Teeth/Source/FDI15_High.obj"),
            HashOf("Assets/Teeth/Source/FDI24_High.obj"),
            HashOf("Assets/Teeth/Source/FDI34_High.obj"),
            HashOf("Assets/Teeth/Source/FDI44_High.obj"),
            HashOf("Assets/Teeth/Source/FDI16_High.obj"),
            HashOf("Assets/Teeth/Source/FDI26_High.obj"),
            HashOf("Assets/Teeth/Source/FDI36_High.obj"),
            HashOf("Assets/Teeth/Source/FDI46_High.obj"));
        return hashes;
    }

    private static void LogLaterality(
        string layout15, string layout25, StlMeshStats stats15, StlMeshStats stats25,
        (int Dup, int Unassigned) own, ToothSurfaceLayoutStats.RedHeight red, MeshGeometry3D crown)
    {
        static double MeanX(string json, string key)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty(key).GetProperty("meanX").GetDouble();
        }
        static double MeanY(string json, string key)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty(key).GetProperty("meanY").GetDouble();
        }

        var m15 = MeanX(layout15, "mesial");
        var d15 = MeanX(layout15, "distal");
        var b15 = MeanY(layout15, "buccal");
        var p15 = MeanY(layout15, "inner");
        var m25 = MeanX(layout25, "mesial");
        var d25 = MeanX(layout25, "distal");
        var b25 = MeanY(layout25, "buccal");
        var p25 = MeanY(layout25, "inner");
        var ok15 = m15 < 0 && d15 > 0 && b15 > 0 && p15 < 0;
        var ok25 = m25 > 0 && d25 < 0 && b25 > 0 && p25 < 0;
        var ok = ok15 && ok25;
        var cancelled = Math.Abs(m25 - m15) < 0.05 && Math.Abs(d25 - d15) < 0.05;
        var cervical = red.Mean <= 0.40 && red.PctHigh <= 15;
        if (cancelled)
            throw new InvalidDataException("FDI 25 laterality cancelled: mesial/distal match FDI 15.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m15=" + m15 + " d15=" + d15 + " m25=" + m25 + " d25=" + d25);
    }

    private static ClinicalSurface[]? ReadLabelsFromFile(string path, int nTri)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var dto = JsonSerializer.Deserialize<Dto>(stream, JsonOpts);
            if (dto?.Labels is null || dto.Labels.Length != nTri)
                return null;
            var labels = new ClinicalSurface[nTri];
            for (var i = 0; i < nTri; i++)
                labels[i] = (ClinicalSurface)(dto.Labels[i] - '0');
            return labels;
        }
        catch
        {
            return null;
        }
    }

    private static void DumpMeans(MeshGeometry3D crown, ClinicalSurfaceMap map)
    {
        var idx = crown.TriangleIndices;
        var n = map.TriangleSurface.Length;
        var sx = new double[5];
        var sy = new double[5];
        var sz = new double[5];
        var nn = new int[5];
        for (var t = 0; t < n; t++)
        {
            var a = crown.Positions[idx[t * 3]];
            var b = crown.Positions[idx[t * 3 + 1]];
            var c = crown.Positions[idx[t * 3 + 2]];
            var s = (int)map.SurfaceOf(t);
            sx[s] += (a.X + b.X + c.X) / 3.0;
            sy[s] += (a.Y + b.Y + c.Y) / 3.0;
            sz[s] += (a.Z + b.Z + c.Z) / 3.0;
            nn[s]++;
        }
        string One(int s, string name) =>
            name + "=" + nn[s] + " mean=" +
            (nn[s] == 0 ? "n/a" : $"{sx[s] / nn[s]:0.03},{sy[s] / nn[s]:0.03},{sz[s] / nn[s]:0.03}");
        Console.WriteLine(One(0, "O") + " | " + One(1, "B") + " | " + One(2, "P") + " | " + One(3, "M") + " | " + One(4, "D"));
        Console.WriteLine("pct O=" + (100.0 * nn[0] / n).ToString("0.0") + " B=" + (100.0 * nn[1] / n).ToString("0.0") +
                          " P=" + (100.0 * nn[2] / n).ToString("0.0") + " M=" + (100.0 * nn[3] / n).ToString("0.0") +
                          " D=" + (100.0 * nn[4] / n).ToString("0.0"));
    }

    public static void Save(ClinicalSurfaceMap map, string path)
    {
        var n = map.TriangleSurface.Length;
        var labels = new char[n];
        for (var i = 0; i < n; i++)
            labels[i] = (char)('0' + (int)map.SurfaceOf(i));
        var dto = new Dto
        {
            Mesh = "FDI25_High.obj",
            TriangleCount = n,
            Source = MaxillarySecondPremolarTemplate.PipelineSource,
            Curated = map.Overrides.Count,
            Occlusal = map.Counts[0],
            Buccal = map.Counts[1],
            Palatal = map.Counts[2],
            Mesial = map.Counts[3],
            Distal = map.Counts[4],
            Labels = new string(labels)
        };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOpts));
    }

    private static ClinicalSurfaceMap? Read(MeshGeometry3D crown, Stream stream)
    {
        var dto = JsonSerializer.Deserialize<Dto>(stream, JsonOpts);
        var nTri = crown.TriangleIndices.Count / 3;
        if (dto is null || dto.TriangleCount != nTri || string.IsNullOrEmpty(dto.Labels) || dto.Labels.Length != nTri)
            return null;
        if (!string.Equals(dto.Mesh, "FDI25_High.obj", StringComparison.OrdinalIgnoreCase))
            return null;
        var labels = new ClinicalSurface[nTri];
        var counts = new int[5];
        for (var i = 0; i < nTri; i++)
        {
            var s = dto.Labels[i] - '0';
            if ((uint)s > 4) return null;
            labels[i] = (ClinicalSurface)s;
            counts[s]++;
        }
        return new ClinicalSurfaceMap
        {
            SourceCrown = crown,
            TriangleSurface = labels,
            OcclusalDirection = new Vector3D(0, 0, 1),
            Counts = counts
        };
    }

    private static string? FindObj(string fileName)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var a = Path.Combine(dir, "Assets", "Teeth", "Source", fileName);
            var b = Path.Combine(dir, "MyOrganizer.Wpf", "Assets", "Teeth", "Source", fileName);
            if (File.Exists(a)) return a;
            if (File.Exists(b)) return b;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return null;
    }

    private sealed class Dto
    {
        public string Mesh { get; set; } = "";
        public int TriangleCount { get; set; }
        public string Source { get; set; } = "";
        public int Curated { get; set; }
        public int Occlusal { get; set; }
        public int Buccal { get; set; }
        public int Palatal { get; set; }
        public int Mesial { get; set; }
        public int Distal { get; set; }
        public string Labels { get; set; } = "";
    }
}
