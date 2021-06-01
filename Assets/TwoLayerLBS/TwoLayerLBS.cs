using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.InteropServices;

public class TwoLayerLBS : MonoBehaviour
{
    // # influcences of virtual bone
    public int numVirtualInfluences;
    // # influcences of vertex
    public int numVertexInfluences;
    public int numMasterJoints;
    public int numVirtualJoints;
    public ComputeShader computeShader;
    int computeShaderID;
    public string weightFile;
    public string indexFile;
    ComputeBuffer cbVirtualJoints;
    ComputeBuffer cbMasterJoints;
    ComputeBuffer cbVirtualWeight;
    ComputeBuffer cbVirtualIndex;
    float[] masterJoints;
    float[] virtualJoints;
    Matrix4x4[] masterInvBindPose;

    public GameObject joints;
    bool[] isActive;
    float[] virtualWeight;
    uint[] virtualIndices;
    Vector3[] initPos;

    public bool viewPrimary;
    public bool viewSecondary;

    [DllImport("kernel32.dll")]
    extern static int QueryPerformanceCounter(ref long x);

    [DllImport("kernel32.dll")]
    extern static int QueryPerformanceFrequency(ref long x);

    void ReadVirtualWeight(ref float[] weight, ref uint[] index)
    {
        StreamReader wsr = new StreamReader(Application.dataPath + weightFile);
        StreamReader isr = new StreamReader(Application.dataPath + indexFile);
        for (int i = 0; i < numVirtualJoints; ++i)
        {
            var wl = wsr.ReadLine().Split(',');
            var il = isr.ReadLine().Split(',');
            for (int j = 0; j < numVirtualInfluences; ++j)
            {
                int jid = (int)float.Parse(il[j]);
                weight[i * numVirtualInfluences + j] = float.Parse(wl[j]);
                index[i * numVirtualInfluences + j] = jid >= 0 ? (uint)jid : 0;
            }
        }
    }

    void ReadVertexWeight()
    {
        var renderer = GetComponent<SkinnedMeshRenderer>();
        var mesh = renderer.sharedMesh;

        var a = renderer.bones;
        var boneWeights = mesh.GetAllBoneWeights();
        var bonesPerVertex = mesh.GetBonesPerVertex();
        float[] maxw = new float[numVirtualJoints];
        int bonesIndex = 0;
        initPos = new Vector3[numVirtualJoints];
        for (int v = 0; v < mesh.vertexCount; ++v)
        {
            for (int i = 0; i < bonesPerVertex[v]; ++i, ++bonesIndex)
            {
                int id = boneWeights[bonesIndex].boneIndex;
                float w = boneWeights[bonesIndex].weight;
                if (maxw[id] < w)
                {
                    maxw[id] = w;
                    initPos[id] = mesh.vertices[v];
                }
            }
        }
        isActive = new bool[numVirtualJoints];
        for (int i = 0; i < numVirtualJoints; ++i)
        {
            isActive[i] = maxw[i] > 0;
        }
    }

    static Matrix4x4 ComposeMatrix(AnimationClip clip, EditorCurveBinding ecb, float t)
    {
        // translation
        ecb.propertyName = "m_LocalPosition.x";
        var px = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalPosition.y";
        var py = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalPosition.z";
        var pz = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        var p = new Vector3(px, py, pz);
        // rotation
        ecb.propertyName = "m_LocalRotation.x";
        var qx = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalRotation.y";
        var qy = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalRotation.z";
        var qz = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalRotation.w";
        var qw = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        var q = new Quaternion(qx, qy, qz, qw);
        // scale
        ecb.propertyName = "m_LocalScale.x";
        var sx = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalScale.y";
        var sy = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        ecb.propertyName = "m_LocalScale.z";
        var sz = AnimationUtility.GetEditorCurve(clip, ecb).Evaluate(t);
        var s = new Vector3(sx, sy, sz);
        // TRS matrix
        return Matrix4x4.TRS(p, q.normalized, s);
    }

    void Start()
    {
        // compute buffer
        cbVirtualJoints = new ComputeBuffer(numVirtualJoints, sizeof(float) * 12);
        cbMasterJoints = new ComputeBuffer(numMasterJoints, sizeof(float) * 12);
        cbVirtualWeight = new ComputeBuffer(numVirtualJoints * numVirtualInfluences, sizeof(float));
        cbVirtualIndex = new ComputeBuffer(numVirtualJoints * numVirtualInfluences, sizeof(uint));
        // virtual weight & index
        virtualWeight = new float[numVirtualJoints * numVirtualInfluences];
        virtualIndices = new uint[numVirtualJoints * numVirtualInfluences];
        ReadVirtualWeight(ref virtualWeight, ref virtualIndices);
        cbVirtualWeight.SetData(virtualWeight);
        cbVirtualIndex.SetData(virtualIndices);
        // initial pose & inverse bind pose
        masterJoints = new float[numMasterJoints * 12];
        masterInvBindPose = new Matrix4x4[numMasterJoints];
        virtualJoints = new float[numVirtualJoints * 12];
        var animator = GetComponent<Animator>();
        var animClip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        var bindings = AnimationUtility.GetCurveBindings(animClip);
        var ecb = new EditorCurveBinding();
        ecb.type = typeof(Transform);
        for (int i = 0; i < numMasterJoints; ++i)
        {
            ecb.path = "Joints/Joint" + System.String.Format("{0, 2:D2}", i + 1);
            masterInvBindPose[i] = ComposeMatrix(animClip, ecb, 0);
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    masterJoints[i * 12 + c * 3 + r] = masterInvBindPose[i][r, c];
                }
            }
            masterInvBindPose[i] = masterInvBindPose[i].inverse;
        }
        cbMasterJoints.SetData(masterJoints);
        // compute shader
        computeShaderID = computeShader.FindKernel("SkinVirtualJoints");
        computeShader.SetBuffer(computeShaderID, "MasterJoints", cbMasterJoints);
        computeShader.SetBuffer(computeShaderID, "VirtualJoints", cbVirtualJoints);
        computeShader.SetBuffer(computeShaderID, "VirtualWeight", cbVirtualWeight);
        computeShader.SetBuffer(computeShaderID, "VirtualIndex", cbVirtualIndex);
        computeShader.SetInt("NumInfluences", numVirtualInfluences);
        // vertex shader
        var material = GetComponent<SkinnedMeshRenderer>().material;
        material.SetInt("NumInfluences", numVertexInfluences);
        material.SetBuffer("VirtualJoints", cbVirtualJoints);

        ReadVertexWeight();
        for (int j = 0; j < numVirtualJoints; ++j)
        {
            Vector3 p = Vector3.zero;
            for (int i = 0; i < numVirtualInfluences; ++i)
            {
                float w = virtualWeight[j * numVirtualInfluences + i];
                uint id = virtualIndices[j * numVirtualInfluences + i];
                if (w > 0)
                {
                    p.x += w * masterJoints[id * 12 + 9];
                    p.y += w * masterJoints[id * 12 + 10];
                    p.z += w * masterJoints[id * 12 + 11];
                }
            }
            initPos[j] -= p;
        }        
    }

    void Update()
    {
        DisplayBone db = joints.GetComponent<DisplayBone>();
        for (int i = 0; i < numVirtualJoints; ++i)
        {
            db.jointPose[i] = Matrix4x4.identity;
        }

        var animator = GetComponent<Animator>();
        var animClip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        var ecb = new EditorCurveBinding();
        ecb.type = typeof(Transform);

        var mms = new Matrix4x4[numMasterJoints];
        for (int i = 0; i < numMasterJoints; ++i)
        {
            ecb.path = "Joints/Joint" + System.String.Format("{0, 2:D2}", i + 1);
            mms[i] = ComposeMatrix(animClip, ecb, Time.time % animClip.length);
            var sm = mms[i] * masterInvBindPose[i];

            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    masterJoints[i * 12 + c * 3 + r] = sm[r, c];
                    if (viewPrimary)
                    {
                        db.jointPose[i][r, c] = mms[i][r, c];
                    }
                }
            }
        }
        cbMasterJoints.SetData(masterJoints);

        //long freq, before, after;
        //freq = before = after = 0;
        //QueryPerformanceFrequency(ref freq);
        //QueryPerformanceCounter(ref before);
        //for (long l = 0; l < 10000; ++l)
        {
            computeShader.Dispatch(computeShaderID, numVirtualJoints, 1, 1);
        }
        cbVirtualJoints.GetData(virtualJoints);

        for (int j = 0; j < numVirtualJoints && viewSecondary; ++j)
        {
            Matrix4x4 p = Matrix4x4.zero;
            if (!isActive[j])
            {
                db.jointPose[j] = Matrix4x4.identity;
                continue;
            }
            for (int i = 0; i < numVirtualInfluences; ++i)
            {
                float w = virtualWeight[j * numVirtualInfluences + i];
                uint id = virtualIndices[j * numVirtualInfluences + i];
                if (w > 0)
                {
                    for (int r = 0; r < 4; ++r)
                    {
                        for (int c = 0; c < 4; ++c)
                        {
                            p[r, c] += w * mms[id][r, c];
                        }
                    }
                }
            }
            p[0, 3] += initPos[j].x;
            p[1, 3] += initPos[j].y;
            p[2, 3] += initPos[j].z;
            db.jointPose[j] = p;
        }
        //QueryPerformanceCounter(ref after);
        //Debug.Log((after - before) * 1.0e6f / freq);
    }

    void OnDestroy()
    {
        cbVirtualJoints.Release();
        cbMasterJoints.Release();
        cbVirtualWeight.Release();
        cbVirtualIndex.Release();
    }
}
