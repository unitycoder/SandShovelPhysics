using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiggingTest
{
    // TODO

    // SHOVEL
    // - check shovel angle, cannot go underground if bad angle
    //   (use joint? or force position if cannot go, check collectors move dir? or just shovel dir against ground (cannot, need to check which parts are underground))
    //   *easier to just use sliderjoint? (when 2 or more points underground) or freeze rb rotation? if joint, requires moving with forces..
    // - check shovel angle, cannot pick sand if pulled up straigth (clear sand from collectors that come from underground, if movement angle is straigth?)
    // - check shovel angle, cannot move underground (except in/out) try joint

    // SANDMESH
    // - bug, detach creates multiple copies?
    // - only pick/lift sand from front side
    // - unparent sand when coming up from ground (or always, only connected with joint?)
    // - full or partial sand drop (if shovel upside down or tilted)
    // - add fixed joint or slider joint to sandmesh when picked up from ground, then it falls if move fast (or by gravity?)
    // - detect if already has sand, and pushing to ground again, push current sand away (current sand attached with joint? then ground collider pushes)
    // - decrease sandmesh size/mass (when particles fall out)
    // - bug: sand doesnt collider with shovel and ground always
    // - needs smooth faces on mesh?
    // - falling sand, check angle (scale sand size in shovel?) 
    // - [x] merge falling sand to ground
    // - [x] dont modify mesh after picked sand
    // - [x] build full sand piece mesh
    // - [x] bug: drop happens in wrong position if move shovel
    // - [x] sandmesh pivot is not center of mesh
    // - [x] add UV map to sandmesh *or use shader triplanar local space?
    // - [x] particles from sand mesh (cannot use procedural mesh in shape?)
    // - keep adding amount to collectors, if too high, drop higher part as a separate piece (or stop collecting, since its full)

    // TERRAIN
    // - [x] make hole in ground (terrain, or paint into heightmap?, or move mesh vertices down, )


    // TODO add helper debug.drawline in local2worldspace

    public class Shovel : MonoBehaviour
    {
        [Header("References")]
        public Transform edgeRoot;
        public MeshFilter sandMesh;
        public ParticleSystem fallingSandPS;
        public MeshFilter groundMesh;
        Collider groundCollider;

        public LayerMask groundMask;

        [Header("Settings Shovel")]
        [Tooltip("Max angle that shovel can be pushed underground")]
        //public float angleThreshold = 2f;
        public float dotThreshold = 0.1f;
        public float errorThreshold = 1f; // if bigger, stop movement
        public float moveThreshold = 0.1f; // if too small, angle comparison errors?
        public bool blockShovelMove = false;

        [Header("Settings Terrain")]
        public bool updateGround = false;

        Vector3 shovelPrevPos;

        List<Collector> collectors = new List<Collector>();
        Collider shovelCollider;
        Vector3 sandMeshOffset;
        Collider sandCollider;


        void Start()
        {
            // collect edges, NOTE if only few points, low resolution to check
            for (int i = 0; i < edgeRoot.transform.childCount; i++)
            {
                // TODO need to make better order of points in hierarchy, or check 1 side at a time..
                var child = edgeRoot.GetChild(i);
                if (child.gameObject.activeSelf == false) continue;

                var c = new Collector();
                c.point = child;
                c.amount = Vector3.zero;
                c.isUnderground = false;

                collectors.Add(c);
            }

            // temp mesh
            var mesh = new Mesh();
            mesh.MarkDynamic();

            sandMesh.mesh = mesh;
            sandMeshOffset = sandMesh.transform.localPosition;

            shovelCollider = GetComponent<Collider>();
            shovelPrevPos = transform.position;

            //groundCollider = groundMesh.GetComponent<Collider>();
            sandCollider = sandMesh.GetComponent<Collider>();
        }


        void Update()
        {
            UpdateSandMesh();
            FallingSandParticles();

            if (updateGround == true)
            {
                // move ground vertices down, from picked area
                if (sandCollider == null) return;

                var verts = groundMesh.mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    // check sandmesh bounds first, if inside (or above?)
                    // check sandmesh collider, if inside (or above?)
                    // OR, groundverts, raycast down to check shovel (if hit, add sand to that position in shovel, or move under the hit point?) *NOTE shovel is convex collider now..

                    var p = verts[i];
                    var groundRay = new Ray(p, Vector3.down);
                    RaycastHit hit;
                    if (shovelCollider.Raycast(groundRay, out hit, 99))
                    {
                        Debug.DrawRay(p, Vector3.up * 0.025f, Color.red);
                        verts[i] = hit.point;
                    }
                    else
                    {
                        //Debug.DrawRay(p, Vector3.up * 0.025f, Color.green);
                    }

                    // inside, TODO only need to check if above sandmesh bottom area (then those ground verts are to be moved down, by collector amount?)
                    //if (sandCollider.ClosestPoint(p) == p)
                    //{
                    //    Debug.DrawRay(p, Vector3.up * 0.025f, Color.red);
                    //}
                }

                // update ground mesh
                // TODO set vertex color/mask or UV's to have different material (broken ground)
                groundMesh.mesh.vertices = verts;
            }

            // clear/drop sand
            if (Input.GetKeyDown(KeyCode.C))
            {
                DetachSand();
            }
        }

        void LateUpdate()
        {
            var forward = -transform.forward;

            // check if shovel moving forward or backward
            var sdot = Vector3.Dot(forward, (transform.position - shovelPrevPos).normalized);
            if (sdot > 0)
            {
                Debug.DrawRay(transform.position, forward, Color.green);
            }
            else if (sdot < 0)
            {
                Debug.DrawRay(transform.position, forward, Color.red);
            }

            float totalError = 0;

            for (int i = 0; i < collectors.Count; i++)
            {
                var p = collectors[i];

                // check only if going underground
                if (p.isUnderground == false) continue;

                // get point movement direction
                var dir = (p.prevPos - p.point.position).normalized;

                // compare to shovel forward
                //var sangle = Vector3.SignedAngle(forward, dir, Vector3.up);
                //var sangle = Vector3.SignedAngle(forward, dir, forward);
                //Debug.Log("sangle=" + sangle);
                var dot = Vector3.Dot(forward, dir);
                //Debug.Log("dot= " + dot);
                //if (Mathf.Abs(sangle) < angleThreshold || sangle > 180 - angleThreshold)
                if (dot > 1 - dotThreshold || dot < -1 + dotThreshold)
                {
                    // good direction
                    Debug.DrawLine(p.point.position, p.point.position + dir * 0.1f, Color.green, 0.1f);
                    totalError -= Mathf.Abs(dot);
                }
                else
                {
                    // bad direction
                    Debug.DrawLine(p.point.position, p.point.position + dir * 0.1f, Color.red, 0.1f);
                    totalError += Mathf.Abs(dot);
                }

                //Debug.Log("totalErr= " + totalError);
                if (totalError > errorThreshold)
                {
                    // dont allow move, move back to last known good pos?
                    if (blockShovelMove) transform.position = shovelPrevPos;
                    //Debug.Log("StopShovelMove");
                }
                shovelPrevPos = transform.position;

                //Debug.DrawLine(p.prevPos, p.point.position, Color.blue, 1f);

                // save previous pos if moved enough
                if (Vector3.Distance(p.prevPos, p.point.position) > moveThreshold) p.prevPos = p.point.position;
                collectors[i] = p;
            }
        }

        private void FixedUpdate()
        {

        }

        void FallingSandParticles()
        {
            // loop verts
            // spawn particles from vert pos

            var verts = sandMesh.mesh.vertices;
            var tris = sandMesh.mesh.triangles;
            var normals = sandMesh.mesh.normals;

            if (verts.Length == 0) return;

            var r = Random.Range(0, tris.Length / 3 - 3);
            Vector3 p0 = verts[tris[r * 3]];
            Vector3 p1 = verts[tris[r * 3 + 1]];
            Vector3 p2 = verts[tris[r * 3 + 2]];
            var centroid = (p0 + p1 + p2) / 3f;

            //for (int i = 0; i < verts.Length; i++)
            //{
            //    //Vector3 p0 = verts[tris[i]];
            //    //Vector3 p1 = verts[tris[i + 1]];
            //    //Vector3 p2 = verts[tris[i + 2]];
            //    //var centroid = (p0 + p1 + p2) / 3f;

            //    Debug.DrawRay(sandMesh.transform.TransformPoint(verts[i]), sandMesh.transform.TransformDirection(normals[i]) * 0.25f, Color.red);
            //    //var n = Vector3.Cross(p1 - p0, p1 - p2).normalized;
            //    //Debug.DrawRay(sandMesh.transform.TransformPoint(centroid), n * 0.25f, Color.red);
            //}

            //Debug.DrawRay(sandMesh.transform.TransformPoint(centroid), sandMesh.transform.TransformDirection(normals[r]), Color.red);

            var pos = sandMesh.transform.TransformPoint(centroid) + sandMesh.transform.TransformDirection(normals[r]) * 0.05f;
            //var pos = sandMesh.transform.TransformPoint(verts[Random.Range(0, verts.Length)]);

            var emits = new ParticleSystem.EmitParams();
            emits.position = pos + Random.insideUnitSphere * 0.02f;
            fallingSandPS.Emit(emits, 1);
        }

        void UpdateSandMesh()
        {
            int undergroundPoints = 0;
            float sandAmount = 0;

            // check edgepoints
            for (int i = 0; i < collectors.Count; i++)
            {
                var s = collectors[i];

                // check if underground
                RaycastHit hit;
                if (Physics.Raycast(s.point.position, Vector3.up, out hit, 999, groundMask))
                {
                    s.isUnderground = true;
                    // get max depth (how much sand to collect)
                    if (hit.distance > s.amount.y)
                    {
                        s.amount.y = hit.distance;
                        sandAmount += s.amount.y;
                        s.hasSand = true;
                    }
                    //Debug.DrawRay(s.point.position, Vector3.up * hit.distance, Color.red);
                    undergroundPoints++;

                }
                else
                {
                    //s.amount.y = 0;
                    // TODO needs another variable, was underground, so that this collector keeps sand (mesh)
                    s.isUnderground = false;
                    //Debug.DrawRay(collectors[i].point.position, Vector3.up, Color.green);
                }

                collectors[i] = s;
            }

            if (undergroundPoints == 0 && sandAmount > 0)
            {
                // detach from shovel, no more underground
                //sandMesh.transform.parent = null;
                //DetachSand();

                return;
            }

            // build mesh from edge points to surface, use quads.. TODO instead of clearing mesh, modify existing vertices? (but cannot if need to remove or add..)
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            var topVerts = new List<Vector3>();
            var bottomVerts = new List<Vector3>();

            var center = Vector3.zero;

            int triCount = 0;
            for (int i = 0; i < collectors.Count; i++)
            {
                var s = collectors[i];

                if (s.hasSand == true)
                {
                    // get center
                    center += collectors[i].point.position;

                    // lower
                    var pos2 = transform.InverseTransformPoint(s.point.position) - sandMeshOffset;
                    verts.Add(pos2);
                    bottomVerts.Add(pos2);
                    tris.Add(triCount++);
                    uvs.Add(pos2);

                    // upper
                    var pos = transform.InverseTransformPoint(s.point.position + s.amount) - sandMeshOffset;
                    verts.Add(pos);
                    topVerts.Add(pos);
                    tris.Add(triCount++);
                    uvs.Add(pos);

                    //Debug.DrawRay(s.point.position + s.amount, Vector3.up * 0.05f, Color.red);
                    //Debug.DrawRay(s.point.position, Vector3.up * 0.05f, Color.green);
                    //Debug.DrawLine(pos, pos2, Color.red);

                    // find next pair
                    bool gotPair = false;
                    for (i++; i < collectors.Count; i++)
                    {
                        var s2 = collectors[i];
                        if (s2.hasSand == true)
                        {
                            // upper
                            var pos3 = transform.InverseTransformPoint(s2.point.position + s2.amount) - sandMeshOffset;
                            verts.Add(pos3);
                            topVerts.Add(pos3);
                            tris.Add(triCount++);
                            uvs.Add(pos3);

                            // lower
                            var pos4 = transform.InverseTransformPoint(s2.point.position) - sandMeshOffset;
                            verts.Add(pos4);
                            bottomVerts.Add(pos4);
                            tris.Add(triCount++);
                            uvs.Add(pos4);

                            //Debug.DrawLine(s.point.position, s.point.position + s.amount, i % 2 == 0 ? Color.red : Color.green);
                            //Debug.DrawLine(s.point.position + s.amount, s2.point.position + s2.amount, i % 2 == 0 ? Color.white : Color.blue);
                            //Debug.DrawLine(s2.point.position, s2.point.position + s2.amount, i % 2 == 0 ? Color.blue : Color.yellow);
                            //Debug.DrawLine(s.point.position, s2.point.position, i % 2 == 0 ? Color.magenta : Color.cyan);

                            gotPair = true;
                            break;
                        }
                    }

                    if (gotPair == false)
                    {
                        //Debug.Log("no pair remove..");
                        // remove this quad? needed or not?
                        if (verts.Count < 1) continue;

                        verts.RemoveAt(verts.Count - 1);
                        verts.RemoveAt(verts.Count - 1);

                        topVerts.RemoveAt(topVerts.Count - 1);
                        bottomVerts.RemoveAt(bottomVerts.Count - 1);

                        uvs.RemoveAt(uvs.Count - 1);
                        uvs.RemoveAt(uvs.Count - 1);

                        tris.RemoveAt(tris.Count - 1);
                        tris.RemoveAt(tris.Count - 1);

                        triCount--;
                        triCount--;
                    }
                    else
                    {
                        // return to previous pair end point
                        i--;
                    }

                } // if collector underground
            } // loop collectors

            // get center
            center = center / (float)collectors.Count;
            sandMesh.transform.position = center;
            sandMeshOffset = sandMesh.transform.localPosition;

            // build top mesh, FIXME some duplicate verts?
            for (int i = 0; i < topVerts.Count / 2; i += 2)
            {
                //// top left
                //var v1 = transform.TransformPoint(topVerts[i]);
                //// bottom left
                //var v2 = transform.TransformPoint(topVerts[i + 1]);
                //// bottom right
                //var v3 = transform.TransformPoint(topVerts[topVerts.Count - 1 - i - 1]);
                //// top right
                //var v4 = transform.TransformPoint(topVerts[topVerts.Count - 1 - i]);
                //Debug.DrawLine(v1, v2, Color.red);
                //Debug.DrawLine(v2, v3, Color.yellow);
                //Debug.DrawLine(v3, v4, Color.blue);
                //Debug.DrawLine(v4 + Vector3.up * 0.005f, v1 + Vector3.up * 0.005f, Color.magenta);

                verts.Add(topVerts[topVerts.Count - 1 - i]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(topVerts[topVerts.Count - 1 - i - 1]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(topVerts[i + 1]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(topVerts[i]);
                uvs.Add(verts[verts.Count - 1]);

                tris.Add(triCount++);
                tris.Add(triCount++);
                tris.Add(triCount++);
                tris.Add(triCount++);
            }

            // build bottom mesh
            for (int i = 0; i < bottomVerts.Count / 2; i += 2)
            {
                verts.Add(bottomVerts[i]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(bottomVerts[i + 1]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(bottomVerts[bottomVerts.Count - 1 - i - 1]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(bottomVerts[bottomVerts.Count - 1 - i]);
                uvs.Add(verts[verts.Count - 1]);

                tris.Add(triCount++);
                tris.Add(triCount++);
                tris.Add(triCount++);
                tris.Add(triCount++);
            }

            // close top back gap
            if (verts.Count > 0)
            {

                verts.Add(topVerts[0]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(bottomVerts[0]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(bottomVerts[bottomVerts.Count - 1]);
                uvs.Add(verts[verts.Count - 1]);
                verts.Add(topVerts[topVerts.Count - 1]);
                uvs.Add(verts[verts.Count - 1]);
                tris.Add(triCount++);
                tris.Add(triCount++);
                tris.Add(triCount++);
                tris.Add(triCount++);
            }

            //Debug.Log(verts.Count + "   " + tris.Count);

            var mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.SetIndices(tris.ToArray(), MeshTopology.Quads, 0);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            sandMesh.mesh = mesh;
        }

        void DetachSand()
        {
            Debug.Log("DetachSand");
            var go = Instantiate(sandMesh.gameObject, sandMesh.transform.position, sandMesh.transform.rotation);

            go.layer = LayerMask.NameToLayer("SandPiece");

            //var mc = go.AddComponent<MeshCollider>();
            var mc = go.GetComponent<MeshCollider>();

            var rb = go.AddComponent<Rigidbody>();
            // TODO set mass from size
            mc.sharedMesh = sandMesh.mesh;
            mc.convex = true;

            //Physics.IgnoreCollision(mc, shovelCollider);
            //var rb = go.AddComponent<Rigidbody>();
            //rb.AddForce(0, 7, 0, ForceMode.Impulse);

            var ss = go.AddComponent<SandPiece>();
            ss.groundMF = groundMesh;

            // clear point values
            for (int i = 0; i < collectors.Count; i++)
            {
                var s = collectors[i];
                s.isUnderground = false;
                s.amount = Vector3.zero;
                s.hasSand = false;
                collectors[i] = s;
            }
        }

    }
}
