using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// A ring that fills clockwise from the top to show dock dwell progress.
    /// The fill mesh holds every segment and draws only the first ones, so updates never allocate.
    /// </summary>
    public sealed class DwellRing : MonoBehaviour
    {
        int segments;
        int[] fillIndices;
        Mesh fillMesh;
        int shownSegments = -1;

        public void Init(GameConfig config, MaterialLibrary materials)
        {
            segments = Mathf.Max(3, config.ringSegments);
            gameObject.AddComponent<Billboard>();

            var track = CreatePart("Track", 0f, materials.GetUnlit(config.ringTrackColor));
            track.GetComponent<MeshFilter>().sharedMesh = BuildAnnulus(config.ringRadius, config.ringThickness, out _);

            var fill = CreatePart("Fill", -config.ringFillOffset, materials.GetUnlit(config.ringFillColor));
            fillMesh = BuildAnnulus(config.ringRadius, config.ringThickness * 0.7f, out fillIndices);
            fill.GetComponent<MeshFilter>().sharedMesh = fillMesh;

            SetProgress(0f);
        }

        /// <summary>0 hides the ring; values up to 1 fill it.</summary>
        public void SetProgress(float progress)
        {
            int count = progress <= 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(progress * segments), 1, segments);
            if (count == shownSegments)
                return;
            shownSegments = count;
            gameObject.SetActive(count > 0);
            if (count > 0)
                fillMesh.SetIndices(fillIndices, 0, count * 6, MeshTopology.Triangles, 0, false);
        }

        GameObject CreatePart(string name, float z, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        /// <summary>Flat ring in local X-Y, front face toward -Z, segments ordered clockwise from the top.</summary>
        Mesh BuildAnnulus(float radius, float thickness, out int[] indices)
        {
            float outer = radius + thickness * 0.5f;
            float inner = radius - thickness * 0.5f;
            var vertices = new Vector3[(segments + 1) * 2];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.PI * 0.5f - i * Mathf.PI * 2f / segments;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;
            }

            indices = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int inA = i * 2, outA = i * 2 + 1, inB = i * 2 + 2, outB = i * 2 + 3;
                int k = i * 6;
                indices[k] = inA; indices[k + 1] = outA; indices[k + 2] = outB;
                indices[k + 3] = inA; indices[k + 4] = outB; indices[k + 5] = inB;
            }

            var mesh = new Mesh { name = "DwellRing" };
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
