using System.Globalization;
using System.IO;

namespace MyOrganizer.Wpf.Controls;

public enum ToothJaw
{
    Maxilla,
    Mandible
}

public enum ToothSide
{
    Right,
    Left
}

public enum AnatomicalSourceKind
{
    MaxillaryCentralIncisor,
    MaxillaryLateralIncisor,
    MaxillaryCanine,
    MaxillaryFirstPremolar,
    MaxillarySecondPremolar,
    MaxillaryFirstMolar,
    MaxillarySecondMolar,
    MaxillaryThirdMolar,
    MandibularCentralIncisor,
    MandibularLateralIncisor,
    MandibularCanine,
    MandibularFirstPremolar,
    MandibularSecondPremolar,
    MandibularFirstMolar,
    MandibularSecondMolar,
    MandibularThirdMolar
}

public sealed class ToothAssetAttribution
{
    public required string Institution { get; init; }
    public required string License { get; init; }
    public required string LicenseUrl { get; init; }
    public required string SketchfabUrl { get; init; }
}

/// <summary>
/// Anatomy-specific library entry. Not patient clinical state.
/// </summary>
public sealed class ToothAssetDefinition
{
    public required string FdiNumber { get; init; }
    public required ToothKind ToothKind { get; init; }
    public required ToothJaw Jaw { get; init; }
    public required ToothSide Side { get; init; }
    public required AnatomicalSourceKind SourceKind { get; init; }
    public required string DisplayName { get; init; }
    public required string SourceZipFileName { get; init; }
    public required string InnerObjName { get; init; }
    public string? RuntimeMesh { get; init; }
    public required bool MirrorX { get; init; }
    public required string OrientationProfile { get; init; }
    public string? SurfaceMap { get; init; }
    public required bool RuntimeImported { get; init; }
    public required bool SurfaceMapAvailable { get; init; }
    public required bool ClinicalInteraction { get; init; }
    public required ToothAssetAttribution Attribution { get; init; }
    public string? SourceNote { get; init; }

    public bool SwapMesialDistal => MirrorX;
    public bool MandibularContralateralMirror =>
        MirrorX && (OrientationProfile == MandibularFirstMolarTemplate.OrientationProfile
            || OrientationProfile == MandibularSecondMolarTemplate.OrientationProfile
            || OrientationProfile == MandibularThirdMolarTemplate.OrientationProfile);
    public bool MaxillaryContralateralMirror =>
        MirrorX && (OrientationProfile == MaxillaryFirstMolarTemplate.OrientationProfile
            || OrientationProfile == MaxillarySecondMolarTemplate.OrientationProfile
            || OrientationProfile == MaxillaryThirdMolarTemplate.OrientationProfile);
    public bool FirstMolarContralateralMirror =>
        MandibularContralateralMirror || MaxillaryContralateralMirror;
    public bool FirstPremolarContralateralMirror =>
        MirrorX && (OrientationProfile == MaxillaryFirstPremolarTemplate.OrientationProfile
            || OrientationProfile == MandibularFirstPremolarTemplate.OrientationProfile);
    public bool SecondPremolarContralateralMirror =>
        MirrorX && (OrientationProfile == MaxillarySecondPremolarTemplate.OrientationProfile
            || OrientationProfile == MandibularSecondPremolarTemplate.OrientationProfile);
    public bool CanineContralateralMirror =>
        MirrorX && (OrientationProfile == MaxillaryCanineTemplate.OrientationProfile
            || OrientationProfile == MandibularCanineTemplate.OrientationProfile);
    public bool IncisorContralateralMirror =>
        MirrorX && (OrientationProfile == MaxillaryCentralIncisorTemplate.OrientationProfile
            || OrientationProfile == MaxillaryLateralIncisorTemplate.OrientationProfile
            || OrientationProfile == MandibularCentralIncisorTemplate.OrientationProfile
            || OrientationProfile == MandibularLateralIncisorTemplate.OrientationProfile);
    public bool ContralateralCameraMirror =>
        FirstMolarContralateralMirror || FirstPremolarContralateralMirror || SecondPremolarContralateralMirror || CanineContralateralMirror || IncisorContralateralMirror;
    public string ChewingSurfaceName =>
        ToothKind is ToothKind.Incisor or ToothKind.Canine ? "Incisal" : "Occlusal";
    public string InnerSurfaceName => Jaw == ToothJaw.Maxilla ? "Palatal" : "Lingual";
    public string ContralateralFdi => ToothAssetRegistry.Contralateral(FdiNumber);

    public bool SourceAvailable
    {
        get
        {
            foreach (var path in ToothAssetRegistry.SourceZipCandidates(SourceZipFileName))
            {
                if (File.Exists(path))
                    return true;
            }
            return RuntimeImported;
        }
    }
}

/// <summary>
/// Permanent adult FDI library. 32 positions, 16 left Dundee sources, contralateral MirrorX.
/// FDI 16 is the only imported runtime inspector.
/// </summary>
public static class ToothAssetRegistry
{
    public const string ApprovedFdi = "16";

    private static readonly ToothAssetAttribution Dundee = new()
    {
        Institution = "University of Dundee, School of Dentistry",
        License = "CC BY 4.0",
        LicenseUrl = "https://creativecommons.org/licenses/by/4.0/",
        SketchfabUrl = "https://sketchfab.com/DundeeDental"
    };

    private static readonly IReadOnlyDictionary<string, ToothAssetDefinition> ByFdi = Build();

    public static IReadOnlyList<ToothAssetDefinition> All { get; } =
        ByFdi.Values.OrderBy(d => d.FdiNumber, StringComparer.Ordinal).ToList();

    public static ToothAssetDefinition Get(string fdi) =>
        ByFdi.TryGetValue(Normalize(fdi), out var def)
            ? def
            : throw new KeyNotFoundException("Unknown FDI " + fdi);

    public static bool TryGet(string fdi, out ToothAssetDefinition definition) =>
        ByFdi.TryGetValue(Normalize(fdi), out definition!);

    public static string Contralateral(string fdi)
    {
        if (!ToothFdi.TryParse(fdi, out var n))
            return fdi;
        var quad = n / 10;
        var type = n % 10;
        var other = quad switch
        {
            1 => 2,
            2 => 1,
            3 => 4,
            _ => 3
        };
        return (other * 10 + type).ToString(CultureInfo.InvariantCulture);
    }

    public static IEnumerable<string> SourceZipCandidates(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            yield return Path.Combine(dir.FullName, "Assets", "Teeth", "Source", fileName);
            yield return Path.Combine(dir.FullName, "MyOrganizer.Wpf", "Assets", "Teeth", "Source", fileName);
            dir = dir.Parent;
        }
    }

    public static string Normalize(string fdi) => fdi.Trim();

    private static IReadOnlyDictionary<string, ToothAssetDefinition> Build()
    {
        var sources = new (AnatomicalSourceKind Kind, string Name, ToothKind ToothKind, ToothJaw Jaw,
            string LeftFdi, string Zip, string Obj, string Url, string? Note)[]
        {
            (AnatomicalSourceKind.MaxillaryCentralIncisor, "Maxillary Central Incisor", ToothKind.Incisor, ToothJaw.Maxilla,
                "21", "maxillary-left-central-incisor.zip", "UL1sketch1_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-left-central-incisor-c8a7c2d9280d4c92bc651cfa1459866a", null),
            (AnatomicalSourceKind.MaxillaryLateralIncisor, "Maxillary Lateral Incisor", ToothKind.Incisor, ToothJaw.Maxilla,
                "22", "maxillary-lateral-incisor.zip", "UL2sketch_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-lateral-incisor-5e89ddbfc6454e2e8e09c645574b8932", null),
            (AnatomicalSourceKind.MaxillaryCanine, "Maxillary Canine", ToothKind.Canine, ToothJaw.Maxilla,
                "23", "maxillary-canine.zip", "UL3sketch1_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-canine-bd930c9b9da14f2a9a8c9b130b0e08a2", null),
            (AnatomicalSourceKind.MaxillaryFirstPremolar, "Maxillary First Premolar", ToothKind.Premolar, ToothJaw.Maxilla,
                "24", "maxillary-first-premolar.zip", "UL4sketch_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-first-premolar-f9b48a29d34f4923b683433f030c5c70", null),
            (AnatomicalSourceKind.MaxillarySecondPremolar, "Maxillary Second Premolar", ToothKind.Premolar, ToothJaw.Maxilla,
                "25", "maxillary-second-premolar.zip", "UL5sketch_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-second-premolar-69f3142830064588b000b04bea0ee09f", null),
            (AnatomicalSourceKind.MaxillaryFirstMolar, "Maxillary First Molar", ToothKind.Molar, ToothJaw.Maxilla,
                "26", "maxillary-first-molar.zip", "UL6sketch_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-first-molar-e719a474ef7e4bd7abec508f85f1e984", null),
            (AnatomicalSourceKind.MaxillarySecondMolar, "Maxillary Second Molar", ToothKind.Molar, ToothJaw.Maxilla,
                "27", "maxillary-second-molar.zip", "UL7sketch_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-second-molar-e035713849d1438791306e25235ac452", null),
            (AnatomicalSourceKind.MaxillaryThirdMolar, "Maxillary Third Molar", ToothKind.Molar, ToothJaw.Maxilla,
                "28", "maxillary-third-molar.zip", "UL8sketch_1.OBJ",
                "https://sketchfab.com/3d-models/maxillary-third-molar-1b3c50ded70c4b6297d4526a733a9cf1", null),
            (AnatomicalSourceKind.MandibularCentralIncisor, "Mandibular Central Incisor", ToothKind.Incisor, ToothJaw.Mandible,
                "31", "mandibular-left-central-incisor.zip", "LL2sketch_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-left-central-incisor-90dcbf474e5a4d97b8783b7eb2b9c4b7",
                "Sketchfab title is central (FDI 31). Inner ZBrush file is LL2sketch_1.OBJ; Palmer LL2 usually means lateral. Identity follows the Sketchfab title, not the Palmer code."),
            (AnatomicalSourceKind.MandibularLateralIncisor, "Mandibular Lateral Incisor", ToothKind.Incisor, ToothJaw.Mandible,
                "32", "mandibular-left-lateral-incisor.zip", "LL1sketch_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-left-lateral-incisor-00fa4f74e10b4769830bf60469c65e27",
                "Sketchfab title is lateral (FDI 32). Inner ZBrush file is LL1sketch_1.OBJ; Palmer LL1 usually means central. Identity follows the Sketchfab title, not the Palmer code."),
            (AnatomicalSourceKind.MandibularCanine, "Mandibular Canine", ToothKind.Canine, ToothJaw.Mandible,
                "33", "mandibular-left-canine.zip", "LL3sketch_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-left-canine-1082011ab5aa46bb96b2af6a02a4ec0c", null),
            (AnatomicalSourceKind.MandibularFirstPremolar, "Mandibular First Premolar", ToothKind.Premolar, ToothJaw.Mandible,
                "34", "mandibular-first-premolar.zip", "L4sketch_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-first-premolar-935637a703dc49eb9eeec9b15a8a5c4c", null),
            (AnatomicalSourceKind.MandibularSecondPremolar, "Mandibular Second Premolar", ToothKind.Premolar, ToothJaw.Mandible,
                "35", "mandibular-left-second-premolar.zip", "LL5sketch1_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-left-second-premolar-fe59fe04725446479bc1115bb12d0ad8", null),
            (AnatomicalSourceKind.MandibularFirstMolar, "Mandibular First Molar", ToothKind.Molar, ToothJaw.Mandible,
                "36", "mandibular-first-molar.zip", "LL6sketch_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-first-molar-e1c919d6603846eca873154eeededdd6", null),
            (AnatomicalSourceKind.MandibularSecondMolar, "Mandibular Second Molar", ToothKind.Molar, ToothJaw.Mandible,
                "37", "mandibular-second-molar.zip", "LL7sketch_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-second-molar-b77dcbc5052e4740b87cdb1964649742", null),
            (AnatomicalSourceKind.MandibularThirdMolar, "Mandibular Third Molar", ToothKind.Molar, ToothJaw.Mandible,
                "38", "mandibular-third-molar.zip", "LL8sketc_1.OBJ",
                "https://sketchfab.com/3d-models/mandibular-third-molar-561bb06b3b084b84978163906de1c2b5", null)
        };

        var map = new Dictionary<string, ToothAssetDefinition>(StringComparer.Ordinal);
        foreach (var src in sources)
        {
            var attr = new ToothAssetAttribution
            {
                Institution = Dundee.Institution,
                License = Dundee.License,
                LicenseUrl = Dundee.LicenseUrl,
                SketchfabUrl = src.Url
            };
            Add(map, Pair(src.LeftFdi, src.Kind, src.Name, src.ToothKind, src.Jaw, ToothSide.Left,
                mirrorX: false, src.Zip, src.Obj, attr, src.Note));
            Add(map, Pair(Contralateral(src.LeftFdi), src.Kind, src.Name, src.ToothKind, src.Jaw, ToothSide.Right,
                mirrorX: true, src.Zip, src.Obj, attr, src.Note));
        }

        // #region agent log
        AgentLog("A", "registry-built",
            "{\"count\":" + map.Count +
            ",\"imported\":" + map.Values.Count(d => d.RuntimeImported) +
            ",\"sources\":" + sources.Length +
            ",\"fdi16Mirror\":" + (map["16"].MirrorX ? "true" : "false") +
            ",\"fdi26Mirror\":" + (map["26"].MirrorX ? "true" : "false") +
            ",\"same16_26\":" + (map["16"].SourceKind == map["26"].SourceKind ? "true" : "false") +
            ",\"fdi26Imported\":" + (map["26"].RuntimeImported ? "true" : "false") +
            ",\"fdi26Mirror\":" + (map["26"].MirrorX ? "true" : "false") +
            ",\"fdi26Map\":" + (map["26"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi26Interact\":" + (map["26"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi36Imported\":" + (map["36"].RuntimeImported ? "true" : "false") +
            ",\"fdi36Mirror\":" + (map["36"].MirrorX ? "true" : "false") +
            ",\"fdi36Map\":" + (map["36"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi36Interact\":" + (map["36"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi46Imported\":" + (map["46"].RuntimeImported ? "true" : "false") +
            ",\"fdi46Mirror\":" + (map["46"].MirrorX ? "true" : "false") +
            ",\"fdi46Map\":" + (map["46"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi46Interact\":" + (map["46"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi14Imported\":" + (map["14"].RuntimeImported ? "true" : "false") +
            ",\"fdi14Mirror\":" + (map["14"].MirrorX ? "true" : "false") +
            ",\"fdi24Imported\":" + (map["24"].RuntimeImported ? "true" : "false") +
            ",\"fdi24Mirror\":" + (map["24"].MirrorX ? "true" : "false") +
            ",\"fdi24Map\":" + (map["24"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi24Interact\":" + (map["24"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi24Profile\":\"" + map["24"].OrientationProfile + "\"" +
            ",\"fdi34Imported\":" + (map["34"].RuntimeImported ? "true" : "false") +
            ",\"fdi34Mirror\":" + (map["34"].MirrorX ? "true" : "false") +
            ",\"fdi34Map\":" + (map["34"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi34Interact\":" + (map["34"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi34Profile\":\"" + map["34"].OrientationProfile + "\"" +
            ",\"fdi44Imported\":" + (map["44"].RuntimeImported ? "true" : "false") +
            ",\"fdi44Mirror\":" + (map["44"].MirrorX ? "true" : "false") +
            ",\"fdi44Map\":" + (map["44"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi44Interact\":" + (map["44"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi44Profile\":\"" + map["44"].OrientationProfile + "\"" +
            ",\"fdi15Imported\":" + (map["15"].RuntimeImported ? "true" : "false") +
            ",\"fdi15Mirror\":" + (map["15"].MirrorX ? "true" : "false") +
            ",\"fdi15Map\":" + (map["15"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi15Profile\":\"" + map["15"].OrientationProfile + "\"" +
            ",\"fdi25Imported\":" + (map["25"].RuntimeImported ? "true" : "false") +
            ",\"fdi25Mirror\":" + (map["25"].MirrorX ? "true" : "false") +
            ",\"fdi25Map\":" + (map["25"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi25Profile\":\"" + map["25"].OrientationProfile + "\"" +
            ",\"fdi35Imported\":" + (map["35"].RuntimeImported ? "true" : "false") +
            ",\"fdi35Mirror\":" + (map["35"].MirrorX ? "true" : "false") +
            ",\"fdi35Profile\":\"" + map["35"].OrientationProfile + "\"" +
            ",\"fdi45Imported\":" + (map["45"].RuntimeImported ? "true" : "false") +
            ",\"fdi45Mirror\":" + (map["45"].MirrorX ? "true" : "false") +
            ",\"fdi45Map\":" + (map["45"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi45Interact\":" + (map["45"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi45Profile\":\"" + map["45"].OrientationProfile + "\"" +
            ",\"fdi13Imported\":" + (map["13"].RuntimeImported ? "true" : "false") +
            ",\"fdi13Mirror\":" + (map["13"].MirrorX ? "true" : "false") +
            ",\"fdi13Map\":" + (map["13"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi13Profile\":\"" + map["13"].OrientationProfile + "\"" +
            ",\"fdi23Imported\":" + (map["23"].RuntimeImported ? "true" : "false") +
            ",\"fdi23Mirror\":" + (map["23"].MirrorX ? "true" : "false") +
            ",\"fdi23Map\":" + (map["23"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi23Interact\":" + (map["23"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi23Profile\":\"" + map["23"].OrientationProfile + "\"" +
            ",\"fdi33Imported\":" + (map["33"].RuntimeImported ? "true" : "false") +
            ",\"fdi33Mirror\":" + (map["33"].MirrorX ? "true" : "false") +
            ",\"fdi33Map\":" + (map["33"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi33Profile\":\"" + map["33"].OrientationProfile + "\"" +
            ",\"fdi43Imported\":" + (map["43"].RuntimeImported ? "true" : "false") +
            ",\"fdi43Mirror\":" + (map["43"].MirrorX ? "true" : "false") +
            ",\"fdi43Map\":" + (map["43"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi43Interact\":" + (map["43"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi43Profile\":\"" + map["43"].OrientationProfile + "\"" +
            ",\"fdi11Imported\":" + (map["11"].RuntimeImported ? "true" : "false") +
            ",\"fdi11Mirror\":" + (map["11"].MirrorX ? "true" : "false") +
            ",\"fdi11Map\":" + (map["11"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi11Interact\":" + (map["11"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi11Profile\":\"" + map["11"].OrientationProfile + "\"" +
            ",\"fdi21Imported\":" + (map["21"].RuntimeImported ? "true" : "false") +
            ",\"fdi21Mirror\":" + (map["21"].MirrorX ? "true" : "false") +
            ",\"fdi21Map\":" + (map["21"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi21Interact\":" + (map["21"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi21Profile\":\"" + map["21"].OrientationProfile + "\"" +
            ",\"fdi12Imported\":" + (map["12"].RuntimeImported ? "true" : "false") +
            ",\"fdi12Mirror\":" + (map["12"].MirrorX ? "true" : "false") +
            ",\"fdi12Map\":" + (map["12"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi12Interact\":" + (map["12"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi12Profile\":\"" + map["12"].OrientationProfile + "\"" +
            ",\"fdi22Imported\":" + (map["22"].RuntimeImported ? "true" : "false") +
            ",\"fdi22Mirror\":" + (map["22"].MirrorX ? "true" : "false") +
            ",\"fdi22Map\":" + (map["22"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi22Interact\":" + (map["22"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi22Profile\":\"" + map["22"].OrientationProfile + "\"" +
            ",\"fdi31Imported\":" + (map["31"].RuntimeImported ? "true" : "false") +
            ",\"fdi31Mirror\":" + (map["31"].MirrorX ? "true" : "false") +
            ",\"fdi31Map\":" + (map["31"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi31Interact\":" + (map["31"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi31Profile\":\"" + map["31"].OrientationProfile + "\"" +
            ",\"fdi41Imported\":" + (map["41"].RuntimeImported ? "true" : "false") +
            ",\"fdi41Mirror\":" + (map["41"].MirrorX ? "true" : "false") +
            ",\"fdi41Map\":" + (map["41"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi41Interact\":" + (map["41"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi41Profile\":\"" + map["41"].OrientationProfile + "\"" +
            ",\"fdi32Imported\":" + (map["32"].RuntimeImported ? "true" : "false") +
            ",\"fdi32Mirror\":" + (map["32"].MirrorX ? "true" : "false") +
            ",\"fdi32Map\":" + (map["32"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi32Interact\":" + (map["32"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi32Profile\":\"" + map["32"].OrientationProfile + "\"" +
            ",\"fdi42Imported\":" + (map["42"].RuntimeImported ? "true" : "false") +
            ",\"fdi42Mirror\":" + (map["42"].MirrorX ? "true" : "false") +
            ",\"fdi42Map\":" + (map["42"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi42Interact\":" + (map["42"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi42Profile\":\"" + map["42"].OrientationProfile + "\"" +
            ",\"fdi17Imported\":" + (map["17"].RuntimeImported ? "true" : "false") +
            ",\"fdi17Mirror\":" + (map["17"].MirrorX ? "true" : "false") +
            ",\"fdi17Map\":" + (map["17"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi17Interact\":" + (map["17"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi17Profile\":\"" + map["17"].OrientationProfile + "\"" +
            ",\"fdi27Imported\":" + (map["27"].RuntimeImported ? "true" : "false") +
            ",\"fdi27Mirror\":" + (map["27"].MirrorX ? "true" : "false") +
            ",\"fdi27Map\":" + (map["27"].SurfaceMapAvailable ? "true" : "false") +
            ",\"fdi27Interact\":" + (map["27"].ClinicalInteraction ? "true" : "false") +
            ",\"fdi27Profile\":\"" + map["27"].OrientationProfile + "\"}");
        // #endregion
        return map;
    }

    private static void Add(Dictionary<string, ToothAssetDefinition> map, ToothAssetDefinition def) =>
        map[def.FdiNumber] = def;

    private static ToothAssetDefinition Pair(
        string fdi,
        AnatomicalSourceKind kind,
        string name,
        ToothKind toothKind,
        ToothJaw jaw,
        ToothSide side,
        bool mirrorX,
        string zip,
        string obj,
        ToothAssetAttribution attr,
        string? note)
    {
        var imported13 = fdi == "13";
        var imported11 = fdi == "11";
        var imported12 = fdi == "12";
        var imported22 = fdi == "22";
        var imported31 = fdi == "31";
        var imported41 = fdi == "41";
        var imported32 = fdi == "32";
        var imported42 = fdi == "42";
        var imported17 = fdi == "17";
        var imported27 = fdi == "27";
        var imported37 = fdi == "37";
        var imported47 = fdi == "47";
        var imported18 = fdi == "18";
        var imported28 = fdi == "28";
        var imported38 = fdi == "38";
        var imported48 = fdi == "48";
        var imported21 = fdi == "21";
        var imported23 = fdi == "23";
        var imported14 = fdi == "14";
        var imported15 = fdi == "15";
        var imported25 = fdi == "25";
        var imported24 = fdi == "24";
        var imported33 = fdi == "33";
        var imported43 = fdi == "43";
        var imported34 = fdi == "34";
        var imported44 = fdi == "44";
        var imported35 = fdi == "35";
        var imported45 = fdi == "45";
        var imported16 = fdi == ApprovedFdi;
        var imported26 = fdi == "26";
        var imported36 = fdi == "36";
        var imported46 = fdi == "46";
        var importedMandibularFirstMolar = imported36 || imported46;
        var importedMaxillaryPremolar = imported14 || imported24;
        var importedMandibularFirstPremolar = imported34 || imported44;
        var importedMaxillarySecondPremolar = imported15 || imported25;
        var importedMandibularSecondPremolar = imported35 || imported45;
        var importedMaxillaryCanine = imported13 || imported23;
        var importedMaxillaryCentralIncisor = imported11 || imported21;
        var importedMaxillaryLateralIncisor = imported12 || imported22;
        var importedMandibularCentralIncisor = imported31 || imported41;
        var importedMandibularLateralIncisor = imported32 || imported42;
        var importedMaxillarySecondMolar = imported17 || imported27;
        var importedMandibularSecondMolar = imported37 || imported47;
        var importedMaxillaryThirdMolar = imported18 || imported28;
        var importedMandibularThirdMolar = imported38 || imported48;
        var importedMandibularCanine = imported33 || imported43;
        var imported = importedMandibularThirdMolar || importedMaxillaryThirdMolar || importedMandibularSecondMolar || importedMaxillarySecondMolar || importedMandibularLateralIncisor || importedMandibularCentralIncisor || importedMaxillaryLateralIncisor || importedMaxillaryCentralIncisor || importedMaxillaryCanine || importedMandibularCanine || importedMaxillaryPremolar || importedMaxillarySecondPremolar || importedMandibularFirstPremolar || importedMandibularSecondPremolar || imported16 || imported26 || importedMandibularFirstMolar;
        return new ToothAssetDefinition
        {
            FdiNumber = fdi,
            ToothKind = toothKind,
            Jaw = jaw,
            Side = side,
            SourceKind = kind,
            DisplayName = name,
            SourceZipFileName = zip,
            InnerObjName = obj,
            RuntimeMesh = imported11 ? "FDI11_High.obj"
                : imported21 ? "FDI21_High.obj"
                : imported12 ? "FDI12_High.obj"
                : imported22 ? "FDI22_High.obj"
                : imported31 ? "FDI31_High.obj"
                : imported41 ? "FDI41_High.obj"
                : imported32 ? "FDI32_High.obj"
                : imported42 ? "FDI42_High.obj"
                : imported17 ? "FDI17_High.obj"
                : imported27 ? "FDI27_High.obj"
                : imported37 ? "FDI37_High.obj"
                : imported47 ? "FDI47_High.obj"
                : imported18 ? "FDI18_High.obj"
                : imported28 ? "FDI28_High.obj"
                : imported38 ? "FDI38_High.obj"
                : imported48 ? "FDI48_High.obj"
                : imported13 ? "FDI13_High.obj"
                : imported23 ? "FDI23_High.obj"
                : imported33 ? "FDI33_High.obj"
                : imported43 ? "FDI43_High.obj"
                : imported14 ? "FDI14_High.obj"
                : imported15 ? "FDI15_High.obj"
                : imported25 ? "FDI25_High.obj"
                : imported24 ? "FDI24_High.obj"
                : imported34 ? "FDI34_High.obj"
                : imported44 ? "FDI44_High.obj"
                : imported35 ? "FDI35_High.obj"
                : imported45 ? "FDI45_High.obj"
                : imported16 ? "FDI16_High.obj"
                : imported26 ? "FDI26_High.obj"
                : imported36 ? "FDI36_High.obj"
                : imported46 ? "FDI46_High.obj"
                : null,
            MirrorX = imported17 || imported18 ? false : imported27 || imported28 || imported26 || mirrorX,
            OrientationProfile = importedMaxillaryCentralIncisor ? MaxillaryCentralIncisorTemplate.OrientationProfile
                : importedMaxillaryLateralIncisor ? MaxillaryLateralIncisorTemplate.OrientationProfile
                : importedMandibularCentralIncisor ? MandibularCentralIncisorTemplate.OrientationProfile
                : importedMandibularLateralIncisor ? MandibularLateralIncisorTemplate.OrientationProfile
                : importedMaxillarySecondMolar ? MaxillarySecondMolarTemplate.OrientationProfile
                : importedMaxillaryThirdMolar ? MaxillaryThirdMolarTemplate.OrientationProfile
                : importedMandibularThirdMolar ? MandibularThirdMolarTemplate.OrientationProfile
                : importedMandibularSecondMolar ? MandibularSecondMolarTemplate.OrientationProfile
                : importedMaxillaryCanine ? MaxillaryCanineTemplate.OrientationProfile
                : importedMandibularCanine ? MandibularCanineTemplate.OrientationProfile
                : importedMaxillaryPremolar ? MaxillaryFirstPremolarTemplate.OrientationProfile
                : importedMaxillarySecondPremolar ? MaxillarySecondPremolarTemplate.OrientationProfile
                : importedMandibularFirstPremolar ? MandibularFirstPremolarTemplate.OrientationProfile
                : importedMandibularSecondPremolar ? MandibularSecondPremolarTemplate.OrientationProfile
                : imported16 ? "ApprovedFdi16"
                : imported26 ? MaxillaryFirstMolarTemplate.OrientationProfile
                : importedMandibularFirstMolar ? MandibularFirstMolarTemplate.OrientationProfile
                : "Pending",
            SurfaceMap = imported11 ? "FDI11SurfaceMap.json"
                : imported21 ? "FDI21SurfaceMap.json"
                : imported12 ? "FDI12SurfaceMap.json"
                : imported22 ? "FDI22SurfaceMap.json"
                : imported31 ? "FDI31SurfaceMap.json"
                : imported41 ? "FDI41SurfaceMap.json"
                : imported32 ? "FDI32SurfaceMap.json"
                : imported42 ? "FDI42SurfaceMap.json"
                : imported17 ? "FDI17SurfaceMap.json"
                : imported27 ? "FDI27SurfaceMap.json"
                : imported37 ? "FDI37SurfaceMap.json"
                : imported47 ? "FDI47SurfaceMap.json"
                : imported18 ? "FDI18SurfaceMap.json"
                : imported28 ? "FDI28SurfaceMap.json"
                : imported38 ? "FDI38SurfaceMap.json"
                : imported48 ? "FDI48SurfaceMap.json"
                : imported13 ? "FDI13SurfaceMap.json"
                : imported23 ? "FDI23SurfaceMap.json"
                : imported33 ? "FDI33SurfaceMap.json"
                : imported43 ? "FDI43SurfaceMap.json"
                : imported14 ? "FDI14SurfaceMap.json"
                : imported15 ? "FDI15SurfaceMap.json"
                : imported25 ? "FDI25SurfaceMap.json"
                : imported24 ? "FDI24SurfaceMap.json"
                : imported34 ? "FDI34SurfaceMap.json"
                : imported44 ? "FDI44SurfaceMap.json"
                : imported35 ? "FDI35SurfaceMap.json"
                : imported45 ? "FDI45SurfaceMap.json"
                : imported16 ? "FDI16SurfaceMap.json"
                : imported26 ? "FDI26SurfaceMap.json"
                : imported36 ? "FDI36SurfaceMap.json"
                : imported46 ? "FDI46SurfaceMap.json"
                : null,
            RuntimeImported = imported,
            SurfaceMapAvailable = imported,
            ClinicalInteraction = imported,
            Attribution = attr,
            SourceNote = note
        };
    }

    // #region agent log
    private static void AgentLog(string hypothesisId, string message, string dataJson)
    {
        var line = "{\"sessionId\":\"ee2893\",\"runId\":\"registry-v1\",\"hypothesisId\":\"" + hypothesisId +
                   "\",\"location\":\"ToothAssetRegistry.cs\",\"message\":\"" + message +
                   "\",\"data\":" + dataJson + ",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        try { File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", line); }
        catch { /* lab logging must not break startup */ }
    }
    // #endregion
}
