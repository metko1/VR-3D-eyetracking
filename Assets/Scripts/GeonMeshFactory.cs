using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates 24 distinct geon-like meshes based on Biederman's
/// Recognition-by-Components primitives. All meshes are normalized to fit
/// within a unit bounding box centered at the origin.
/// Compatible with Unity 2018.4.36f1.
/// </summary>
public static class GeonMeshFactory
{
    public enum GeonType
    {
        Cube = 0,
        Cylinder,
        Sphere,
        Cone,
        TruncatedCone,
        TallBox,
        FlatBox,
        TriangularPrism,
        PentagonalPrism,
        HexagonalPrism,
        CurvedWedge,
        Wedge,
        HalfCylinder,
        QuarterCylinder,
        Ellipsoid,
        HalfTorus,
        CurvedTruncatedCone,
        Barrel,
        Pyramid,
        TruncatedPyramid,
        CurvedCylinder,
        Torus,
        ObliqueCylinder,
        RoundedCube
    }

    public const int GeonCount = 24;

    public static string GetGeonName(GeonType type)
    {
        switch (type)
        {
            case GeonType.Cube: return "Cube";
            case GeonType.Cylinder: return "Cylinder";
            case GeonType.Sphere: return "Sphere";
            case GeonType.Cone: return "Cone";
            case GeonType.TruncatedCone: return "TruncatedCone";
            case GeonType.TallBox: return "TallBox";
            case GeonType.FlatBox: return "FlatBox";
            case GeonType.TriangularPrism: return "TriangularPrism";
            case GeonType.PentagonalPrism: return "PentagonalPrism";
            case GeonType.HexagonalPrism: return "HexagonalPrism";
            case GeonType.CurvedWedge: return "CurvedWedge";
            case GeonType.Wedge: return "Wedge";
            case GeonType.HalfCylinder: return "HalfCylinder";
            case GeonType.QuarterCylinder: return "QuarterCylinder";
            case GeonType.Ellipsoid: return "Ellipsoid";
            case GeonType.HalfTorus: return "HalfTorus";
            case GeonType.CurvedTruncatedCone: return "CurvedTruncatedCone";
            case GeonType.Barrel: return "Barrel";
            case GeonType.Pyramid: return "Pyramid";
            case GeonType.TruncatedPyramid: return "TruncatedPyramid";
            case GeonType.CurvedCylinder: return "CurvedCylinder";
            case GeonType.Torus: return "Torus";
            case GeonType.ObliqueCylinder: return "ObliqueCylinder";
            case GeonType.RoundedCube: return "RoundedCube";
            default: return "Unknown";
        }
    }

    public static Mesh Create(GeonType type)
    {
        Mesh mesh;
        switch (type)
        {
            case GeonType.Cube: mesh = CreateCube(); break;
            case GeonType.Cylinder: mesh = CreateCylinder(16, 0.5f, 0.5f, 1f); break;
            case GeonType.Sphere: mesh = CreateSphere(16, 12); break;
            case GeonType.Cone: mesh = CreateCone(16, 16, 0.5f, 1f); break;
            case GeonType.TruncatedCone: mesh = CreateCylinder(16, 0.5f, 0.25f, 1f); break;
            case GeonType.TallBox: mesh = CreateBox(0.5f, 1f, 0.5f); break;
            case GeonType.FlatBox: mesh = CreateBox(1f, 0.3f, 0.7f); break;
            case GeonType.TriangularPrism: mesh = CreatePrism(3); break;
            case GeonType.PentagonalPrism: mesh = CreatePrism(5); break;
            case GeonType.HexagonalPrism: mesh = CreatePrism(6); break;
            case GeonType.CurvedWedge: mesh = CreateCurvedWedge(); break;
            case GeonType.Wedge: mesh = CreateWedge(); break;
            case GeonType.HalfCylinder: mesh = CreatePartialCylinder(Mathf.PI); break;
            case GeonType.QuarterCylinder: mesh = CreatePartialCylinder(Mathf.PI * 0.5f); break;
            case GeonType.Ellipsoid: mesh = CreateEllipsoid(0.5f, 0.35f, 0.35f, 16, 12); break;
            case GeonType.HalfTorus: mesh = CreateHalfTorus(0.35f, 0.06f, 24, 10); break;
            case GeonType.CurvedTruncatedCone: mesh = CreateCurvedTruncatedCone(20, 12, 0.55f, 0.18f, 0.05f); break;
            case GeonType.Barrel: mesh = CreateBarrel(16, 12); break;
            case GeonType.Pyramid: mesh = CreatePyramid(4, false); break;
            case GeonType.TruncatedPyramid: mesh = CreatePyramid(4, true); break;
            case GeonType.CurvedCylinder: mesh = CreateCurvedCylinder(16, 12); break;
            case GeonType.Torus: mesh = CreateTorus(48, 0.4f, 0.15f); break;
            case GeonType.ObliqueCylinder: mesh = CreateObliqueCylinder(16); break;
            case GeonType.RoundedCube: mesh = CreateRoundedCube(0.15f, 4); break;
            default: mesh = CreateCube(); break;
        }

        mesh.name = GetGeonName(type);
        NormalizeMesh(mesh);
        return mesh;
    }

    /// <summary>
    /// Rescales mesh so its bounding box fits within a unit cube centered at origin.
    /// </summary>
    static void NormalizeMesh(Mesh mesh)
    {
        var bounds = mesh.bounds;
        Vector3 center = bounds.center;
        float maxExtent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        if (maxExtent < 0.0001f) return;

        float scale = 0.5f / maxExtent;
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = (verts[i] - center) * scale;
        }
        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    // ─────────────────────── Primitive Generators ───────────────────────

    static Mesh CreateCube()
    {
        return CreateBox(1f, 1f, 1f);
    }

    static Mesh CreateBox(float sx, float sy, float sz)
    {
        float hx = sx * 0.5f, hy = sy * 0.5f, hz = sz * 0.5f;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var norms = new List<Vector3>();

        // Each face has 4 verts with shared normal
        AddQuad(verts, norms, tris, // front
            new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz),
            new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.forward);
        AddQuad(verts, norms, tris, // back
            new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz),
            new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), Vector3.back);
        AddQuad(verts, norms, tris, // top
            new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz),
            new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz), Vector3.up);
        AddQuad(verts, norms, tris, // bottom
            new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz),
            new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz), Vector3.down);
        AddQuad(verts, norms, tris, // right
            new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz),
            new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), Vector3.right);
        AddQuad(verts, norms, tris, // left
            new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz),
            new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz), Vector3.left);

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateCylinder(int segments, float radiusBottom, float radiusTop, float height)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();
        float halfH = height * 0.5f;

        // Side
        for (int i = 0; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            Vector3 bot = new Vector3(cos * radiusBottom, -halfH, sin * radiusBottom);
            Vector3 top = new Vector3(cos * radiusTop, halfH, sin * radiusTop);

            // Slant outward normal: perpendicular to the line from (R_bot,-h/2) to (R_top,+h/2)
            float nr = height;
            float ny = radiusBottom - radiusTop;
            Vector3 n = new Vector3(cos * nr, ny, sin * nr).normalized;
            if (n.sqrMagnitude < 0.001f) n = new Vector3(cos, 0, sin);

            verts.Add(bot);
            verts.Add(top);
            norms.Add(n);
            norms.Add(n);
        }

        for (int i = 0; i < segments; i++)
        {
            int bi = i * 2;
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 2); tris.Add(bi + 1); tris.Add(bi + 3);
        }

        // Bottom cap
        if (radiusBottom > 0.001f)
        {
            int centerIdx = verts.Count;
            verts.Add(new Vector3(0, -halfH, 0));
            norms.Add(Vector3.down);
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radiusBottom, -halfH, Mathf.Sin(a) * radiusBottom));
                norms.Add(Vector3.down);
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(centerIdx);
                tris.Add(centerIdx + 1 + i);
                tris.Add(centerIdx + 1 + (i + 1) % (segments + 1));
            }
        }

        // Top cap
        if (radiusTop > 0.001f)
        {
            int centerIdx = verts.Count;
            verts.Add(new Vector3(0, halfH, 0));
            norms.Add(Vector3.up);
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radiusTop, halfH, Mathf.Sin(a) * radiusTop));
                norms.Add(Vector3.up);
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(centerIdx);
                tris.Add(centerIdx + 1 + (i + 1) % (segments + 1));
                tris.Add(centerIdx + 1 + i);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateCone(int segments, int heightSegments, float radius, float height)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();
        float halfH = height * 0.5f;

        // Outward slant normal at each circumferential seam. The cone is a straight
        // slant, so this normal is constant along the surface for a given angle. Its
        // upward tilt R/sqrt(R²+h²) makes it orthogonal to the slant — i.e. the apex
        // copy of this normal points "somewhat up" while still being perpendicular
        // to the cone surface.
        float nr = height;
        float ny = radius;
        var seamN = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            seamN[i] = new Vector3(Mathf.Cos(a) * nr, ny, Mathf.Sin(a) * nr).normalized;
        }

        // Side: stacked rings from base (t=0) to apex (t=1). The top ring collapses
        // to the apex point; each duplicate keeps the slant normal at its angle.
        for (int j = 0; j <= heightSegments; j++)
        {
            float t = (float)j / heightSegments;
            float y = -halfH + t * height;
            float r = radius * (1f - t);
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                norms.Add(seamN[i]);
            }
        }

        int stride = segments + 1;
        for (int j = 0; j < heightSegments; j++)
        {
            for (int i = 0; i < segments; i++)
            {
                int v00 = j * stride + i;
                int v01 = (j + 1) * stride + i;
                int v10 = j * stride + (i + 1);
                int v11 = (j + 1) * stride + (i + 1);
                tris.Add(v00); tris.Add(v01); tris.Add(v10);
                tris.Add(v10); tris.Add(v01); tris.Add(v11);
            }
        }

        // Bottom cap
        int centerIdx = verts.Count;
        verts.Add(new Vector3(0, -halfH, 0));
        norms.Add(Vector3.down);
        for (int i = 0; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, -halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.down);
        }
        for (int i = 0; i < segments; i++)
        {
            tris.Add(centerIdx);
            tris.Add(centerIdx + 1 + i);
            tris.Add(centerIdx + 1 + (i + 1) % (segments + 1));
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateSphere(int lonSegments, int latSegments)
    {
        return CreateEllipsoid(0.5f, 0.5f, 0.5f, lonSegments, latSegments);
    }

    static Mesh CreateEllipsoid(float rx, float ry, float rz, int lonSeg, int latSeg)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        for (int lat = 0; lat <= latSeg; lat++)
        {
            float theta = Mathf.PI * lat / latSeg;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            for (int lon = 0; lon <= lonSeg; lon++)
            {
                float phi = 2f * Mathf.PI * lon / lonSeg;
                float sinP = Mathf.Sin(phi);
                float cosP = Mathf.Cos(phi);

                Vector3 pos = new Vector3(rx * sinT * cosP, ry * cosT, rz * sinT * sinP);
                // Normal for ellipsoid: gradient of (x/rx)^2+(y/ry)^2+(z/rz)^2
                Vector3 n = new Vector3(pos.x / (rx * rx), pos.y / (ry * ry), pos.z / (rz * rz)).normalized;
                verts.Add(pos);
                norms.Add(n);
            }
        }

        int cols = lonSeg + 1;
        for (int lat = 0; lat < latSeg; lat++)
        {
            for (int lon = 0; lon < lonSeg; lon++)
            {
                int cur = lat * cols + lon;
                int next = cur + cols;
                tris.Add(cur); tris.Add(cur + 1); tris.Add(next);
                tris.Add(cur + 1); tris.Add(next + 1); tris.Add(next);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreatePrism(int sides)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();
        float halfH = 0.5f;
        float radius = 0.5f;

        // Side faces
        for (int i = 0; i < sides; i++)
        {
            float a0 = 2f * Mathf.PI * i / sides;
            float a1 = 2f * Mathf.PI * ((i + 1) % sides) / sides;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -halfH, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -halfH, Mathf.Sin(a1) * radius);
            Vector3 t0 = b0 + Vector3.up;
            Vector3 t1 = b1 + Vector3.up;

            Vector3 normal = Vector3.Cross(t0 - b0, b1 - b0).normalized;
            int idx = verts.Count;
            verts.Add(b0); verts.Add(b1); verts.Add(t1); verts.Add(t0);
            norms.Add(normal); norms.Add(normal); norms.Add(normal); norms.Add(normal);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
            tris.Add(idx); tris.Add(idx + 3); tris.Add(idx + 2);
        }

        // Top cap
        int topCenter = verts.Count;
        verts.Add(new Vector3(0, halfH, 0));
        norms.Add(Vector3.up);
        for (int i = 0; i < sides; i++)
        {
            float a = 2f * Mathf.PI * i / sides;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.up);
        }
        for (int i = 0; i < sides; i++)
        {
            tris.Add(topCenter);
            tris.Add(topCenter + 1 + (i + 1) % sides);
            tris.Add(topCenter + 1 + i);
        }

        // Bottom cap
        int botCenter = verts.Count;
        verts.Add(new Vector3(0, -halfH, 0));
        norms.Add(Vector3.down);
        for (int i = 0; i < sides; i++)
        {
            float a = 2f * Mathf.PI * i / sides;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, -halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.down);
        }
        for (int i = 0; i < sides; i++)
        {
            tris.Add(botCenter);
            tris.Add(botCenter + 1 + i);
            tris.Add(botCenter + 1 + (i + 1) % sides);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateWedge()
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        float h = 0.5f, w = 0.5f, d = 0.5f;
        // Wedge: a box where the top edge on one side collapses to the bottom
        // Vertices: bottom rect + top edge
        Vector3 v0 = new Vector3(-w, -h, -d); // bottom-back-left
        Vector3 v1 = new Vector3(w, -h, -d);  // bottom-back-right
        Vector3 v2 = new Vector3(w, -h, d);   // bottom-front-right
        Vector3 v3 = new Vector3(-w, -h, d);  // bottom-front-left
        Vector3 v4 = new Vector3(-w, h, -d);  // top-back-left
        Vector3 v5 = new Vector3(w, h, -d);   // top-back-right

        // Bottom
        AddQuad(verts, norms, tris, v0, v1, v2, v3, Vector3.down);
        // Back
        AddQuad(verts, norms, tris, v1, v0, v4, v5, Vector3.back);
        // Slope (front-top)
        Vector3 slopeN = Vector3.Cross(v2 - v3, v4 - v3).normalized;
        AddQuad(verts, norms, tris, v3, v2, v5, v4, slopeN);
        // Left triangle
        int idx = verts.Count;
        Vector3 leftN = Vector3.left;
        verts.Add(v0); verts.Add(v3); verts.Add(v4);
        norms.Add(leftN); norms.Add(leftN); norms.Add(leftN);
        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        // Right triangle
        idx = verts.Count;
        Vector3 rightN = Vector3.right;
        verts.Add(v2); verts.Add(v1); verts.Add(v5);
        norms.Add(rightN); norms.Add(rightN); norms.Add(rightN);
        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateCurvedWedge()
    {
        // Like a wedge but the slope face is curved (convex)
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        int segments = 8;
        float h = 0.5f, w = 0.5f, d = 0.5f;

        // Generate curved slope surface
        // Goes from bottom-front to top-back in an arc
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 0.5f;
            float y = -h + (h * 2f) * Mathf.Sin(angle);
            float z = d - (d * 2f) * (1f - Mathf.Cos(angle));

            Vector3 left = new Vector3(-w, y, z);
            Vector3 right = new Vector3(w, y, z);
            Vector3 n = new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

            verts.Add(left); verts.Add(right);
            norms.Add(n); norms.Add(n);
        }

        for (int i = 0; i < segments; i++)
        {
            int bi = i * 2;
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 1); tris.Add(bi + 3); tris.Add(bi + 2);
        }

        // Bottom face
        AddQuad(verts, norms, tris,
            new Vector3(-w, -h, -d), new Vector3(w, -h, -d),
            new Vector3(w, -h, d), new Vector3(-w, -h, d), Vector3.down);

        // Back face
        AddQuad(verts, norms, tris,
            new Vector3(w, -h, -d), new Vector3(-w, -h, -d),
            new Vector3(-w, h, -d), new Vector3(w, h, -d), Vector3.back);

        // Side faces (triangulated fans along the curve)
        // Left side
        int leftStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            verts.Add(verts[i * 2]); // reuse left-side curve positions
            norms.Add(Vector3.left);
        }
        verts.Add(new Vector3(-w, -h, -d)); norms.Add(Vector3.left);
        int leftBottomBack = verts.Count - 1;
        for (int i = 0; i < segments; i++)
        {
            tris.Add(leftBottomBack);
            tris.Add(leftStart + i);
            tris.Add(leftStart + i + 1);
        }

        // Right side
        int rightStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            verts.Add(verts[i * 2 + 1]);
            norms.Add(Vector3.right);
        }
        verts.Add(new Vector3(w, -h, -d)); norms.Add(Vector3.right);
        int rightBottomBack = verts.Count - 1;
        for (int i = 0; i < segments; i++)
        {
            tris.Add(rightBottomBack);
            tris.Add(rightStart + i + 1);
            tris.Add(rightStart + i);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreatePartialCylinder(float arcAngle)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        int segments = 16;
        float radius = 0.5f;
        float halfH = 0.5f;
        float startAngle = -arcAngle * 0.5f;

        // Curved outer surface
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = startAngle + t * arcAngle;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            Vector3 bot = new Vector3(cos * radius, -halfH, sin * radius);
            Vector3 top = new Vector3(cos * radius, halfH, sin * radius);
            Vector3 n = new Vector3(cos, 0, sin);

            verts.Add(bot); verts.Add(top);
            norms.Add(n); norms.Add(n);
        }

        for (int i = 0; i < segments; i++)
        {
            int bi = i * 2;
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 2); tris.Add(bi + 1); tris.Add(bi + 3);
        }

        // Side walls — one at each edge of the arc, from arc edge to center axis
        float endAngle = startAngle + arcAngle;

        // Wall at start edge
        float cosS = Mathf.Cos(startAngle);
        float sinS = Mathf.Sin(startAngle);
        Vector3 wallNormS = new Vector3(sinS, 0, -cosS).normalized; // perpendicular, pointing outward (matches winding)
        AddQuad(verts, norms, tris,
            new Vector3(0, -halfH, 0),
            new Vector3(0, halfH, 0),
            new Vector3(cosS * radius, halfH, sinS * radius),
            new Vector3(cosS * radius, -halfH, sinS * radius),
            wallNormS);

        // Wall at end edge
        float cosE = Mathf.Cos(endAngle);
        float sinE = Mathf.Sin(endAngle);
        Vector3 wallNormE = new Vector3(-sinE, 0, cosE).normalized; // perpendicular, pointing outward (matches winding)
        AddQuad(verts, norms, tris,
            new Vector3(cosE * radius, -halfH, sinE * radius),
            new Vector3(cosE * radius, halfH, sinE * radius),
            new Vector3(0, halfH, 0),
            new Vector3(0, -halfH, 0),
            wallNormE);

        // Top cap (fan)
        int topCenter = verts.Count;
        verts.Add(new Vector3(0, halfH, 0)); norms.Add(Vector3.up);
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = startAngle + t * arcAngle;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.up);
        }
        for (int i = 0; i < segments; i++)
        {
            tris.Add(topCenter);
            tris.Add(topCenter + 1 + i + 1);
            tris.Add(topCenter + 1 + i);
        }

        // Bottom cap
        int botCenter = verts.Count;
        verts.Add(new Vector3(0, -halfH, 0)); norms.Add(Vector3.down);
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = startAngle + t * arcAngle;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, -halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.down);
        }
        for (int i = 0; i < segments; i++)
        {
            tris.Add(botCenter);
            tris.Add(botCenter + 1 + i);
            tris.Add(botCenter + 1 + i + 1);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateHalfTorus(float majorR, float minorR, int majorSeg, int minorSeg)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        // Half torus: sweep from 0 to PI
        for (int i = 0; i <= majorSeg; i++)
        {
            float u = Mathf.PI * i / majorSeg;
            float cu = Mathf.Cos(u);
            float su = Mathf.Sin(u);

            for (int j = 0; j <= minorSeg; j++)
            {
                float v = 2f * Mathf.PI * j / minorSeg;
                float cv = Mathf.Cos(v);
                float sv = Mathf.Sin(v);

                float x = (majorR + minorR * cv) * cu;
                float y = minorR * sv;
                float z = (majorR + minorR * cv) * su;

                Vector3 n = new Vector3(cv * cu, sv, cv * su);
                verts.Add(new Vector3(x, y, z));
                norms.Add(n.normalized);
            }
        }

        int cols = minorSeg + 1;
        for (int i = 0; i < majorSeg; i++)
        {
            for (int j = 0; j < minorSeg; j++)
            {
                int cur = i * cols + j;
                int next = cur + cols;
                tris.Add(cur); tris.Add(cur + 1); tris.Add(next);
                tris.Add(cur + 1); tris.Add(next + 1); tris.Add(next);
            }
        }

        // Both caps face -Z (the U-shape opens toward -Z)
        AddDiscCap(verts, norms, tris, majorR, minorR, minorSeg, 0, Vector3.back);
        AddDiscCap(verts, norms, tris, majorR, minorR, minorSeg, Mathf.PI, Vector3.back);

        // Rotate -90° around X so the arch peaks at +Y and both caps face -Y.
        // Mapping: (x, y, z) → (x, z, -y). Proper rotation, winding preserved.
        for (int k = 0; k < verts.Count; k++)
        {
            Vector3 v = verts[k];
            verts[k] = new Vector3(v.x, v.z, -v.y);
            Vector3 nrm = norms[k];
            norms[k] = new Vector3(nrm.x, nrm.z, -nrm.y);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static void AddDiscCap(List<Vector3> verts, List<Vector3> norms, List<int> tris,
        float majorR, float minorR, int minorSeg, float u, Vector3 normal)
    {
        float cu = Mathf.Cos(u);
        float su = Mathf.Sin(u);

        int centerIdx = verts.Count;
        verts.Add(new Vector3(majorR * cu, 0, majorR * su));
        norms.Add(normal);

        for (int j = 0; j <= minorSeg; j++)
        {
            float v = 2f * Mathf.PI * j / minorSeg;
            float cv = Mathf.Cos(v);
            float sv = Mathf.Sin(v);
            float x = (majorR + minorR * cv) * cu;
            float y = minorR * sv;
            float z = (majorR + minorR * cv) * su;
            verts.Add(new Vector3(x, y, z));
            norms.Add(normal);
        }

        bool flip = u < 0.01f; // flip winding for the start cap
        for (int j = 0; j < minorSeg; j++)
        {
            if (flip)
            {
                tris.Add(centerIdx);
                tris.Add(centerIdx + 1 + (j + 1) % (minorSeg + 1));
                tris.Add(centerIdx + 1 + j);
            }
            else
            {
                tris.Add(centerIdx);
                tris.Add(centerIdx + 1 + j);
                tris.Add(centerIdx + 1 + (j + 1) % (minorSeg + 1));
            }
        }
    }

    static Mesh CreateCurvedTruncatedCone(int pathSeg, int radialSeg, float bendRadius, float bigR, float smallR)
    {
        // A truncated cone whose centerline follows a 90° arc — like a tapered
        // horn. Replaces the old Capsule shape.
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        for (int i = 0; i <= pathSeg; i++)
        {
            float t = (float)i / pathSeg;
            float angle = t * Mathf.PI * 0.5f;
            float r = Mathf.Lerp(bigR, smallR, t);

            Vector3 center = new Vector3(
                bendRadius * Mathf.Sin(angle),
                bendRadius * (1f - Mathf.Cos(angle)),
                0);

            Vector3 normalDir = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
            Vector3 binormal = Vector3.forward;

            for (int j = 0; j <= radialSeg; j++)
            {
                float phi = 2f * Mathf.PI * j / radialSeg;
                Vector3 offset = normalDir * Mathf.Cos(phi) + binormal * Mathf.Sin(phi);
                verts.Add(center + offset * r);
                norms.Add(offset);
            }
        }

        int cols = radialSeg + 1;
        for (int i = 0; i < pathSeg; i++)
        {
            for (int j = 0; j < radialSeg; j++)
            {
                int cur = i * cols + j;
                int next = cur + cols;
                tris.Add(cur); tris.Add(cur + 1); tris.Add(next);
                tris.Add(cur + 1); tris.Add(next + 1); tris.Add(next);
            }
        }

        Vector3 startCapN = new Vector3(-1, 0, 0);
        Vector3 endCapN = new Vector3(0, 1, 0);
        AddTubeCap(verts, norms, tris, startCapN, radialSeg, 0, cols, true);
        AddTubeCap(verts, norms, tris, endCapN, radialSeg, pathSeg * cols, cols, false);

        // Rotate +90° around Z so the broader (start) cap, originally facing -X,
        // ends up facing -Y (down). Mapping: (x, y, z) → (-y, x, z).
        for (int k = 0; k < verts.Count; k++)
        {
            Vector3 v = verts[k];
            verts[k] = new Vector3(-v.y, v.x, v.z);
            Vector3 nrm = norms[k];
            norms[k] = new Vector3(-nrm.y, nrm.x, nrm.z);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateBarrel(int lonSeg, int latSeg)
    {
        // Cylinder that bulges in the middle
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        float halfH = 0.5f;
        float baseR = 0.35f;
        float bulgeR = 0.5f;

        for (int lat = 0; lat <= latSeg; lat++)
        {
            float t = (float)lat / latSeg;
            float y = -halfH + t;
            // Sine-based bulge
            float r = baseR + (bulgeR - baseR) * Mathf.Sin(t * Mathf.PI);

            for (int lon = 0; lon <= lonSeg; lon++)
            {
                float phi = 2f * Mathf.PI * lon / lonSeg;
                float x = Mathf.Cos(phi) * r;
                float z = Mathf.Sin(phi) * r;
                verts.Add(new Vector3(x, y, z));

                // Approximate normal
                float dr = (bulgeR - baseR) * Mathf.Cos(t * Mathf.PI) * Mathf.PI;
                Vector3 n = new Vector3(Mathf.Cos(phi), -dr, Mathf.Sin(phi)).normalized;
                norms.Add(n);
            }
        }

        int cols = lonSeg + 1;
        for (int lat = 0; lat < latSeg; lat++)
        {
            for (int lon = 0; lon < lonSeg; lon++)
            {
                int cur = lat * cols + lon;
                int next = cur + cols;
                tris.Add(cur); tris.Add(next); tris.Add(cur + 1);
                tris.Add(cur + 1); tris.Add(next); tris.Add(next + 1);
            }
        }

        // Top cap
        int topCenter = verts.Count;
        verts.Add(new Vector3(0, halfH, 0)); norms.Add(Vector3.up);
        for (int lon = 0; lon <= lonSeg; lon++)
        {
            float phi = 2f * Mathf.PI * lon / lonSeg;
            verts.Add(new Vector3(Mathf.Cos(phi) * baseR, halfH, Mathf.Sin(phi) * baseR));
            norms.Add(Vector3.up);
        }
        for (int i = 0; i < lonSeg; i++)
        {
            tris.Add(topCenter); tris.Add(topCenter + 1 + i + 1); tris.Add(topCenter + 1 + i);
        }

        // Bottom cap
        int botCenter = verts.Count;
        verts.Add(new Vector3(0, -halfH, 0)); norms.Add(Vector3.down);
        for (int lon = 0; lon <= lonSeg; lon++)
        {
            float phi = 2f * Mathf.PI * lon / lonSeg;
            verts.Add(new Vector3(Mathf.Cos(phi) * baseR, -halfH, Mathf.Sin(phi) * baseR));
            norms.Add(Vector3.down);
        }
        for (int i = 0; i < lonSeg; i++)
        {
            tris.Add(botCenter); tris.Add(botCenter + 1 + i); tris.Add(botCenter + 1 + i + 1);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreatePyramid(int sides, bool truncated)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        float halfH = 0.5f;
        float baseR = 0.5f;
        float topR = truncated ? 0.2f : 0f;

        // Side faces
        for (int i = 0; i < sides; i++)
        {
            float a0 = 2f * Mathf.PI * i / sides;
            float a1 = 2f * Mathf.PI * ((i + 1) % sides) / sides;

            Vector3 b0 = new Vector3(Mathf.Cos(a0) * baseR, -halfH, Mathf.Sin(a0) * baseR);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * baseR, -halfH, Mathf.Sin(a1) * baseR);

            if (truncated)
            {
                Vector3 t0 = new Vector3(Mathf.Cos(a0) * topR, halfH, Mathf.Sin(a0) * topR);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * topR, halfH, Mathf.Sin(a1) * topR);
                Vector3 n = Vector3.Cross(t0 - b0, b1 - b0).normalized;
                int idx = verts.Count;
                verts.Add(b0); verts.Add(b1); verts.Add(t1); verts.Add(t0);
                norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
                tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
                tris.Add(idx); tris.Add(idx + 3); tris.Add(idx + 2);
            }
            else
            {
                Vector3 apex = new Vector3(0, halfH, 0);
                Vector3 n = Vector3.Cross(apex - b0, b1 - b0).normalized;
                int idx = verts.Count;
                verts.Add(b0); verts.Add(b1); verts.Add(apex);
                norms.Add(n); norms.Add(n); norms.Add(n);
                tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
            }
        }

        // Base
        int baseCenter = verts.Count;
        verts.Add(new Vector3(0, -halfH, 0)); norms.Add(Vector3.down);
        for (int i = 0; i < sides; i++)
        {
            float a = 2f * Mathf.PI * i / sides;
            verts.Add(new Vector3(Mathf.Cos(a) * baseR, -halfH, Mathf.Sin(a) * baseR));
            norms.Add(Vector3.down);
        }
        for (int i = 0; i < sides; i++)
        {
            tris.Add(baseCenter);
            tris.Add(baseCenter + 1 + i);
            tris.Add(baseCenter + 1 + (i + 1) % sides);
        }

        // Top face (truncated only)
        if (truncated)
        {
            int topCenter = verts.Count;
            verts.Add(new Vector3(0, halfH, 0)); norms.Add(Vector3.up);
            for (int i = 0; i < sides; i++)
            {
                float a = 2f * Mathf.PI * i / sides;
                verts.Add(new Vector3(Mathf.Cos(a) * topR, halfH, Mathf.Sin(a) * topR));
                norms.Add(Vector3.up);
            }
            for (int i = 0; i < sides; i++)
            {
                tris.Add(topCenter);
                tris.Add(topCenter + 1 + (i + 1) % sides);
                tris.Add(topCenter + 1 + i);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateCurvedCylinder(int pathSeg, int radialSeg)
    {
        // Cylinder that follows a curved path (quarter arc)
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        float bendRadius = 0.4f;
        float tubeRadius = 0.0375f;

        for (int i = 0; i <= pathSeg; i++)
        {
            float t = (float)i / pathSeg;
            float angle = t * Mathf.PI * 0.5f; // 90 degree bend

            // Center of the tube at this point along the path
            Vector3 center = new Vector3(
                bendRadius * Mathf.Sin(angle),
                bendRadius * (1f - Mathf.Cos(angle)),
                0);

            // Tangent along path
            Vector3 tangent = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            Vector3 normal = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
            Vector3 binormal = Vector3.forward;

            for (int j = 0; j <= radialSeg; j++)
            {
                float phi = 2f * Mathf.PI * j / radialSeg;
                Vector3 offset = normal * Mathf.Cos(phi) + binormal * Mathf.Sin(phi);
                verts.Add(center + offset * tubeRadius);
                norms.Add(offset);
            }
        }

        int cols = radialSeg + 1;
        for (int i = 0; i < pathSeg; i++)
        {
            for (int j = 0; j < radialSeg; j++)
            {
                int cur = i * cols + j;
                int next = cur + cols;
                tris.Add(cur); tris.Add(cur + 1); tris.Add(next);
                tris.Add(cur + 1); tris.Add(next + 1); tris.Add(next);
            }
        }

        // Caps — use path tangent for reliable normals
        // At i=0, tangent = (1,0,0), outward cap normal = -tangent
        // At i=pathSeg, tangent = (0,1,0), outward cap normal = +tangent
        Vector3 startCapN = new Vector3(-1, 0, 0);
        Vector3 endCapN = new Vector3(0, 1, 0);
        AddTubeCap(verts, norms, tris, startCapN, radialSeg, 0, cols, true);
        AddTubeCap(verts, norms, tris, endCapN, radialSeg, pathSeg * cols, cols, false);

        // Rotate 180° around X so the end cap (originally +Y) faces -Y.
        // Mapping: (x, y, z) → (x, -y, -z). Proper rotation, winding preserved.
        for (int k = 0; k < verts.Count; k++)
        {
            Vector3 v = verts[k];
            verts[k] = new Vector3(v.x, -v.y, -v.z);
            Vector3 nrm = norms[k];
            norms[k] = new Vector3(nrm.x, -nrm.y, -nrm.z);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static void AddTubeCap(List<Vector3> verts, List<Vector3> norms, List<int> tris,
        Vector3 capNormal, int radialSeg, int ringStart, int cols, bool flip)
    {
        // Compute center of the ring
        Vector3 c = Vector3.zero;
        for (int j = 0; j < radialSeg; j++)
        {
            c += verts[ringStart + j];
        }
        c /= radialSeg;

        Vector3 capN = capNormal;

        int centerIdx = verts.Count;
        verts.Add(c);
        norms.Add(capN);

        for (int j = 0; j <= radialSeg; j++)
        {
            verts.Add(verts[ringStart + (j % (radialSeg + 1))]);
            norms.Add(capN);
        }

        for (int j = 0; j < radialSeg; j++)
        {
            if (flip)
            {
                tris.Add(centerIdx);
                tris.Add(centerIdx + 1 + j + 1);
                tris.Add(centerIdx + 1 + j);
            }
            else
            {
                tris.Add(centerIdx);
                tris.Add(centerIdx + 1 + j);
                tris.Add(centerIdx + 1 + j + 1);
            }
        }
    }

    static Mesh CreateTorus(int segments, float majorR, float squareSide)
    {
        // A ring with a square cross-section, lying flat in the XZ plane.
        // Built as four independent quad strips (top, bottom, outer, inner) so the
        // four faces have sharp shading boundaries instead of being averaged.
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        float s = squareSide * 0.5f;
        float rIn = majorR - s;
        float rOut = majorR + s;

        // Top face: annulus at Y=+s, normal +Y. Each segment is a quad between
        // an inner-ring point and an outer-ring point.
        int topStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            verts.Add(new Vector3(rIn * cos, +s, rIn * sin));
            verts.Add(new Vector3(rOut * cos, +s, rOut * sin));
            norms.Add(Vector3.up); norms.Add(Vector3.up);
        }
        for (int i = 0; i < segments; i++)
        {
            int bi = topStart + i * 2;
            // Quad order: inner_i, outer_i, inner_{i+1}, outer_{i+1}.
            // Winding chosen so cross(v1-v0, v2-v0) points +Y.
            tris.Add(bi); tris.Add(bi + 2); tris.Add(bi + 1);
            tris.Add(bi + 1); tris.Add(bi + 2); tris.Add(bi + 3);
        }

        // Bottom face: same layout at Y=-s, winding reversed for -Y normal.
        int botStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            verts.Add(new Vector3(rIn * cos, -s, rIn * sin));
            verts.Add(new Vector3(rOut * cos, -s, rOut * sin));
            norms.Add(Vector3.down); norms.Add(Vector3.down);
        }
        for (int i = 0; i < segments; i++)
        {
            int bi = botStart + i * 2;
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 1); tris.Add(bi + 3); tris.Add(bi + 2);
        }

        // Outer cylinder face at radius rOut, normal pointing radially outward.
        int outerStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            Vector3 n = new Vector3(cos, 0, sin);
            verts.Add(new Vector3(rOut * cos, -s, rOut * sin));
            verts.Add(new Vector3(rOut * cos, +s, rOut * sin));
            norms.Add(n); norms.Add(n);
        }
        for (int i = 0; i < segments; i++)
        {
            int bi = outerStart + i * 2;
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 2); tris.Add(bi + 1); tris.Add(bi + 3);
        }

        // Inner cylinder face at radius rIn, normal pointing toward the center
        // (the hole). Same vertex layout as outer but winding reversed.
        int innerStart = verts.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            Vector3 n = new Vector3(-cos, 0, -sin);
            verts.Add(new Vector3(rIn * cos, -s, rIn * sin));
            verts.Add(new Vector3(rIn * cos, +s, rIn * sin));
            norms.Add(n); norms.Add(n);
        }
        for (int i = 0; i < segments; i++)
        {
            int bi = innerStart + i * 2;
            tris.Add(bi); tris.Add(bi + 2); tris.Add(bi + 1);
            tris.Add(bi + 1); tris.Add(bi + 2); tris.Add(bi + 3);
        }

        // Rotate +90° around X so the ring's hole-axis becomes Z, matching the
        // HalfTorus arch orientation (which also lies in the XY plane).
        // Mapping: (x, y, z) → (x, -z, y).
        for (int k = 0; k < verts.Count; k++)
        {
            Vector3 v = verts[k];
            verts[k] = new Vector3(v.x, -v.z, v.y);
            Vector3 nrm = norms[k];
            norms[k] = new Vector3(nrm.x, -nrm.z, nrm.y);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateObliqueCylinder(int segments)
    {
        // Cylinder where the top is offset from the bottom (sheared)
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        float radius = 0.4f;
        float halfH = 0.5f;
        float shearX = 0.3f; // top offset

        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            Vector3 bot = new Vector3(cos * radius, -halfH, sin * radius);
            Vector3 top = new Vector3(cos * radius + shearX, halfH, sin * radius);
            Vector3 n = new Vector3(cos, 0, sin); // approximate

            verts.Add(bot); verts.Add(top);
            norms.Add(n); norms.Add(n);
        }

        for (int i = 0; i < segments; i++)
        {
            int bi = i * 2;
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 2); tris.Add(bi + 1); tris.Add(bi + 3);
        }

        // Bottom cap
        int botCenter = verts.Count;
        verts.Add(new Vector3(0, -halfH, 0)); norms.Add(Vector3.down);
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, -halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.down);
        }
        for (int i = 0; i < segments; i++)
        {
            tris.Add(botCenter);
            tris.Add(botCenter + 1 + i);
            tris.Add(botCenter + 1 + (i + 1) % (segments + 1));
        }

        // Top cap
        int topCenter = verts.Count;
        verts.Add(new Vector3(shearX, halfH, 0)); norms.Add(Vector3.up);
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            verts.Add(new Vector3(Mathf.Cos(a) * radius + shearX, halfH, Mathf.Sin(a) * radius));
            norms.Add(Vector3.up);
        }
        for (int i = 0; i < segments; i++)
        {
            tris.Add(topCenter);
            tris.Add(topCenter + 1 + (i + 1) % (segments + 1));
            tris.Add(topCenter + 1 + i);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    static Mesh CreateRoundedCube(float bevelRadius, int bevelSeg)
    {
        // Simple approach: use a sphere with weighted axes to approximate
        // a cube with rounded edges. Scale vertices to create flat faces with rounded edges.
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        int seg = 12;
        float size = 0.5f;
        float inner = size - bevelRadius;

        for (int lat = 0; lat <= seg; lat++)
        {
            float theta = Mathf.PI * lat / seg;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            for (int lon = 0; lon <= seg * 2; lon++)
            {
                float phi = 2f * Mathf.PI * lon / (seg * 2);
                float sinP = Mathf.Sin(phi);
                float cosP = Mathf.Cos(phi);

                // Unit sphere direction
                Vector3 dir = new Vector3(sinT * cosP, cosT, sinT * sinP);

                // Push towards cube faces
                Vector3 pos = new Vector3(
                    Mathf.Sign(dir.x) * inner + dir.x * bevelRadius,
                    Mathf.Sign(dir.y) * inner + dir.y * bevelRadius,
                    Mathf.Sign(dir.z) * inner + dir.z * bevelRadius
                );

                // Fix singularity at poles
                if (Mathf.Abs(dir.x) < 0.001f) pos.x = dir.x * bevelRadius;
                if (Mathf.Abs(dir.y) < 0.001f) pos.y = dir.y * bevelRadius;
                if (Mathf.Abs(dir.z) < 0.001f) pos.z = dir.z * bevelRadius;

                verts.Add(pos);
                norms.Add(dir); // use sphere normal for smooth shading
            }
        }

        int cols = seg * 2 + 1;
        for (int lat = 0; lat < seg; lat++)
        {
            for (int lon = 0; lon < seg * 2; lon++)
            {
                int cur = lat * cols + lon;
                int next = cur + cols;
                tris.Add(cur); tris.Add(cur + 1); tris.Add(next);
                tris.Add(cur + 1); tris.Add(next + 1); tris.Add(next);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    // ─────────────────────── Helpers ───────────────────────

    static void AddQuad(List<Vector3> verts, List<Vector3> norms, List<int> tris,
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
    {
        int idx = verts.Count;
        verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
        norms.Add(normal); norms.Add(normal); norms.Add(normal); norms.Add(normal);
        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
    }

}
