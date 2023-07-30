using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiggingTest
{
    public class SandPiece : MonoBehaviour
    {
        public MeshFilter groundMF;
        MeshCollider groundMc;

        public AnimationCurve animCurve;

        MeshFilter sandMeshFilter;
        MeshCollider mc;
        Rigidbody rb;

        public LayerMask groundMask;
        public LayerMask sandMask;

        bool isGrounded = false;

        Vector3[] sandVerts;
        Vector3[] groundVerts;
        int[] groundTris;

        void Start()
        {
            sandMeshFilter = GetComponent<MeshFilter>();
            mc = GetComponent<MeshCollider>();
            rb = GetComponent<Rigidbody>();

            sandVerts = sandMeshFilter.mesh.vertices;

            groundVerts = groundMF.mesh.vertices;
            groundTris = groundMF.mesh.triangles;

            groundMask = LayerMask.GetMask("Ground");
            sandMask = LayerMask.GetMask("SandPiece");

            groundMc = groundMF.GetComponent<MeshCollider>();

        }

        void Update()
        {
            if (!isGrounded) return;

            //if (!rb.IsSleeping()) return;

            var mr = sandMeshFilter.GetComponent<MeshRenderer>();

            // test 4
            var b = mr.bounds;
            var center = b.center;
            center.y -= b.extents.y;
            var nb = new Bounds(center, b.size);
            //DrawBounds(nb, 3f);

            Vector3 bs = new Vector3(0, nb.size.y, 0);

            for (int i = 0; i < groundVerts.Length; i++)
            {
                var v = groundMF.transform.TransformPoint(groundVerts[i]);

                if (nb.Contains(v))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(v + bs, Vector3.down, out hit, 1, sandMask))
                    {
                        groundVerts[i].y += Vector3.Distance(v, hit.point) * 0.75f;
                    }
                }
            }

            // TODO smoothen mesh?

            groundMF.mesh.vertices = groundVerts;
            groundMc.sharedMesh = groundMF.mesh;
            //Debug.Break();



            //// test 3
            //var b = mr.bounds;
            //var center = b.center;
            //center.y -= b.extents.y;
            //var nb = new Bounds(center, b.size);
            //DrawBounds(nb, 3f);
            ////Debug.DrawRay(nb.center, Vector3.up, Color.blue, 5);
            //for (int i = 0; i < groundVerts.Length; i++)
            //{
            //    var v = groundMF.transform.TransformPoint(groundVerts[i]);

            //    if (nb.Contains(v))
            //    {
            //        var dist = 1 - Remap(Vector3.Distance(v, nb.center), 0, nb.size.magnitude, 0, 1);
            //        //Debug.DrawRay(v, Vector3.up * dist, Color.green, 5);
            //        //Debug.DrawRay(groundVerts[i], Vector3.up * dist, Color.blue, 5);
            //        groundVerts[i].y += dist;
            //        //groundVerts[i].y = Mathf.Infinity; // hole generator!
            //    }
            //}
            //groundMF.mesh.vertices = groundVerts;

            // test 2
            //// merge mesh to ground instantly
            //for (int i = 0; i < sandVerts.Length; i++)
            //{
            //    RaycastHit hit;

            //    // cannot check per vertex, too low resolution and have gaps
            //    var v = transform.TransformPoint(sandVerts[i]);

            //    if (Physics.Raycast(v, Vector3.down, out hit, 1, groundMask))
            //    {
            //        // TODO take closest vert
            //        groundVerts[groundTris[hit.triangleIndex * 3 + 0]] += groundMF.transform.InverseTransformPoint(new Vector3(0, hit.distance, 0));
            //        groundVerts[groundTris[hit.triangleIndex * 3 + 1]] += groundMF.transform.InverseTransformPoint(new Vector3(0, hit.distance, 0));
            //        groundVerts[groundTris[hit.triangleIndex * 3 + 2]] += groundMF.transform.InverseTransformPoint(new Vector3(0, hit.distance, 0));

            //    }
            //}
            //groundMF.mesh.vertices = groundVerts;
            //// TODO update ground collider



            // test 1
            //for (int i = 0; i < verts.Length; i++)
            //{
            //    RaycastHit hit;
            //    var v = transform.TransformPoint(verts[i]);

            //    if (Physics.Raycast(v, Vector3.down * 0.01f, out hit, groundMask))
            //    {
            //        // in ground, raise ground
            //    }
            //    else
            //    {
            //        verts[i] = verts[i] + Vector3.down * (sinkSpeed * Time.deltaTime);
            //    }
            //}
            //mf.mesh.vertices = verts;
            //mc.sharedMesh = mf.mesh;


            Destroy(gameObject);

        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                // TODO could wait until rb sleep?
                isGrounded = true;
                rb.velocity = Vector3.zero;
            }
        }

        float Remap(float source, float sourceFrom, float sourceTo, float targetFrom, float targetTo)
        {
            return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
        }

        // https://gist.github.com/xanathar/735e17ac129a72a277ee
        public static float CubicEaseOut(float t, float b, float c, float d)
        {
            return c * ((t = t / d - 1) * t * t + 1) + b;
        }

        void DrawBounds(Bounds b, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }
    }
}
