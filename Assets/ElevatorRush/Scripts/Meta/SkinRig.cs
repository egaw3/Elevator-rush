using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Where a passenger's skin accessories attach, measured once in the rest pose: the head
    /// and torso bones (the figure itself for the primitive fallback), and the head and chest
    /// centers as bone-space poses. Sizes come from the actual mesh vertices, because a skinned
    /// renderer's bounds are padded far beyond the mesh.
    /// </summary>
    public sealed class SkinRig
    {
        // The torso is about this wide relative to the head on the chibi character (the body
        // mesh's own width includes the arms, so it cannot be used).
        const float TorsoToHeadWidth = 0.85f;
        // The skull top is measured over the outer part of the head only, so a hair bun in the
        // middle does not lift hats off the head; hats cover the bun.
        const float SkullSideFraction = 0.25f;

        public Transform headBone;
        public Transform bodyBone;
        public Matrix4x4 headAnchor;
        public Matrix4x4 bodyAnchor;
        /// <summary>Head group unit in meters per axis: the head width (x, y) and depth (z).</summary>
        public Vector3 headUnit;
        /// <summary>Body group unit in meters per axis: torso width (x, y) and depth (z).</summary>
        public Vector3 bodyUnit;
        /// <summary>Top of the head above its center, in head units.</summary>
        public float headTop;

        /// <summary>
        /// Measures a rig from the head and body meshes in the figure's rest pose. Bones may be
        /// null; then the groups hang directly off the figure.
        /// </summary>
        public static SkinRig Measure(Transform figure, Renderer head, Renderer body, Transform headBone, Transform bodyBone)
        {
            var headBounds = MeshBounds(figure, head, out float skullTop);
            var bodyBounds = MeshBounds(figure, body, out _);
            float headWidth = Mathf.Max(0.01f, headBounds.size.x);
            float torsoWidth = Mathf.Min(bodyBounds.size.x, headWidth * TorsoToHeadWidth);
            var rig = new SkinRig
            {
                headBone = headBone != null ? headBone : figure,
                bodyBone = bodyBone != null ? bodyBone : figure,
                headUnit = new Vector3(headWidth, headWidth, Mathf.Max(0.01f, headBounds.size.z)),
                bodyUnit = new Vector3(torsoWidth, torsoWidth, Mathf.Max(0.01f, bodyBounds.size.z)),
            };
            rig.headTop = (skullTop - headBounds.center.y) / rig.headUnit.y;
            rig.headAnchor = Anchor(figure, rig.headBone, headBounds.center);
            rig.bodyAnchor = Anchor(figure, rig.bodyBone, bodyBounds.center);
            return rig;
        }

        static Matrix4x4 Anchor(Transform figure, Transform bone, Vector3 figurePoint)
        {
            return bone.worldToLocalMatrix * figure.localToWorldMatrix * Matrix4x4.Translate(figurePoint);
        }

        /// <summary>
        /// The tight box around a renderer's vertices in the figure's space, and the highest point
        /// of its outer part (left and right of the middle), which is the skull top on a head.
        /// </summary>
        public static Bounds MeshBounds(Transform figure, Renderer renderer, out float skullTop)
        {
            Mesh mesh = null;
            bool temporary = false;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh();
                temporary = true;
                skinned.BakeMesh(mesh, true);
            }
            else
            {
                var filter = renderer.GetComponent<MeshFilter>();
                mesh = filter != null ? filter.sharedMesh : null;
            }

            Bounds result;
            if (mesh == null || mesh.vertexCount == 0)
            {
                var b = renderer.bounds;
                result = new Bounds(figure.InverseTransformPoint(b.center), b.size);
                skullTop = result.max.y;
            }
            else
            {
                var vertices = mesh.vertices;
                var toFigure = figure.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var points = new Vector3[vertices.Length];
                result = new Bounds(toFigure.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                for (int i = 0; i < vertices.Length; i++)
                {
                    points[i] = toFigure.MultiplyPoint3x4(vertices[i]);
                    result.Encapsulate(points[i]);
                }
                float side = result.extents.x * SkullSideFraction * 2f;
                skullTop = float.MinValue;
                foreach (var q in points)
                    if (Mathf.Abs(q.x - result.center.x) > side)
                        skullTop = Mathf.Max(skullTop, q.y);
                if (skullTop == float.MinValue)
                    skullTop = result.max.y;
            }
            if (temporary)
                Object.Destroy(mesh);
            return result;
        }
    }
}
