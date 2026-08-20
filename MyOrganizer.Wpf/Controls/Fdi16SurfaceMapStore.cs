using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Deterministic FDI 16 crown surface map packed with this exact Dundee mesh.
/// Runtime loads the asset. Classification is only used to regenerate it.
/// </summary>
internal static class Fdi16SurfaceMapStore
{
    public const string PackUri =
        "pack://application:,,,/MyOrganizer.Wpf;component/Assets/Teeth/FDI16SurfaceMap.json";

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
        Fdi16SurfaceCurator.Apply(CrownSurfaceClassifier.Classify(crown));

    public static void DumpTopology()
    {
        var obj = FindObj() ?? throw new FileNotFoundException("FDI16_High.obj not found.");
        var json = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj)!, "..", "FDI16SurfaceMap.json"));
        using var fs = File.OpenRead(obj);
        var parts = StlToothLoader.LoadAlignedParts(fs, out _, new MeshLoadOptions
        {
            MirrorX = true,
            OrientFdi16 = true
        });
        var map = Read(parts.Crown, File.OpenRead(json))
                  ?? throw new InvalidDataException("FDI16SurfaceMap.json could not be read.");
        _ = map;
    }

    public static string PatchCejRedContinuity()
    {
        var obj = FindObj() ?? throw new FileNotFoundException("FDI16_High.obj not found.");
        var json = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj)!, "..", "FDI16SurfaceMap.json"));
        using var fs = File.OpenRead(obj);
        var parts = StlToothLoader.LoadAlignedParts(fs, out _, new MeshLoadOptions
        {
            MirrorX = true,
            OrientFdi16 = true
        });
        ClinicalSurfaceMap map;
        using (var rs = File.OpenRead(json))
            map = Read(parts.Crown, rs) ?? throw new InvalidDataException("FDI16SurfaceMap.json could not be read.");
        var labels = map.TriangleSurface;
        var nTri = labels.Length;
        var border = OpenBoundary(parts.Crown, nTri);
        var idx = parts.Crown.TriangleIndices;
        var minZ = double.PositiveInfinity;
        var maxZ = double.NegativeInfinity;
        var z = new double[nTri];
        for (var t = 0; t < nTri; t++)
        {
            var a = parts.Crown.Positions[idx[t * 3]];
            var b = parts.Crown.Positions[idx[t * 3 + 1]];
            var c = parts.Crown.Positions[idx[t * 3 + 2]];
            z[t] = (a.Z + b.Z + c.Z) / 3.0;
            minZ = Math.Min(minZ, z[t]);
            maxZ = Math.Max(maxZ, z[t]);
        }
        var zSpan = Math.Max(1e-9, maxZ - minZ);
        var fromHist = new int[5];
        var skipMin = 1d;
        var skipMax = 0d;
        var borderRedMin = 1d;
        var borderRedMax = 0d;
        var nBorderRed = 0;
        var nBorderGap = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (!border[t]) continue;
            var z01 = (z[t] - minZ) / zSpan;
            if (labels[t] == ClinicalSurface.Occlusal)
            {
                nBorderRed++;
                borderRedMin = Math.Min(borderRedMin, z01);
                borderRedMax = Math.Max(borderRedMax, z01);
                continue;
            }
            nBorderGap++;
            skipMin = Math.Min(skipMin, z01);
            skipMax = Math.Max(skipMax, z01);
            fromHist[(int)labels[t]]++;
        }
        var neighbors = CrownSurfaceClassifier.BuildNeighbors(idx, nTri);
        var nGapNextToRed = 0;
        var occMin = 1d;
        var occMax = 0d;
        var nOcc = 0;
        var nSkirt = 0;
        for (var t = 0; t < nTri; t++)
        {
            var z01 = (z[t] - minZ) / zSpan;
            if (labels[t] == ClinicalSurface.Occlusal)
            {
                nOcc++;
                occMin = Math.Min(occMin, z01);
                occMax = Math.Max(occMax, z01);
            }
            if (border[t] && labels[t] != ClinicalSurface.Occlusal)
            {
                foreach (var nb in neighbors[t])
                {
                    if (labels[nb] == ClinicalSurface.Occlusal)
                    {
                        nGapNextToRed++;
                        break;
                    }
                }
            }
            if (labels[t] == ClinicalSurface.Occlusal || z01 > 0.38) continue;
            var nextToRed = false;
            foreach (var nb in neighbors[t])
            {
                if (labels[nb] == ClinicalSurface.Occlusal) { nextToRed = true; break; }
            }
            if (nextToRed) nSkirt++;
        }
        var cejHist = new int[5];
        var nCej = 0;
        var nOpenLow = 0;
        var nOpenHigh = 0;
        var openLowHist = new int[5];
        for (var t = 0; t < nTri; t++)
        {
            var z01 = (z[t] - minZ) / zSpan;
            if (border[t] && z01 < 0.20)
            {
                nOpenLow++;
                openLowHist[(int)labels[t]]++;
            }
            if (border[t] && z01 > 0.70) nOpenHigh++;
            if (z01 > 0.08) continue;
            nCej++;
            cejHist[(int)labels[t]]++;
        }
        Console.WriteLine("FDI16 CEJ scan gaps=" + nBorderGap + " nextToRed=" + nGapNextToRed + " cejO=" + cejHist[0]);
        var flipped = 0;
        var fromD = 0;
        var flipMin = 1d;
        var flipMax = 0d;
        for (var t = 0; t < nTri; t++)
        {
            if (!border[t] || labels[t] == ClinicalSurface.Occlusal)
                continue;
            var z01 = (z[t] - minZ) / zSpan;
            if (z01 < 0.70) continue;
            if (labels[t] == ClinicalSurface.Distal) fromD++;
            flipMin = Math.Min(flipMin, z01);
            flipMax = Math.Max(flipMax, z01);
            labels[t] = ClinicalSurface.Occlusal;
            flipped++;
        }

        var highNb = new int[5];
        var nHighNb = 0;
        var highNbMin = 1d;
        var highNbMax = 0d;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] == ClinicalSurface.Occlusal) continue;
            var z01 = (z[t] - minZ) / zSpan;
            if (z01 < 0.68) continue;
            var nextToRed = false;
            foreach (var nb in neighbors[t])
            {
                if (labels[nb] == ClinicalSurface.Occlusal) { nextToRed = true; break; }
            }
            if (!nextToRed) continue;
            nHighNb++;
            highNb[(int)labels[t]]++;
            highNbMin = Math.Min(highNbMin, z01);
            highNbMax = Math.Max(highNbMax, z01);
        }

        var dilated = 0;
        var dilD = 0;
        for (var pass = 0; pass < 2; pass++)
        {
            var add = new List<int>();
            for (var t = 0; t < nTri; t++)
            {
                if (labels[t] == ClinicalSurface.Occlusal) continue;
                var z01 = (z[t] - minZ) / zSpan;
                if (z01 < 0.68) continue;
                var nRed = 0;
                foreach (var nb in neighbors[t])
                    if (labels[nb] == ClinicalSurface.Occlusal) nRed++;
                if (nRed == 0) continue;
                add.Add(t);
            }
            foreach (var t in add)
            {
                if (labels[t] == ClinicalSurface.Distal) dilD++;
                var z01 = (z[t] - minZ) / zSpan;
                flipMin = Math.Min(flipMin, z01);
                flipMax = Math.Max(flipMax, z01);
                labels[t] = ClinicalSurface.Occlusal;
                dilated++;
            }
            if (add.Count == 0) break;
        }

        Array.Clear(map.Counts);
        foreach (var lab in labels)
            map.Counts[(int)lab]++;
        var own = ToothSurfaceTopology.ValidateOwnership(labels);
        var occMinAfter = 1d;
        var nLowOcc = 0;
        var nHighOcc = 0;
        for (var t = 0; t < nTri; t++)
        {
            if (labels[t] != ClinicalSurface.Occlusal) continue;
            var z01 = (z[t] - minZ) / zSpan;
            occMinAfter = Math.Min(occMinAfter, z01);
            if (z01 < 0.35) nLowOcc++;
            if (z01 >= 0.70) nHighOcc++;
        }
        if (own.Dup != 0 || own.Unassigned != 0)
            throw new InvalidDataException("ownership dup=" + own.Dup + " unassigned=" + own.Unassigned);
        if (nLowOcc != 0)
            throw new InvalidDataException("FDI16 table islands: nLowOcc=" + nLowOcc);
        if (occMinAfter < 0.65)
            throw new InvalidDataException("FDI16 band climbed off neck: occMin=" + occMinAfter);
        if (dilated > 1200)
            throw new InvalidDataException("FDI16 dilation too thick: " + dilated);
        Save(map, json, "classifier+cleanup+Fdi16SurfaceCurator+cej-lower-edge");
        Console.WriteLine("FDI16 CEJ dilate border=" + flipped + " dilated=" + dilated + " occlusal=" + map.Counts[0]);
        return json;
    }

    private static bool[] OpenBoundary(MeshGeometry3D crown, int nTri)
    {
        var idx = crown.TriangleIndices;
        var count = new Dictionary<long, int>();
        void Add(int a, int b)
        {
            var lo = Math.Min(a, b);
            var hi = Math.Max(a, b);
            var key = ((long)lo << 32) | (uint)hi;
            count[key] = count.TryGetValue(key, out var n) ? n + 1 : 1;
        }
        for (var t = 0; t < nTri; t++)
        {
            Add(idx[t * 3], idx[t * 3 + 1]);
            Add(idx[t * 3 + 1], idx[t * 3 + 2]);
            Add(idx[t * 3 + 2], idx[t * 3]);
        }
        var border = new bool[nTri];
        bool Open(int a, int b)
        {
            var lo = Math.Min(a, b);
            var hi = Math.Max(a, b);
            var key = ((long)lo << 32) | (uint)hi;
            return count.TryGetValue(key, out var n) && n == 1;
        }
        for (var t = 0; t < nTri; t++)
        {
            var a = idx[t * 3];
            var b = idx[t * 3 + 1];
            var c = idx[t * 3 + 2];
            if (Open(a, b) || Open(b, c) || Open(c, a))
                border[t] = true;
        }
        return border;
    }

    public static string GenerateDefault()
    {
        var obj = FindObj() ?? throw new FileNotFoundException("FDI16_High.obj not found.");
        var json = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj)!, "..", "FDI16SurfaceMap.json"));
        using var fs = File.OpenRead(obj);
        var parts = StlToothLoader.LoadAlignedParts(fs, out _, new MeshLoadOptions
        {
            MirrorX = true,
            OrientFdi16 = true
        });
        var map = Build(parts.Crown);
        Save(map, json);
        return json;
    }

    public static void Save(ClinicalSurfaceMap map, string path, string? source = null)
    {
        var n = map.TriangleSurface.Length;
        var labels = new char[n];
        for (var i = 0; i < n; i++)
            labels[i] = (char)('0' + (int)map.SurfaceOf(i));
        var dto = new Dto
        {
            Mesh = "FDI16_High.obj",
            TriangleCount = n,
            Source = source ?? "classifier+cleanup+Fdi16SurfaceCurator",
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

    private static string? FindObj()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var a = Path.Combine(dir, "Assets", "Teeth", "Source", "FDI16_High.obj");
            var b = Path.Combine(dir, "MyOrganizer.Wpf", "Assets", "Teeth", "Source", "FDI16_High.obj");
            if (File.Exists(a)) return a;
            if (File.Exists(b)) return b;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        var src = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "Assets", "Teeth", "Source", "FDI16_High.obj");
        src = Path.GetFullPath(src);
        return File.Exists(src) ? src : null;
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
