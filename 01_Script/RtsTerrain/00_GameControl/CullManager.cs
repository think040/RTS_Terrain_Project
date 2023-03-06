using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Jobs;

public class CullManager : MonoBehaviour
{
    public ComputeShader cshader;
    public Shader gshader;

    int ki_pvf;
    int ki_ovf;

    int ki_pvf_vertex;
    int ki_ovf_vertex;
    int ki_sphere_vertex;

    int ki_pvf_cull_sphere;
    int ki_ovf_cull_sphere;

    int ki_sphere_center;

    int pass;
    int pass_pvf;
    int pass_ovf;
    int pass_sphere;

    int debugCullMode = 0;
    bool bDrawPvf = true;
    bool bDrawOvf = true;
    bool bDrawSp = true;

    Camera mainCam;

    public void Init()
    {
        {
            ki_pvf = cshader.FindKernel("CS_PVF");
            ki_ovf = cshader.FindKernel("CS_OVF");

            ki_pvf_vertex = cshader.FindKernel("CS_PVF_Vertex");
            ki_ovf_vertex = cshader.FindKernel("CS_OVF_Vertex");
            ki_sphere_vertex = cshader.FindKernel("CS_Sphere_Vertex");

            ki_pvf_cull_sphere = cshader.FindKernel("CS_PVF_Cull_Sphere");
            ki_ovf_cull_sphere = cshader.FindKernel("CS_OVF_Cull_Sphere");

            ki_sphere_center = cshader.FindKernel("CS_Sphere_Center");
        }

        {
            mainCam = Camera.main;
        }

        {

#if UNITY_EDITOR
            bCullDebug = true;
#else
            bCullDebug = false;
#endif
        }

        InitData();
        InitResource();
        InitDebugRender();
    }

    void OnEnable()
    {
        //RenderGOM.Cull += Compute;
        RenderGOM.OnRenderCamDebug += Render;
    }

    void OnDisable()
    {
        //RenderGOM.Cull -= Compute;
        RenderGOM.OnRenderCamDebug -= Render;
    }

    UnitManager[] unitMans;
    ArrowManager arrowMans;
    TorusManager torusMan;
    HpbarManager hpbarMan;
   
    public static int[] spCounts;
    public static int spCount = 0;
    public static int[] cullOffsets;
   
    int dpSpCount;

    int spVtxInCount;
    int vfVtxCount;

    int spVtxOutCount;
    int pvfVtxOutCount;
    int ovfVtxOutCount;

    Transform[] spTrs;

    int pvfCount = 1;
    int planePvfCount;
    int groupPvfCount;
    int totalPvfCount;

    int ovfCount;
    int planeOvfCount;
    int groupOvfCount;
    int totalOvfCount;

    public CSM_Action csmAction;
    float4[] csmPos;
    quaternion[] csmRot;
    float4[] csmfi;

    public static bool bCull = true;
    bool bCullDebug = true;

    
    NativeArray<float3> spOffset;    
    TransformAccessArray spTraa;
   

    NativeArray<float4x4> spTrData;
    SphereTransform spTrans;
    int dpSpTrCount;


    void InitData()
    {
        unitMans = GameManager.unitMan;
        arrowMans = GameManager.arrowMan;
        torusMan = GameManager.torusMan;
        hpbarMan = GameManager.hpbarMan;

        {
            int[] baseVtx;
            sMesh = RenderUtil.CreateSphereMeshWirePartsDetail_Normal(1.0f, 12, 6, out baseVtx);
            cMesh = RenderUtil.CreateBoxMeshWirePartsDetail_Normal(0.5f, out baseVtx);
        }

        spCount = 0;
        {
            spCounts = new int[unitMans.Length + 1];
            cullOffsets = new int[unitMans.Length + 1];
            //_cullOffsets = new int[unitMans.Length + 1];

            for (int i = 0; i < unitMans.Length; i++)
            {
                //_cullOffsets[i] = spCount;
                cullOffsets[i] = spCount;

                spCounts[i] = unitMans[i].count;
                spCount += spCounts[i];

                //unitMans[i].SetCullData(i);
            }

            {
                int i = unitMans.Length;

                //_cullOffsets[i] = spCount;
                cullOffsets[i] = spCount;

                spCounts[i] = ArrowManager.cCount;
                spCount += ArrowManager.cCount;

                //arrowMans.SetCullData(i);
            }

            bCull = true;
            if (spCount == 0) bCull = false;

            dpSpCount = (spCount % 8 == 0) ? (spCount / 8) : (spCount / 8 + 1);
        }

        {
            spTrs = new Transform[spCount];
            _sphere = new float4[spCount];
            spOffset = new NativeArray<float3>(spCount, Allocator.Persistent);
            //spCenter = new NativeArray<float3>(spCount, Allocator.Persistent);

            int start = 0;
            int i = 0;
            for (i = 0; i < unitMans.Length; i++)
            {
                for (int j = 0; j < unitMans[i].count; j++)
                {
                    spTrs[start + j] = unitMans[i].trs[j];
                    SphereCollider col = unitMans[i].unitActors[j].GetComponent<SphereCollider>();
                    _sphere[start + j] = new float4(0.0f, 0.0f, 0.0f, col.radius);
                    spOffset[start + j] = col.center;
                }
                start += unitMans[i].count;
            }

            {
                for (int j = 0; j < ArrowManager.cCount; j++)
                {
                    spTrs[start + j] = arrowMans.arrow[j].transform;
                    SphereCollider col = arrowMans.arrow[i].GetComponent<SphereCollider>();
                    _sphere[start + j] = new float4(0.0f, 0.0f, 0.0f, col.radius);
                    spOffset[start + j] = col.center;
                }
            }
        }

        {
            csmPos = csmAction.pos;
            csmRot = csmAction.rot;
            csmfi = csmAction.fi;
        }

        {
            pvfCount = 1;
            ovfCount = csmAction.csmCount;
        }

        {
            groupPvfCount = 1;
            groupOvfCount = 1;

            totalPvfCount = groupPvfCount * pvfCount;
            totalOvfCount = groupOvfCount * ovfCount;

            planePvfCount = totalPvfCount * 6;
            planeOvfCount = totalOvfCount * 6;
        }

        {
            spVtxInCount = sMesh.vertexCount;
            vfVtxCount = cMesh.vertexCount;

            spVtxOutCount = sMesh.vertexCount * spCount;
            pvfVtxOutCount = totalPvfCount * vfVtxCount;
            ovfVtxOutCount = totalOvfCount * vfVtxCount;
        }

        {
            spTraa = new TransformAccessArray(spTrs);

            //spAction = new SphereAction();

            //spAction.spOffset = spOffset;
            //spAction.spCenter = spCenter;
        }

        {
            spTrData = new NativeArray<float4x4>(spCount, Allocator.Persistent);

            spTrans = new SphereTransform();
            spTrans.spTr = spTrData;

            dpSpTrCount = (spCount % 64 == 0) ? (spCount / 64) : (spCount / 64 + 1);
        }
    }    
    ROBuffer<Info_VF> info_pvf_Buffer;
    ROBuffer<Info_VF> info_ovf_Buffer;
    RWBuffer<float4> plane_pvf_Buffer;
    RWBuffer<float4> plane_ovf_Buffer;

    ROBuffer<Vertex> vf_vertex_Buffer;
    RWBuffer<Vertex> pvf_vertex_Buffer;
    RWBuffer<Vertex> ovf_vertex_Buffer;

    ROBuffer<float4> sphere_Buffer;
    ROBuffer<Vertex> sphere_vertex_In_Buffer;
    RWBuffer<Vertex> sphere_vertex_Out_Buffer;  
    
    ROBuffer<float4> sphere_In_Buffer;
    ROBuffer<float4x4> sphere_trM_Buffer;
    RWBuffer<float4> sphere_Out_Buffer;

    RenderTexture cullResult_pvf_Texture;
    RenderTexture cullResult_ovf_Texture;
    

    float4[] _sphere;   

    public static float[] cullResult_pvf
    {
        get; set;
    }

    public static float[] cullResult_ovf
    {
        get; set;
    }
  
    void InitResource()
    {        
        {
            info_pvf_Buffer =   new ROBuffer<Info_VF>(totalPvfCount);
            info_ovf_Buffer =   new ROBuffer<Info_VF>(totalOvfCount);
            plane_pvf_Buffer =  new RWBuffer<float4>(planePvfCount);
            plane_ovf_Buffer =  new RWBuffer<float4>(planeOvfCount);

            vf_vertex_Buffer =  new ROBuffer<Vertex>(vfVtxCount);
            pvf_vertex_Buffer = new RWBuffer<Vertex>(pvfVtxOutCount );
            ovf_vertex_Buffer = new RWBuffer<Vertex>(ovfVtxOutCount );

            sphere_Buffer =             new ROBuffer<float4>(spCount);
            sphere_vertex_In_Buffer =   new ROBuffer<Vertex>(spVtxInCount   );
            sphere_vertex_Out_Buffer =  new RWBuffer<Vertex>(spVtxOutCount  );

            sphere_In_Buffer =  new ROBuffer<float4>(spCount);
            sphere_trM_Buffer = new ROBuffer<float4x4>(spCount);
            sphere_Out_Buffer = new RWBuffer<float4>(spCount);
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.colorFormat = RenderTextureFormat.RFloat;
                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = spCount;
            }

            {
                rtd.height = pvfCount;
                rtd.volumeDepth = groupPvfCount;
                cullResult_pvf_Texture = new RenderTexture(rtd);
            }

            {
                rtd.height = ovfCount;
                rtd.volumeDepth = groupOvfCount;
                cullResult_ovf_Texture = new RenderTexture(rtd);
            }
        }

        {
            cshader.SetInt("spCount", spCount);

            cshader.SetBuffer(ki_pvf, "info_pvf_Buffer",    info_pvf_Buffer.value);
            cshader.SetBuffer(ki_pvf, "plane_pvf_Buffer",   plane_pvf_Buffer.value);
            cshader.SetBuffer(ki_ovf, "info_ovf_Buffer",    info_ovf_Buffer.value);
            cshader.SetBuffer(ki_ovf, "plane_ovf_Buffer",   plane_ovf_Buffer.value);

            cshader.SetBuffer(ki_pvf_vertex, "info_pvf_Buffer", info_pvf_Buffer.value );
            cshader.SetBuffer(ki_pvf_vertex, "vf_vertex_Buffer", vf_vertex_Buffer.value );
            cshader.SetBuffer(ki_pvf_vertex, "pvf_vertex_Buffer", pvf_vertex_Buffer.value );
            cshader.SetBuffer(ki_ovf_vertex, "info_ovf_Buffer", info_ovf_Buffer.value );
            cshader.SetBuffer(ki_ovf_vertex, "vf_vertex_Buffer", vf_vertex_Buffer.value );
            cshader.SetBuffer(ki_ovf_vertex, "ovf_vertex_Buffer", ovf_vertex_Buffer.value );

            cshader.SetBuffer(ki_sphere_vertex, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_vertex_In_Buffer", sphere_vertex_In_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_vertex_Out_Buffer", sphere_vertex_Out_Buffer.value);

            cshader.SetBuffer(ki_pvf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull_sphere, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull_sphere, "plane_pvf_Buffer", plane_pvf_Buffer.value);
            cshader.SetBuffer(ki_ovf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_ovf_cull_sphere, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_ovf_cull_sphere, "plane_ovf_Buffer", plane_ovf_Buffer.value);

            cshader.SetTexture(ki_pvf_cull_sphere, "cullResult_pvf_Texture", cullResult_pvf_Texture);
            cshader.SetTexture(ki_ovf_cull_sphere, "cullResult_ovf_Texture", cullResult_ovf_Texture);

            cshader.SetBuffer(ki_sphere_center, "sphere_In_Buffer", sphere_In_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_trM_Buffer", sphere_trM_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_Out_Buffer", sphere_Out_Buffer.value);
        }

        {           
            cullResult_pvf = new float[spCount * totalPvfCount];
            cullResult_ovf = new float[spCount * totalOvfCount];            
        }

        {
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> nom = new List<Vector3>();

            cMesh.GetVertices(pos);
            cMesh.GetNormals(nom);

            var data = vf_vertex_Buffer.data;
            for (int i = 0; i < vfVtxCount; i++)
            {
                Vertex vtx = new Vertex();
                vtx.position = pos[i];
                vtx.normal = nom[i];
                data[i] = vtx;
            }
            vf_vertex_Buffer.Write();
        }

        {
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> nom = new List<Vector3>();

            sMesh.GetVertices(pos);
            sMesh.GetNormals(nom);

            var data = sphere_vertex_In_Buffer.data;
            for (int i = 0; i < spVtxInCount; i++)
            {
                Vertex vtx = new Vertex();
                vtx.position = pos[i];
                vtx.normal = nom[i];
                data[i] = vtx;
            }
            sphere_vertex_In_Buffer.Write();
        }
    }

    Material mte;
    MaterialPropertyBlock mpb;

    Mesh cMesh;
    Mesh sMesh;

    GraphicsBuffer idxVf_Buffer;
    GraphicsBuffer idxSp_Buffer;



    void InitDebugRender()
    {
        mte = new Material(gshader);
        mpb = new MaterialPropertyBlock();

        pass = mte.FindPass("Cull");
        pass_pvf = mte.FindPass("Cull_Pvf");
        pass_ovf = mte.FindPass("Cull_Ovf");
        pass_sphere = mte.FindPass("Cull_Sphere");

        idxVf_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)cMesh.GetIndexCount(0), sizeof(int));
        idxSp_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)sMesh.GetIndexCount(0), sizeof(int));

        idxVf_Buffer.SetData(cMesh.GetIndices(0));
        idxSp_Buffer.SetData(sMesh.GetIndices(0));

        mpb.SetTexture("cullResult_pvf_Texture", cullResult_pvf_Texture);
        mpb.SetTexture("cullResult_ovf_Texture", cullResult_ovf_Texture);

        mpb.SetBuffer("sphere_vertex_Out_Buffer", sphere_vertex_Out_Buffer.value);
        mpb.SetBuffer("pvf_vertex_Buffer", pvf_vertex_Buffer.value);
        mpb.SetBuffer("ovf_vertex_Buffer", ovf_vertex_Buffer.value);

        mpb.SetInt("dvCount_sp", sMesh.vertexCount);
        mpb.SetInt("dvCount_vf", cMesh.vertexCount);       
    }

    void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
        {
            mpb.SetInt("cullMode", debugCullMode);
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_light", csmAction.dirW); 
            mpb.SetVector("posW_view", cam.transform.position);
        }

        {           
            {
                if (bDrawSp)
                {
                    cmd.DrawProcedural(idxSp_Buffer, Matrix4x4.identity, mte, pass_sphere, MeshTopology.Lines, idxSp_Buffer.count, spCount, mpb);
                }
            }

            {
                if (bDrawPvf)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_pvf, MeshTopology.Lines, idxVf_Buffer.count, pvfCount, mpb);
                }

                if (bDrawOvf)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_ovf, MeshTopology.Lines, idxVf_Buffer.count, ovfCount, mpb);
                }
            }          
        }

    }

    void ReleaseDebugResource()
    {
        ReleaseGBuffer(idxVf_Buffer);
        ReleaseGBuffer(idxSp_Buffer);
    }   

    void OnDestroy()
    {      
        ReleaseResource();
        ReleaseDebugResource();
    }

    void ReleaseResource()
    {
        BufferBase<Info_VF>.Release(info_pvf_Buffer);
        BufferBase<Info_VF>.Release(info_ovf_Buffer);
        BufferBase<float4>.Release(plane_pvf_Buffer);
        BufferBase<float4>.Release(plane_ovf_Buffer);
        BufferBase<Vertex>.Release(vf_vertex_Buffer);
        BufferBase<Vertex>.Release(pvf_vertex_Buffer);
        BufferBase<Vertex>.Release(ovf_vertex_Buffer);
        BufferBase<float4>.Release(sphere_Buffer);
        BufferBase<Vertex>.Release(sphere_vertex_In_Buffer);
        BufferBase<Vertex>.Release(sphere_vertex_Out_Buffer);
        BufferBase<float4>.Release(sphere_In_Buffer);
        BufferBase<float4x4>.Release(sphere_trM_Buffer);
        BufferBase<float4>.Release(sphere_Out_Buffer);

        ReleaseRenTexture(cullResult_pvf_Texture);
        ReleaseRenTexture(cullResult_ovf_Texture);

        DisposeNa<float3>(spOffset);        
        DisposeTraa(spTraa);

        DisposeNa<float4x4>(spTrData);
    }

    public void Begin()
    {
        {
            for (int i = 0; i < unitMans.Length; i++)
            {
                unitMans[i].SetCullData(i, cullResult_pvf_Texture, cullResult_ovf_Texture);
            }
        
            {
                int i = unitMans.Length;
                arrowMans.SetCullData(i, cullResult_pvf_Texture, cullResult_ovf_Texture);
            }
        
            {
                torusMan.SetCullData(cullResult_pvf_Texture);
                hpbarMan.SetCullData(cullResult_pvf_Texture);
            }
        }
    }


    void Start()
    {

    }


    void Update()
    {
        {
            Compute();
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            debugCullMode++;
            if (debugCullMode > 5)
            {
                debugCullMode = 0;
            }

            //debugCullMode = (++debugCullMode) % 6;
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            debugCullMode--;
            if (debugCullMode < 0)
            {
                debugCullMode = 5;
            }
        }
    }

    void Compute()
    {        
        {
            WriteToResource();
            DispatchCompute();
            //ReadFromResource();
        }
    }

    void WriteToResource()
    {
        {
            var data = info_pvf_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                Info_VF ivf = new Info_VF();
                Transform camTr = mainCam.transform;

                ivf.fi = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
                ivf.pos = camTr.position;
                ivf.rot = ((quaternion)(camTr.rotation)).value;

                data[i] = ivf;
            }
            info_pvf_Buffer.Write();
        }

        {
            var data = info_ovf_Buffer.data;
            for (int i = 0; i < ovfCount; i++)
            {
                Info_VF ivf = new Info_VF();

                ivf.fi = csmfi[i];
                ivf.pos = csmPos[i].xyz;
                ivf.rot = csmRot[i].value;

                data[i] = ivf;
            }
            info_ovf_Buffer.Write();
        }

        {
            spTrans.Schedule<SphereTransform>(spTraa).Complete();

            {
                var data = sphere_In_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    _sphere[i].xyz = spOffset[i];
                    data[i] = _sphere[i];
                }
                sphere_In_Buffer.Write();
            }

            {
                var data = sphere_trM_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    data[i] = spTrData[i];
                }
                sphere_trM_Buffer.Write();
            }
        }
    }

    void DispatchCompute()
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        cmd.DispatchCompute(cshader, ki_pvf, totalPvfCount, 1, 1);
        cmd.DispatchCompute(cshader, ki_ovf, totalOvfCount, 1, 1);

        cmd.DispatchCompute(cshader, ki_sphere_center, dpSpTrCount, 1, 1);

        if (bCullDebug)
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex, totalPvfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_ovf_vertex, totalOvfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_sphere_vertex, spCount, 1, 1);
        }

        cmd.DispatchCompute(cshader, ki_pvf_cull_sphere, dpSpCount, pvfCount, 1);
        cmd.DispatchCompute(cshader, ki_ovf_cull_sphere, dpSpCount, ovfCount, 1);

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void ReadFromResource()
    {
        bool bReadDebug = false;

        if (bReadDebug)
        {
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                {
                    plane_pvf_Buffer.Read();
                }

                {
                    plane_ovf_Buffer.Read();
                }

                {
                    pvf_vertex_Buffer.Read();
                }

                {
                    ovf_vertex_Buffer.Read();
                }

                {
                    sphere_vertex_Out_Buffer.Read();
                }

                {
                    sphere_Out_Buffer.Read();
                }


                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            {
                var read = AsyncGPUReadback.Request(cullResult_pvf_Texture);
                read.WaitForCompletion();

                for (int i = 0; i < groupPvfCount; i++)
                {
                    var na = read.GetData<float>(i);
                    for (int j = 0; j < pvfCount; j++)
                    {
                        for (int k = 0; k < spCount; k++)
                        {
                            cullResult_pvf[i * pvfCount * spCount + j * spCount + k] = na[j * spCount + k];
                        }
                    }
                }
            }

            {
                var read = AsyncGPUReadback.Request(cullResult_ovf_Texture);
                read.WaitForCompletion();

                for (int i = 0; i < groupOvfCount; i++)
                {
                    var na = read.GetData<float>(i);
                    for (int j = 0; j < ovfCount; j++)
                    {
                        for (int k = 0; k < spCount; k++)
                        {
                            cullResult_ovf[i * ovfCount * spCount + j * spCount + k] = na[j * spCount + k];
                        }
                    }
                }
            }
        }                               
    } 

    public void Read_PvfCullData()
    {
        {
            var read = AsyncGPUReadback.Request(cullResult_pvf_Texture);
            read.WaitForCompletion();

            int size0 = pvfCount * spCount;
            for (int i = 0; i < groupPvfCount; i++)
            {
                var na = read.GetData<float>(i);
                for (int j = 0; j < pvfCount; j++)
                {
                    for (int k = 0; k < spCount; k++)
                    {                      
                        cullResult_pvf[i * size0 + j * spCount + k] = na[j * spCount + k];
                    }
                }
            }
        }
    }
  

    void ReleaseGBuffer(GraphicsBuffer gbuffer)
    {
        if (gbuffer != null) gbuffer.Release();
    }

    void ReleaseRenTexture(RenderTexture tex)
    {
        if (tex != null) tex.Release();
    }

    void DisposeNa<T>(NativeArray<T> na) where T : struct
    {
        if (na.IsCreated) na.Dispose();
    }

    void DisposeTraa(TransformAccessArray traa)
    {
        if (traa.isCreated) traa.Dispose();
    }

    [System.Serializable]
    public struct Info_VF
    {
        public float4 fi;
        public float3 pos;
        public float4 rot;
    }

    [System.Serializable]
    public struct Vertex
    {
        public float3 position;
        public float3 normal;
    }   

    [BurstCompile]
    struct SphereTransform : IJobParallelForTransform
    {
        public NativeArray<float4x4> spTr;

        public void Execute(int i, TransformAccess traa)
        {
            float4x4 tr = float4x4.zero;

            tr.c0.xyz = traa.localPosition;
            tr.c1 = ((quaternion)traa.localRotation).value;
            tr.c2.xyz = traa.localScale;

            spTr[i] = tr;
        }
    }  
   
}