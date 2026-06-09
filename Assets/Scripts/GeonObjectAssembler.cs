using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assembles 48 geon objects (24 simple, 24 complex) following the constraints
/// from Meijer & Van der Lubbe (2011):
///   - 24 distinct geons
///   - Each geon used as main body exactly 2 times (once simple, once complex)
///   - Each geon appears 8 times total across all 48 objects
///   - All components within one object are dissimilar
/// </summary>
public static class GeonObjectAssembler
{
    public struct ObjectRecipe
    {
        public int objectId;
        public bool isComplex;
        public int mainBodyGeon;
        public int[] attachmentGeons;
    }

    /// <summary>
    /// Generates 48 object recipes satisfying all paper constraints.
    /// </summary>
    public static ObjectRecipe[] GenerateRecipes(int seed = 42)
    {
        var rng = new System.Random(seed);
        const int geonCount = 24;
        const int simpleCount = 24;
        const int complexCount = 24;
        const int totalObjects = 48;
        const int simpleAttachments = 2;  // 3 total - 1 main
        const int complexAttachments = 4; // 5 total - 1 main
        const int totalUsesPerGeon = 8;

        // Step 1: Assign main bodies.
        // Each geon is main body exactly once for simple, once for complex.
        int[] simpleMainBodies = ShuffledRange(geonCount, rng);
        int[] complexMainBodies = ShuffledRange(geonCount, rng);

        // Track total usage count per geon (including main body uses).
        // Each geon starts with 2 uses (one simple main, one complex main).
        int[] usageCounts = new int[geonCount];
        for (int i = 0; i < geonCount; i++)
        {
            usageCounts[i] = 2; // main body: once simple + once complex
        }

        // Step 2: Assign attachments using a constraint-satisfaction greedy approach.
        // Total attachment slots: 24*2 + 24*4 = 48 + 96 = 144
        // Each geon needs 6 more uses (8 total - 2 main body) = 24*6 = 144. Checks out.

        var recipes = new ObjectRecipe[totalObjects];

        // Build simple objects first (index 0..23), then complex (24..47)
        var objectOrder = new List<int>();
        for (int i = 0; i < totalObjects; i++) objectOrder.Add(i);
        Shuffle(objectOrder, rng);

        // Initialize recipes with main bodies
        for (int i = 0; i < simpleCount; i++)
        {
            recipes[i].objectId = i;
            recipes[i].isComplex = false;
            recipes[i].mainBodyGeon = simpleMainBodies[i];
            recipes[i].attachmentGeons = new int[simpleAttachments];
            for (int j = 0; j < simpleAttachments; j++) recipes[i].attachmentGeons[j] = -1;
        }
        for (int i = 0; i < complexCount; i++)
        {
            int idx = simpleCount + i;
            recipes[idx].objectId = idx;
            recipes[idx].isComplex = true;
            recipes[idx].mainBodyGeon = complexMainBodies[i];
            recipes[idx].attachmentGeons = new int[complexAttachments];
            for (int j = 0; j < complexAttachments; j++) recipes[idx].attachmentGeons[j] = -1;
        }

        // Fill attachment slots in a randomized order to distribute evenly
        foreach (int objIdx in objectOrder)
        {
            int slotCount = recipes[objIdx].isComplex ? complexAttachments : simpleAttachments;
            int mainGeon = recipes[objIdx].mainBodyGeon;
            var usedInObject = new HashSet<int>();
            usedInObject.Add(mainGeon);

            for (int slot = 0; slot < slotCount; slot++)
            {
                // Find candidate geons: not already in this object, usage < totalUsesPerGeon
                var candidates = new List<int>();
                for (int g = 0; g < geonCount; g++)
                {
                    if (!usedInObject.Contains(g) && usageCounts[g] < totalUsesPerGeon)
                    {
                        candidates.Add(g);
                    }
                }

                if (candidates.Count == 0)
                {
                    // Fallback: relax the usage constraint slightly
                    for (int g = 0; g < geonCount; g++)
                    {
                        if (!usedInObject.Contains(g))
                            candidates.Add(g);
                    }
                }

                // Prefer geons with the lowest usage count to keep distribution balanced
                candidates.Sort((a, b) => usageCounts[a].CompareTo(usageCounts[b]));
                int minUsage = usageCounts[candidates[0]];
                var bestCandidates = new List<int>();
                foreach (int c in candidates)
                {
                    if (usageCounts[c] == minUsage)
                        bestCandidates.Add(c);
                    else
                        break;
                }

                int chosen = bestCandidates[rng.Next(bestCandidates.Count)];
                recipes[objIdx].attachmentGeons[slot] = chosen;
                usedInObject.Add(chosen);
                usageCounts[chosen]++;
            }
        }

        return recipes;
    }

    /// <summary>
    /// Builds a GameObject hierarchy from a recipe.
    /// Returns the root GameObject (not yet saved as prefab).
    /// </summary>
    public static GameObject BuildObject(ObjectRecipe recipe, Mesh[] geonMeshes, Material material,
        float mainScale = 1.0f, float attachScale = 0.55f, int seed = 42,
        int searchDirections = 32, float flatOverlap = 0.01f, float curvedOverlap = 0.12f)
    {
        string prefix = recipe.isComplex ? "Complex" : "Simple";
        int localId = recipe.isComplex ? recipe.objectId - 24 + 1 : recipe.objectId + 1;
        string objName = string.Format("{0}_{1:D2}", prefix, localId);

        var root = new GameObject(objName);

        // Main body
        Mesh mainMesh = geonMeshes[recipe.mainBodyGeon];
        var mainBody = CreateMeshChild("MainBody", mainMesh, material, root.transform);
        mainBody.transform.localScale = Vector3.one * mainScale;

        // Cache main body geometry once for raycasting.
        var mainGeom = MeshGeom.From(mainMesh, mainScale);

        // Each attachment uses its own bottom face as the contact face, mated to a
        // patch on the main body that we find by random raycasts. The patch's
        // flatness drives the overlap: flat contact → flatOverlap (clean seam),
        // curved contact → curvedOverlap (sink deeper so the rounding doesn't gap).
        var usedNormals = new List<Vector3>();
        const float spreadDot = 0.6f; // require new patches at least ~53° away from previous ones
        int mainSearchDirections = Mathf.Max(10, searchDirections);
        bool mainAlwaysFlat = IsAlwaysFlat(mainMesh.name);

        for (int i = 0; i < recipe.attachmentGeons.Length; i++)
        {
            int geonIdx = recipe.attachmentGeons[i];
            string childName = string.Format("Attachment_{0}", i);
            Mesh attachMesh = geonMeshes[geonIdx];
            var child = CreateMeshChild(childName, attachMesh, material, root.transform);
            child.transform.localScale = Vector3.one * attachScale;

            var slotRng = new System.Random(recipe.objectId * 100 + i + seed);
            var attachGeom = MeshGeom.From(attachMesh, attachScale);

            FlatPatch mainPatch = FindFlatPatch(mainGeom, slotRng, usedNormals, spreadDot, mainSearchDirections);
            if (mainAlwaysFlat) mainPatch.flatness = 1f;

            // Attachment side: always the bottom face, in scaled local space.
            FlatPatch attachPatch;
            attachPatch.point = new Vector3(0f, -attachMesh.bounds.extents.y * attachScale, 0f);
            attachPatch.normal = Vector3.down;
            attachPatch.flatness = IsAlwaysFlat(attachMesh.name)
                ? 1f
                : FlatnessAlong(attachGeom, Vector3.down, slotRng);

            // Rotate the attachment so its bottom face points opposite the main
            // body's contact normal (i.e. the bottom meets the chosen patch).
            Quaternion rot = Quaternion.FromToRotation(attachPatch.normal, -mainPatch.normal);

            // Overlap is driven by whichever side is curvier. Two flat faces meeting
            // (cube on cube) → flatOverlap. A sphere meeting a cube, or anything
            // meeting a sphere → curvedOverlap, because the contact reduces to a
            // point and a flat-touching offset would leave a visible gap.
            // The flatness^2 bias makes anything short of a true plane lean toward
            // curvedOverlap — a flat 0.5 patch lerps at 0.25, not 0.5.
            float contactFlatness = Mathf.Min(mainPatch.flatness, attachPatch.flatness);
            float t = Mathf.Clamp01(contactFlatness);
            t = t * t;
            float overlap = Mathf.Lerp(curvedOverlap, flatOverlap, t);

            Vector3 rotatedAttachPoint = rot * attachPatch.point;
            Vector3 pos = mainPatch.point - rotatedAttachPoint - mainPatch.normal * overlap;

            // If the attachment's bottom is flat, shrink it until every bottom
            // vertex either sits on the main body's surface or is inside it. This
            // removes the "hanging cantilever" look where part of the bottom face
            // overhangs the main body's contact region.
            const float kFlatBottomThreshold = 0.95f;
            if (attachPatch.flatness >= kFlatBottomThreshold)
            {
                Vector3 contactWorld = mainPatch.point - mainPatch.normal * overlap;
                float fittedScale = FitAttachmentScaleToBody(attachMesh, attachScale, contactWorld, rot, mainPatch.normal, mainGeom);
                if (fittedScale < attachScale * 0.999f)
                {
                    child.transform.localScale = Vector3.one * fittedScale;
                    Vector3 newBottomCenter = new Vector3(0f, -attachMesh.bounds.extents.y * fittedScale, 0f);
                    pos = mainPatch.point - rot * newBottomCenter - mainPatch.normal * overlap;
                }
            }

            child.transform.localPosition = pos;
            child.transform.localRotation = rot;

            usedNormals.Add(mainPatch.normal);
        }

        // Add metadata
        var info = root.AddComponent<GeonObjectInfo>();
        info.objectId = recipe.objectId;
        info.complexity = recipe.isComplex ? GeonObjectInfo.Complexity.Complex : GeonObjectInfo.Complexity.Simple;
        info.mainBodyGeonIndex = recipe.mainBodyGeon;
        info.attachmentGeonIndices = (int[])recipe.attachmentGeons.Clone();
        info.objectName = objName;

        var names = new List<string>();
        names.Add(GeonMeshFactory.GetGeonName((GeonMeshFactory.GeonType)recipe.mainBodyGeon));
        foreach (int g in recipe.attachmentGeons)
            names.Add(GeonMeshFactory.GetGeonName((GeonMeshFactory.GeonType)g));
        info.geonNames = names.ToArray();

        return root;
    }

    static GameObject CreateMeshChild(string name, Mesh mesh, Material material, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        return go;
    }

    // ─────────────────────── Flat-patch raycasting ───────────────────────

    struct FlatPatch
    {
        public Vector3 point;   // contact point in scaled mesh local space
        public Vector3 normal;  // outward surface normal (unit)
        public float flatness;  // 1 = perfectly flat neighborhood, lower = curvier
    }

    struct MeshGeom
    {
        public Vector3[] verts;   // already scaled
        public int[] tris;
        public Vector3[] faceN;   // geometric face normal per triangle (outward)
        public float radius;      // bounding sphere radius for ray origin distance

        public static MeshGeom From(Mesh mesh, float scale)
        {
            var src = mesh.vertices;
            var scaled = new Vector3[src.Length];
            for (int i = 0; i < src.Length; i++) scaled[i] = src[i] * scale;

            var tris = mesh.triangles;
            var faceN = new Vector3[tris.Length / 3];
            // Use a smooth vertex normal sample to disambiguate the outward direction
            // — Unity meshes store normals that already point outward.
            var vNorms = mesh.normals;
            for (int t = 0, k = 0; t < tris.Length; t += 3, k++)
            {
                Vector3 a = scaled[tris[t]];
                Vector3 b = scaled[tris[t + 1]];
                Vector3 c = scaled[tris[t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-12f) { faceN[k] = Vector3.up; continue; }
                n.Normalize();
                Vector3 vn = vNorms != null && vNorms.Length == src.Length ? vNorms[tris[t]] : (a.normalized);
                if (Vector3.Dot(n, vn) < 0f) n = -n;
                faceN[k] = n;
            }

            float maxR = mesh.bounds.extents.magnitude * scale + 1e-3f;
            return new MeshGeom { verts = scaled, tris = tris, faceN = faceN, radius = maxR };
        }
    }

    // Wide perturbation + tight coplanarity threshold so curved surfaces actually
    // score below 1.0. With these values a true plane scores ~1.0, a sphere
    // collapses to ~0.10, and partially-curved geons (cylinder side, cone slant)
    // sit well below 0.5 — enough to give the curved-overlap branch real weight.
    const int kFlatnessNeighbors = 12;
    const float kFlatnessConeRad = 0.35f;     // ~20° perturbation half-angle
    const float kFlatnessCoplanarDot = 0.995f; // ~5.7° from center normal

    // Geons whose flat caps are too small for random raycasts to land on
    // reliably. We force their flatness to 1 so the curved-overlap branch
    // doesn't fire and bury attachments into them. Add more names as needed.
    static readonly System.Collections.Generic.HashSet<string> kAlwaysFlatGeons =
        new System.Collections.Generic.HashSet<string> { "HalfTorus", "CurvedCylinder", "ObliqueCylinder", "Barrel", "Torus", "CurvedTruncatedCone" };

    static bool IsAlwaysFlat(string meshName)
    {
        return !string.IsNullOrEmpty(meshName) && kAlwaysFlatGeons.Contains(meshName);
    }

    // Bisect on uniform scale, returning the largest scale ≤ maxScale at which
    // every "bottom" vertex of the attachment mesh (in scaled mesh local space)
    // lands inside or on the main body when transformed to world space.
    static float FitAttachmentScaleToBody(Mesh attachMesh, float maxScale, Vector3 contactWorld,
        Quaternion rot, Vector3 mainNormal, MeshGeom mainGeom)
    {
        // Collect horizontal offsets of the mesh's bottom-face vertices once.
        var meshVerts = attachMesh.vertices;
        float bottomY = attachMesh.bounds.min.y;
        float yTol = attachMesh.bounds.extents.y * 0.02f + 1e-4f;
        var offsets = new List<Vector3>();
        for (int i = 0; i < meshVerts.Length; i++)
        {
            Vector3 v = meshVerts[i];
            if (v.y - bottomY <= yTol)
                offsets.Add(new Vector3(v.x, 0f, v.z));
        }
        if (offsets.Count == 0) return maxScale;

        if (AllBottomVertsSupported(maxScale, contactWorld, rot, mainNormal, offsets, mainGeom))
            return maxScale;

        // Scale 0 trivially places every vertex at the (on-surface) contact point.
        float lo = 0f, hi = maxScale;
        for (int it = 0; it < 12; it++)
        {
            float mid = (lo + hi) * 0.5f;
            if (AllBottomVertsSupported(mid, contactWorld, rot, mainNormal, offsets, mainGeom)) lo = mid;
            else hi = mid;
        }
        return lo;
    }

    static bool AllBottomVertsSupported(float scale, Vector3 contactWorld, Quaternion rot,
        Vector3 mainNormal, List<Vector3> offsets, MeshGeom mainGeom)
    {
        const float surfaceEps = 0.005f;
        for (int i = 0; i < offsets.Count; i++)
        {
            Vector3 worldVert = contactWorld + rot * (offsets[i] * scale);
            // Nudge slightly into the body so vertices exactly on the contact plane
            // count as supported rather than as "outside on the boundary".
            Vector3 testPoint = worldVert - mainNormal * surfaceEps;
            if (!IsPointInsideMain(mainGeom, testPoint)) return false;
        }
        return true;
    }

    static bool IsPointInsideMain(MeshGeom geom, Vector3 point)
    {
        // Standard ray-casting parity test. The direction is deliberately
        // off-axis to avoid coplanar/edge-grazing degeneracies on axis-aligned
        // geons (cube faces, prism sides).
        Vector3 dir = new Vector3(0.317f, 0.547f, 0.776f).normalized;
        int hits = 0;
        for (int t = 0; t < geom.tris.Length; t += 3)
        {
            Vector3 a = geom.verts[geom.tris[t]];
            Vector3 b = geom.verts[geom.tris[t + 1]];
            Vector3 c = geom.verts[geom.tris[t + 2]];
            if (RayTriangle(point, dir, a, b, c, out _)) hits++;
        }
        return (hits & 1) == 1;
    }

    static FlatPatch FindFlatPatch(MeshGeom geom, System.Random rng, List<Vector3> avoidNormals, float avoidDot, int samples)
    {
        int kSamples = Mathf.Max(10, samples);

        FlatPatch best = default;
        best.flatness = -1f;

        for (int s = 0; s < kSamples; s++)
        {
            Vector3 dir = RandomUnitVector(rng);
            Vector3 origin = dir * geom.radius * 2f;
            Vector3 rayDir = -dir;

            if (!RaycastMesh(geom, origin, rayDir, out Vector3 hp, out Vector3 hn))
                continue;

            // Reject patches whose normal is close to a previously-used patch so
            // multiple attachments end up spread around the body, not stacked.
            if (avoidNormals != null && IsTooClose(hn, avoidNormals, avoidDot))
                continue;

            float flatness = NeighborhoodFlatness(geom, dir, hn, rng);

            if (flatness > best.flatness)
            {
                best.flatness = flatness;
                best.point = hp;
                best.normal = hn;
            }
        }

        // Fallback: if every random sample was rejected, just take the first hit
        // we can get along a random direction so we still produce something.
        if (best.flatness < 0f)
        {
            for (int s = 0; s < kSamples && best.flatness < 0f; s++)
            {
                Vector3 dir = RandomUnitVector(rng);
                if (RaycastMesh(geom, dir * geom.radius * 2f, -dir, out Vector3 hp, out Vector3 hn))
                {
                    best.point = hp;
                    best.normal = hn;
                    best.flatness = 0f;
                }
            }
        }

        return best;
    }

    // Flatness around the patch hit by `dir` with center normal `centerN`. Count
    // perturbed rays whose hit normal stays within the coplanarity threshold.
    static float NeighborhoodFlatness(MeshGeom geom, Vector3 dir, Vector3 centerN, System.Random rng)
    {
        int flatHits = 0;
        for (int n = 0; n < kFlatnessNeighbors; n++)
        {
            Vector3 perturbed = PerturbDirection(dir, kFlatnessConeRad, rng);
            if (RaycastMesh(geom, perturbed * geom.radius * 2f, -perturbed, out _, out Vector3 pn))
            {
                if (Vector3.Dot(pn, centerN) > kFlatnessCoplanarDot) flatHits++;
            }
        }
        return (float)flatHits / kFlatnessNeighbors;
    }

    // Flatness at a specific outward direction — used for the attachment's bottom
    // face. Returns 1 for a true flat face (cube bottom), low for a curved patch
    // (sphere south pole, ellipsoid tip).
    static float FlatnessAlong(MeshGeom geom, Vector3 outwardDir, System.Random rng)
    {
        Vector3 origin = outwardDir * geom.radius * 2f;
        if (!RaycastMesh(geom, origin, -outwardDir, out _, out Vector3 hn)) return 0f;
        return NeighborhoodFlatness(geom, outwardDir, hn, rng);
    }

    static bool IsTooClose(Vector3 n, List<Vector3> used, float dotThreshold)
    {
        for (int i = 0; i < used.Count; i++)
            if (Vector3.Dot(used[i], n) > dotThreshold) return true;
        return false;
    }

    static bool RaycastMesh(MeshGeom geom, Vector3 origin, Vector3 dir, out Vector3 point, out Vector3 normal)
    {
        float bestT = float.MaxValue;
        int bestIdx = -1;
        for (int t = 0, k = 0; t < geom.tris.Length; t += 3, k++)
        {
            Vector3 a = geom.verts[geom.tris[t]];
            Vector3 b = geom.verts[geom.tris[t + 1]];
            Vector3 c = geom.verts[geom.tris[t + 2]];
            if (RayTriangle(origin, dir, a, b, c, out float dist) && dist > 1e-5f && dist < bestT)
            {
                bestT = dist;
                bestIdx = k;
            }
        }
        if (bestIdx < 0) { point = default; normal = default; return false; }
        point = origin + dir * bestT;
        normal = geom.faceN[bestIdx];
        return true;
    }

    static bool RayTriangle(Vector3 o, Vector3 d, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
    {
        const float eps = 1e-7f;
        Vector3 e1 = v1 - v0;
        Vector3 e2 = v2 - v0;
        Vector3 h = Vector3.Cross(d, e2);
        float a = Vector3.Dot(e1, h);
        if (a > -eps && a < eps) { t = 0; return false; }
        float f = 1f / a;
        Vector3 s = o - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0f || u > 1f) { t = 0; return false; }
        Vector3 q = Vector3.Cross(s, e1);
        float v = f * Vector3.Dot(d, q);
        if (v < 0f || u + v > 1f) { t = 0; return false; }
        t = f * Vector3.Dot(e2, q);
        return t > eps;
    }

    static Vector3 RandomUnitVector(System.Random rng)
    {
        // Marsaglia uniform sphere sampling
        float u1 = (float)rng.NextDouble() * 2f - 1f;
        float u2 = (float)rng.NextDouble() * Mathf.PI * 2f;
        float r = Mathf.Sqrt(Mathf.Max(0f, 1f - u1 * u1));
        return new Vector3(r * Mathf.Cos(u2), u1, r * Mathf.Sin(u2));
    }

    static Vector3 PerturbDirection(Vector3 dir, float coneHalfAngleRad, System.Random rng)
    {
        // Build an arbitrary orthonormal frame around dir
        Vector3 t = Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right;
        Vector3 bx = Vector3.Cross(dir, t).normalized;
        Vector3 by = Vector3.Cross(dir, bx);
        float phi = (float)rng.NextDouble() * Mathf.PI * 2f;
        float theta = coneHalfAngleRad * (float)rng.NextDouble();
        return (Mathf.Cos(theta) * dir + Mathf.Sin(theta) * (Mathf.Cos(phi) * bx + Mathf.Sin(phi) * by)).normalized;
    }

    // ─────────────────────── Helpers ───────────────────────

    static int[] ShuffledRange(int count, System.Random rng)
    {
        var arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
        }
        return arr;
    }

    static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}
