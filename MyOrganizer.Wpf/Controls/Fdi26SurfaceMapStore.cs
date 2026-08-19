using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 26 crown map generated from MaxillaryFirstMolarTemplate
/// + left laterality. Runtime loads the packed JSON. Does not copy FDI 16 indices.
/// </summary>
internal static class Fdi26SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI26SurfaceMap.json";

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
        MaxillaryFirstMolarTemplate.Generate(crown, ToothSide.Left);

    public static string GenerateDefault()
    {
        var obj26 = FindObj("FDI26_High.obj") ?? throw new FileNotFoundException("FDI26_High.obj not found.");
        var obj16 = FindObj("FDI16_High.obj") ?? throw new FileNotFoundException("FDI16_High.obj not found.");
        var json26 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj26)!, "..", "FDI26SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts16;
        using (var fs = File.OpenRead(obj16))
            parts16 = StlToothLoader.LoadAlignedParts(fs, out _, new MeshLoadOptions
            {
                MirrorX = true,
                OrientFdi16 = true,
                OrientationProfile = "ApprovedFdi16"
            });
        var frozen16 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json26)!, "FDI16SurfaceMap.json"),
            parts16.Crown.TriangleIndices.Count / 3);
        if (frozen16 is null)
            throw new InvalidDataException("frozen FDI16SurfaceMap.json could not be read for laterality compare.");
        var layout16 = ToothSurfaceLayoutStats.Json("16", "frozen-readonly", parts16.Crown, frozen16);
        ToothSurfaceLayoutStats.Log("A", "16", "frozen-readonly", parts16.Crown, frozen16);
        ToothSurfaceLayoutStats.LogRedHeight("A", "16", parts16.Crown, frozen16);

        ToothMeshParts parts26;
        StlMeshStats stats26;
        using (var fs = File.OpenRead(obj26))
            parts26 = StlToothLoader.LoadAlignedParts(fs, out stats26, MaxillaryFirstMolarTemplate.LoadOptions(ToothSide.Left));
        var map = Build(parts26.Crown);
        DumpMeans(parts26.Crown, map);
        ToothSurfaceLayoutStats.Log("A", "26", "template-left", parts26.Crown, map.TriangleSurface);
        ToothSurfaceTopology.LogAnalyze("A", "26", "template-left", parts26.Crown, map.TriangleSurface);
        ToothSurfaceLayoutStats.LogRedHeight("A", "26", parts26.Crown, map.TriangleSurface);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts26.Crown, map.TriangleSurface);
        var layout26 = ToothSurfaceLayoutStats.Json("26", "template-left", parts26.Crown, map.TriangleSurface);
        LogLaterality(layout16, layout26, stats26, own, red, parts26.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean < 0.65 || red.PctLow > 10)
            throw new InvalidDataException(
                "FDI26 RED is not high-cervical meanZ01=" + red.Mean.ToString("0.333") +
                " pctLow=" + red.PctLow.ToString("0.0"));
        Save(map, json26);
        var after = LogFrozenHashes("post");
        if (after.Map16 != frozen.Map16 || after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj16 != frozen.Obj16 || after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved 16/36/46 assets were modified while generating FDI 26.");
        return json26;
    }

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
            HashOf("Assets/Teeth/FDI16SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI36SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI46SurfaceMap.json"),
            HashOf("Assets/Teeth/Source/FDI16_High.obj"),
            HashOf("Assets/Teeth/Source/FDI36_High.obj"),
            HashOf("Assets/Teeth/Source/FDI46_High.obj"));
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi26-template\",\"hypothesisId\":\"E\",\"location\":\"Fdi26SurfaceMapStore.cs\",\"message\":\"frozen-hashes\",\"data\":{\"when\":\"" +
                       when +
                       "\",\"map16\":\"" + hashes.Map16 +
                       "\",\"map36\":\"" + hashes.Map36 +
                       "\",\"map46\":\"" + hashes.Map46 +
                       "\",\"obj16\":\"" + hashes.Obj16 +
                       "\",\"obj36\":\"" + hashes.Obj36 +
                       "\",\"obj46\":\"" + hashes.Obj46 +
                       "\"},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        return hashes;
    }

    private readonly record struct FrozenHashes(
        string Map16, string Map36, string Map46, string Obj16, string Obj36, string Obj46);

    private static void LogLaterality(
        string layout16, string layout26, StlMeshStats stats26,
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

        var m16 = MeanX(layout16, "mesial");
        var d16 = MeanX(layout16, "distal");
        var b16 = MeanY(layout16, "buccal");
        var p16 = MeanY(layout16, "inner");
        var m26 = MeanX(layout26, "mesial");
        var d26 = MeanX(layout26, "distal");
        var b26 = MeanY(layout26, "buccal");
        var p26 = MeanY(layout26, "inner");
        var ok =
            m16 > 0 && d16 < 0 && b16 > 0 && p16 < 0 &&
            m26 < 0 && d26 > 0 && b26 > 0 && p26 < 0;
        var cancelled = Math.Abs(m26 - m16) < 0.05 && Math.Abs(d26 - d16) < 0.05;
        // #region agent log
        try
        {
            var nTri = crown.TriangleIndices.Count / 3;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi26-template\",\"hypothesisId\":\"A\",\"location\":\"Fdi26SurfaceMapStore.cs\",\"message\":\"laterality\",\"data\":{" +
                       "\"mirrored\":" + (stats26.Mirrored ? "true" : "false") +
                       ",\"yawDeg\":" + stats26.YawDeg.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dx\":" + stats26.Dx.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dy\":" + stats26.Dy.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dz\":" + stats26.Dz.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"nTri\":" + nTri +
                       ",\"m16\":" + m16.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d16\":" + d16.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b16\":" + b16.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"p16\":" + p16.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"m26\":" + m26.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d26\":" + d26.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b26\":" + b26.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"p26\":" + p26.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"canonicalAxesOk\":" + (ok ? "true" : "false") +
                       ",\"lateralityCancelled\":" + (cancelled ? "true" : "false") +
                       ",\"dup\":" + own.Dup +
                       ",\"unassigned\":" + own.Unassigned +
                       ",\"redMeanZ01\":" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"redPctLow\":" + red.PctLow.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"redPctHigh\":" + red.PctHigh.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"highCervicalZone\":" + (red.Mean >= 0.65 && red.PctLow <= 10 ? "true" : "false") +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        if (cancelled)
            throw new InvalidDataException("FDI 26 laterality cancelled: mesial/distal match FDI 16.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m16=" + m16 + " d16=" + d16 + " m26=" + m26 + " d26=" + d26);
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
            Mesh = "FDI26_High.obj",
            TriangleCount = n,
            Source = MaxillaryFirstMolarTemplate.PipelineSource,
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
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi26-template\",\"hypothesisId\":\"C\",\"location\":\"Fdi26SurfaceMapStore.cs\",\"message\":\"saved\",\"data\":{\"path\":\"" +
                       path.Replace("\\", "\\\\") + "\",\"n\":" + n +
                       ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"palatal\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"curated\":" + map.Overrides.Count +
                       ",\"mesh\":\"FDI26_High.obj\"" +
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
        if (!string.Equals(dto.Mesh, "FDI26_High.obj", StringComparison.OrdinalIgnoreCase))
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
