using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 43 crown map generated from MandibularCanineTemplate
/// + right laterality. Runtime loads the packed JSON. Does not copy FDI 33 indices.
/// </summary>
internal static class Fdi43SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI43SurfaceMap.json";

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
        MandibularCanineTemplate.Generate(crown, ToothSide.Right);

    public static string GenerateDefault()
    {
        var obj43 = FindObj("FDI43_High.obj") ?? throw new FileNotFoundException("FDI43_High.obj not found.");
        var obj33 = FindObj("FDI33_High.obj") ?? throw new FileNotFoundException("FDI33_High.obj not found.");
        var json43 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj43)!, "..", "FDI43SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts33;
        StlMeshStats stats33;
        using (var fs = File.OpenRead(obj33))
            parts33 = StlToothLoader.LoadAlignedParts(
                fs, out stats33, MandibularCanineTemplate.LoadOptions(ToothSide.Left));
        var frozen33 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json43)!, "FDI33SurfaceMap.json"),
            parts33.Crown.TriangleIndices.Count / 3);
        if (frozen33 is null)
            throw new InvalidDataException("frozen FDI33SurfaceMap.json could not be read for laterality compare.");
        var layout33 = ToothSurfaceLayoutStats.Json("33", "frozen-readonly", parts33.Crown, frozen33);

        ToothMeshParts parts43;
        StlMeshStats stats43;
        using (var fs = File.OpenRead(obj43))
            parts43 = StlToothLoader.LoadAlignedParts(
                fs, out stats43, MandibularCanineTemplate.LoadOptions(ToothSide.Right));
        var map = Build(parts43.Crown);
        DumpMeans(parts43.Crown, map);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts43.Crown, map.TriangleSurface);
        var layout43 = ToothSurfaceLayoutStats.Json("43", "template-right", parts43.Crown, map.TriangleSurface);
        LogLaterality(layout33, layout43, stats33, stats43, own, red, parts43.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 15)
            throw new InvalidDataException(
                "FDI43 color 0 is not the cervical neck meanZ01=" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        if (stats43.CrownMeanZ < stats43.RootMeanZ)
            throw new InvalidDataException("FDI43 crown/root Z inverted.");
        Save(map, json43);
        var after = LogFrozenHashes("post");
        if (after.Map33 != frozen.Map33 || after.Obj33 != frozen.Obj33 ||
            after.Map13 != frozen.Map13 || after.Map23 != frozen.Map23 ||
            after.Map14 != frozen.Map14 || after.Map15 != frozen.Map15 ||
            after.Map24 != frozen.Map24 || after.Map25 != frozen.Map25 ||
            after.Map34 != frozen.Map34 || after.Map35 != frozen.Map35 ||
            after.Map44 != frozen.Map44 || after.Map45 != frozen.Map45 ||
            after.Map16 != frozen.Map16 || after.Map26 != frozen.Map26 ||
            after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj13 != frozen.Obj13 || after.Obj23 != frozen.Obj23 ||
            after.Obj14 != frozen.Obj14 || after.Obj15 != frozen.Obj15 ||
            after.Obj24 != frozen.Obj24 || after.Obj25 != frozen.Obj25 ||
            after.Obj34 != frozen.Obj34 || after.Obj35 != frozen.Obj35 ||
            after.Obj44 != frozen.Obj44 || after.Obj45 != frozen.Obj45 ||
            after.Obj16 != frozen.Obj16 || after.Obj26 != frozen.Obj26 ||
            after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved teeth were modified while generating FDI 43.");
        return json43;
    }

    private readonly record struct FrozenHashes(
        string Map13, string Map23, string Map33, string Map14, string Map15, string Map24, string Map25,
        string Map34, string Map35, string Map44, string Map45,
        string Map16, string Map26, string Map36, string Map46,
        string Obj13, string Obj23, string Obj33, string Obj14, string Obj15, string Obj24, string Obj25,
        string Obj34, string Obj35, string Obj44, string Obj45,
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
            HashOf("Assets/Teeth/FDI13SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI23SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI33SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI14SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI15SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI24SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI25SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI34SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI35SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI44SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI45SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI16SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI26SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI36SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI46SurfaceMap.json"),
            HashOf("Assets/Teeth/Source/FDI13_High.obj"),
            HashOf("Assets/Teeth/Source/FDI23_High.obj"),
            HashOf("Assets/Teeth/Source/FDI33_High.obj"),
            HashOf("Assets/Teeth/Source/FDI14_High.obj"),
            HashOf("Assets/Teeth/Source/FDI15_High.obj"),
            HashOf("Assets/Teeth/Source/FDI24_High.obj"),
            HashOf("Assets/Teeth/Source/FDI25_High.obj"),
            HashOf("Assets/Teeth/Source/FDI34_High.obj"),
            HashOf("Assets/Teeth/Source/FDI35_High.obj"),
            HashOf("Assets/Teeth/Source/FDI44_High.obj"),
            HashOf("Assets/Teeth/Source/FDI45_High.obj"),
            HashOf("Assets/Teeth/Source/FDI16_High.obj"),
            HashOf("Assets/Teeth/Source/FDI26_High.obj"),
            HashOf("Assets/Teeth/Source/FDI36_High.obj"),
            HashOf("Assets/Teeth/Source/FDI46_High.obj"));
        return hashes;
    }

    private static void LogLaterality(
        string layout33, string layout43, StlMeshStats stats33, StlMeshStats stats43,
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

        var m33 = MeanX(layout33, "mesial");
        var d33 = MeanX(layout33, "distal");
        var b33 = MeanY(layout33, "buccal");
        var l33 = MeanY(layout33, "inner");
        var m43 = MeanX(layout43, "mesial");
        var d43 = MeanX(layout43, "distal");
        var b43 = MeanY(layout43, "buccal");
        var l43 = MeanY(layout43, "inner");
        var ok33 = m33 > 0 && d33 < 0 && b33 > 0 && l33 < 0;
        var ok43 = m43 < 0 && d43 > 0 && b43 > 0 && l43 < 0;
        var ok = ok33 && ok43;
        var cancelled = Math.Abs(m43 - m33) < 0.05 && Math.Abs(d43 - d33) < 0.05;
        var cervical = red.Mean <= 0.40 && red.PctHigh <= 15;
        if (cancelled)
            throw new InvalidDataException("FDI 43 laterality cancelled: mesial/distal match FDI 33.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m33=" + m33 + " d33=" + d33 + " m43=" + m43 + " d43=" + d43);
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
        Console.WriteLine(One(0, "O") + " | " + One(1, "B") + " | " + One(2, "L") + " | " + One(3, "M") + " | " + One(4, "D"));
        Console.WriteLine("pct O=" + (100.0 * nn[0] / n).ToString("0.0") + " B=" + (100.0 * nn[1] / n).ToString("0.0") +
                          " L=" + (100.0 * nn[2] / n).ToString("0.0") + " M=" + (100.0 * nn[3] / n).ToString("0.0") +
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
            Mesh = "FDI43_High.obj",
            TriangleCount = n,
            Source = MandibularCanineTemplate.PipelineSource,
            Curated = map.Overrides.Count,
            Occlusal = map.Counts[0],
            Buccal = map.Counts[1],
            Lingual = map.Counts[2],
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
        if (!string.Equals(dto.Mesh, "FDI43_High.obj", StringComparison.OrdinalIgnoreCase))
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
        public int Lingual { get; set; }
        public int Mesial { get; set; }
        public int Distal { get; set; }
        public string Labels { get; set; } = "";
    }
}
