using System.Windows;
using System.Windows.Media;

namespace MyOrganizer.Wpf.Controls;

/// <summary>
/// Occlusal diagram for the right-panel surface selector, plus legacy facial outlines.
/// The live odontogram is drawn by ToothVectorArt (layered Path geometry, no bitmaps).
/// </summary>
internal static class ToothGeometries
{
    internal readonly record struct FacialSet(
        Geometry Crown,
        Geometry Root,
        Geometry Canal,
        Geometry Implant,
        Geometry Highlight,
        Geometry? Fissure,
        Geometry Buccal,
        Geometry Lingual,
        Geometry Mesial,
        Geometry Distal,
        Geometry Occlusal);

    internal readonly record struct OcclusalSet(
        Geometry Outline,
        Geometry Buccal,
        Geometry Lingual,
        Geometry Mesial,
        Geometry Distal,
        Geometry Occlusal,
        Geometry Highlight,
        Geometry? Fissure);

    public static FacialSet Facial(string fdi)
    {
        var upper = ToothFdi.IsUpper(fdi);
        return ToothFdi.Visual(fdi) switch
        {
            ToothVisualType.CentralIncisor => upper ? UpperCentralIncisor() : LowerCentralIncisor(),
            ToothVisualType.LateralIncisor => upper ? UpperLateralIncisor() : LowerLateralIncisor(),
            ToothVisualType.Canine => upper ? UpperCanine() : LowerCanine(),
            ToothVisualType.FirstPremolar => upper ? UpperFirstPremolar() : LowerFirstPremolar(),
            ToothVisualType.SecondPremolar => upper ? UpperSecondPremolar() : LowerSecondPremolar(),
            ToothVisualType.FirstMolar => upper ? UpperFirstMolar() : LowerFirstMolar(),
            ToothVisualType.SecondMolar => upper ? UpperSecondMolar() : LowerSecondMolar(),
            _ => upper ? UpperThirdMolar() : LowerThirdMolar()
        };
    }

    public static FacialSet Facial(ToothKind kind) => kind switch
    {
        ToothKind.Incisor => LowerCentralIncisor(),
        ToothKind.Canine => LowerCanine(),
        ToothKind.Premolar => LowerSecondPremolar(),
        _ => LowerFirstMolar()
    };

    public static FacialSet Facial(ToothVisualType visual) => Facial(visual switch
    {
        ToothVisualType.LateralIncisor => "42",
        ToothVisualType.Canine => "43",
        ToothVisualType.FirstPremolar => "44",
        ToothVisualType.SecondPremolar => "45",
        ToothVisualType.FirstMolar => "46",
        ToothVisualType.SecondMolar => "47",
        ToothVisualType.ThirdMolar => "48",
        _ => "41"
    });

    public static OcclusalSet Occlusal(ToothKind kind) => kind switch
    {
        ToothKind.Incisor => IncisorOcclusal(),
        ToothKind.Canine => CanineOcclusal(),
        ToothKind.Premolar => PremolarOcclusal(),
        _ => MolarOcclusal()
    };

    private static FacialSet UpperCentralIncisor() => Face(
        "M 24.8,8.2 C 30.2,3.2 49.8,3.2 55.2,8.2 C 58.2,17.4 58.4,33.2 55.4,47 C 53.2,55.6 47.6,60.4 40,61.4 C 32.4,60.4 26.8,55.6 24.6,47 C 21.6,33.2 21.8,17.4 24.8,8.2 Z",
        "M 24.8,8.2 C 30.2,3.2 49.8,3.2 55.2,8.2 C 58.2,17.4 58.4,33.2 55.4,47 C 53.6,55.2 50.2,62.8 47.4,72.4 C 44.8,82.6 44.2,98.4 45.4,111.6 C 46.2,120.2 48.6,124.8 46.6,125.8 C 44.2,127 41.6,124.2 40.8,117.4 C 39.6,105.8 38.8,88.2 37.2,75.4 C 35.4,65.2 30.8,57.8 24.6,47 C 21.6,33.2 21.8,17.4 24.8,8.2 Z",
        "M 41.4,63 C 42.4,85 43,106 42.2,121",
        "M 28.8,10 C 34.2,6 46.2,6 51.6,10 C 47.4,16.2 33,16.2 28.8,10 Z",
        null);

    private static FacialSet UpperLateralIncisor() => Face(
        "M 29.4,9.2 C 33.6,5.2 46.6,5.2 50.8,9.2 C 52.6,16.8 52.6,28.6 51.2,39.6 C 49.8,49.6 46.2,55.8 42.2,58 C 41.2,58.6 38.8,58.6 37.8,58 C 33.8,55.8 30.2,49.6 28.8,39.6 C 27.4,28.6 27.4,16.8 29.4,9.2 Z",
        "M 35.6,56.8 C 33.6,68.5 32.6,85 33.8,101 C 34.6,112.5 37.2,120.2 39.6,123.8 C 40.2,124.8 40.8,125.2 41.2,125.2 C 41.8,125.2 42.4,124.6 42.8,123.6 C 45.4,119.6 47.6,111.5 48,100.4 C 48.8,84.5 47.4,68 45.2,56.8 C 43.2,58.4 37.6,58.4 35.6,56.8 Z",
        "M 40.8,60.5 C 41.2,81 41.6,102 41.4,119",
        "M 32.4,11.2 C 36.2,8 44.2,8 48,11.2 C 44.8,16.4 35.6,16.4 32.4,11.2 Z",
        null);

    private static FacialSet UpperCanine() => Face(
        "M 29.6,16.2 C 33.8,8.4 37.6,5.4 40.2,5.4 C 42.8,5.4 46.8,8.4 51,16.2 C 55.8,26.4 57.8,38.6 56.2,49.4 C 54.8,57.2 50.6,62.2 44.6,63.6 C 42.4,64.2 38,64.2 35.8,63.6 C 29.8,62.2 25.4,57.2 24,49.4 C 22.4,38.6 24.4,26.4 29.6,16.2 Z",
        "M 29.6,16.2 C 33.8,8.4 37.6,5.4 40.2,5.4 C 42.8,5.4 46.8,8.4 51,16.2 C 55.8,26.4 57.8,38.6 56.2,49.4 C 55.2,57 52.8,64.4 50.2,74.2 C 47.4,86.2 46.8,102.4 48.2,114.8 C 49.2,122.2 51.4,125.8 49.2,126.4 C 46.6,127.2 44,123.6 43,117.2 C 41.6,106.4 41,90.6 39.4,77.4 C 37.6,66.8 32.8,58.6 24,49.4 C 22.4,38.6 24.4,26.4 29.6,16.2 Z",
        "M 42.6,65 C 43.8,87 44.4,108 43.6,122",
        "M 33.8,12.2 C 37,8.2 43.6,8.2 46.8,12.2 C 48.4,17.6 44.2,21.4 40.2,21.4 C 36.2,21.4 32.2,17.6 33.8,12.2 Z",
        null);

    private static FacialSet UpperFirstPremolar() => Face(
        "M 23.4,18.8 C 26.2,12.2 33.4,10.4 38.2,14.2 C 40,15.6 41.6,16.2 43.2,15.4 C 45.4,14 51.6,11.2 57.4,15.6 C 61.8,19.2 63.4,28.2 62.2,39.4 C 61,50.6 55.4,58.4 46.2,61.2 C 43.2,62.2 36.6,62.2 33.6,61.2 C 24.4,58.4 18.8,50.6 17.6,39.4 C 16.4,28.2 18.4,19.2 23.4,18.8 Z",
        "M 23.4,18.8 C 26.2,12.2 33.4,10.4 38.2,14.2 C 40,15.6 41.6,16.2 43.2,15.4 C 45.4,14 51.6,11.2 57.4,15.6 C 61.8,19.2 63.4,28.2 62.2,39.4 C 61.2,50 57.4,59.2 52.6,68.8 C 48.4,78.6 47.2,94.2 48.6,108.4 C 49.6,117.6 52.8,123.2 50.4,124.6 C 47.4,126.2 44.4,122.4 43.2,115.2 C 41.6,103.4 40.8,86.8 38.6,74.2 C 36.2,63.8 29.8,56.6 17.6,39.4 C 16.4,28.2 18.4,19.2 23.4,18.8 Z",
        "M 43.6,63 C 44.8,84 45.6,105 44.8,120",
        "M 26.8,17.4 C 33.2,13.2 48.6,13.4 54.8,18 C 49.6,23.6 32.2,23.2 26.8,17.4 Z",
        "M 41.2,16.8 C 41.2,24.2 41.2,31.4 41.2,37.2");

    private static FacialSet UpperSecondPremolar() => Face(
        "M 22.4,20.2 C 24.2,13 31.2,10.6 36.4,14.4 C 38.6,16.2 40.4,17 42.2,16.2 C 44.8,14.4 51.6,10.8 57.8,14.8 C 62.4,18 64.2,26.6 63.4,37.6 C 62.4,50.2 56.6,58 47.2,61 C 44,62.2 36,62.2 32.8,61 C 23.4,58 17.6,50.2 16.6,37.6 C 15.8,26.6 17.6,18 22.4,20.2 Z",
        "M 29.4,58.8 C 24.8,67.5 23,82.5 24.4,97 C 25.4,108 28.8,116.4 33,120 C 35.8,122.4 38.8,121 39.6,116 C 40.4,109 40.6,93 40.8,78 C 41,68.8 41.2,62.4 41.4,60.4 C 41.8,62.4 42.2,69 42.6,79 C 43.2,95 44,110 45.8,116.4 C 47.4,121.6 51.2,123.2 54.6,120.2 C 59,116 61.8,106 62,94 C 62.4,79 60,65 55.2,58.8 C 48.8,62.2 36,62.2 29.4,58.8 Z",
        "M 32.4,62 C 29.6,82 29.4,102 32.4,115 M 49.6,62 C 52.8,82 54.2,102 52.2,115",
        "M 25.4,17.4 C 32,13.4 48.6,13.4 55.2,17.4 C 49.6,22.8 31,22.8 25.4,17.4 Z",
        "M 40.4,17 C 40.4,24.2 40.4,31.4 40.4,37.4");

    private static FacialSet UpperFirstMolar() => Face(
        "M 14.6,54.2 C 8.4,46.8 7.2,34.2 11.2,22.4 C 14.8,13.2 24.2,9.2 32.2,11 C 35.4,8.2 38.4,8.4 40.6,11.6 C 43,8.4 46.8,8 51.4,10.6 C 59.8,9 68.4,14.2 71.8,23.8 C 75.4,35.6 73.6,48.2 67.2,54.8 C 56.8,61.2 24.8,61.2 14.6,54.2 Z",
        "M 14.6,54.2 C 8.4,46.8 7.2,34.2 11.2,22.4 C 14.8,13.2 24.2,9.2 32.2,11 C 35.4,8.2 38.4,8.4 40.6,11.6 C 43,8.4 46.8,8 51.4,10.6 C 59.8,9 68.4,14.2 71.8,23.8 C 75.4,35.6 73.6,48.2 67.2,54.8 C 69.8,65.4 70.8,82 69.6,97.4 C 68.6,109.2 64.2,117.8 58.2,120 C 54.4,121.4 52,117.2 51.4,109.6 C 50.8,100.4 50.4,93.2 50.2,90.6 C 50.6,100.8 50.8,112.4 49.4,121.2 C 48.4,126.4 44.4,127.6 41,126.6 C 37.8,125.6 37,121.2 37.4,113.8 C 37.8,103.2 38,94.4 38.2,90.4 C 37.4,99.6 36.2,111.4 33.4,119.2 C 31,124.2 26.2,124.6 21.8,120.4 C 17.4,116 16.2,104.2 16.8,88.6 C 17.2,74.2 16.2,62.4 14.6,54.2 Z",
        "M 22.8,72 C 18.4,92 17.6,110 21.8,119 M 40.4,70 C 40.8,92 41.2,112 40.6,124 M 58.6,72 C 63.4,92 65.2,110 61.2,119",
        "M 17.8,18.2 C 26.4,12.8 54.2,12.8 62.8,18.2 C 54.2,24.4 26.4,24.4 17.8,18.2 Z",
        "M 40.4,15.4 C 40.4,26.2 40.4,36.6 40.4,44.2 M 27.2,28.6 C 33.4,31 47.8,31 54,28.6");

    private static FacialSet UpperSecondMolar() => Face(
        "M 16.4,54.4 C 10.2,48.2 8.6,36.8 11.6,25.4 C 14.2,16 21.8,11.6 29.2,12.4 C 33,10.4 36.8,10.6 40,13.2 C 43.2,10.6 47.2,10.2 51.2,12.2 C 58.6,11.2 66,16 68.6,25.6 C 71.4,37 69.6,48.4 63.4,54.6 C 53.8,60.4 26,60.4 16.4,54.4 Z",
        "M 19.6,54.2 C 13.4,60.6 10.2,75.2 11,91.4 C 11.6,104.8 15.8,114.8 21.4,118.6 C 25.4,121.2 30.2,119.4 31.6,113 C 32.8,105.6 33.6,90.5 34.4,76.2 C 35,67.8 36.2,62.4 37.4,60.8 C 38,66.8 37.8,83.5 38.4,98.4 C 38.8,109.5 38.2,118 40.2,121.6 C 42.2,118 41.6,109.5 42,98.4 C 42.6,83.5 42.4,66.8 43,60.8 C 44.2,62.4 45.4,67.8 46,76.2 C 46.8,90.5 48,105.6 49.8,113 C 51.4,119.4 56.4,121.4 60.6,118.4 C 66.4,114 69.8,103.2 70,90.4 C 70.2,74.2 66.6,60.4 60.2,54.2 C 49.8,59.6 30,59.6 19.6,54.2 Z",
        "M 24.4,62 C 20.8,80 20.2,100 23.6,114 M 40.2,61 C 40.4,82 40.6,102 40.4,118 M 56.4,62 C 60.2,80 61.8,100 58.8,114",
        "M 18.8,19.2 C 26.6,14.4 53.6,14.4 61.4,19.2 C 53.6,24.8 26.6,24.8 18.8,19.2 Z",
        "M 40,16.4 C 40,26 40,35.4 40,43 M 28,28.6 C 33.6,30.8 46.6,30.8 52.2,28.6");

    private static FacialSet UpperThirdMolar() => Face(
        "M 18.8,53.6 C 13.2,47.8 11.8,37.6 14.6,27.2 C 16.8,18.8 23.4,14.6 30,15.2 C 33.4,13.4 36.8,13.6 40,15.8 C 43.2,13.6 46.8,13.2 50.4,15 C 57,14.2 63.4,18.6 65.6,27.4 C 68.2,37.8 66.6,48 61,53.8 C 52.2,59.2 27.6,59.2 18.8,53.6 Z",
        "M 22.2,53.4 C 16.8,59 14.4,71.8 15.2,85.6 C 15.8,97.4 19.4,106.4 24.2,110 C 27.6,112.4 31.6,110.8 32.8,105.2 C 33.8,98.4 34.4,85.5 35.2,73.4 C 35.8,66.2 36.8,61.6 37.8,60.4 C 38.4,65.8 38.2,80.4 38.8,93.6 C 39.2,103.4 38.8,110.8 40.4,113.8 C 42,110.8 41.6,103.4 42,93.6 C 42.6,80.4 42.4,65.8 43,60.4 C 44,61.6 45,66.2 45.6,73.4 C 46.4,85.5 47.4,98.4 49,105.2 C 50.4,110.8 54.6,112.6 58,110 C 62.8,106 66,96.6 66.4,85 C 66.8,71.2 64,58.8 58.4,53.4 C 49.2,58.2 31.4,58.2 22.2,53.4 Z",
        "M 26.4,61 C 23.4,77 23,94 26,106 M 40.4,60 C 40.6,78 40.8,96 40.6,110 M 54.4,61 C 57.6,77 58.4,94 55.6,106",
        "M 21,20.2 C 28,16 52.2,16 59.2,20.2 C 52.2,25.4 28,25.4 21,20.2 Z",
        "M 40,17.6 C 40,26.2 40,34.6 40,41.2 M 29.4,28.4 C 34.4,30.4 45.8,30.4 50.8,28.4");

    private static FacialSet LowerCentralIncisor() => Face(
        "M 29.4,8.8 C 33.8,4.2 46.2,4.2 50.6,8.8 C 52.8,17.4 53,32.8 51.2,46.2 C 49.6,54.8 45.2,59.4 40,60.4 C 34.8,59.4 30.4,54.8 28.8,46.2 C 27,32.8 27.2,17.4 29.4,8.8 Z",
        "M 29.4,8.8 C 33.8,4.2 46.2,4.2 50.6,8.8 C 52.8,17.4 53,32.8 51.2,46.2 C 50,54.4 47.8,62 45.6,71.6 C 43.6,82.2 43.2,98.4 44.2,111.4 C 44.8,119.2 47,123.8 45.4,124.8 C 43.4,126 41.4,123 40.8,116.8 C 39.8,105.2 39.2,88.6 37.8,75.6 C 36.4,65.4 33.4,57.8 28.8,46.2 C 27,32.8 27.2,17.4 29.4,8.8 Z",
        "M 41,61 C 41.8,83 42.2,104 41.6,120",
        "M 32.4,10.8 C 36.2,7.2 44.2,7.2 48,10.8 C 45.2,16 35.2,16 32.4,10.8 Z",
        null);

    private static FacialSet LowerLateralIncisor() => Face(
        "M 31.6,10 C 34.4,6.2 45.6,6.2 48.4,10 C 50,17.8 50.2,29.6 49,41 C 48,50.4 45,55.8 42,57.6 C 41.2,58 38.8,58 38,57.6 C 35,55.8 32,50.4 31,41 C 29.8,29.6 30,17.8 31.6,10 Z",
        "M 36.2,56.4 C 34.6,68.2 34,85 35,101.4 C 35.6,113 37.6,120.8 39.4,124.4 C 39.8,125.2 40.2,125.6 40.6,125.6 C 41,125.6 41.4,125.2 41.8,124.2 C 43.6,120.4 45.6,112.2 46,100.6 C 46.8,84.4 46,67.6 44.4,56.4 C 42.8,57.8 37.8,57.8 36.2,56.4 Z",
        "M 40.4,60.5 C 40.8,81 41.2,102 41,119",
        "M 33.6,11.6 C 36.8,8.6 43.6,8.6 46.8,11.6 C 44.2,16.4 36.2,16.4 33.6,11.6 Z",
        null);

    private static FacialSet LowerCanine() => Face(
        "M 30.2,15.8 C 34.2,8.2 37.8,5.2 40.2,5.2 C 42.6,5.2 46.4,8.2 50.4,15.8 C 55,25.8 56.8,37.8 55.4,48.4 C 54.2,56 50.2,61 44.4,62.4 C 42.4,63 38,63 36,62.4 C 30.2,61 26.2,56 25,48.4 C 23.6,37.8 25.4,25.8 30.2,15.8 Z",
        "M 30.2,15.8 C 34.2,8.2 37.8,5.2 40.2,5.2 C 42.6,5.2 46.4,8.2 50.4,15.8 C 55,25.8 56.8,37.8 55.4,48.4 C 54.4,55.8 52.2,63.2 49.6,72.8 C 46.8,84.6 46.2,101 47.6,113.4 C 48.6,121 50.8,124.8 48.6,125.6 C 46,126.6 43.4,123 42.4,116.6 C 41,105.8 40.4,89.8 38.8,76.8 C 37.2,66.4 32.6,58.2 25,48.4 C 23.6,37.8 25.4,25.8 30.2,15.8 Z",
        "M 42.2,64 C 43.4,86 44,108 43.2,121",
        "M 34.4,11.6 C 37.2,7.8 43.4,7.8 46.2,11.6 C 47.6,16.8 43.8,20.4 40.2,20.4 C 36.6,20.4 33,16.8 34.4,11.6 Z",
        null);

    private static FacialSet LowerFirstPremolar() => Face(
        "M 24.2,18.4 C 26.8,12 33.6,10.2 38.2,14.4 C 39.8,15.8 41.2,16.6 42.6,15.4 C 44.6,13.8 50.4,11.4 56.2,15.8 C 60.4,19.4 61.8,28.2 60.6,39 C 59.4,50 54,57.6 45.4,60.4 C 42.6,61.4 37,61.4 34.2,60.4 C 25.6,57.6 20.2,50 19,39 C 17.8,28.2 19.4,19.4 24.2,18.4 Z",
        "M 24.2,18.4 C 26.8,12 33.6,10.2 38.2,14.4 C 39.8,15.8 41.2,16.6 42.6,15.4 C 44.6,13.8 50.4,11.4 56.2,15.8 C 60.4,19.4 61.8,28.2 60.6,39 C 59.6,49.4 56.2,58.4 51.8,67.8 C 47.8,77.4 46.8,93.2 48,107.2 C 48.8,116.2 51.8,122 49.6,123.4 C 46.8,125 44,121.2 42.8,114.2 C 41.2,102.6 40.4,86.2 38.4,74 C 36.2,63.8 30.2,56.2 19,39 C 17.8,28.2 19.4,19.4 24.2,18.4 Z",
        "M 42.4,62 C 43.6,83 44.2,104 43.4,119",
        "M 27.4,17 C 33.2,13.2 48.2,13.6 53.8,18.2 C 48.6,23.4 32.4,22.8 27.4,17 Z",
        "M 40.6,16.8 C 40.6,24.2 40.6,31.2 40.6,36.8");

    private static FacialSet LowerSecondPremolar() => Face(
        "M 22.8,20.4 C 24.6,13.4 31.4,11 36.6,14.8 C 38.6,16.4 40.4,17.2 42.2,16.4 C 44.8,14.6 51.4,11.2 57.4,15.4 C 61.8,18.6 63.4,27 62.6,37.6 C 61.6,49.8 56,57.6 46.8,60.6 C 43.8,61.6 36.2,61.6 33.2,60.6 C 24,57.6 18.4,49.8 17.4,37.6 C 16.6,27 18.2,18.6 22.8,20.4 Z",
        "M 34,58.6 C 31.6,69 30.4,84.8 31.8,100.4 C 32.8,112.2 36.2,120.4 39.4,123.8 C 40.2,124.8 41,125.2 41.4,125.2 C 42,125.2 42.8,124.6 43.4,123.4 C 46.6,118.8 49.4,109.6 49.8,98 C 50.4,82.4 48.6,67.2 45.8,58.6 C 43.4,60.2 36.4,60.2 34,58.6 Z",
        "M 40.8,62 C 41.4,83 41.8,104 41.6,120",
        "M 26,18 C 32.4,14 48.2,14 54.6,18 C 49.2,23.4 31.4,23.4 26,18 Z",
        "M 40.4,17.4 C 40.4,24.6 40.4,31.6 40.4,37.6");

    private static FacialSet LowerFirstMolar() => Face(
        "M 14.2,53.8 C 8,46.4 7,33.8 11.2,22 C 15,13 24.4,9 32.4,10.8 C 35.6,8 38.6,8.2 40.8,11.4 C 43.2,8.2 47.2,7.8 51.8,10.4 C 60.4,8.8 69.2,14 72.6,23.6 C 76.2,35.4 74.4,48 67.8,54.6 C 57.2,61.2 24.6,61.2 14.2,53.8 Z",
        "M 14.2,53.8 C 8,46.4 7,33.8 11.2,22 C 15,13 24.4,9 32.4,10.8 C 35.6,8 38.6,8.2 40.8,11.4 C 43.2,8.2 47.2,7.8 51.8,10.4 C 60.4,8.8 69.2,14 72.6,23.6 C 76.2,35.4 74.4,48 67.8,54.6 C 70.6,66 72,83.4 71,98.6 C 70.2,110.4 65.4,119.2 58.6,121.2 C 54.4,122.4 52,118 51.4,110.2 C 50.8,101.2 50.2,95.4 49.4,93.2 C 46.8,91.4 43.2,91.2 40.8,93.6 C 39.6,101.8 38,111.6 35.4,118.6 C 32.8,124.2 26.8,124.8 21.4,120.4 C 16.6,116.2 15.4,104.4 16.2,88.8 C 16.8,74.6 15.6,62 14.2,53.8 Z",
        "M 24.2,72 C 20.2,92 19.4,110 23.6,119 M 53.8,74 C 59.6,94 64.8,112 67.4,120",
        "M 17.4,17.8 C 26.2,12.4 54.4,12.4 63.2,17.8 C 54.4,24.2 26.2,24.2 17.4,17.8 Z",
        "M 40.6,15.2 C 40.6,26 40.6,36.6 40.6,44.4 M 27,28.4 C 33.4,31 48.2,31 54.6,28.4");

    private static FacialSet LowerSecondMolar() => Face(
        "M 16.2,54.2 C 10,48 8.4,36.4 11.6,24.8 C 14.4,15.4 22.2,11 29.6,12 C 33.4,9.8 37.2,10 40.2,12.8 C 43.2,10 47.2,9.6 51.2,11.6 C 58.8,10.4 66.4,15.2 69.2,24.8 C 72.2,36.4 70.4,48 64.2,54.4 C 54.4,60.2 26,60.2 16.2,54.2 Z",
        "M 19.8,54 C 13.6,60.2 11.2,74.8 12.6,91.2 C 13.6,104.8 18.2,114.8 24.2,118.4 C 28.4,121 33.2,118.6 34.4,112 C 35.6,104 36.2,89 36.8,75 C 37.4,66.4 38.4,61.6 39.6,60.4 C 41.2,62 43.2,68.4 45.2,81.2 C 47.8,99.6 51.2,112.8 55.8,117.6 C 60,122 66.6,121.6 70,115.6 C 73.4,109.2 72.8,96.8 69.4,83.4 C 65.6,68.2 59.6,57.4 52.8,54 C 42.4,58.8 30,58.8 19.8,54 Z",
        "M 25.4,62 C 21.6,80 21.4,100 25,114 M 53.4,63 C 58.2,82 62.6,102 65,114",
        "M 18.6,19 C 26.4,14.2 53.8,14.2 61.6,19 C 53.8,24.6 26.4,24.6 18.6,19 Z",
        "M 40,16.2 C 40,26 40,35.6 40,43.2 M 27.8,28.4 C 33.6,30.6 46.8,30.6 52.6,28.4");

    private static FacialSet LowerThirdMolar() => Face(
        "M 19,53.4 C 13.4,47.6 12,37.4 14.8,27 C 17,18.6 23.8,14.4 30.2,15.2 C 33.6,13.4 37,13.6 40,15.8 C 43.2,13.6 46.8,13.2 50.4,15 C 56.8,14.2 63.2,18.6 65.4,27.2 C 68,37.6 66.4,47.8 60.8,53.6 C 51.8,59 27.8,59 19,53.4 Z",
        "M 22.6,53.2 C 17.2,58.8 15.2,71.4 16.4,85.2 C 17.2,96.8 21,105.4 26,108.8 C 29.4,111 33.4,109 34.4,103.2 C 35.4,96 35.8,83.4 36.4,72.2 C 36.8,65.4 37.8,61.2 39,60.2 C 40.4,61.6 42.2,67.2 43.8,78.4 C 46,94.6 49,106 53.2,110.2 C 56.8,113.6 62.4,113 65.2,108 C 68,102.6 67.4,91.8 64.6,80.4 C 61.4,67.2 56.2,56.6 50.4,53.2 C 41.2,57.6 31.4,57.6 22.6,53.2 Z",
        "M 27.2,61 C 24,77 24,94 27.2,106 M 51.6,62 C 55.6,78 59,96 61.2,106",
        "M 21.2,20 C 28.2,16 51.8,16 58.8,20 C 51.8,25.2 28.2,25.2 21.2,20 Z",
        "M 40,17.4 C 40,26 40,34.4 40,41 M 29.6,28.2 C 34.6,30.2 45.6,30.2 50.6,28.2");

    private static FacialSet Face(string crownData, string rootData, string canalData, string highlightData, string? fissureData)
    {
        var crown = P(crownData);
        var bounds = crown.Bounds;
        var pad = Math.Max(1.1, bounds.Width * 0.07);
        var buccalH = bounds.Height * 0.34;
        var lingualH = bounds.Height * 0.28;
        var sideW = bounds.Width * 0.26;

        return new FacialSet(
            Crown: crown,
            Root: P(rootData),
            Canal: P(canalData),
            Implant: ImplantFixture(),
            Highlight: P(highlightData),
            Fissure: fissureData is null ? null : P(fissureData),
            Buccal: Inside(crown, new Rect(bounds.X + pad, bounds.Y, bounds.Width - 2 * pad, buccalH)),
            Lingual: Inside(crown, new Rect(bounds.X + pad, bounds.Bottom - lingualH, bounds.Width - 2 * pad, lingualH)),
            Mesial: Inside(crown, new Rect(bounds.X, bounds.Y + buccalH * 0.72, sideW, bounds.Height * 0.42)),
            Distal: Inside(crown, new Rect(bounds.Right - sideW, bounds.Y + buccalH * 0.72, sideW, bounds.Height * 0.42)),
            Occlusal: Inside(crown, new Rect(bounds.X + sideW, bounds.Y + buccalH * 0.82, bounds.Width - 2 * sideW, bounds.Height * 0.36)));
    }

    private static Geometry Inside(Geometry crown, Rect region)
    {
        var g = new CombinedGeometry(GeometryCombineMode.Intersect, crown, new RectangleGeometry(region));
        g.Freeze();
        return g;
    }

    private static Geometry ImplantFixture() => P(
        "M 33.8,58.4 C 38,57.4 42,57.4 46.2,58.4 C 47.8,59.8 48.2,62.6 47.2,65.8 L 45.6,73 L 34.4,78.5 L 45.8,84 L 34.2,89.5 L 45.4,95 L 35.2,101.5 L 40,121 L 44.8,101.5 L 34.6,95 L 45.8,89.5 L 34.2,84 L 45.6,78.5 L 34.4,73 L 32.8,65.8 C 31.8,62.6 32.2,59.8 33.8,58.4 Z");

    private static OcclusalSet MolarOcclusal() => new(
        Outline: P("M 12,22 C 20,8 36,6 50,6 C 64,6 80,8 88,22 C 94,34 94,50 88,66 C 80,84 64,92 50,92 C 36,92 20,84 12,66 C 6,50 6,34 12,22 Z"),
        Buccal: P("M 18,18 C 30,10 70,10 82,18 C 76,28 64,34 50,34 C 36,34 24,28 18,18 Z"),
        Lingual: P("M 18,80 C 30,90 70,90 82,80 C 76,70 64,66 50,66 C 36,66 24,70 18,80 Z"),
        Mesial: P("M 12,26 C 8,40 8,56 14,70 L 30,62 L 30,36 Z"),
        Distal: P("M 88,26 C 92,40 92,56 86,70 L 70,62 L 70,36 Z"),
        Occlusal: P("M 32,36 C 40,30 60,30 68,36 C 74,44 74,56 68,64 C 60,70 40,70 32,64 C 26,56 26,44 32,36 Z"),
        Highlight: P("M 26,14 C 38,8 62,8 74,14 C 62,20 38,20 26,14 Z"),
        Fissure: P("M 50,38 L 50,62 M 36,50 L 64,50"));

    private static OcclusalSet PremolarOcclusal() => new(
        Outline: P("M 20,22 C 30,10 44,8 50,8 C 56,8 70,10 80,22 C 88,34 88,52 80,68 C 70,84 56,90 50,90 C 44,90 30,84 20,68 C 12,52 12,34 20,22 Z"),
        Buccal: P("M 26,18 C 36,12 64,12 74,18 C 68,28 60,34 50,34 C 40,34 32,28 26,18 Z"),
        Lingual: P("M 26,80 C 36,88 64,88 74,80 C 68,70 60,66 50,66 C 40,66 32,70 26,80 Z"),
        Mesial: P("M 18,26 C 14,40 14,54 20,68 L 34,60 L 34,36 Z"),
        Distal: P("M 82,26 C 86,40 86,54 80,68 L 66,60 L 66,36 Z"),
        Occlusal: P("M 36,36 C 42,30 58,30 64,36 C 70,44 70,56 64,64 C 58,70 42,70 36,64 C 30,56 30,44 36,36 Z"),
        Highlight: P("M 30,14 C 40,10 60,10 70,14 C 60,20 40,20 30,14 Z"),
        Fissure: P("M 50,38 L 50,62"));

    private static OcclusalSet CanineOcclusal() => new(
        Outline: P("M 28,16 C 40,6 60,6 72,16 C 82,28 84,48 78,66 C 70,84 56,90 50,90 C 44,90 30,84 22,66 C 16,48 18,28 28,16 Z"),
        Buccal: P("M 34,14 C 44,8 56,8 66,14 C 60,26 56,34 50,34 C 44,34 40,26 34,14 Z"),
        Lingual: P("M 28,74 C 38,86 62,86 72,74 C 66,68 58,64 50,64 C 42,64 34,68 28,74 Z"),
        Mesial: P("M 24,24 C 18,40 18,56 24,70 L 38,62 L 38,36 Z"),
        Distal: P("M 76,24 C 82,40 82,56 76,70 L 62,62 L 62,36 Z"),
        Occlusal: P("M 38,36 C 44,30 56,30 62,36 C 66,44 66,56 62,64 C 56,70 44,70 38,64 C 34,56 34,44 38,36 Z"),
        Highlight: P("M 36,12 C 46,8 54,8 64,12 C 54,18 46,18 36,12 Z"),
        Fissure: null);

    private static OcclusalSet IncisorOcclusal() => new(
        Outline: P("M 22,28 C 30,12 70,12 78,28 C 84,40 84,56 78,70 C 70,86 30,86 22,70 C 16,56 16,40 22,28 Z"),
        Buccal: P("M 26,26 C 36,16 64,16 74,26 L 70,40 L 30,40 Z"),
        Lingual: P("M 28,72 C 38,82 62,82 72,72 L 68,60 L 32,60 Z"),
        Mesial: P("M 22,32 C 18,44 18,56 24,68 L 36,60 L 36,40 Z"),
        Distal: P("M 78,32 C 82,44 82,56 76,68 L 64,60 L 64,40 Z"),
        Occlusal: P("M 36,40 C 44,36 56,36 64,40 C 68,48 68,56 64,60 C 56,64 44,64 36,60 C 32,56 32,48 36,40 Z"),
        Highlight: P("M 28,24 C 40,16 60,16 72,24 C 60,30 40,30 28,24 Z"),
        Fissure: null);

    private static Geometry P(string data)
    {
        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }
}

internal static class ToothBrushes
{
    public static readonly Brush Outline = Freeze(Color.FromRgb(0xB0, 0x9A, 0x7A));
    public static readonly Brush Seam = Freeze(Color.FromRgb(0xD2, 0xC4, 0xA8));
    public static readonly Brush Fissure = Freeze(Color.FromArgb(0x70, 0xB8, 0xA4, 0x88));
    public static readonly Brush HoverStroke = Freeze(Color.FromRgb(0x3B, 0x82, 0xF6));
    public static readonly Brush SelectedStroke = Freeze(Color.FromRgb(0x25, 0x63, 0xEB));
    public static readonly Brush WholeSelected = Freeze(Color.FromRgb(0x1D, 0x4E, 0xD8));
    public static readonly Brush Number = Freeze(Color.FromRgb(0x16, 0x3A, 0x5F));
    public static readonly Brush Highlight = Freeze(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
    public static readonly Brush HoverPlate = Freeze(Color.FromArgb(0x40, 0x37, 0x82, 0xF6));
    public static readonly Brush SelectedPlate = Freeze(Color.FromArgb(0x88, 0x37, 0x82, 0xF6));
    public static readonly Brush RootFill = CreateRoot();
    public static readonly Brush RootStroke = Freeze(Color.FromRgb(0xC4, 0xA8, 0x84));
    public static readonly Brush CanalStroke = Freeze(Color.FromRgb(0xC6, 0x28, 0x28));
    public static readonly Brush ImplantFill = Freeze(Color.FromRgb(0x6B, 0x72, 0x80));
    public static readonly Brush ImplantStroke = Freeze(Color.FromRgb(0x4B, 0x55, 0x63));
    public static readonly Brush Filling = Freeze(Color.FromRgb(0xC5, 0xCB, 0xD1));
    public static readonly Brush FillingStroke = Freeze(Color.FromRgb(0x9A, 0xA3, 0xAD));
    public static readonly Brush CariesSurface = Freeze(Color.FromRgb(0xC4, 0xA4, 0x72));
    public static readonly Brush CariesMedium = Freeze(Color.FromRgb(0x8D, 0x6E, 0x4A));
    public static readonly Brush CariesDeep = Freeze(Color.FromRgb(0x4E, 0x34, 0x2E));
    public static readonly Brush CrownMetal = CreateCrownMetal();
    public static readonly Brush MissingStroke = Freeze(Color.FromRgb(0xB0, 0xB8, 0xC0));
    public static readonly Brush SelectorFill = Freeze(Color.FromArgb(0x55, 0x37, 0x82, 0xF6));
    public static readonly LinearGradientBrush Enamel = CreateEnamel();

    private static LinearGradientBrush CreateEnamel()
    {
        var g = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0.3, 0.05),
            EndPoint = new System.Windows.Point(0.8, 1)
        };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFC, 0xF6), 0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xF4, 0xE9, 0xD4), 0.5));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xE4, 0xD2, 0xB0), 1));
        g.Freeze();
        return g;
    }

    private static LinearGradientBrush CreateCrownMetal()
    {
        var g = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0.28, 0.08),
            EndPoint = new System.Windows.Point(0.78, 1)
        };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xEE, 0xF1, 0xF4), 0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xC8, 0xCF, 0xD6), 0.55));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xA8, 0xB2, 0xBC), 1));
        g.Freeze();
        return g;
    }

    private static LinearGradientBrush CreateRoot()
    {
        var g = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0.3, 0),
            EndPoint = new System.Windows.Point(0.8, 1)
        };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xF0, 0xDC, 0xC0), 0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xD8, 0xB8, 0x90), 1));
        g.Freeze();
        return g;
    }

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
