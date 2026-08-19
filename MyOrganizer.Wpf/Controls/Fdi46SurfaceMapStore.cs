using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Mesh-specific FDI 46 crown map generated from MandibularFirstMolarTemplate
/// + right laterality. Runtime loads the packed JSON. Does not copy FDI 36 indices.
/// </summary>
internal static class Fdi46SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI46SurfaceMap.json";

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
        MandibularFirstMolarTemplate.Generate(crown, ToothSide.Right);

    public static string GenerateDefault()
    {
        var obj46 = FindObj("FDI46_High.obj") ?? throw new FileNotFoundException("FDI46_High.obj not found.");
        var obj36 = FindObj("FDI36_High.obj") ?? throw new FileNotFoundException("FDI36_High.obj not found.");
        var json46 = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj46)!, "..", "FDI46SurfaceMap.json"));
        LogFrozenHashes();

        ToothMeshParts parts36;
        using (var fs = File.OpenRead(obj36))
            parts36 = StlToothLoader.LoadAlignedParts(fs, out _, MandibularFirstMolarTemplate.LoadOptions(ToothSide.Left));
        var frozen36 = ReadLabelsFromFile(
            Path.Combine(Path.GetDirectoryName(json46)!, "FDI36SurfaceMap.json"),
            parts36.Crown.TriangleIndices.Count / 3);
        if (frozen36 is null)
            throw new InvalidDataException("frozen FDI36SurfaceMap.json could not be read for laterality compare.");
        var layout36 = ToothSurfaceLayoutStats.Json("36", "frozen-readonly", parts36.Crown, frozen36);
        ToothSurfaceLayoutStats.Log("A", "36", "frozen-readonly", parts36.Crown, frozen36);

        ToothMeshParts parts46;
        StlMeshStats stats46;
        using (var fs = File.OpenRead(obj46))
            parts46 = StlToothLoader.LoadAlignedParts(fs, out stats46, MandibularFirstMolarTemplate.LoadOptions(ToothSide.Right));
        var map = Build(parts46.Crown);
        DumpMeans(parts46.Crown, map);
        ToothSurfaceLayoutStats.Log("A", "46", "template-right", parts46.Crown, map.TriangleSurface);
        ToothSurfaceTopology.LogAnalyze("A", "46", "template-right", parts46.Crown, map.TriangleSurface);
        ToothSurfaceLayoutStats.LogRedHeight("A", "46", parts46.Crown, map.TriangleSurface);
        var own = ToothSurfaceTopology.ValidateOwnership(map.TriangleSurface);
        var red = ToothSurfaceLayoutStats.RedHeightOf(parts46.Crown, map.TriangleSurface);
        var layout46 = ToothSurfaceLayoutStats.Json("46", "template-right", parts46.Crown, map.TriangleSurface);
        LogLaterality(layout36, layout46, stats46, own, red, parts46.Crown);
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (red.Mean > 0.40 || red.PctHigh > 10)
            throw new InvalidDataException(
                "FDI46 RED is not cervical meanZ01=" + red.Mean.ToString("0.333") +
                " pctHigh=" + red.PctHigh.ToString("0.0"));
        Save(map, json46);
        return json46;
    }

    private static void LogFrozenHashes()
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

        // #region agent log
        try
        {
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi46-template\",\"hypothesisId\":\"E\",\"location\":\"Fdi46SurfaceMapStore.cs\",\"message\":\"frozen-hashes\",\"data\":{\"map16\":\"" +
                       HashOf("Assets/Teeth/FDI16SurfaceMap.json") +
                       "\",\"map36\":\"" + HashOf("Assets/Teeth/FDI36SurfaceMap.json") +
                       "\",\"obj16\":\"" + HashOf("Assets/Teeth/Source/FDI16_High.obj") +
                       "\",\"obj36\":\"" + HashOf("Assets/Teeth/Source/FDI36_High.obj") +
                       "\"},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
    }

    private static void LogLaterality(
        string layout36, string layout46, StlMeshStats stats46,
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

        var m36 = MeanX(layout36, "mesial");
        var d36 = MeanX(layout36, "distal");
        var b36 = MeanY(layout36, "buccal");
        var l36 = MeanY(layout36, "inner");
        var m46 = MeanX(layout46, "mesial");
        var d46 = MeanX(layout46, "distal");
        var b46 = MeanY(layout46, "buccal");
        var l46 = MeanY(layout46, "inner");
        var ok =
            m36 > 0 && d36 < 0 && b36 > 0 && l36 < 0 &&
            m46 < 0 && d46 > 0 && b46 > 0 && l46 < 0;
        var cancelled = Math.Abs(m46 - m36) < 0.05 && Math.Abs(d46 - d36) < 0.05;
        // #region agent log
        try
        {
            var nTri = crown.TriangleIndices.Count / 3;
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi46-template\",\"hypothesisId\":\"A\",\"location\":\"Fdi46SurfaceMapStore.cs\",\"message\":\"laterality\",\"data\":{" +
                       "\"mirrored\":" + (stats46.Mirrored ? "true" : "false") +
                       ",\"yawDeg\":" + stats46.YawDeg.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dx\":" + stats46.Dx.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dy\":" + stats46.Dy.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"dz\":" + stats46.Dz.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"nTri\":" + nTri +
                       ",\"m36\":" + m36.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d36\":" + d36.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b36\":" + b36.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l36\":" + l36.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"m46\":" + m46.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"d46\":" + d46.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"b46\":" + b46.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"l46\":" + l46.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"canonicalAxesOk\":" + (ok ? "true" : "false") +
                       ",\"lateralityCancelled\":" + (cancelled ? "true" : "false") +
                       ",\"dup\":" + own.Dup +
                       ",\"unassigned\":" + own.Unassigned +
                       ",\"redMeanZ01\":" + red.Mean.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"redPctTable\":" + red.PctHigh.ToString("0.###", CultureInfo.InvariantCulture) +
                       ",\"cervicalZone\":" + (red.Mean <= 0.40 && red.PctHigh <= 10 ? "true" : "false") +
                       "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line);
        }
        catch { }
        // #endregion
        if (cancelled)
            throw new InvalidDataException("FDI 46 laterality cancelled: mesial/distal match FDI 36.");
        if (!ok)
            throw new InvalidDataException(
                "laterality axes failed m36=" + m36 + " d36=" + d36 + " m46=" + m46 + " d46=" + d46);
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
            Mesh = "FDI46_High.obj",
            TriangleCount = n,
            Source = MandibularFirstMolarTemplate.PipelineSource,
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
            var line = "{\"sessionId\":\"ee2893\",\"runId\":\"fdi46-template\",\"hypothesisId\":\"D\",\"location\":\"Fdi46SurfaceMapStore.cs\",\"message\":\"saved\",\"data\":{\"path\":\"" +
                       path.Replace("\\", "\\\\") + "\",\"n\":" + n +
                       ",\"occlusal\":" + map.Counts[0] + ",\"buccal\":" + map.Counts[1] +
                       ",\"lingual\":" + map.Counts[2] + ",\"mesial\":" + map.Counts[3] +
                       ",\"distal\":" + map.Counts[4] + ",\"curated\":" + map.Overrides.Count +
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
        if (!string.Equals(dto.Mesh, "FDI46_High.obj", StringComparison.OrdinalIgnoreCase))
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
