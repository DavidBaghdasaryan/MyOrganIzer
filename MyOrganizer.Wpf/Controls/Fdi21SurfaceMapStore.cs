using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 21 crown map generated from MaxillaryCentralIncisorTemplate
/// + left laterality. Runtime loads the packed JSON. Does not copy FDI 11 indices.
/// </summary>
internal static class Fdi21SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI21SurfaceMap.json";

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
        MaxillaryCentralIncisorTemplate.Generate(crown, ToothSide.Left);

    public static string GenerateDefault()
    {
        var obj21 = FindObj("FDI21_High.obj") ?? throw new FileNotFoundException("FDI21_High.obj not found.");
        var obj11 = FindObj("FDI11_High.obj") ?? throw new FileNotFoundException("FDI11_High.obj not found.");
        var json21 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj21)!, "..", "FDI21SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts11;
        StlMeshStats stats11;
        using (var fs = File.OpenRead(obj11))
            parts11 = StlToothLoader.LoadAlignedParts(
                fs, out stats11, MaxillaryCentralIncisorTemplate.LoadOptions(ToothSide.Right));
        var frozen11 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json21)!, "FDI11SurfaceMap.json"),
            parts11.Crown.TriangleIndices.Count / 3);
        if (frozen11 is null)
            throw new InvalidDataException("frozen FDI11SurfaceMap.json could not be read for laterality compare.");
        var layout11 = ToothSurfaceLayoutStats.Json("11", "frozen-readonly", parts11.Crown, frozen11);

        ToothMeshParts parts21;
        StlMeshStats stats21;
        using (var fs = File.OpenRead(obj21))
            parts21 = StlToothLoader.LoadAlignedParts(
                fs, out stats21, MaxillaryCentralIncisorTemplate.LoadOptions(ToothSide.Left));
        var map = Build(parts21.Crown);
        DumpMeans(parts21.Crown, map);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts21.Crown, map.TriangleSurface);
        var layout21 = ToothSurfaceLayoutStats.Json("21", "template-left", parts21.Crown, map.TriangleSurface);
        LogLaterality(layout11, layout21, stats11, stats21, own, red, parts21.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 15)
            throw new InvalidDataException(
                "FDI21 color 0 is not the cervical neck meanZ01=" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        if (stats21.CrownMeanZ < stats21.RootMeanZ)
            throw new InvalidDataException("FDI21 crown/root Z inverted.");
        Save(map, json21);
        var after = LogFrozenHashes("post");
        if (after.Map11 != frozen.Map11 || after.Obj11 != frozen.Obj11 ||
            after.Map13 != frozen.Map13 || after.Map23 != frozen.Map23 ||
            after.Map33 != frozen.Map33 || after.Map43 != frozen.Map43 ||
            after.Map14 != frozen.Map14 || after.Map15 != frozen.Map15 ||
            after.Map24 != frozen.Map24 || after.Map25 != frozen.Map25 ||
            after.Map34 != frozen.Map34 || after.Map35 != frozen.Map35 ||
            after.Map44 != frozen.Map44 || after.Map45 != frozen.Map45 ||
            after.Map16 != frozen.Map16 || after.Map26 != frozen.Map26 ||
            after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj13 != frozen.Obj13 || after.Obj23 != frozen.Obj23 ||
            after.Obj33 != frozen.Obj33 || after.Obj43 != frozen.Obj43 ||
            after.Obj14 != frozen.Obj14 || after.Obj15 != frozen.Obj15 ||
            after.Obj24 != frozen.Obj24 || after.Obj25 != frozen.Obj25 ||
            after.Obj34 != frozen.Obj34 || after.Obj35 != frozen.Obj35 ||
            after.Obj44 != frozen.Obj44 || after.Obj45 != frozen.Obj45 ||
            after.Obj16 != frozen.Obj16 || after.Obj26 != frozen.Obj26 ||
            after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved teeth were modified while generating FDI 21.");
        return json21;
    }

    private readonly record struct FrozenHashes(
        string Map11, string Map13, string Map23, string Map33, string Map43,
        string Map14, string Map15, string Map24, string Map25, string Map34, string Map35, string Map44, string Map45,
        string Map16, string Map26, string Map36, string Map46,
        string Obj11, string Obj13, string Obj23, string Obj33, string Obj43,
        string Obj14, string Obj15, string Obj24, string Obj25, string Obj34, string Obj35, string Obj44, string Obj45,
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
            HashOf("Assets/Teeth/FDI11SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI13SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI23SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI33SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI43SurfaceMap.json"),
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
            HashOf("Assets/Teeth/Source/FDI11_High.obj"),
            HashOf("Assets/Teeth/Source/FDI13_High.obj"),
            HashOf("Assets/Teeth/Source/FDI23_High.obj"),
            HashOf("Assets/Teeth/Source/FDI33_High.obj"),
            HashOf("Assets/Teeth/Source/FDI43_High.obj"),
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
        string layout11, string layout21, StlMeshStats stats11, StlMeshStats stats21,
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

        var m11 = MeanX(layout11, "mesial");
        var d11 = MeanX(layout11, "distal");
        var b11 = MeanY(layout11, "buccal");
        var p11 = MeanY(layout11, "inner");
        var m21 = MeanX(layout21, "mesial");
        var d21 = MeanX(layout21, "distal");
        var b21 = MeanY(layout21, "buccal");
        var p21 = MeanY(layout21, "inner");
        var ok11 = m11 < 0 && d11 > 0 && b11 > 0 && p11 < 0;
        var ok21 = m21 > 0 && d21 < 0 && b21 > 0 && p21 < 0;
        var ok = ok11 && ok21;
        var cancelled = Math.Abs(m21 - m11) < 0.05 && Math.Abs(d21 - d11) < 0.05;
        var cervical = red.Mean <= 0.40 && red.PctHigh <= 15;
        if (cancelled)
            throw new InvalidDataException("FDI 21 laterality cancelled: mesial/distal match FDI 11.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m11=" + m11 + " d11=" + d11 + " m21=" + m21 + " d21=" + d21);
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
            Mesh = "FDI21_High.obj",
            TriangleCount = n,
            Source = MaxillaryCentralIncisorTemplate.PipelineSource,
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
        if (!string.Equals(dto.Mesh, "FDI21_High.obj", StringComparison.OrdinalIgnoreCase))
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
