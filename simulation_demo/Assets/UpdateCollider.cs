using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateCollider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponent<MeshCollider>().sharedMesh = GetPosedMesh(gameObject.GetComponent<SkinnedMeshRenderer>());
    }

    public static Mesh GetPosedMesh(SkinnedMeshRenderer skin)
    {
        // reference: https://forum.unity.com/threads/bakemesh-scales-wrong.442212/
        float MIN_VALUE = 0.00001f;

        Mesh mesh = new Mesh();
        Mesh sharedMesh = skin.sharedMesh;

        GameObject root = skin.gameObject;

        Vector3[] vertices = sharedMesh.vertices;
        Matrix4x4[] bindposes = sharedMesh.bindposes;
        BoneWeight[] boneWeights = sharedMesh.boneWeights;
        Transform[] bones = skin.bones;
        Vector3[] newVert = new Vector3[vertices.Length];

        Vector3 localPt;

        for (int i = 0; i < vertices.Length; i++)
        {
            BoneWeight bw = boneWeights[i];

            if (Mathf.Abs(bw.weight0) > MIN_VALUE)
            {
                localPt = bindposes[bw.boneIndex0].MultiplyPoint3x4(vertices[i]);
                newVert[i] +=
                    root.transform.InverseTransformPoint
                        (
                    bones[bw.boneIndex0].transform.localToWorldMatrix.MultiplyPoint3x4(localPt)) * bw.weight0;
            }
            if (Mathf.Abs(bw.weight1) > MIN_VALUE)
            {
                localPt = bindposes[bw.boneIndex1].MultiplyPoint3x4(vertices[i]);
                newVert[i] +=
                    root.transform.InverseTransformPoint
                        (
                    bones[bw.boneIndex1].transform.localToWorldMatrix.MultiplyPoint3x4(localPt)) * bw.weight1;
            }
            if (Mathf.Abs(bw.weight2) > MIN_VALUE)
            {
                localPt = bindposes[bw.boneIndex2].MultiplyPoint3x4(vertices[i]);
                newVert[i] += root.transform.InverseTransformPoint
                    (
                    bones[bw.boneIndex2].transform.localToWorldMatrix.MultiplyPoint3x4(localPt)) * bw.weight2;
            }
            if (Mathf.Abs(bw.weight3) > MIN_VALUE)
            {
                localPt = bindposes[bw.boneIndex3].MultiplyPoint3x4(vertices[i]);
                newVert[i] +=
                    root.transform.InverseTransformPoint
                        (
                    bones[bw.boneIndex3].transform.localToWorldMatrix.MultiplyPoint3x4(localPt)) * bw.weight3;
            }

        }

        mesh.vertices = newVert;
        mesh.triangles = skin.sharedMesh.triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
