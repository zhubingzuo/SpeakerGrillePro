using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;

namespace SpeakerGrillePro
{
    [ComVisible(true)]
    [Guid("7A88B123-7C5D-4B8C-9E2B-7E7314B42650")]
    [ProgId("SpeakerGrillePro.SwAddin")]
    public class SwAddin : ISwAddin
    {
        private SldWorks _swApp;
        private int _addinId;
        private CommandManager _cmdMgr;
        private const int CmdGroupId = 48521;

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            RuntimeLog.Write("ConnectToSW ENTER, cookie=" + Cookie);
            try
            {
                _swApp = (SldWorks)ThisSW;
                _addinId = Cookie;
                RuntimeLog.Write("SldWorks cast OK. Revision=" + SafeRevision());

                bool callbackOk = _swApp.SetAddinCallbackInfo2(0, this, _addinId);
                RuntimeLog.Write("SetAddinCallbackInfo2 returned " + callbackOk);

                try
                {
                    AddCommandManager();
                    RuntimeLog.Write("CommandManager creation OK");
                }
                catch (Exception uiEx)
                {
                    // Keep the add-in loaded even if the toolbar/menu API fails.
                    RuntimeLog.Write("CommandManager creation FAILED: " + uiEx);
                    MessageBox.Show(
                        "SpeakerGrillePro 已经被 SOLIDWORKS 成功加载，但工具栏创建失败。\n\n" +
                        "诊断日志：\n" + RuntimeLog.LogPath + "\n\n" +
                        "错误：" + uiEx.Message,
                        "SpeakerGrillePro v24 诊断", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                RuntimeLog.Write("CONNECT_OK");
                MessageBox.Show(
                    "SpeakerGrillePro v24 已成功加载到 SOLIDWORKS。\n\n" +
                    "如果工具栏正常，你会看到“喇叭孔生成器”。\n" +
                    "运行日志：" + RuntimeLog.LogPath,
                    "SpeakerGrillePro v24", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("CONNECT_FATAL: " + ex);
                try
                {
                    MessageBox.Show(
                        "SpeakerGrillePro 加载失败。\n\n" + ex.Message + "\n\n诊断日志：\n" + RuntimeLog.LogPath,
                        "SpeakerGrillePro v24", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
                return false;
            }
        }

        private string SafeRevision()
        {
            try { return _swApp.RevisionNumber(); } catch { return "unknown"; }
        }

        public bool DisconnectFromSW()
        {
            RuntimeLog.Write("DisconnectFromSW ENTER");
            try
            {
                if (_cmdMgr != null) _cmdMgr.RemoveCommandGroup(CmdGroupId);
            }
            catch { }
            if (_cmdMgr != null) Marshal.ReleaseComObject(_cmdMgr);
            if (_swApp != null) Marshal.ReleaseComObject(_swApp);
            _cmdMgr = null;
            _swApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
        }

        private void AddCommandManager()
        {
            RuntimeLog.Write("AddCommandManager ENTER");
            _cmdMgr = _swApp.GetCommandManager(_addinId);
            if (_cmdMgr == null) throw new Exception("GetCommandManager returned null");

            // v24: unified multi-style fixed-size add-in. Remove legacy standalone honeycomb command groups
            // so the user sees one unified generator instead of two separate toolbars.
            try { _cmdMgr.RemoveCommandGroup(48631); } catch { }
            try { _cmdMgr.RemoveCommandGroup(48632); } catch { }

            int errors = 0;
            const string title = "喇叭孔生成器 Pro（多样式）";
            const string tip = "以草图点为中心生成圆孔、蜂窝、方孔、菱形、三角或声波孔阵列";
            CommandGroup group = _cmdMgr.CreateCommandGroup2(CmdGroupId, title, tip, tip, -1, false, ref errors);
            RuntimeLog.Write("CreateCommandGroup2 errors=" + errors + ", groupNull=" + (group == null));
            if (group == null) throw new Exception("CreateCommandGroup2 returned null, errors=" + errors);
            int cmdIndex = group.AddCommandItem2("生成喇叭孔", -1,
                "选择草图点后，以该点为中心生成多种喇叭孔", "生成喇叭孔", 0,
                "GenerateSpeakerGrille", "CanGenerateSpeakerGrille", 0,
                (int)(swCommandItemType_e.swMenuItem | swCommandItemType_e.swToolbarItem));
            RuntimeLog.Write("AddCommandItem2 index=" + cmdIndex);
            group.HasMenu = true;
            group.HasToolbar = true;
            bool activated = group.Activate();
            RuntimeLog.Write("CommandGroup Activate returned " + activated);
            Marshal.ReleaseComObject(group);
        }

        public int CanGenerateSpeakerGrille()
        {
            try
            {
                ModelDoc2 model = _swApp.IActiveDoc2;
                if (model == null || model.GetType() != (int)swDocumentTypes_e.swDocPART) return 0;
                SelectionMgr sel = (SelectionMgr)model.SelectionManager;
                if (sel.GetSelectedObjectCount2(-1) != 1) return 0;
                return sel.GetSelectedObjectType3(1, -1) == (int)swSelectType_e.swSelSKETCHPOINTS ? 1 : 0;
            }
            catch { return 0; }
        }

        public void GenerateSpeakerGrille()
        {
            ModelDoc2 model = _swApp.IActiveDoc2;
            if (model == null || model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                MessageBox.Show("请先打开一个零件文件。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectionMgr sel = (SelectionMgr)model.SelectionManager;
            if (sel.GetSelectedObjectCount2(-1) != 1 || sel.GetSelectedObjectType3(1, -1) != (int)swSelectType_e.swSelSKETCHPOINTS)
            {
                MessageBox.Show(
                    "请先选择一个草图点，然后再点击“生成喇叭孔”。\n\n" +
                    "该草图必须建立在要打孔的平面表面上。插件会自动把这个草图点作为喇叭孔阵列中心。",
                    "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SketchPoint centerPoint = sel.GetSelectedObject6(1, -1) as SketchPoint;
            if (centerPoint == null)
            {
                MessageBox.Show("没有读取到选中的草图点。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Sketch sourceSketch = centerPoint.GetSketch();
            if (sourceSketch == null)
            {
                MessageBox.Show("无法读取该草图点所属的草图。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (sourceSketch.Is3D())
            {
                MessageBox.Show("当前版本只支持二维草图中的草图点，不支持 3D 草图点。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int refType = (int)swSelectType_e.swSelNOTHING;
            object reference = sourceSketch.GetReferenceEntity(ref refType);
            if (reference == null || refType != (int)swSelectType_e.swSelFACES)
            {
                MessageBox.Show(
                    "这个草图不是直接建立在模型平面表面上的。\n\n" +
                    "请在你希望生成喇叭孔的水平/平面表面上新建一个二维草图，放置一个草图点，退出草图后选中该点，再运行插件。",
                    "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Face2 face = reference as Face2;
            if (face == null)
            {
                MessageBox.Show("无法读取草图所在的模型表面。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Surface surf = face.GetSurface() as Surface;
            if (surf == null || !surf.IsPlane())
            {
                MessageBox.Show("草图所在表面不是平面。当前版本只支持平面表面。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // For a 2D sketch, SketchPoint.X/Y/Z are in the source sketch coordinate system.
            // Convert the selected point to model space first; later CreateGrille converts it into
            // the newly-created grille sketch coordinate system. This makes the center accurate on
            // Top/Right/custom planar faces instead of only on the Front plane.
            MathUtility sourceMu = (MathUtility)_swApp.GetMathUtility();
            MathPoint sourceLocalPoint = (MathPoint)sourceMu.CreatePoint(
                new double[] { centerPoint.X, centerPoint.Y, centerPoint.Z });
            MathTransform sourceSketchToModel = sourceSketch.ModelToSketchTransform.IInverse();
            MathPoint sourceModelPoint = (MathPoint)sourceLocalPoint.MultiplyTransform(sourceSketchToModel);
            double[] centerModel = (double[])sourceModelPoint.ArrayData;
            if (centerModel == null || centerModel.Length < 3)
            {
                MessageBox.Show("无法把所选草图点转换到模型坐标系。", "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RuntimeLog.Write(string.Format(CultureInfo.InvariantCulture,
                "Selected sketch point center model XYZ = {0:F6}, {1:F6}, {2:F6}",
                centerModel[0], centerModel[1], centerModel[2]));

            using (var dlg = new GrilleDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    CreateGrille(model, face, centerModel, dlg.Settings);
                    MessageBox.Show(
                        "喇叭孔已生成，并以你选择的草图点为中心。\n\n" +
                        "水平/垂直偏移仍可用于在草图点基础上进行微调。",
                        "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    RuntimeLog.Write("Generate failed: " + ex);
                    MessageBox.Show("生成失败：\n" + ex.Message + "\n\n如果方便，把报错和模型截图发给我，我可以继续针对你的模型修改插件。",
                        "喇叭孔生成器", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CreateGrille(ModelDoc2 model, Face2 face, double[] modelCenter, GrilleSettings s)
        {
            if (modelCenter == null || modelCenter.Length < 3) throw new Exception("无法读取所选草图点坐标。\n");

            SketchManager skMgr = model.SketchManager;

            // If the user clicked the command while still editing the point's sketch, exit it first.
            // The center coordinates and support face were already cached above, so the point can safely be deselected.
            try
            {
                Sketch active = model.GetActiveSketch2() as Sketch;
                if (active != null)
                {
                    RuntimeLog.Write("An active sketch was detected; exiting sketch edit mode before grille creation.");
                    skMgr.InsertSketch(true);
                }
            }
            catch (Exception activeSketchEx)
            {
                RuntimeLog.Write("Active-sketch exit check warning: " + activeSketchEx.Message);
            }

            model.ClearSelection2(true);
            Entity entity = (Entity)face;
            if (!entity.Select4(false, null)) throw new Exception("无法选择草图点所在的目标表面。\n");

            skMgr.InsertSketch(true);
            Sketch sketch = skMgr.ActiveSketch;
            if (sketch == null) throw new Exception("无法在草图点所在表面建立喇叭孔草图。\n");

            // Transform the user's selected sketch-point model coordinates into the NEW grille sketch coordinates.
            MathUtility mu = (MathUtility)_swApp.GetMathUtility();
            MathPoint mp = (MathPoint)mu.CreatePoint(modelCenter);
            MathTransform xform = sketch.ModelToSketchTransform;
            MathTransform sketchToModel = xform.IInverse();
            MathPoint sp = (MathPoint)mp.MultiplyTransform(xform);
            double[] center = (double[])sp.ArrayData;
            if (center == null || center.Length < 3) throw new Exception("无法把所选草图点转换到喇叭孔草图坐标系。\n");

            double cx = center[0] + Mm(s.OffsetXmm);
            double cy = center[1] + Mm(s.OffsetYmm);
            RuntimeLog.Write(string.Format(CultureInfo.InvariantCulture,
                "Grille center sketch XY = {0:F6}, {1:F6}; offsets mm = {2:F3}, {3:F3}",
                cx, cy, s.OffsetXmm, s.OffsetYmm));

            // v24 fixed-size sizing only.
            // The user explicitly controls grille width/height, feature sizes and pitch in millimetres.
            // Real Face-boundary filtering is still applied later so holes outside the selected planar face
            // or inside trimmed openings are rejected, but the plug-in does not auto-resize the grille.
            double W = Mm(s.WidthMm), H = Mm(s.HeightMm), corner = Mm(s.CornerRadiusMm);
            double featureScale = 1.0;
            corner = Math.Max(0.0, Math.Min(corner, Math.Min(W, H) * 0.5));

            double centerSize = Mm(Clamp(s.CenterDiaMm, 0.60, 8.0));
            double middleSize = Mm(Clamp(s.MiddleDiaMm, 0.55, 8.0));
            double outerSize = Mm(Clamp(s.OuterDiaMm, 0.50, 8.0));
            double pitchEff = Mm(Clamp(s.PitchMm, 1.20, 12.0));
            double honeyWebEff = Mm(Clamp(s.HoneycombWebMm, 0.30, 3.0));

            // Keep circular/polygon patterns from self-intersecting when scaling down.
            double minimumWeb = Mm(Math.Max(0.30, 0.35 * featureScale));
            double maxFeature = Math.Max(centerSize, Math.Max(middleSize, outerSize));
            if (s.ShapeMode != 1)
            {
                double requiredExtent = maxFeature;
                if (s.ShapeMode == 4) requiredExtent = maxFeature * Math.Sqrt(2.0); // diamond vertex-to-vertex width
                if (pitchEff <= requiredExtent + minimumWeb)
                    pitchEff = requiredExtent + minimumWeb;
            }

            var holes = new List<Hole>();
            int skippedEdgeCount = 0;
            double maxR = 0.0;
            double skipPitch = pitchEff;

            Func<double, double> sizeForRho = delegate(double rho)
            {
                if (rho <= s.CenterZone) return centerSize;
                if (rho <= s.MiddleZone) return middleSize;
                return outerSize;
            };

            if (s.ShapeMode == 0 || s.ShapeMode == 2)
            {
                // Round holes. Mode 0 uses the proven staggered/triangular lattice; mode 2 uses
                // a calmer orthogonal matrix for products with a more architectural front face.
                double pitch = pitchEff;
                double dy = s.ShapeMode == 0 ? pitch * Math.Sqrt(3.0) / 2.0 : pitch;
                maxR = maxFeature * 0.5;
                skipPitch = pitch;
                int rowMax = (int)Math.Ceiling(H / Math.Max(1e-9, dy)) + 4;
                int colMax = (int)Math.Ceiling(W / Math.Max(1e-9, pitch)) + 4;

                for (int row = -rowMax; row <= rowMax; row++)
                {
                    double y = row * dy;
                    double xShift = (s.ShapeMode == 0 && ((Math.Abs(row) & 1) == 1)) ? pitch * 0.5 : 0.0;
                    for (int col = -colMax; col <= colMax; col++)
                    {
                        double x = col * pitch + xShift;
                        if (!InsideRoundedRect(x, y, W * 0.5 - maxR, H * 0.5 - maxR, Math.Max(0, corner - maxR))) continue;
                        double nx = x / Math.Max(1e-9, W * 0.5);
                        double ny = y / Math.Max(1e-9, H * 0.5);
                        double rho = Math.Sqrt(nx * nx + ny * ny);
                        double dia = sizeForRho(rho);
                        bool inSkipShell = IsInOutermostSkipShell(x, y, W, H, corner, maxR, pitch, s.SkipStartZone);
                        bool protectedRoundedCorner = IsProtectedRoundedCorner(x, y, W, H, corner, maxR, pitch);
                        if (inSkipShell && !protectedRoundedCorner && ShouldSkipSparseSymmetric(row, x, pitch, s.EdgeSkipPercent))
                        {
                            skippedEdgeCount++;
                            continue;
                        }
                        holes.Add(new Hole(cx + x, cy + y, dia * 0.5));
                    }
                }
                RuntimeLog.Write("PATTERN_MODE " + (s.ShapeMode == 0 ? "ROUND_HEX" : "ROUND_MATRIX") +
                    " pitch_mm=" + (pitch * 1000.0).ToString("F3", CultureInfo.InvariantCulture));
            }
            else if (s.ShapeMode == 1)
            {
                // True close-packed honeycomb: neighboring flat-top hexagons share the visual
                // rhythm of a real honeycomb, with a controlled solid web between cells.
                double maxFlat = maxFeature;
                maxR = maxFlat / Math.Sqrt(3.0);
                double neighbor = maxFlat + honeyWebEff;
                double xStep = neighbor * Math.Sqrt(3.0) / 2.0;
                double yStep = neighbor;
                skipPitch = neighbor;
                int colMax = (int)Math.Ceiling(W / Math.Max(1e-9, xStep)) + 5;
                int rowMax = (int)Math.Ceiling(H / Math.Max(1e-9, yStep)) + 5;
                for (int col = -colMax; col <= colMax; col++)
                {
                    double x = col * xStep;
                    double yShift = ((Math.Abs(col) & 1) == 1) ? yStep * 0.5 : 0.0;
                    for (int row = -rowMax; row <= rowMax; row++)
                    {
                        double y = row * yStep + yShift;
                        if (!InsideRoundedRect(x, y, W * 0.5 - maxR, H * 0.5 - maxR, Math.Max(0, corner - maxR))) continue;
                        double nx = x / Math.Max(1e-9, W * 0.5);
                        double ny = y / Math.Max(1e-9, H * 0.5);
                        double rho = Math.Sqrt(nx * nx + ny * ny);
                        double flat = sizeForRho(rho);
                        bool inSkipShell = IsInOutermostSkipShell(x, y, W, H, corner, maxR, neighbor, s.SkipStartZone);
                        bool protectedRoundedCorner = IsProtectedRoundedCorner(x, y, W, H, corner, maxR, neighbor);
                        if (inSkipShell && !protectedRoundedCorner && ShouldSkipSparseSymmetric(row, x, neighbor, s.EdgeSkipPercent))
                        {
                            skippedEdgeCount++;
                            continue;
                        }
                        holes.Add(new Hole(cx + x, cy + y, flat / Math.Sqrt(3.0), 0.0));
                    }
                }
                RuntimeLog.Write(string.Format(CultureInfo.InvariantCulture,
                    "PATTERN_MODE HONEYCOMB maxFlat_mm={0:F3}, web_mm={1:F3}, neighbor_mm={2:F3}",
                    maxFlat * 1000.0, honeyWebEff * 1000.0, neighbor * 1000.0));
            }
            else if (s.ShapeMode == 3 || s.ShapeMode == 4 || s.ShapeMode == 5)
            {
                // Polygon perforations: square, diamond and triangle. They use a regular lattice
                // and the same center-to-edge gradual sizing, so changing styles does not alter
                // the chosen grille footprint.
                double pitch = pitchEff;
                double dy = pitch * Math.Sqrt(3.0) / 2.0;
                int sides = s.ShapeMode == 5 ? 3 : 4;
                // Square/diamond use across-flats size; triangle uses the entered size as circumdiameter.
                maxR = sides == 4 ? maxFeature / Math.Sqrt(2.0) : maxFeature * 0.5;
                skipPitch = pitch;
                int rowMax = (int)Math.Ceiling(H / Math.Max(1e-9, dy)) + 4;
                int colMax = (int)Math.Ceiling(W / Math.Max(1e-9, pitch)) + 4;
                for (int row = -rowMax; row <= rowMax; row++)
                {
                    double y = row * dy;
                    double xShift = ((Math.Abs(row) & 1) == 1) ? pitch * 0.5 : 0.0;
                    for (int col = -colMax; col <= colMax; col++)
                    {
                        double x = col * pitch + xShift;
                        if (!InsideRoundedRect(x, y, W * 0.5 - maxR, H * 0.5 - maxR, Math.Max(0, corner - maxR))) continue;
                        double nx = x / Math.Max(1e-9, W * 0.5);
                        double ny = y / Math.Max(1e-9, H * 0.5);
                        double rho = Math.Sqrt(nx * nx + ny * ny);
                        double feature = sizeForRho(rho);
                        double r = sides == 4 ? feature / Math.Sqrt(2.0) : feature * 0.5;
                        double angle = s.ShapeMode == 3 ? Math.PI / 4.0 : 0.0;
                        if (s.ShapeMode == 5 && (((row + col) & 1) != 0)) angle = Math.PI;
                        bool inSkipShell = IsInOutermostSkipShell(x, y, W, H, corner, maxR, pitch, s.SkipStartZone);
                        bool protectedRoundedCorner = IsProtectedRoundedCorner(x, y, W, H, corner, maxR, pitch);
                        if (inSkipShell && !protectedRoundedCorner && ShouldSkipSparseSymmetric(row, x, pitch, s.EdgeSkipPercent))
                        {
                            skippedEdgeCount++;
                            continue;
                        }
                        holes.Add(new Hole(cx + x, cy + y, r, angle));
                    }
                }
                string nm = s.ShapeMode == 3 ? "SQUARE" : (s.ShapeMode == 4 ? "DIAMOND" : "TRIANGLE");
                RuntimeLog.Write("PATTERN_MODE " + nm + " pitch_mm=" + (pitch * 1000.0).ToString("F3", CultureInfo.InvariantCulture));
            }
            else
            {
                // Radial sound-wave circles: concentric rings around the picked point. This works
                // especially well on compact products where a conventional rectangular grid feels rigid.
                double pitch = pitchEff;
                maxR = maxFeature * 0.5;
                skipPitch = pitch;
                double maxRing = Math.Sqrt((W * 0.5) * (W * 0.5) + (H * 0.5) * (H * 0.5));
                int rings = Math.Max(1, (int)Math.Ceiling(maxRing / pitch));
                holes.Add(new Hole(cx, cy, centerSize * 0.5));
                for (int ring = 1; ring <= rings; ring++)
                {
                    double rr = ring * pitch;
                    int n = Math.Max(6, (int)Math.Round(2.0 * Math.PI * rr / pitch));
                    double phase = (ring & 1) == 1 ? Math.PI / n : 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        double a = phase + 2.0 * Math.PI * i / n;
                        double x = rr * Math.Cos(a);
                        double y = rr * Math.Sin(a);
                        if (!InsideRoundedRect(x, y, W * 0.5 - maxR, H * 0.5 - maxR, Math.Max(0, corner - maxR))) continue;
                        double nx = x / Math.Max(1e-9, W * 0.5);
                        double ny = y / Math.Max(1e-9, H * 0.5);
                        double rho = Math.Sqrt(nx * nx + ny * ny);
                        double dia = sizeForRho(rho);
                        holes.Add(new Hole(cx + x, cy + y, dia * 0.5));
                    }
                }
                RuntimeLog.Write("PATTERN_MODE RADIAL_WAVE pitch_mm=" + (pitch * 1000.0).ToString("F3", CultureInfo.InvariantCulture));
            }

            RuntimeLog.Write("EDGE_SKIP v24 skipped=" + skippedEdgeCount +
                ", startZone=" + s.SkipStartZone.ToString("F3", CultureInfo.InvariantCulture) +
                ", percent=" + s.EdgeSkipPercent.ToString("F2", CultureInfo.InvariantCulture));

            // Filter every candidate against the ACTUAL trimmed support face.
            int candidateCount = holes.Count;
            var supportedHoles = new List<Hole>();
            foreach (Hole h in holes)
            {
                bool ok;
                if (s.ShapeMode == 0 || s.ShapeMode == 2 || s.ShapeMode == 6)
                    ok = IsCircleFullySupportedByFace(face, sketchToModel, mu, h.X, h.Y, h.R);
                else if (s.ShapeMode == 1)
                    ok = IsRegularPolygonFullySupportedByFace(face, sketchToModel, mu, h.X, h.Y, h.R, 6, h.Angle);
                else if (s.ShapeMode == 5)
                    ok = IsRegularPolygonFullySupportedByFace(face, sketchToModel, mu, h.X, h.Y, h.R, 3, h.Angle);
                else
                    ok = IsRegularPolygonFullySupportedByFace(face, sketchToModel, mu, h.X, h.Y, h.R, 4, h.Angle);
                if (ok) supportedHoles.Add(h);
            }
            holes = supportedHoles;
            RuntimeLog.Write("FACE_FILTER candidates=" + candidateCount +
                ", kept=" + holes.Count + ", rejected=" + (candidateCount - holes.Count));

            if (holes.Count == 0)
                throw new Exception("当前参数生成的孔全部落在目标表面之外、开口区域或过于靠近边缘。\n请缩小孔区尺寸，或把草图点向实体平面内部移动。\n");
            if (holes.Count > 1800)
            {
                var result = MessageBox.Show("当前参数将生成 " + holes.Count + " 个孔，SolidWorks 可能需要较长时间。\n是否继续？",
                    "喇叭孔生成器", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    skMgr.InsertSketch(true);
                    return;
                }
            }

            // Create all profiles directly in the ACTIVE grille-sketch coordinate system.
            skMgr.AddToDB = true;
            skMgr.DisplayWhenAdded = false;
            int createdProfileCount = 0;
            int createdSegmentCount = 0;
            try
            {
                foreach (Hole h in holes)
                {
                    if (s.ShapeMode == 0 || s.ShapeMode == 2 || s.ShapeMode == 6)
                    {
                        SketchSegment seg = skMgr.CreateCircleByRadius(h.X, h.Y, 0.0, h.R);
                        if (seg != null) createdProfileCount++;
                    }
                    else
                    {
                        int sides = s.ShapeMode == 1 ? 6 : (s.ShapeMode == 5 ? 3 : 4);
                        int segs = CreateRegularPolygon(skMgr, h.X, h.Y, h.R, sides, h.Angle);
                        createdSegmentCount += segs;
                        if (segs == sides) createdProfileCount++;
                    }
                }
            }
            finally
            {
                skMgr.DisplayWhenAdded = true;
                skMgr.AddToDB = false;
            }
            RuntimeLog.Write(((s.ShapeMode == 0 || s.ShapeMode == 2 || s.ShapeMode == 6) ? "CIRCLE_CREATE" : "POLYGON_CREATE") +
                " requested=" + holes.Count + ", created=" + createdProfileCount +
                ((s.ShapeMode == 0 || s.ShapeMode == 2 || s.ShapeMode == 6) ? "" : ", segments=" + createdSegmentCount));
            if (createdProfileCount != holes.Count)
                RuntimeLog.Write("PROFILE_CREATE WARNING: one or more profiles were not fully created.");

            // v24 PRIMARY CUT PATH:
            // IMPORTANT: do NOT rebuild before the first cut. In v17 the call to EditRebuild3()
            // caused SOLIDWORKS to drop the active-sketch state on this model, so GetActiveSketch2()
            // returned NULL even though the circles had just been created. FeatureCut3 is now called
            // immediately while SketchManager.ActiveSketch is still alive.
            Sketch activeByManager = null;
            Sketch activeByModel = null;
            try { activeByManager = skMgr.ActiveSketch; } catch { }
            try { activeByModel = model.GetActiveSketch2() as Sketch; } catch { }
            RuntimeLog.Write("ACTIVE_SKETCH_AFTER_CREATE manager=" +
                (activeByManager == null ? "NULL" : "OK") + ", model=" +
                (activeByModel == null ? "NULL" : "OK"));

            Feature cut = TryActiveSketchFeatureCut3(model, skMgr);
            if (cut != null)
            {
                cut.Name = "SpeakerGrille_Cut";
                RuntimeLog.Write("CUT_OK active-sketch-featurecut3: " + cut.Name);
                model.EditRebuild3();
                model.GraphicsRedraw2();
                return;
            }

            RuntimeLog.Write("Active-sketch FeatureCut3 returned NULL; switching to exited-sketch fallback paths.");

            // If FeatureCut3 did not consume/exit the sketch, exit it explicitly before fallback.
            try
            {
                Sketch stillActive = model.GetActiveSketch2() as Sketch;
                if (stillActive != null)
                {
                    skMgr.InsertSketch(true);
                    RuntimeLog.Write("Exited grille sketch after active FeatureCut3 fallback.");
                }
            }
            catch (Exception exitEx)
            {
                RuntimeLog.Write("Exit grille sketch warning: " + exitEx.Message);
            }
            model.EditRebuild3();
            model.GraphicsRedraw2();

            // Secondary fallback: rediscover the generated sketch and try FeatureCut3 again
            // from an explicitly selected sketch, followed by the older FeatureCut4 strategies.
            Feature grilleSketchFeature = FindNewestSketchFeature(model);
            if (grilleSketchFeature == null)
                throw new Exception("孔草图已生成，但无法重新定位刚生成的草图特征，因此不能自动切除。\n请把 SpeakerGrillePro_runtime.log 发给我。");

            try { grilleSketchFeature.Name = "SpeakerGrille_Sketch"; } catch { }
            cut = TrySelectedSketchFeatureCut3(model, grilleSketchFeature);
            if (cut == null)
                cut = CreateRobustCut(model, grilleSketchFeature);

            if (cut == null)
                throw new Exception(
                    "孔草图已生成，但 v24 的自动切除仍然失败。\n\n" +
                    "插件已经依次尝试：\n" +
                    "1. 活动草图 FeatureCut3（SOLIDWORKS 官方示例参数）\n" +
                    "2. 重新选择草图后 FeatureCut3\n" +
                    "3. FeatureCut4 宏录制器参数完全贯穿\n" +
                    "4. 单向 / 反向 / 双向完全贯穿\n" +
                    "5. Through All Both\n" +
                    "6. 双向 20 mm 盲孔切除\n\n" +
                    "请把 bin\\SpeakerGrillePro_runtime.log 发给我。");

            cut.Name = "SpeakerGrille_Cut";
            RuntimeLog.Write("CUT_OK: " + cut.Name);
            model.EditRebuild3();
        }


        private bool IsCircleFullySupportedByFace(Face2 face, MathTransform sketchToModel,
            MathUtility mu, double cx, double cy, double radius)
        {
            const double tol = 0.00001; // 0.01 mm
            int samples = 16;

            for (int i = -1; i < samples; i++)
            {
                double x = cx;
                double y = cy;
                if (i >= 0)
                {
                    double a = 2.0 * Math.PI * i / samples;
                    double rr = radius + 0.00005; // +0.05 mm safety margin
                    x += rr * Math.Cos(a);
                    y += rr * Math.Sin(a);
                }

                MathPoint local = (MathPoint)mu.CreatePoint(new double[] { x, y, 0.0 });
                MathPoint modelPoint = (MathPoint)local.MultiplyTransform(sketchToModel);
                double[] xyz = (double[])modelPoint.ArrayData;
                if (xyz == null || xyz.Length < 3) return false;

                object closestObj = face.GetClosestPointOn(xyz[0], xyz[1], xyz[2]);
                double[] closest = closestObj as double[];
                if (closest == null || closest.Length < 3) return false;

                double dx = closest[0] - xyz[0];
                double dy = closest[1] - xyz[1];
                double dz = closest[2] - xyz[2];
                if (dx * dx + dy * dy + dz * dz > tol * tol)
                    return false;
            }

            return true;
        }

        private bool IsHexFullySupportedByFace(Face2 face, MathTransform sketchToModel,
            MathUtility mu, double cx, double cy, double circumRadius)
        {
            const double tol = 0.00001;   // 0.01 mm
            const double margin = 0.00005; // 0.05 mm safety margin
            for (int i = -1; i < 12; i++)
            {
                double x = cx;
                double y = cy;
                if (i >= 0)
                {
                    bool vertex = (i % 2) == 0;
                    int k = i / 2;
                    double a = vertex ? (Math.PI * k / 3.0) : (Math.PI * k / 3.0 + Math.PI / 6.0);
                    double rr = vertex ? (circumRadius + margin) :
                        (circumRadius * Math.Cos(Math.PI / 6.0) + margin);
                    x += rr * Math.Cos(a);
                    y += rr * Math.Sin(a);
                }
                MathPoint local = (MathPoint)mu.CreatePoint(new double[] { x, y, 0.0 });
                MathPoint modelPoint = (MathPoint)local.MultiplyTransform(sketchToModel);
                double[] xyz = (double[])modelPoint.ArrayData;
                if (xyz == null || xyz.Length < 3) return false;
                double[] closest = face.GetClosestPointOn(xyz[0], xyz[1], xyz[2]) as double[];
                if (closest == null || closest.Length < 3) return false;
                double dx = closest[0] - xyz[0];
                double dy = closest[1] - xyz[1];
                double dz = closest[2] - xyz[2];
                if (dx * dx + dy * dy + dz * dz > tol * tol) return false;
            }
            return true;
        }

        private bool IsRegularPolygonFullySupportedByFace(Face2 face, MathTransform sketchToModel,
            MathUtility mu, double cx, double cy, double circumRadius, int sides, double angle)
        {
            const double tol = 0.00001;    // 0.01 mm
            const double margin = 0.00005; // 0.05 mm
            if (sides < 3) return false;

            // Center + every vertex + every edge midpoint. This is conservative enough for planar
            // trimmed faces and prevents polygons from crossing screen openings or exterior edges.
            int sampleCount = sides * 2;
            for (int i = -1; i < sampleCount; i++)
            {
                double x = cx, y = cy;
                if (i >= 0)
                {
                    bool vertex = (i % 2) == 0;
                    int k = i / 2;
                    double a = angle + 2.0 * Math.PI * k / sides;
                    double rr;
                    if (vertex)
                    {
                        rr = circumRadius + margin;
                    }
                    else
                    {
                        a += Math.PI / sides;
                        rr = circumRadius * Math.Cos(Math.PI / sides) + margin;
                    }
                    x += rr * Math.Cos(a);
                    y += rr * Math.Sin(a);
                }

                MathPoint local = (MathPoint)mu.CreatePoint(new double[] { x, y, 0.0 });
                MathPoint modelPoint = (MathPoint)local.MultiplyTransform(sketchToModel);
                double[] xyz = (double[])modelPoint.ArrayData;
                if (xyz == null || xyz.Length < 3) return false;
                double[] closest = face.GetClosestPointOn(xyz[0], xyz[1], xyz[2]) as double[];
                if (closest == null || closest.Length < 3) return false;
                double dx = closest[0] - xyz[0];
                double dy = closest[1] - xyz[1];
                double dz = closest[2] - xyz[2];
                if (dx * dx + dy * dy + dz * dz > tol * tol) return false;
            }
            return true;
        }

        private static int CreateRegularPolygon(SketchManager skMgr, double cx, double cy,
            double r, int sides, double angle)
        {
            if (sides < 3) return 0;
            double[] vx = new double[sides];
            double[] vy = new double[sides];
            for (int i = 0; i < sides; i++)
            {
                double a = angle + 2.0 * Math.PI * i / sides;
                vx[i] = cx + r * Math.Cos(a);
                vy[i] = cy + r * Math.Sin(a);
            }
            int created = 0;
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                SketchSegment seg = skMgr.CreateLine(vx[i], vy[i], 0.0, vx[j], vy[j], 0.0);
                if (seg != null) created++;
            }
            return created;
        }

        private bool TryGetCenteredFaceSpan(Face2 face, MathTransform modelToSketch, MathUtility mu,
            double cx, double cy, out double width, out double height)
        {
            width = 0.0;
            height = 0.0;
            try
            {
                double[] b = face.GetBox() as double[];
                if (b == null || b.Length < 6) return false;
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                for (int ix = 0; ix < 2; ix++)
                for (int iy = 0; iy < 2; iy++)
                for (int iz = 0; iz < 2; iz++)
                {
                    double mx = ix == 0 ? b[0] : b[3];
                    double my = iy == 0 ? b[1] : b[4];
                    double mz = iz == 0 ? b[2] : b[5];
                    MathPoint mp = (MathPoint)mu.CreatePoint(new double[] { mx, my, mz });
                    MathPoint sp = (MathPoint)mp.MultiplyTransform(modelToSketch);
                    double[] a = (double[])sp.ArrayData;
                    if (a == null || a.Length < 2) continue;
                    if (a[0] < minX) minX = a[0];
                    if (a[0] > maxX) maxX = a[0];
                    if (a[1] < minY) minY = a[1];
                    if (a[1] > maxY) maxY = a[1];
                }
                if (minX == double.MaxValue || minY == double.MaxValue) return false;
                double left = cx - minX;
                double right = maxX - cx;
                double down = cy - minY;
                double up = maxY - cy;
                if (left <= 0 || right <= 0 || down <= 0 || up <= 0) return false;
                // Conservative 1 mm total inset before percentages are applied.
                width = Math.Max(0.0, 2.0 * Math.Min(left, right) - Mm(1.0));
                height = Math.Max(0.0, 2.0 * Math.Min(down, up) - Mm(1.0));
                return width > Mm(5.0) && height > Mm(5.0);
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("TryGetCenteredFaceSpan warning: " + ex.Message);
                return false;
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static int CreateRegularHexagon(SketchManager skMgr, double cx, double cy, double r)
        {
            double[] vx = new double[6];
            double[] vy = new double[6];
            for (int i = 0; i < 6; i++)
            {
                // Flat-top regular hexagon. Combined with the v24 axial lattice, neighboring
                // cells interlock into a true honeycomb with a controlled uniform web.
                double a = Math.PI * i / 3.0;
                vx[i] = cx + r * Math.Cos(a);
                vy[i] = cy + r * Math.Sin(a);
            }
            int created = 0;
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                SketchSegment seg = skMgr.CreateLine(vx[i], vy[i], 0.0, vx[j], vy[j], 0.0);
                if (seg != null) created++;
            }
            return created;
        }

        private Feature FindNewestSketchFeature(ModelDoc2 model)
        {
            Feature newest = null;
            try
            {
                Feature f = model.FirstFeature() as Feature;
                while (f != null)
                {
                    try
                    {
                        string typeName = f.GetTypeName2();
                        if (string.Equals(typeName, "ProfileFeature", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(typeName, "3DProfileFeature", StringComparison.OrdinalIgnoreCase))
                        {
                            newest = f;
                        }
                    }
                    catch { }
                    f = f.IGetNextFeature() as Feature;
                }
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("FindNewestSketchFeature exception: " + ex);
            }

            RuntimeLog.Write("Newest sketch feature = " + (newest == null ? "<null>" : newest.Name));
            return newest;
        }

        private Feature TryActiveSketchFeatureCut3(ModelDoc2 model, SketchManager skMgr)
        {
            try
            {
                Sketch modelActive = null;
                Sketch managerActive = null;
                try { modelActive = model.GetActiveSketch2() as Sketch; } catch { }
                try { managerActive = skMgr.ActiveSketch; } catch { }

                RuntimeLog.Write("CUT_ATTEMPT active-sketch-featurecut3: modelActive=" +
                    (modelActive == null ? "NULL" : "OK") + ", managerActive=" +
                    (managerActive == null ? "NULL" : "OK"));

                // Do not abort merely because GetActiveSketch2() is NULL. On the user's 2025 SP5
                // model, SketchManager.ActiveSketch can still be valid while ModelDoc2 reports NULL.
                // FeatureCut3 acts on the current sketch-edit context, so try it whenever either API
                // reports an active sketch.
                if (modelActive == null && managerActive == null)
                {
                    RuntimeLog.Write("CUT_RESULT active-sketch-featurecut3: SKIPPED_NO_ACTIVE_SKETCH");
                    return null;
                }

                FeatureManager fm = model.FeatureManager;
                object cutObj = fm.FeatureCut3(
                    true, false, false,
                    (int)swEndConditions_e.swEndCondThroughAll,
                    (int)swEndConditions_e.swEndCondBlind,
                    0.010, 0.010,
                    false, false, false, false,
                    0.01745329251994, 0.01745329251994,
                    false, false, false, false, false,
                    true, true,
                    false, false, false,
                    (int)swStartConditions_e.swStartSketchPlane,
                    0.0, false);

                Feature cut = cutObj as Feature;
                RuntimeLog.Write("CUT_RESULT active-sketch-featurecut3: " +
                    (cut == null ? "NULL" : "SUCCESS"));
                return cut;
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("CUT_EXCEPTION active-sketch-featurecut3: " + ex);
                return null;
            }
        }

        private Feature TrySelectedSketchFeatureCut3(ModelDoc2 model, Feature sketchFeature)
        {
            try
            {
                bool selected = SelectSketchForCut(model, sketchFeature);
                RuntimeLog.Write("CUT_ATTEMPT selected-sketch-featurecut3: sketch selected=" + selected);
                if (!selected) return null;

                model.EditRebuild3();
                FeatureManager fm = model.FeatureManager;
                Feature cut = (Feature)fm.FeatureCut3(
                    true, false, false,
                    (int)swEndConditions_e.swEndCondThroughAll,
                    (int)swEndConditions_e.swEndCondBlind,
                    0.010, 0.010,
                    false, false, false, false,
                    0.01745329251994, 0.01745329251994,
                    false, false, false, false, false,
                    true, true,
                    false, false, false,
                    (int)swStartConditions_e.swStartSketchPlane,
                    0.0, false);

                RuntimeLog.Write("CUT_RESULT selected-sketch-featurecut3: " +
                    (cut == null ? "NULL" : "SUCCESS"));
                return cut;
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("CUT_EXCEPTION selected-sketch-featurecut3: " + ex);
                return null;
            }
        }

        private Feature CreateRobustCut(ModelDoc2 model, Feature sketchFeature)
        {
            Feature cut = null;

            cut = TryRecordedThroughAllCut(model, sketchFeature);
            if (cut != null) return cut;

            cut = TryCut(model, sketchFeature, "single-through-all-default",
                true, false,
                (int)swEndConditions_e.swEndCondThroughAll,
                (int)swEndConditions_e.swEndCondBlind, 0.020, 0.020);
            if (cut != null) return cut;

            cut = TryCut(model, sketchFeature, "single-through-all-reversed",
                true, true,
                (int)swEndConditions_e.swEndCondThroughAll,
                (int)swEndConditions_e.swEndCondBlind, 0.020, 0.020);
            if (cut != null) return cut;

            cut = TryCut(model, sketchFeature, "double-through-all",
                false, false,
                (int)swEndConditions_e.swEndCondThroughAll,
                (int)swEndConditions_e.swEndCondThroughAll, 0.020, 0.020);
            if (cut != null) return cut;

            cut = TryCut(model, sketchFeature, "through-all-both",
                true, false,
                (int)swEndConditions_e.swEndCondThroughAllBoth,
                (int)swEndConditions_e.swEndCondBlind, 0.020, 0.020);
            if (cut != null) return cut;

            cut = TryCut(model, sketchFeature, "double-blind-20mm",
                false, false,
                (int)swEndConditions_e.swEndCondBlind,
                (int)swEndConditions_e.swEndCondBlind, 0.020, 0.020);
            return cut;
        }

        private bool SelectSketchForCut(ModelDoc2 model, Feature sketchFeature)
        {
            model.ClearSelection2(true);

            bool selected = false;
            try
            {
                selected = model.Extension.SelectByID2(
                    sketchFeature.Name, "SKETCH", 0.0, 0.0, 0.0,
                    false, 0, null, 0);
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("SelectByID2 warning: " + ex.Message);
            }

            if (!selected)
            {
                try { selected = sketchFeature.Select2(false, 0); }
                catch { selected = false; }
            }

            RuntimeLog.Write("SelectSketchForCut name=" + sketchFeature.Name + ", selected=" + selected);
            return selected;
        }

        private Feature TryRecordedThroughAllCut(ModelDoc2 model, Feature sketchFeature)
        {
            try
            {
                if (!SelectSketchForCut(model, sketchFeature))
                    return null;

                model.EditRebuild3();

                FeatureManager fm = model.FeatureManager;
                Feature cut = fm.FeatureCut4(
                    true, false, false,
                    (int)swEndConditions_e.swEndCondThroughAll,
                    (int)swEndConditions_e.swEndCondBlind,
                    0.010, 0.010,
                    false, false, false, false,
                    1.0, 1.0,
                    false, false, false, false,
                    false,
                    true,
                    true,
                    true,
                    true,
                    false,
                    (int)swStartConditions_e.swStartSketchPlane,
                    0.0, false, false);

                RuntimeLog.Write("CUT_RESULT recorded-through-all: " +
                    (cut == null ? "NULL" : "SUCCESS"));
                return cut;
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("CUT_EXCEPTION recorded-through-all: " + ex);
                return null;
            }
        }

        private Feature TryCut(ModelDoc2 model, Feature sketchFeature, string tag,
            bool singleEnded, bool reverseDirection1, int end1, int end2, double depth1, double depth2)
        {
            try
            {
                bool selected = SelectSketchForCut(model, sketchFeature);
                RuntimeLog.Write("CUT_ATTEMPT " + tag + ": sketch selected=" + selected);
                if (!selected) return null;

                model.EditRebuild3();
                FeatureManager fm = model.FeatureManager;
                Feature cut = fm.FeatureCut4(
                    singleEnded, false, reverseDirection1,
                    end1, end2,
                    depth1, depth2,
                    false, false, false, false,
                    0.0, 0.0,
                    false, false, false, false,
                    false,       // NormalCut
                    false,       // UseFeatScope
                    true,        // UseAutoSelect: affect applicable bodies automatically
                    false, false, false,
                    (int)swStartConditions_e.swStartSketchPlane,
                    0.0, false, false);

                RuntimeLog.Write("CUT_RESULT " + tag + ": " + (cut == null ? "NULL" : "SUCCESS"));
                return cut;
            }
            catch (Exception ex)
            {
                RuntimeLog.Write("CUT_EXCEPTION " + tag + ": " + ex);
                return null;
            }
        }

        private static bool InsideRoundedRect(double x, double y, double halfW, double halfH, double r)
        {
            if (halfW <= 0 || halfH <= 0) return false;
            double ax = Math.Abs(x), ay = Math.Abs(y);
            if (ax > halfW || ay > halfH) return false;
            if (r <= 1e-9) return true;
            double ix = halfW - r, iy = halfH - r;
            if (ax <= ix || ay <= iy) return true;
            double dx = ax - ix, dy = ay - iy;
            return dx * dx + dy * dy <= r * r;
        }

        private static bool IsInOutermostSkipShell(
            double x, double y, double width, double height, double cornerRadius,
            double maxHoleRadius, double pitch, double skipStartZone)
        {
            // First keep the historical normalized-radius gate, but this is no longer enough
            // by itself because an ellipse can classify visually interior points as "edge".
            double nx = x / Math.Max(1e-9, width * 0.5);
            double ny = y / Math.Max(1e-9, height * 0.5);
            double rho = Math.Sqrt(nx * nx + ny * ny);
            if (rho < Math.Max(0.0, Math.Min(1.0, skipStartZone))) return false;

            // Effective center boundary after accounting for the largest hole radius.
            double outerHalfW = width * 0.5 - maxHoleRadius;
            double outerHalfH = height * 0.5 - maxHoleRadius;
            double outerR = Math.Max(0.0, cornerRadius - maxHoleRadius);

            // Only the outermost ~1.15 pitches may skip.  Everything inside this inset
            // rounded rectangle is a protected, fully populated hex grid.
            double shell = Math.Max(pitch * 1.15, maxHoleRadius * 2.0);
            double innerHalfW = outerHalfW - shell;
            double innerHalfH = outerHalfH - shell;
            double innerR = Math.Max(0.0, outerR - shell);

            if (innerHalfW <= 0 || innerHalfH <= 0) return false;

            bool inProtectedInterior = InsideRoundedRect(
                x, y, innerHalfW, innerHalfH, innerR);
            return !inProtectedInterior;
        }


        private static bool IsProtectedRoundedCorner(
            double x, double y, double width, double height, double cornerRadius,
            double maxHoleRadius, double pitch)
        {
            double halfW = width * 0.5 - maxHoleRadius;
            double halfH = height * 0.5 - maxHoleRadius;
            double r = Math.Max(0.0, cornerRadius - maxHoleRadius);
            if (r <= 1e-9) return false;

            // The rounded corner arc begins at (halfW-r, halfH-r).
            // Add one pitch of protection inward so the transition from straight edge to
            // corner arc cannot lose a single isolated lattice point.
            double ax = Math.Abs(x);
            double ay = Math.Abs(y);
            double protectX = Math.Max(0.0, halfW - r - pitch);
            double protectY = Math.Max(0.0, halfH - r - pitch);
            return ax >= protectX && ay >= protectY;
        }

        private static bool ShouldSkipSparseSymmetric(int row, double x, double pitch, double percent)
        {
            if (percent <= 0 || pitch <= 0) return false;

            // Clamp to a deliberately sparse range. The user's percentage remains meaningful,
            // but the pattern is prevented from becoming visually patchy.
            double p = Math.Max(0.5, Math.Min(20.0, percent));
            int period = (int)Math.Round(100.0 / p);
            if (period < 5) period = 5;

            // hx is the absolute half-pitch index. Using |x| and |row| makes the skip rule
            // mirror/180-degree symmetric even on alternating half-pitch rows.
            int ar = Math.Abs(row);
            int hx = (int)Math.Round(Math.Abs(2.0 * x / pitch));

            // This arithmetic lattice phase deliberately avoids random/hash clusters.
            // Neighboring hex-grid sites change the phase by small non-zero amounts, so
            // skipped points remain isolated instead of forming blank islands.
            int phase = (ar * 7 + hx * 11 + (ar & 1) * 3) % period;
            return phase == 0;
        }

        private static double Mm(double v) { return v / 1000.0; }

        [ComRegisterFunction]
        public static void Register(Type t)
        {
            string guid = "{" + t.GUID.ToString().ToUpperInvariant() + "}";
            using (RegistryKey addin = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\SolidWorks\Addins\" + guid))
            {
                addin.SetValue(null, 1, RegistryValueKind.DWord);
                addin.SetValue("Title", "喇叭孔生成器");
                addin.SetValue("Description", "多样式固定尺寸喇叭孔快速生成器");
            }
            using (RegistryKey startup = Registry.CurrentUser.CreateSubKey(@"Software\SolidWorks\AddInsStartup\" + guid))
                startup.SetValue(null, 1, RegistryValueKind.DWord);
        }

        [ComUnregisterFunction]
        public static void Unregister(Type t)
        {
            string guid = "{" + t.GUID.ToString().ToUpperInvariant() + "}";
            try { Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\SolidWorks\Addins\" + guid, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\SolidWorks\AddInsStartup\" + guid, false); } catch { }
        }
    }

    internal static class RuntimeLog
    {
        private static readonly object Sync = new object();
        public static readonly string LogPath = BuildPath();

        private static string BuildPath()
        {
            try
            {
                string asm = Assembly.GetExecutingAssembly().Location;
                string dir = Path.GetDirectoryName(asm);
                if (!string.IsNullOrEmpty(dir)) return Path.Combine(dir, "SpeakerGrillePro_runtime.log");
            }
            catch { }
            return Path.Combine(Path.GetTempPath(), "SpeakerGrillePro_runtime.log");
        }

        public static void Write(string text)
        {
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + text + System.Environment.NewLine);
                }
            }
            catch
            {
                try
                {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "SpeakerGrillePro_runtime.log"), DateTime.Now.ToString("s") + " | " + text + System.Environment.NewLine);
                }
                catch { }
            }
        }
    }

    internal struct Hole
    {
        public double X, Y, R, Angle;
        public Hole(double x, double y, double r) { X = x; Y = y; R = r; Angle = 0.0; }
        public Hole(double x, double y, double r, double angle) { X = x; Y = y; R = r; Angle = angle; }
    }

    public sealed class GrilleSettings
    {
        // Pattern modes:
        // 0 round/triangular lattice, 1 close-packed honeycomb, 2 round/square lattice,
        // 3 square holes, 4 diamond holes, 5 triangle holes, 6 radial sound-wave circles.
        public int ShapeMode = 0;
        public double HoneycombWebMm = 0.55;
        public double WidthMm = 80;
        public double HeightMm = 44;
        public double CornerRadiusMm = 12;
        public double PitchMm = 3.0;
        public double CenterDiaMm = 2.0;
        public double MiddleDiaMm = 1.5;
        public double OuterDiaMm = 1.0;
        public double CenterZone = 0.42;
        public double MiddleZone = 0.72;
        public double SkipStartZone = 0.94;
        public double EdgeSkipPercent = 0;
        public double OffsetXmm = 0;
        public double OffsetYmm = 0;
    }

    public sealed class GrilleDialog : Form
    {
        private readonly Dictionary<string, TextBox> _boxes = new Dictionary<string, TextBox>();
        private ComboBox _shapeCombo;
        private int _nextFieldRow = 2;
        public GrilleSettings Settings { get; private set; }

        public GrilleDialog()
        {
            Text = "喇叭孔生成器 Pro v24 - 多样式";
            Width = 560;
            Height = 720;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 19, Padding = new Padding(14), AutoSize = false };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
            Controls.Add(panel);

            AddHeader(panel, "v24：7 种孔型 + 固定毫米尺寸（已移除自动适配）");
            AddShapeSelector(panel);
            AddField(panel, "区域宽度", "Width", 80, "mm");
            AddField(panel, "区域高度", "Height", 44, "mm");
            AddField(panel, "外轮廓圆角（参考值）", "Corner", 12, "mm");
            AddField(panel, "孔中心距 / 基准节距", "Pitch", 3.0, "mm");
            AddField(panel, "蜂窝筋宽（蜂窝模式）", "HoneyWeb", 0.55, "mm");
            AddField(panel, "中心孔特征尺寸", "D1", 2.0, "mm");
            AddField(panel, "中间孔特征尺寸", "D2", 1.5, "mm");
            AddField(panel, "外围孔特征尺寸", "D3", 1.0, "mm");
            AddField(panel, "中心区域比例", "Z1", 0.42, "0~1");
            AddField(panel, "中间区域比例", "Z2", 0.72, "0~1");
            AddField(panel, "开始跳孔比例", "SkipStart", 0.94, "0~1");
            AddField(panel, "边缘跳孔率", "SkipPct", 0, "%");
            AddField(panel, "水平偏移", "OffsetX", 0, "mm");
            AddField(panel, "垂直偏移", "OffsetY", 0, "mm");

            var info = new Label
            {
                Text = "v24 已删除自动适配模型功能。请直接按模型尺寸设置区域宽度/高度、孔尺寸和节距。插件仍会逐孔检查真实 Face 边界，避免孔落到所选面的外部或裁剪开口中。\n\n特征尺寸定义：圆孔=直径；蜂窝/方孔/菱形=对边尺寸；三角孔=外接圆直径。",
                AutoSize = true, MaximumSize = new System.Drawing.Size(455, 0), ForeColor = System.Drawing.Color.DimGray
            };
            panel.Controls.Add(info, 0, panel.RowCount - 2);
            panel.SetColumnSpan(info, 3);

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
            var ok = new Button { Text = "生成", Width = 100, DialogResult = DialogResult.None };
            var cancel = new Button { Text = "取消", Width = 100, DialogResult = DialogResult.Cancel };
            ok.Click += delegate { ValidateAndClose(); };
            buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
            panel.Controls.Add(buttons, 0, panel.RowCount - 1);
            panel.SetColumnSpan(buttons, 3);
            CancelButton = cancel;
        }

        private void AddHeader(TableLayoutPanel p, string text)
        {
            var l = new Label { Text = text, AutoSize = true, MaximumSize = new System.Drawing.Size(455, 0), Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) };
            p.Controls.Add(l, 0, 0); p.SetColumnSpan(l, 3);
        }

        private void AddShapeSelector(TableLayoutPanel p)
        {
            int r = 1;
            var l = new Label { Text = "孔型 / 排列样式", Anchor = AnchorStyles.Left, AutoSize = true };
            _shapeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _shapeCombo.Items.Add("圆孔 - 六角错列渐变（经典）");
            _shapeCombo.Items.Add("蜂窝 - 紧密六边形（真蜂窝）");
            _shapeCombo.Items.Add("圆孔 - 方阵极简");
            _shapeCombo.Items.Add("方形孔 - 错列阵列");
            _shapeCombo.Items.Add("菱形孔 - 错列阵列");
            _shapeCombo.Items.Add("三角孔 - 交错阵列");
            _shapeCombo.Items.Add("圆孔 - 同心声波");
            _shapeCombo.SelectedIndex = 0;
            var u = new Label { Text = "7 种", Anchor = AnchorStyles.Left, AutoSize = true, ForeColor = System.Drawing.Color.DimGray };
            p.Controls.Add(l, 0, r); p.Controls.Add(_shapeCombo, 1, r); p.Controls.Add(u, 2, r);
            _shapeCombo.SelectedIndexChanged += delegate { ApplyPreset(_shapeCombo.SelectedIndex); };
        }

        private void ApplyPreset(int mode)
        {
            if (!_boxes.ContainsKey("D1")) return;
            if (mode == 0) { Set("D1",2.0); Set("D2",1.5); Set("D3",1.0); Set("Pitch",3.0); Set("Corner",12); }
            else if (mode == 1) { Set("D1",2.4); Set("D2",2.4); Set("D3",2.4); Set("HoneyWeb",0.55); Set("Corner",14); }
            else if (mode == 2) { Set("D1",1.9); Set("D2",1.45); Set("D3",1.0); Set("Pitch",3.0); Set("Corner",12); }
            else if (mode == 3) { Set("D1",2.0); Set("D2",1.6); Set("D3",1.1); Set("Pitch",3.2); Set("Corner",10); }
            else if (mode == 4) { Set("D1",2.1); Set("D2",1.6); Set("D3",1.1); Set("Pitch",3.3); Set("Corner",12); }
            else if (mode == 5) { Set("D1",2.2); Set("D2",1.7); Set("D3",1.2); Set("Pitch",3.3); Set("Corner",12); }
            else { Set("D1",2.0); Set("D2",1.5); Set("D3",1.0); Set("Pitch",3.2); Set("Corner",16); }
        }

        private void Set(string key, double v)
        {
            if (_boxes.ContainsKey(key)) _boxes[key].Text = v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void AddField(TableLayoutPanel p, string label, string key, double value, string unit)
        {
            int r = _nextFieldRow++;
            var l = new Label { Text = label, Anchor = AnchorStyles.Left, AutoSize = true };
            var t = new TextBox { Text = value.ToString("0.###", CultureInfo.InvariantCulture), Dock = DockStyle.Fill };
            var u = new Label { Text = unit, Anchor = AnchorStyles.Left, AutoSize = true, ForeColor = System.Drawing.Color.DimGray };
            _boxes[key] = t;
            p.Controls.Add(l, 0, r); p.Controls.Add(t, 1, r); p.Controls.Add(u, 2, r);
        }

        private double V(string key)
        {
            double v;
            string raw = _boxes[key].Text.Trim().Replace(',', '.');
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) throw new Exception(key + " 不是有效数字");
            return v;
        }

        private void ValidateAndClose()
        {
            try
            {
                var s = new GrilleSettings
                {
                    ShapeMode = _shapeCombo == null ? 0 : _shapeCombo.SelectedIndex,
                    HoneycombWebMm = V("HoneyWeb"),
                    WidthMm = V("Width"), HeightMm = V("Height"), CornerRadiusMm = V("Corner"), PitchMm = V("Pitch"),
                    CenterDiaMm = V("D1"), MiddleDiaMm = V("D2"), OuterDiaMm = V("D3"),
                    CenterZone = V("Z1"), MiddleZone = V("Z2"), SkipStartZone = V("SkipStart"), EdgeSkipPercent = V("SkipPct"),
                    OffsetXmm = V("OffsetX"), OffsetYmm = V("OffsetY")
                };
                if (s.WidthMm <= 0 || s.HeightMm <= 0) throw new Exception("区域宽度和高度必须大于 0。\n");
                if (s.CenterDiaMm <= 0 || s.MiddleDiaMm <= 0 || s.OuterDiaMm <= 0) throw new Exception("孔尺寸必须大于 0。\n");
                if (!(s.CenterDiaMm >= s.MiddleDiaMm && s.MiddleDiaMm >= s.OuterDiaMm)) throw new Exception("建议保持：中心孔尺寸 ≥ 中间孔尺寸 ≥ 外围孔尺寸。\n");
                if (s.PitchMm <= 0) throw new Exception("基准节距必须大于 0。\n");
                if (s.ShapeMode != 1 && s.PitchMm <= 0.4) throw new Exception("基准节距过小。\n");
                if (s.ShapeMode == 1)
                {
                    if (s.HoneycombWebMm <= 0) throw new Exception("蜂窝筋宽必须大于 0。建议 0.4~0.8 mm。\n");
                    if (s.HoneycombWebMm < 0.2) throw new Exception("蜂窝筋宽过小。建议至少 0.2 mm。\n");
                }
                if (s.CenterZone <= 0 || s.CenterZone >= s.MiddleZone || s.MiddleZone >= 1.0) throw new Exception("区域比例应满足：0 < 中心比例 < 中间比例 < 1。\n");
                if (s.SkipStartZone < s.MiddleZone || s.SkipStartZone > 1) throw new Exception("开始跳孔比例应位于中间区域之外，并且 ≤ 1。\n");
                if (s.EdgeSkipPercent < 0 || s.EdgeSkipPercent > 20) throw new Exception("边缘跳孔率应设置在 0~20% 之间；建议默认 0%。\n");
                Settings = s;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }

}
