using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Waterpolo stands (low-poly) + a crowd of 2D eggs that hop during Play. Everything is generated here, no other assets.
// Put it on an empty GameObject at the centre of the pool and set length/width to the pool rectangle.
[ExecuteAlways]
public class Gradas : MonoBehaviour
{
    [Header("Piscina (rectángulo interior, metros)")]
    public float length = 32f;
    public float width = 18f;

    [Header("Gradas")]
    public float gap = 2f;                 // walkway between the pool edge and the first row
    public int rows = 8;
    public float rowDepth = 0.9f, rowRise = 0.45f, baseHeight = 1f;
    public Material standsMaterial;        // optional; grey default material if empty

    [Header("Público")]
    public float seatSpacing = 0.6f;
    public float eggHeight = 0.8f;
    public float jumpHeight = 0.15f;
    public Vector2 jumpSpeed = new Vector2(4f, 8f);
    [Range(0f, 1f)] public float fill = 0.85f;
    public int seed = 1;
    public Color[] colors =
    {
        new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.95f), new Color(0.95f, 0.8f, 0.15f),
        new Color(0.25f, 0.8f, 0.3f), new Color(0.95f, 0.5f, 0.1f), new Color(0.7f, 0.3f, 0.9f), Color.white,
    };

    const int EggSides = 10, EggVerts = EggSides + 1;  // fan: centre + outline

    GameObject standsGo, crowdGo;
    Mesh crowd;
    Vector3[] baseVerts, verts;
    float[] phase, speed;
    bool dirty = true;

    void OnEnable() => dirty = true;
    void OnValidate() => dirty = true;
    void OnDisable() => Clear();

    void Update()
    {
        if (dirty) Build();
        if (Application.isPlaying) Animate(Time.time);
    }

    public void Animate(float t)
    {
        if (crowd == null) return;
        for (int e = 0; e < phase.Length; e++)
        {
            var dy = Vector3.up * (Mathf.Abs(Mathf.Sin(t * speed[e] + phase[e])) * jumpHeight);
            for (int j = e * EggVerts; j < (e + 1) * EggVerts; j++) verts[j] = baseVerts[j] + dy;
        }
        crowd.SetVertices(verts);
    }

    void Clear()
    {
        foreach (var go in new[] { standsGo, crowdGo })
        {
            if (go == null) continue;
            Kill(go.GetComponent<MeshFilter>().sharedMesh);
            foreach (var m in go.GetComponent<MeshRenderer>().sharedMaterials) if (m != standsMaterial) Kill(m);
            Kill(go);
        }
        standsGo = crowdGo = null; crowd = null;
    }

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    void Build()
    {
        dirty = false;
        Clear();
        var rnd = new System.Random(seed);
        float depth = gap + rows * rowDepth;
        // (edge centre, outward normal, length along the edge); long sides also cover the corners
        var sides = new[]
        {
            (new Vector3(0, 0, width / 2), Vector3.forward, length + 2 * depth),
            (new Vector3(0, 0, -width / 2), Vector3.back, length + 2 * depth),
            (new Vector3(length / 2, 0, 0), Vector3.right, width + 2 * gap),
            (new Vector3(-length / 2, 0, 0), Vector3.left, width + 2 * gap),
        };

        var sv = new List<Vector3>(); var st = new List<int>();
        var cv = new List<Vector3>(); var ct = new List<int>[colors.Length];
        for (int c = 0; c < ct.Length; c++) ct[c] = new List<int>();
        var ph = new List<float>(); var sp = new List<float>();

        foreach (var (edge, n, span) in sides)
        {
            var a = Vector3.Cross(Vector3.up, n);  // along the stand
            Vector3 P(float s, float d, float h) => edge + a * s + n * d + Vector3.up * h;
            float s0 = -span / 2, s1 = span / 2;
            for (int k = 0; k < rows; k++)
            {
                float d0 = gap + k * rowDepth, d1 = d0 + rowDepth, top = baseHeight + (k + 1) * rowRise;
                float below = k == 0 ? 0f : top - rowRise;  // riser goes down to the previous row's top
                Quad(sv, st, P(s0, d0, top), P(s1, d0, top), P(s1, d1, top), P(s0, d1, top), Vector3.up);
                Quad(sv, st, P(s0, d0, below), P(s1, d0, below), P(s1, d0, top), P(s0, d0, top), -n);
                Quad(sv, st, P(s0, d0, 0), P(s0, d1, 0), P(s0, d1, top), P(s0, d0, top), -a);  // end caps
                Quad(sv, st, P(s1, d0, 0), P(s1, d1, 0), P(s1, d1, top), P(s1, d0, top), a);

                int seats = Mathf.FloorToInt(span / seatSpacing);
                for (int i = 0; i < seats; i++)
                {
                    if (rnd.NextDouble() > fill) continue;
                    var foot = P(s0 + seatSpacing * (i + 0.5f), d0 + rowDepth * 0.45f, top);
                    Egg(cv, ct[rnd.Next(colors.Length)], foot, a, -n);
                    ph.Add((float)rnd.NextDouble() * Mathf.PI * 2f);
                    sp.Add(Mathf.Lerp(jumpSpeed.x, jumpSpeed.y, (float)rnd.NextDouble()));
                }
            }
            float back = baseHeight + rows * rowRise;
            Quad(sv, st, P(s0, depth, 0), P(s0, depth, back), P(s1, depth, back), P(s1, depth, 0), n);
        }

        var lit = standsMaterial != null ? standsMaterial : DefaultMaterial(new Color(0.55f, 0.57f, 0.6f));
        var standsMesh = new Mesh { name = "Escalones" };
        standsMesh.SetVertices(sv); standsMesh.SetTriangles(st, 0); standsMesh.RecalculateNormals();
        standsGo = Child("Escalones", standsMesh, new[] { lit });

        crowd = new Mesh { name = "Publico", indexFormat = cv.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        crowd.MarkDynamic();
        crowd.SetVertices(cv);
        crowd.subMeshCount = colors.Length;
        for (int c = 0; c < colors.Length; c++) crowd.SetTriangles(ct[c], c);
        crowd.RecalculateNormals();
        crowd.RecalculateBounds();
        var b = crowd.bounds; b.Expand(new Vector3(0, jumpHeight * 2, 0)); crowd.bounds = b;  // keep hops inside the culling box
        var mats = new Material[colors.Length];
        for (int c = 0; c < colors.Length; c++) mats[c] = DefaultMaterial(colors[c]);
        crowdGo = Child("Publico", crowd, mats);
        crowdGo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        baseVerts = cv.ToArray(); verts = cv.ToArray();
        phase = ph.ToArray(); speed = sp.ToArray();
    }

    // flat egg facing `face`, bottom at `foot`, wider at the bottom
    void Egg(List<Vector3> v, List<int> t, Vector3 foot, Vector3 a, Vector3 face)
    {
        int c = v.Count;
        v.Add(foot + Vector3.up * eggHeight * 0.45f);
        for (int i = 0; i < EggSides; i++)
        {
            float th = 2f * Mathf.PI * i / EggSides, y = -Mathf.Cos(th);
            float x = 0.38f * eggHeight * Mathf.Sin(th) * (1f - 0.15f * y);
            v.Add(foot + a * x + Vector3.up * ((y + 1f) * 0.5f * eggHeight));
        }
        for (int i = 0; i < EggSides; i++) Tri(v, t, c, c + 1 + i, c + 1 + (i + 1) % EggSides, face);
    }

    static void Quad(List<Vector3> v, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 face)
    {
        int c = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
        Tri(v, t, c, c + 1, c + 2, face); Tri(v, t, c, c + 2, c + 3, face);
    }

    // Unity's front face is the one whose Cross(b-a, c-a) points at the viewer
    static void Tri(List<Vector3> v, List<int> t, int i0, int i1, int i2, Vector3 face)
    {
        bool flip = Vector3.Dot(Vector3.Cross(v[i1] - v[i0], v[i2] - v[i0]), face) < 0;
        t.Add(i0); t.Add(flip ? i2 : i1); t.Add(flip ? i1 : i2);
    }

    GameObject Child(string name, Mesh mesh, Material[] mats)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterials = mats;
        return go;
    }

    // the active pipeline's own default lit material (URP or Built-in), recoloured
    static Material DefaultMaterial(Color color)
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        var m = rp != null ? new Material(rp.defaultMaterial) : new Material(Shader.Find("Standard"));
        m.color = color;
        m.hideFlags = HideFlags.DontSave;
        return m;
    }
}
