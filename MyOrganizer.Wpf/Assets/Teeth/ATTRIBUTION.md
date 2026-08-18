University of Dundee School of Dentistry — Maxillary First Molar
=================================================================

Title:    Maxillary First Molar
Author:   University of Dundee, School of Dentistry (@DundeeDental)
Credits:  Created by Emily McDougall; content expert Dr. Andrew Mason;
          initial 3D preparation Mark Roughley
Source:   https://sketchfab.com/3d-models/maxillary-first-molar-e719a474ef7e4bd7abec508f85f1e984
License:  Creative Commons Attribution 4.0 International (CC BY 4.0)
          https://creativecommons.org/licenses/by/4.0/
          Commercial use allowed; author must be credited.

The Sketchfab listing describes this as a left maxillary first molar
(UL6 / FDI 26), created in ZBrush from CT data (~28.5k triangles).

Project modifications:
- Original Sketchfab archive kept as maxillary-first-molar.zip.
- Extracted source mesh stored as Assets/Teeth/Source/FDI16_High.obj
  (UL6sketch_1.OBJ, ZBrush 4.6, 28506 vertices, 28504 quad faces).
- Quad faces triangulated at load time (~57k triangles).
- Converted/loaded into native WPF Viewport3D (no Sketchfab runtime).
- Left → right chirality correction to obtain FDI 16
  (permanent maxillary right first molar).
- Long-axis alignment so the crown/occlusal is +Z.
- Yaw so palatal is −Y and mesial is +X in Tooth Lab occlusal view.
- Uniform scale to fit the existing camera (proportions preserved).
- Vertex normals recalculated.
- Dundee texture maps are not used; Tooth Lab applies an enamel material.
- No anatomical sculpting or five-surface masking.

Alternative considered:
Maxillary First Molar with Cusp of Carabelli (University of Dundee)
https://sketchfab.com/3d-models/maxillary-first-molar-with-cusp-of-carabelli-9117c7a9bf0848f29bc4e85931697e7b
The primary model’s annotations mark Carabelli as absent; the standard
morphology is preferred for a professional odontogram unless the
Carabelli variant is clearly cleaner.

Place the original download (OBJ, STL, or ZIP containing one of those)
in Assets/Teeth/Source/. Do not delete the high-quality source if an
optimized runtime copy is added later under Assets/Teeth/Runtime/.
