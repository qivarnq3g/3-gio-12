using UnityEngine;
namespace BaGio12.Story
{
    // Only a ray that hits the fabric submesh counts; front/body photos are ordinary photos.
    public sealed class NightThreeCaseBack : MonoBehaviour
    {
        public Mesh mesh;
        public int submesh, firstTriangle, triangleCount;
        public Vector3 SurfaceCentre
        {
            get
            {
                var indices=mesh.GetTriangles(submesh);var vertices=mesh.vertices;Vector3 centre=Vector3.zero;
                foreach(int index in indices)centre+=vertices[index];
                return transform.TransformPoint(centre/indices.Length);
            }
        }
        public Vector3 SurfaceNormal
        {
            get
            {
                var indices=mesh.GetTriangles(submesh);var vertices=mesh.vertices;Vector3 normal=Vector3.zero;
                for(int i=0;i<indices.Length;i+=3)
                { var face=Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]); if(face.sqrMagnitude>normal.sqrMagnitude)normal=face; }
                return transform.TransformDirection(normal.normalized);
            }
        }
        public bool IsEvidence(RaycastHit hit) => hit.collider.gameObject==gameObject && hit.triangleIndex>=firstTriangle && hit.triangleIndex<firstTriangle+triangleCount;
    }
}
