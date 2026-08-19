using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 31 crown map generated from MandibularCentralIncisorTemplate
/// + left laterality. Runtime loads the packed JSON. Does not copy FDI 11/21/12/22 indices.
/// </summary>
internal static class Fdi31SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI31SurfaceMap.json";

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
        MandibularCentralIncisorTemplate.Generate(crown, ToothSide.Left);

    public static string GenerateDefault()
    {
        var obj31 = FindObj("FDI31_High.obj") ?? throw new FileNotFoundException("FDI31_High.obj not found.");
        var json31 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj31)!, "..", "FDI31SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts31;
        StlMeshStats stats31;
        using (var fs = File.OpenRead(obj31))
            parts31 = StlToothLoader.LoadAlignedParts(
                fs, out stats31, MandibularCentralIncisorTemplate.LoadOptions(ToothSide.Left));
        var map = Build(parts31.Crown);
        DumpMeans(parts31.Crown, map);
        ToothSurfaceLayoutStats.Log("A", "31", "template-left", parts31.Crown, map.TriangleSurface);
        ToothSurfaceTopology.LogAnalyze("A", "31", "template-left", parts31.Crown, map.TriangleSurface);
        ToothSurfaceLayoutStats.LogRedHeight("A", "31", parts31.Crown, map.TriangleSurface);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts31.Crown, map.TriangleSurface);
        var layout = ToothSurfaceLayoutStats.Json("31", "template-left", parts31.Crown, map.TriangleSurface);
        LogLayout(layout, stats31, own, red, parts31.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 15)
            throw new InvalidDataException(
                "FDI31 color 0 is not the cervical neck meanZ01=" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        if (stats31.CrownMeanZ < stats31.RootMeanZ)
            throw new InvalidDataException("FDI31 crown/root Z inverted.");
        Save(map, json31);
        var after = LogFrozenHashes("post");
        if (after.Map11 != frozen.Map11 || after.Map12 != frozen.Map12 ||
            after.Map21 != frozen.Map21 || after.Map22 != frozen.Map22 ||
            after.Map13 != frozen.Map13 || after.Map23 != frozen.Map23 ||
            after.Map33 != frozen.Map33 || after.Map43 != frozen.Map43 ||
            after.Map14 != frozen.Map14 || after.Map15 != frozen.Map15 ||
            after.Map24 != frozen.Map24 || after.Map25 != frozen.Map25 ||
            after.Map34 != frozen.Map34 || after.Map35 != frozen.Map35 ||
            after.Map44 != frozen.Map44 || after.Map45 != frozen.Map45 ||
            after.Map16 != frozen.Map16 || after.Map26 != frozen.Map26 ||
            after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj11 != frozen.Obj11 || after.Obj12 != frozen.Obj12 ||
            after.Obj21 != frozen.Obj21 || after.Obj22 != frozen.Obj22 ||
            after.Obj13 != frozen.Obj13 || after.Obj23 != frozen.Obj23 ||
            after.Obj33 != frozen.Obj33 || after.Obj43 != frozen.Obj43 ||
            after.Obj14 != frozen.Obj14 || after.Obj15 != frozen.Obj15 ||
            after.Obj24 != frozen.Obj24 || after.Obj25 != frozen.Obj25 ||
            after.Obj34 != frozen.Obj34 || after.Obj35 != frozen.Obj35 ||
            after.Obj44 != frozen.Obj44 || after.Obj45 != frozen.Obj45 ||
            after.Obj16 != frozen.Obj16 || after.Obj26 != frozen.Obj26 ||
            after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved teeth were modified while generating FDI 31.");
        return json31;
    }

    private readonly record struct FrozenHashes(
        string Map11, string Map12, string Map21, string Map22, string Map13, string Map23, string Map33, string Map43,
        string Map14, string Map15, string Map24, string Map25, string Map34, string Map35, string Map44, string Map45,
        string Map16, string Map26, string Map36, string Map46,
        string Obj11, string Obj12, string Obj21, string Obj22, string Obj13, string Obj23, string Obj33, string Obj43,
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
            HashOf("Assets/Teeth/FDI12SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI21SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI22SurfaceMap.json"),
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
            HashOf("Assets/Teeth/Source/FDI12_High.obj"),
            HashOf("Assets/Teeth/Source/FDI21_High.obj"),
            HashOf("Assets/Teeth/Source/FDI22_High.obj"),
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
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi31-template\",\"hypothesisId\":\"C\",\"location\":\"Fdi31SurfaceMapStore.cs\",\"message\":\"frozen-hashes\",\"data\":{\"when\":\"" +
                       when +
                       "\",\"map22\":\"" + hashes.Map22 +
                       "\",\"map12\":\"" + hashes.Map12 +
                       "\",\"map33\":\"" + hashes.Map33 +
                       "\",\"map16\":\"" + hashes.Map16 +
                       "\",\"obj22\":\"" + hashes.Obj22 +
                       "\"},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        return hashes;
    }

    private static void LogLayout(
        string layout, StlMeshStats stats31,
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

        var m = MeanX(layout, "mesial");
        var d = MeanX(layout, "distal");
        var b = MeanY(layout, "buccal");
        var l = MeanY(layout, "inner");
        var ok = m > 0 && d < 0 && b > 0 && l < 0;
        var cervical = red.Mean <= 0.40 && red.PctHigh <= 15;
        // #region agent log
        try
        {
            var nTri = crown.TriangleIndices.Count / 3;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi31-template\",\"hypothesisId\":\"A\",\"location\":\"Fdi31SurfaceMapStore.cs\",\"message\":\"laterality\",\"data\":{" +
                       "\"mirrored\":" + (stats31.Mirrored ? "true" : "false") +
                       ",\"yawDeg\":" + stats31.YawDeg.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dx\":" + stats31.Dx.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dy\":" + stats31.Dy.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dz\":" + stats31.Dz.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"crownMeanZ\":" + stats31.CrownMeanZ.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"rootMeanZ\":" + stats31.RootMeanZ.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"crownUp\":" + (stats31.CrownMeanZ > stats31.RootMeanZ ? "true" : "false") +
                       ",\"nTri\":" + nTri +
                       ",\"m31\":" + m.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d31\":" + d.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b31\":" + b.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l31\":" + l.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"canonicalAxesOk\":" + (ok ? "true" : "false") +
                       ",\"copiedMaxillaryIncisorTriangleIds\":false" +
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
        if (!ok)
            throw new InvalidDataException(
                "FDI31 axes failed m=" + m + " d=" + d + " b=" + b + " l=" + l);
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
            Mesh = "FDI31_High.obj",
            TriangleCount = n,
            Source = MandibularCentralIncisorTemplate.PipelineSource,
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
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi31-template\",\"hypothesisId\":\"D\",\"location\":\"Fdi31SurfaceMapStore.cs\",\"message\":\"saved\",\"data\":{\"n\":" +
                       n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"curated\":" + map.Overrides.Count +
                       ",\"mesh\":\"FDI31_High.obj\",\"copiedMaxillaryIncisorTriangleIds\":false" +
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
        if (!string.Equals(dto.Mesh, "FDI31_High.obj", StringComparison.OrdinalIgnoreCase))
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
