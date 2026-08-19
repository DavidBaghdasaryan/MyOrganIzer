using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 44 crown map generated from MandibularFirstPremolarTemplate
/// + right laterality. Runtime loads the packed JSON. Does not copy FDI 34 indices.
/// </summary>
internal static class Fdi44SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI44SurfaceMap.json";

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
        MandibularFirstPremolarTemplate.Generate(crown, ToothSide.Right);

    public static string GenerateDefault()
    {
        var obj44 = FindObj("FDI44_High.obj") ?? throw new FileNotFoundException("FDI44_High.obj not found.");
        var obj34 = FindObj("FDI34_High.obj") ?? throw new FileNotFoundException("FDI34_High.obj not found.");
        var json44 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj44)!, "..", "FDI44SurfaceMap.json"));
        var frozen = LogFrozenHashes("pre");

        ToothMeshParts parts34;
        StlMeshStats stats34;
        using (var fs = File.OpenRead(obj34))
            parts34 = StlToothLoader.LoadAlignedParts(
                fs, out stats34, MandibularFirstPremolarTemplate.LoadOptions(ToothSide.Left));
        var frozen34 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json44)!, "FDI34SurfaceMap.json"),
            parts34.Crown.TriangleIndices.Count / 3);
        if (frozen34 is null)
            throw new InvalidDataException("frozen FDI34SurfaceMap.json could not be read for laterality compare.");
        var layout34 = ToothSurfaceLayoutStats.Json("34", "frozen-readonly", parts34.Crown, frozen34);
        ToothSurfaceLayoutStats.Log("A", "34", "frozen-readonly", parts34.Crown, frozen34);
        ToothSurfaceLayoutStats.LogRedHeight("A", "34", parts34.Crown, frozen34);

        ToothMeshParts parts44;
        StlMeshStats stats44;
        using (var fs = File.OpenRead(obj44))
            parts44 = StlToothLoader.LoadAlignedParts(
                fs, out stats44, MandibularFirstPremolarTemplate.LoadOptions(ToothSide.Right));
        var map = Build(parts44.Crown);
        DumpMeans(parts44.Crown, map);
        ToothSurfaceLayoutStats.Log("A", "44", "template-right", parts44.Crown, map.TriangleSurface);
        ToothSurfaceTopology.LogAnalyze("A", "44", "template-right", parts44.Crown, map.TriangleSurface);
        ToothSurfaceLayoutStats.LogRedHeight("A", "44", parts44.Crown, map.TriangleSurface);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts44.Crown, map.TriangleSurface);
        var layout44 = ToothSurfaceLayoutStats.Json("44", "template-right", parts44.Crown, map.TriangleSurface);
        LogLaterality(layout34, layout44, stats34, stats44, own, red, parts44.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 15)
            throw new InvalidDataException(
                "FDI44 color 0 is not the cervical neck meanZ01=" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        if (stats44.CrownMeanZ < stats44.RootMeanZ)
            throw new InvalidDataException("FDI44 crown/root Z inverted.");
        Save(map, json44);
        var after = LogFrozenHashes("post");
        if (after.Map34 != frozen.Map34 || after.Obj34 != frozen.Obj34 ||
            after.Map14 != frozen.Map14 || after.Map24 != frozen.Map24 ||
            after.Map16 != frozen.Map16 || after.Map26 != frozen.Map26 ||
            after.Map36 != frozen.Map36 || after.Map46 != frozen.Map46 ||
            after.Obj14 != frozen.Obj14 || after.Obj24 != frozen.Obj24 ||
            after.Obj16 != frozen.Obj16 || after.Obj26 != frozen.Obj26 ||
            after.Obj36 != frozen.Obj36 || after.Obj46 != frozen.Obj46)
            throw new InvalidDataException("approved 14/24/34/16/26/36/46 assets were modified while generating FDI 44.");
        return json44;
    }

    private readonly record struct FrozenHashes(
        string Map14, string Map24, string Map34, string Map16, string Map26, string Map36, string Map46,
        string Obj14, string Obj24, string Obj34, string Obj16, string Obj26, string Obj36, string Obj46);

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
            HashOf("Assets/Teeth/FDI24SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI34SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI16SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI26SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI36SurfaceMap.json"),
            HashOf("Assets/Teeth/FDI46SurfaceMap.json"),
            HashOf("Assets/Teeth/Source/FDI14_High.obj"),
            HashOf("Assets/Teeth/Source/FDI24_High.obj"),
            HashOf("Assets/Teeth/Source/FDI34_High.obj"),
            HashOf("Assets/Teeth/Source/FDI16_High.obj"),
            HashOf("Assets/Teeth/Source/FDI26_High.obj"),
            HashOf("Assets/Teeth/Source/FDI36_High.obj"),
            HashOf("Assets/Teeth/Source/FDI46_High.obj"));
        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi44-template\",\"hypothesisId\":\"C\",\"location\":\"Fdi44SurfaceMapStore.cs\",\"message\":\"frozen-hashes\",\"data\":{\"when\":\"" +
                       when +
                       "\",\"map34\":\"" + hashes.Map34 +
                       "\",\"map14\":\"" + hashes.Map14 +
                       "\",\"map24\":\"" + hashes.Map24 +
                       "\",\"map16\":\"" + hashes.Map16 +
                       "\",\"map36\":\"" + hashes.Map36 +
                       "\",\"obj34\":\"" + hashes.Obj34 +
                       "\"},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        return hashes;
    }

    private static void LogLaterality(
        string layout34, string layout44, StlMeshStats stats34, StlMeshStats stats44,
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

        var m34 = MeanX(layout34, "mesial");
        var d34 = MeanX(layout34, "distal");
        var b34 = MeanY(layout34, "buccal");
        var l34 = MeanY(layout34, "inner");
        var m44 = MeanX(layout44, "mesial");
        var d44 = MeanX(layout44, "distal");
        var b44 = MeanY(layout44, "buccal");
        var l44 = MeanY(layout44, "inner");
        var ok34 = m34 > 0 && d34 < 0 && b34 > 0 && l34 < 0;
        var ok44 = m44 < 0 && d44 > 0 && b44 > 0 && l44 < 0;
        var ok = ok34 && ok44;
        var cancelled = Math.Abs(m44 - m34) < 0.05 && Math.Abs(d44 - d34) < 0.05;
        var cervical = red.Mean <= 0.40 && red.PctHigh <= 15;
        // #region agent log
        try
        {
            var nTri = crown.TriangleIndices.Count / 3;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi44-template\",\"hypothesisId\":\"A\",\"location\":\"Fdi44SurfaceMapStore.cs\",\"message\":\"laterality\",\"data\":{" +
                       "\"mirrored34\":" + (stats34.Mirrored ? "true" : "false") +
                       ",\"mirrored44\":" + (stats44.Mirrored ? "true" : "false") +
                       ",\"yawDeg\":" + stats44.YawDeg.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dx\":" + stats44.Dx.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dy\":" + stats44.Dy.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dz\":" + stats44.Dz.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"crownMeanZ\":" + stats44.CrownMeanZ.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"rootMeanZ\":" + stats44.RootMeanZ.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"crownUp\":" + (stats44.CrownMeanZ > stats44.RootMeanZ ? "true" : "false") +
                       ",\"nTri\":" + nTri +
                       ",\"m34\":" + m34.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d34\":" + d34.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b34\":" + b34.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l34\":" + l34.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"m44\":" + m44.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d44\":" + d44.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b44\":" + b44.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l44\":" + l44.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"canonicalAxesOk\":" + (ok ? "true" : "false") +
                       ",\"lateralityCancelled\":" + (cancelled ? "true" : "false") +
                       ",\"copiedFdi34TriangleIds\":false" +
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
            throw new InvalidDataException("FDI 44 laterality cancelled: mesial/distal match FDI 34.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m34=" + m34 + " d34=" + d34 + " m44=" + m44 + " d44=" + d44);
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
            Mesh = "FDI44_High.obj",
            TriangleCount = n,
            Source = MandibularFirstPremolarTemplate.PipelineSource,
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
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi44-template\",\"hypothesisId\":\"D\",\"location\":\"Fdi44SurfaceMapStore.cs\",\"message\":\"saved\",\"data\":{\"n\":" +
                       n + ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"curated\":" + map.Overrides.Count +
                       ",\"mesh\":\"FDI44_High.obj\",\"copiedFdi34TriangleIds\":false" +
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
        if (!string.Equals(dto.Mesh, "FDI44_High.obj", StringComparison.OrdinalIgnoreCase))
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
