using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 45 crown map generated from MandibularSecondPremolarTemplate
/// + right laterality. Runtime loads the packed JSON. Does not copy FDI 35 indices.
/// </summary>
internal static class Fdi45SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI45SurfaceMap.json";

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
        MandibularSecondPremolarTemplate.Generate(crown, ToothSide.Right);

    public static string GenerateDefault()
    {
        var obj45 = FindObj("FDI45_High.obj") ?? throw new FileNotFoundException("FDI45_High.obj not found.");
        var obj35 = FindObj("FDI35_High.obj") ?? throw new FileNotFoundException("FDI35_High.obj not found.");
        var json45 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj45)!, "..", "FDI45SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts35;
        StlMeshStats stats35;
        using (var fs = File.OpenRead(obj35))
            parts35 = StlToothLoader.LoadAlignedParts(
                fs, out stats35, MandibularSecondPremolarTemplate.LoadOptions(ToothSide.Left));
        var frozen35 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json45)!, "FDI35SurfaceMap.json"),
            parts35.Crown.TriangleIndices.Count / 3);
        if (frozen35 is null)
            throw new InvalidDataException("frozen FDI35SurfaceMap.json could not be read for laterality compare.");
        var layout35 = ToothSurfaceLayoutStats.Json("35", "frozen-readonly", parts35.Crown, frozen35);
        ToothSurfaceLayoutStats.Log("A", "35", "frozen-readonly", parts35.Crown, frozen35);
        ToothSurfaceLayoutStats.LogRedHeight("A", "35", parts35.Crown, frozen35);

        ToothMeshParts parts45;
        StlMeshStats stats45;
        using (var fs = File.OpenRead(obj45))
            parts45 = StlToothLoader.LoadAlignedParts(
                fs, out stats45, MandibularSecondPremolarTemplate.LoadOptions(ToothSide.Right));
        var map = Build(parts45.Crown);
        DumpMeans(parts45.Crown, map);
        ToothSurfaceLayoutStats.Log("A", "45", "template-right", parts45.Crown, map.TriangleSurface);
        ToothSurfaceTopology.LogAnalyze("A", "45", "template-right", parts45.Crown, map.TriangleSurface);
        ToothSurfaceLayoutStats.LogRedHeight("A", "45", parts45.Crown, map.TriangleSurface);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts45.Crown, map.TriangleSurface);
        var layout45 = ToothSurfaceLayoutStats.Json("45", "template-right", parts45.Crown, map.TriangleSurface);
        LogLaterality(layout35, layout45, stats35, stats45, own, red, parts45.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 15)
            throw new InvalidDataException(
                "FDI45 color 0 is not the cervical neck meanZ01=" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        if (stats45.CrownMeanZ < stats45.RootMeanZ)
            throw new InvalidDataException("FDI45 crown/root Z inverted.");
        Save(map, json45);
        var after = LogFrozenHashes("post");
        if (after.Map35 != frozen.Map35 || after.Obj35 != frozen.Obj35 ||
            after.Map14 != frozen.Map14 || after.Map15 != frozen.Map15 ||
            after.Map24 != frozen.Map24 || after.Map25 != frozen.Map25 ||
            after.Map34 != frozen.Map34 || after.Map44 != frozen.Map44 ||
            after.Map16 != frozen.Map16 || after.Map26 != frozen.Map26 ||
            after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj14 != frozen.Obj14 || after.Obj15 != frozen.Obj15 ||
            after.Obj24 != frozen.Obj24 || after.Obj25 != frozen.Obj25 ||
            after.Obj34 != frozen.Obj34 || after.Obj44 != frozen.Obj44 ||
            after.Obj16 != frozen.Obj16 || after.Obj26 != frozen.Obj26 ||
            after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved teeth were modified while generating FDI 45.");
        return json45;
    }

    private readonly record struct FrozenHashes(
        string Map14, string Map15, string Map24, string Map25, string Map34, string Map35, string Map44,
        string Map16, string Map26, string Map36, string Map46,
        string Obj14, string Obj15, string Obj24, string Obj25, string Obj34, string Obj35, string Obj44,
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
            HashOf("Assets/Teeth/FDI25SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI34SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI35SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI44SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI16SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI26SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI36SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI46SurfaceMap.json"),
            HashOf("Assets/Teeth/Source/FDI14_High.obj"),
            HashOf("Assets/Teeth/Source/FDI15_High.obj"),
            HashOf("Assets/Teeth/Source/FDI24_High.obj"),
            HashOf("Assets/Teeth/Source/FDI25_High.obj"),
            HashOf("Assets/Teeth/Source/FDI34_High.obj"),
            HashOf("Assets/Teeth/Source/FDI35_High.obj"),
            HashOf("Assets/Teeth/Source/FDI44_High.obj"),
            HashOf("Assets/Teeth/Source/FDI16_High.obj"),
            HashOf("Assets/Teeth/Source/FDI26_High.obj"),
            HashOf("Assets/Teeth/Source/FDI36_High.obj"),
            HashOf("Assets/Teeth/Source/FDI46_High.obj"));
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi45-template\",\"hypothesisId\":\"C\",\"location\":\"Fdi45SurfaceMapStore.cs\",\"message\":\"frozen-hashes\",\"data\":{\"when\":\"" +
                       when +
                       "\",\"map35\":\"" + hashes.Map35 +
                       "\",\"map15\":\"" + hashes.Map15 +
                       "\",\"map34\":\"" + hashes.Map34 +
                       "\",\"map16\":\"" + hashes.Map16 +
                       "\",\"obj35\":\"" + hashes.Obj35 +
                       "\"},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        return hashes;
    }

    private static void LogLaterality(
        string layout35, string layout45, StlMeshStats stats35, StlMeshStats stats45,
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

        var m35 = MeanX(layout35, "mesial");
        var d35 = MeanX(layout35, "distal");
        var b35 = MeanY(layout35, "buccal");
        var l35 = MeanY(layout35, "inner");
        var m45 = MeanX(layout45, "mesial");
        var d45 = MeanX(layout45, "distal");
        var b45 = MeanY(layout45, "buccal");
        var l45 = MeanY(layout45, "inner");
        var ok35 = m35 > 0 && d35 < 0 && b35 > 0 && l35 < 0;
        var ok45 = m45 < 0 && d45 > 0 && b45 > 0 && l45 < 0;
        var ok = ok35 && ok45;
        var cancelled = Math.Abs(m45 - m35) < 0.05 && Math.Abs(d45 - d35) < 0.05;
        var cervical = red.Mean <= 0.40 && red.PctHigh <= 15;
        // #region agent log
        try
        {
            var nTri = crown.TriangleIndices.Count / 3;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi45-template\",\"hypothesisId\":\"A\",\"location\":\"Fdi45SurfaceMapStore.cs\",\"message\":\"laterality\",\"data\":{" +
                       "\"mirrored35\":" + (stats35.Mirrored ? "true" : "false") +
                       ",\"mirrored45\":" + (stats45.Mirrored ? "true" : "false") +
                       ",\"yawDeg\":" + stats45.YawDeg.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dx\":" + stats45.Dx.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dy\":" + stats45.Dy.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dz\":" + stats45.Dz.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"crownMeanZ\":" + stats45.CrownMeanZ.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"rootMeanZ\":" + stats45.RootMeanZ.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"crownUp\":" + (stats45.CrownMeanZ > stats45.RootMeanZ ? "true" : "false") +
                       ",\"nTri\":" + nTri +
                       ",\"m35\":" + m35.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d35\":" + d35.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b35\":" + b35.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l35\":" + l35.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"m45\":" + m45.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d45\":" + d45.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b45\":" + b45.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l45\":" + l45.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"canonicalAxesOk\":" + (ok ? "true" : "false") +
                       ",\"lateralityCancelled\":" + (cancelled ? "true" : "false") +
                       ",\"copiedFdi35TriangleIds\":false" +
                       ",\"dup\":" + own.Dup +
                       ",\"unassigned\":" + own.Unassigned +
                       ",\"occMeanZ01\":" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"occPctLow\":" + red.PctLow.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"occPctHigh\":" + red.PctHigh.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"cervicalNeck\":" + (cervical ? "true" : "false") +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        if (cancelled)
            throw new InvalidDataException("FDI 45 laterality cancelled: mesial/distal match FDI 35.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m35=" + m35 + " d35=" + d35 + " m45=" + m45 + " d45=" + d45);
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
            Mesh = "FDI45_High.obj",
            TriangleCount = n,
            Source = MandibularSecondPremolarTemplate.PipelineSource,
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
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi45-template\",\"hypothesisId\":\"D\",\"location\":\"Fdi45SurfaceMapStore.cs\",\"message\":\"saved\",\"data\":{\"n\":" +
                       n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"curated\":" + map.Overrides.Count +
                       ",\"mesh\":\"FDI45_High.obj\",\"copiedFdi35TriangleIds\":false" +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
    }

    private static ClinicalSurfaceMap? Read(MeshGeometry3D crown, Stream stream)
    {
        var dto = JsonSerializer.Deserialize<Dto>(stream, JsonOpts);
        var nTri = crown.TriangleIndices.Count / 3;
        if (dto is null || dto.TriangleCount != nTri || string.IsNullOrEmpty(dto.Labels) || dto.Labels.Length != nTri)
            return null;
        if (!string.Equals(dto.Mesh, "FDI45_High.obj", StringComparison.OrdinalIgnoreCase))
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
