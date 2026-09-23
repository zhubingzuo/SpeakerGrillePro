// SpeakerGrillePro - geometric pattern regression harness
//
// WHAT IT DOES
//   Loads the REAL built add-in (bin\SpeakerGrillePro.dll) and calls the private
//   SwAddin.CreateGrille(...) once per hole pattern, intercepting the SOLIDWORKS COM API with
//   RealProxy stubs. Every hole the algorithm emits (SketchManager.CreateCircleByRadius /
//   CreateLine) is recorded, and the recorded geometry is then checked independently:
//
//     * boundary  - every hole (using its OWN radius) must lie inside the configured region
//     * symmetry  - every hole must have a mirror partner about x=0, y=0 and both axes
//     * min web   - minimum edge-to-edge distance between neighbouring round holes (mm)
//     * shape     - the shape the add-in actually emits for polygon patterns, classified from the
//                   recorded vertices: "square(axis-aligned)" / "diamond(45 deg)" / "hexagon" /
//                   "triangle". NOTE: a first-vertex angle of 45 deg means an AXIS-ALIGNED square
//                   (its edges are horizontal/vertical), not a rotated one - do not judge
//                   orientation from that angle alone.
//
//   A self-test shrinks the check region on purpose and requires the checker to report
//   violations. Without that step a silently broken checker would look like a clean pass.
//
// WHAT IT DOES NOT VERIFY
//   * The real face / trimmed-opening filter: the stub face answers "the point is on the face"
//     for every sample, which isolates the configured-region constraint. Face filtering was
//     validated separately on a real model (FACE_FILTER candidates=319, kept=319, rejected=0).
//   * The Cut-Extrude step: it necessarily fails against the stubs. The exception happens after
//     all holes are emitted, so the recorded set is complete.
//
// LIMITATIONS / MAINTENANCE
//   * The per-mode parameter presets below mirror GrilleDialog.ApplyPreset. If you change that
//     method, update SetPresets() too.
//   * Coordinates are METRES inside the add-in (Mm() divides by 1000). The checkers convert.
//     Forgetting this makes the boundary check vacuously true - do not "simplify" it away.
//
// EXIT CODE
//   0 = no boundary or symmetry violations, and every run produced holes
//   1 = violations found, or a run produced no holes at all (harness/algorithm problem)
//
// USAGE
//   csc /target:exe /platform:x64 /out:obj\harness.exe Harness.cs
//       /reference:bin\SolidWorks.Interop.sldworks.dll
//       /reference:bin\SolidWorks.Interop.swconst.dll
//       /reference:bin\SolidWorks.Interop.swpublished.dll
//   harness.exe <full path to bin\SpeakerGrillePro.dll>
//   (tools\verify\verify_patterns.ps1 wraps both steps.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using SolidWorks.Interop.sldworks;

// Face2 and Entity are UNRELATED interfaces (neither inherits the other); the add-in performs
// (Entity)face, which C# allows and only succeeds when the object implements both. A transparent
// proxy for Face2 alone therefore throws InvalidCastException, so proxy this combined interface.
public interface IFakeFace : Face2, Entity { }

class FakeProxy : RealProxy
{
    public Func<string, object[], object> H;
    public FakeProxy(Type t) : base(t) { }
    public override IMessage Invoke(IMessage msg)
    {
        IMethodCallMessage call = (IMethodCallMessage)msg;
        object ret = H(call.MethodName, call.Args);
        return new ReturnMessage(ret, null, 0, call.LogicalCallContext, call);
    }
}

class RunResult
{
    public int ShapeMode;
    public string Region = "";
    public double SkipPercent;
    public double CheckScale;
    public int HoleCount;
    public int Outside;
    public double WorstViolation;
    public int NoMirrorX, NoMirrorY, NoMirrorXY;
    public double MinWeb = double.MaxValue;
    public bool RoundHoles;
    public string Shape = "-";
    public string StopNote = "";
    public bool ProducedGeometry;
}

class Runner
{
    List<double[]> _circles = new List<double[]>();  // x, y, r
    List<double[]> _lines = new List<double[]>();    // x1, y1, x2, y2

    static string F(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
    public static string Mm(double v) { return (v * 1000.0).ToString("0.###", CultureInfo.InvariantCulture); }

    static T Make<T>(Func<string, object[], object> h)
    {
        FakeProxy fp = new FakeProxy(typeof(T));
        fp.H = h;
        return (T)fp.GetTransparentProxy();
    }

    // Identity transform: model coordinates equal sketch coordinates, so the fake face reports
    // every sample point as lying exactly on the face (distance 0).
    object MakePoint(double[] data)
    {
        double[] d = (double[])data.Clone();
        return Make<MathPoint>(delegate(string n, object[] a)
        {
            if (n == "MultiplyTransform") return MakePoint(d);
            if (n == "get_ArrayData") return d;
            return null;
        });
    }

    object MakeMathTransform()
    {
        return Make<MathTransform>(delegate(string n, object[] a)
        {
            if (n == "IInverse") return MakeMathTransform();
            return null;
        });
    }

    object MakeSketch()
    {
        return Make<Sketch>(delegate(string n, object[] a)
        {
            if (n == "get_ModelToSketchTransform") return MakeMathTransform();
            return null;
        });
    }

    object MakeSegment() { return Make<SketchSegment>(delegate(string n, object[] a) { return null; }); }

    object MakeSketchManager()
    {
        return Make<SketchManager>(delegate(string n, object[] a)
        {
            if (n == "InsertSketch") return true;
            if (n == "get_ActiveSketch") return MakeSketch();
            if (n == "CreateCircleByRadius")
            {
                _circles.Add(new double[] { Convert.ToDouble(a[0]), Convert.ToDouble(a[1]), Convert.ToDouble(a[3]) });
                return MakeSegment();
            }
            if (n == "CreateLine")
            {
                _lines.Add(new double[] { Convert.ToDouble(a[0]), Convert.ToDouble(a[1]), Convert.ToDouble(a[3]), Convert.ToDouble(a[4]) });
                return MakeSegment();
            }
            return null;
        });
    }

    object MakeModel()
    {
        object skMgr = MakeSketchManager();
        return Make<ModelDoc2>(delegate(string n, object[] a)
        {
            if (n == "get_SketchManager") return skMgr;
            if (n == "ClearSelection2") return true;
            if (n == "EditRebuild3") return true;
            if (n == "GetActiveSketch2") return null;
            return null;
        });
    }

    object MakeSwApp()
    {
        return Make<SldWorks>(delegate(string n, object[] a)
        {
            if (n == "GetMathUtility")
                return Make<MathUtility>(delegate(string n2, object[] a2)
                {
                    if (n2 == "CreatePoint") return MakePoint((double[])a2[0]);
                    return null;
                });
            return null;
        });
    }

    static void SetField(Type t, object o, string f, double v) { t.GetField(f).SetValue(o, v); }
    static void SetInt(Type t, object o, string f, int v) { t.GetField(f).SetValue(o, v); }
    static double GetField(Type t, object o, string f) { return Convert.ToDouble(t.GetField(f).GetValue(o)); }

    // Mirrors GrilleDialog.ApplyPreset. Keep in sync with the source.
    static void SetPresets(Type gs, object g, int shapeMode, int regionMode, double edgeSkipPercent)
    {
        SetInt(gs, g, "ShapeMode", shapeMode);
        SetInt(gs, g, "RegionMode", regionMode);
        SetField(gs, g, "WidthMm", 80.0);
        SetField(gs, g, "HeightMm", 44.0);
        SetField(gs, g, "CircleDiameterMm", 60.0);
        SetField(gs, g, "PitchMm", 3.0);
        SetField(gs, g, "CenterDiaMm", 2.0);
        SetField(gs, g, "MiddleDiaMm", 1.5);
        SetField(gs, g, "OuterDiaMm", 1.0);
        SetField(gs, g, "CenterZone", 0.42);
        SetField(gs, g, "MiddleZone", 0.72);
        SetField(gs, g, "SkipStartZone", 0.94);
        SetField(gs, g, "EdgeSkipPercent", edgeSkipPercent);
        SetField(gs, g, "HoneycombWebMm", 0.55);
        SetField(gs, g, "SoundWaveWebMm", 1.20);
        SetField(gs, g, "CornerRadiusMm", 12.0);
        SetField(gs, g, "OffsetXmm", 0.0);
        SetField(gs, g, "OffsetYmm", 0.0);

        if (shapeMode == 0) { SetField(gs, g, "CornerRadiusMm", 12); SetField(gs, g, "CenterDiaMm", 2.0); SetField(gs, g, "MiddleDiaMm", 1.5); SetField(gs, g, "OuterDiaMm", 1.0); SetField(gs, g, "PitchMm", 3.0); }
        if (shapeMode == 1) { SetField(gs, g, "CornerRadiusMm", 14); SetField(gs, g, "CenterDiaMm", 2.4); SetField(gs, g, "MiddleDiaMm", 2.4); SetField(gs, g, "OuterDiaMm", 2.4); SetField(gs, g, "HoneycombWebMm", 0.55); }
        if (shapeMode == 2) { SetField(gs, g, "CornerRadiusMm", 12); SetField(gs, g, "CenterDiaMm", 1.9); SetField(gs, g, "MiddleDiaMm", 1.45); SetField(gs, g, "OuterDiaMm", 1.0); SetField(gs, g, "PitchMm", 3.0); }
        if (shapeMode == 3) { SetField(gs, g, "CornerRadiusMm", 10); SetField(gs, g, "CenterDiaMm", 2.0); SetField(gs, g, "MiddleDiaMm", 1.6); SetField(gs, g, "OuterDiaMm", 1.1); SetField(gs, g, "PitchMm", 3.2); }
        if (shapeMode == 4) { SetField(gs, g, "CornerRadiusMm", 12); SetField(gs, g, "CenterDiaMm", 2.1); SetField(gs, g, "MiddleDiaMm", 1.6); SetField(gs, g, "OuterDiaMm", 1.1); SetField(gs, g, "PitchMm", 3.3); }
        if (shapeMode == 5) { SetField(gs, g, "CornerRadiusMm", 12); SetField(gs, g, "CenterDiaMm", 2.2); SetField(gs, g, "MiddleDiaMm", 1.7); SetField(gs, g, "OuterDiaMm", 1.2); SetField(gs, g, "PitchMm", 3.3); }
        if (shapeMode == 6) { SetField(gs, g, "CornerRadiusMm", 16); SetField(gs, g, "CenterDiaMm", 2.0); SetField(gs, g, "MiddleDiaMm", 1.5); SetField(gs, g, "OuterDiaMm", 1.0); SetField(gs, g, "PitchMm", 3.2); SetField(gs, g, "SoundWaveWebMm", 1.20); }
        // Mode 7 is locked to a circular footprint by design (the geometry layer forces RegionMode=1).
        if (shapeMode == 7) { SetField(gs, g, "CircleDiameterMm", 60); SetField(gs, g, "CenterDiaMm", 2.2); SetField(gs, g, "MiddleDiaMm", 1.6); SetField(gs, g, "OuterDiaMm", 1.0); SetField(gs, g, "PitchMm", 3.0); SetInt(gs, g, "RegionMode", 1); }
    }

    // Reconstructs the first polygon from its consecutive CreateLine endpoints and classifies it.
    // CreateRegularPolygon emits (v0,v1), (v1,v2), ... so line i starts at vertex i.
    static string DescribePolygon(List<double[]> lines, int sides)
    {
        if (lines.Count < sides || sides < 3) return "-";
        double[] vx = new double[sides];
        double[] vy = new double[sides];
        for (int i = 0; i < sides; i++) { vx[i] = lines[i][0]; vy[i] = lines[i][1]; }
        double cx = 0, cy = 0;
        for (int i = 0; i < sides; i++) { cx += vx[i]; cy += vy[i]; }
        cx /= sides; cy /= sides;

        double maxAbs = 0;
        for (int i = 0; i < sides; i++)
            maxAbs = Math.Max(maxAbs, Math.Max(Math.Abs(vx[i] - cx), Math.Abs(vy[i] - cy)));

        // Vertices at the corners of the bounding box -> axis-aligned square (each axis-aligned
        // square has |dx| and |dy| equally large). Vertices on the box edge midpoints -> diamond.
        int cornerLike = 0;
        for (int i = 0; i < sides; i++)
        {
            double ax = Math.Abs(vx[i] - cx), ay = Math.Abs(vy[i] - cy);
            if (Math.Min(ax, ay) > 0.25 * maxAbs) cornerLike++;
        }

        if (sides == 6) return "hexagon(flat-top)";
        if (sides == 3) return "triangle";
        if (sides == 4 && cornerLike == 4) return "square(axis-aligned)";
        if (sides == 4 && cornerLike == 0) return "diamond(45 deg)";
        return "quad(ambiguous," + cornerLike + ")";
    }

    static bool MirrorExists(List<double[]> holes, double x, double y, double r)
    {
        const double tol = 1e-6;
        foreach (double[] h in holes)
            if (Math.Abs(h[0] - x) < tol && Math.Abs(h[1] - y) < tol && Math.Abs(h[2] - r) < tol) return true;
        return false;
    }

    public static RunResult Run(string dllPath, int shapeMode, int regionMode, double edgeSkipPercent, double checkScale)
    {
        Runner r = new Runner();
        RunResult res = new RunResult();
        res.ShapeMode = shapeMode;
        res.SkipPercent = edgeSkipPercent;
        res.CheckScale = checkScale;

        Assembly asm = Assembly.LoadFrom(dllPath);
        Type addinType = asm.GetType("SpeakerGrillePro.SwAddin");
        object addin = Activator.CreateInstance(addinType);
        addinType.GetField("_swApp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(addin, r.MakeSwApp());

        object face = Make<IFakeFace>(delegate(string n, object[] a)
        {
            if (n == "GetClosestPointOn") return new double[] { Convert.ToDouble(a[0]), Convert.ToDouble(a[1]), Convert.ToDouble(a[2]) };
            if (n == "Select4") return true;
            return null;
        });

        Type gs = asm.GetType("SpeakerGrillePro.GrilleSettings");
        object g = Activator.CreateInstance(gs);
        SetPresets(gs, g, shapeMode, regionMode, edgeSkipPercent);

        MethodInfo mi = addinType.GetMethod("CreateGrille", BindingFlags.NonPublic | BindingFlags.Instance);
        try { mi.Invoke(addin, new object[] { r.MakeModel(), face, new double[] { 0, 0, 0 }, g }); }
        catch (Exception ex)
        {
            Exception inner = ex.InnerException != null ? ex.InnerException : ex;
            res.StopNote = inner.GetType().Name;
        }

        // ---- collect the emitted geometry ----
        List<double[]> holes = new List<double[]>();
        res.RoundHoles = r._circles.Count > 0;
        if (r._circles.Count > 0)
        {
            foreach (double[] c in r._circles) holes.Add(new double[] { c[0], c[1], c[2] });
        }
        else if (r._lines.Count > 0)
        {
            int sides = shapeMode == 1 ? 6 : (shapeMode == 5 ? 3 : 4);
            for (int i = 0; i + sides <= r._lines.Count; i += sides)
            {
                double sx = 0, sy = 0;
                for (int k = 0; k < sides; k++) { sx += r._lines[i + k][0]; sy += r._lines[i + k][1]; }
                double cx = sx / sides, cy = sy / sides;
                double dx = r._lines[i][0] - cx, dy = r._lines[i][1] - cy;
                holes.Add(new double[] { cx, cy, Math.Sqrt(dx * dx + dy * dy) });
            }
        }
        res.HoleCount = holes.Count;
        res.ProducedGeometry = holes.Count > 0;

        // ---- check region definition, in METRES (the add-in works in metres) ----
        bool circleRegion = (shapeMode == 7) || regionMode == 1;
        double circRReal = GetField(gs, g, "CircleDiameterMm") / 2000.0;
        double corner = GetField(gs, g, "CornerRadiusMm") / 1000.0;
        double circR = circRReal * checkScale;
        double halfW = 0.040 * checkScale;
        double halfH = 0.022 * checkScale;
        res.Region = circleRegion
            ? ("circle D=" + F(circRReal * 2000.0))
            : ("roundrect 80x44 R" + F(corner * 1000.0));

        foreach (double[] h in holes)
        {
            double x = h[0], y = h[1], rr = h[2];
            bool ok; double viol;
            if (circleRegion)
            {
                viol = Math.Sqrt(x * x + y * y) + rr - circR;
                ok = viol <= 1e-6;
            }
            else
            {
                double ax = Math.Abs(x), ay = Math.Abs(y);
                double ia = halfW - rr, ib = halfH - rr, irc = Math.Max(0.0, corner - rr);
                viol = Math.Max(ax + rr - halfW, ay + rr - halfH);
                if (ia < 0 || ib < 0) ok = false;
                else if (ax > ia + 1e-6 || ay > ib + 1e-6) ok = false;
                else
                {
                    double dx = ax - (ia - irc), dy = ay - (ib - irc);
                    double cornerViol = (dx > 0 && dy > 0) ? (Math.Sqrt(dx * dx + dy * dy) - irc) : 0.0;
                    ok = cornerViol <= 1e-6;
                    if (cornerViol > viol) viol = cornerViol;
                }
            }
            if (!ok) { res.Outside++; if (viol > res.WorstViolation) res.WorstViolation = viol; }
        }

        foreach (double[] h in holes)
        {
            if (!MirrorExists(holes, -h[0], h[1], h[2])) res.NoMirrorX++;
            if (!MirrorExists(holes, h[0], -h[1], h[2])) res.NoMirrorY++;
            if (!MirrorExists(holes, -h[0], -h[1], h[2])) res.NoMirrorXY++;
        }

        for (int i = 0; i < holes.Count; i++)
            for (int j = i + 1; j < holes.Count; j++)
            {
                double dx = holes[i][0] - holes[j][0], dy = holes[i][1] - holes[j][1];
                double web = Math.Sqrt(dx * dx + dy * dy) - holes[i][2] - holes[j][2];
                if (web < res.MinWeb) res.MinWeb = web;
            }

        if (r._lines.Count > 0)
        {
            int sides = shapeMode == 1 ? 6 : (shapeMode == 5 ? 3 : 4);
            res.Shape = DescribePolygon(r._lines, sides);
        }
        return res;
    }
}

class Program
{
    static int _failures;

    static void Report(RunResult r)
    {
        bool violation = r.Outside > 0 || r.NoMirrorX > 0 || r.NoMirrorY > 0 || r.NoMirrorXY > 0;
        bool empty = !r.ProducedGeometry;
        if (violation || empty) _failures++;

        Console.WriteLine("mode=" + r.ShapeMode +
            " skip=" + r.SkipPercent.ToString("0", CultureInfo.InvariantCulture).PadLeft(2) +
            "  " + r.Region.PadRight(20) +
            " check=" + (r.CheckScale * 100).ToString("0", CultureInfo.InvariantCulture).PadLeft(3) + "%" +
            "  holes=" + r.HoleCount.ToString().PadLeft(4) +
            "  outside=" + r.Outside.ToString().PadLeft(4) +
            "  asym(x/y/xy)=" + r.NoMirrorX + "/" + r.NoMirrorY + "/" + r.NoMirrorXY +
            "  minWeb_mm=" + (r.RoundHoles && r.MinWeb != double.MaxValue ? Runner.Mm(r.MinWeb) : "n/a").PadLeft(6) +
            "  shape=" + r.Shape.PadRight(19) +
            (empty ? "   <<< NO GEOMETRY EMITTED" : (r.StopNote.Length > 0 ? "   [cut not simulated: " + r.StopNote + "]" : "")));

        if (r.Outside > 0)
            Console.WriteLine("        worst boundary violation: " + Runner.Mm(r.WorstViolation) + " mm");
        if (r.NoMirrorX + r.NoMirrorY + r.NoMirrorXY > 0)
            Console.WriteLine("        no mirror partner for " + (r.NoMirrorX + r.NoMirrorY + r.NoMirrorXY) + " of " + r.HoleCount + " holes");
    }

    static void RunGroup(string dllPath, string title, int regionMode, double skip, double checkScale)
    {
        Console.WriteLine("### " + title + " ###");
        for (int m = 0; m <= 6; m++) Report(Runner.Run(dllPath, m, regionMode, skip, checkScale));
        Console.WriteLine();
    }

    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("usage: harness.exe <full path to bin\\SpeakerGrillePro.dll>");
            return 2;
        }
        string dll = args[0];

        RunGroup(dll, "patterns 0-6, ROUNDED RECT region (80 x 44 mm)", 0, 0, 1.0);
        RunGroup(dll, "patterns 0-6, CIRCLE region (D = 60 mm)", 1, 0, 1.0);

        Console.WriteLine("### pattern 7 (circular footprint locked by design) ###");
        Report(Runner.Run(dll, 7, 1, 0, 1.0));
        Console.WriteLine();

        Console.WriteLine("### edge-skip symmetry probe (EdgeSkipPercent = 15) ###");
        Report(Runner.Run(dll, 0, 0, 15, 1.0));
        Report(Runner.Run(dll, 2, 0, 15, 1.0));
        Console.WriteLine();

        // The boundary checker must be able to fail, otherwise a clean result proves nothing.
        // Only "this run produced boundary violations" counts - a run that fails for some other
        // reason (e.g. no geometry at all) must NOT be mistaken for a working checker.
        Console.WriteLine("### checker self-test: identical geometry, check region shrunk to 25% ###");
        int before = _failures;
        RunResult s0 = Runner.Run(dll, 0, 0, 0, 0.25);
        RunResult s6 = Runner.Run(dll, 6, 0, 0, 0.25);
        RunResult s7 = Runner.Run(dll, 7, 1, 0, 0.25);
        Report(s0);
        Report(s6);
        Report(s7);
        int flagged = (s0.Outside > 0 ? 1 : 0) + (s6.Outside > 0 ? 1 : 0) + (s7.Outside > 0 ? 1 : 0);
        _failures = before;   // the self-test is expected to fail; only "flagged" decides
        Console.WriteLine();

        if (flagged < 3)
        {
            Console.WriteLine("SELF-TEST FAILED: the boundary checker flagged the deliberately shrunk region in only " + flagged + " of 3 runs.");
            Console.WriteLine("                 A clean result from this harness would be meaningless.");
            _failures++;
        }
        else
        {
            Console.WriteLine("SELF-TEST OK: the boundary checker flagged the shrunk region in 3 of 3 runs.");
        }
        Console.WriteLine();

        if (_failures == 0)
        {
            Console.WriteLine("VERDICT: PASS - no boundary violations, no mirror-symmetry violations, geometry produced everywhere.");
            return 0;
        }
        Console.WriteLine("VERDICT: FAIL - " + _failures + " problem(s) found.");
        return 1;
    }
}
