using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

using Utility_JSB;

public class TerrainManager : MonoBehaviour
{
    public Terrain terrain;
    TerrainData tData;
    RenderTexture hMap;
    public Texture2D bakedTex;
    public Texture2D bakedNormalTex;
    public Texture2D waterTex;

    public ComputeShader cshader;
    public Shader gshader;

    public Camera mainCam;
    public CSM_Action csm_action;
    public bool bDebug = true;
    public bool bNormalLine = true;
    public float maxTessFactor = 64;

    public void Init()
    {
        InitTerrain();
        InitCompute();
        InitResource();

        InitRendering();
        InitRendering_debug();
    }   

    void OnEnable()
    {
        {
            RenderGOM.Cull += Compute;

            RenderGOM.RenderCSM += RenderCSM;
            RenderGOM.OnRenderCamAlpha += RenderCAM;
        }

#if UNITY_EDITOR
        {
            RenderGOM.RenderCSM += RenderCSM_Debug;

            //RenderGOM.OnRenderCam += RenderCAM_Debug;
            //RenderGOM.OnRenderCam += RenderCAM_Line;

            RenderGOM.OnRenderCamDebug += RenderCAM_Debug;
            RenderGOM.OnRenderCamDebug += RenderCAM_Line;
        }
#endif
    }

    void OnDisable()
    {
        {
            RenderGOM.Cull -= Compute;

            RenderGOM.RenderCSM -= RenderCSM;
            RenderGOM.OnRenderCamAlpha -= RenderCAM;
        }

#if UNITY_EDITOR
        {
            RenderGOM.RenderCSM -= RenderCSM_Debug;

            //RenderGOM.OnRenderCam -= RenderCAM_Debug;
            //RenderGOM.OnRenderCam -= RenderCAM_Line;

            RenderGOM.OnRenderCamDebug -= RenderCAM_Debug;
            RenderGOM.OnRenderCamDebug -= RenderCAM_Line;
        }
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
        ChangeLightDirection();
    }

    public static float4x4 matT
    {
        get; set;
    }

    public static float4 t1_t0
    {
        get; set;
    }

    public bool bWire = false;
    void Update()
    {

        {
            W = RenderUtil.GetWfromL(transform);
            W_IT = math.transpose(math.inverse(W));
        }

        {
            float4x4 V = float4x4.zero;
            float4x4 C = float4x4.zero;

            V = RenderUtil.GetVfromW(mainCam);
            C = RenderUtil.GetCfromV(mainCam, true);
            S = RenderUtil.GetSfromN(mainCam, true);
            CV = math.mul(C, V);
            dirW_mainCam = new float4(-math.rotate(mainCam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)), 0.0f);
            posW_mainCam = new float4(mainCam.transform.position, 0.0f);
        }

        {
            //dirW_light = new float4(-math.rotate(mainCam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)), 0.0f);
            dirW_light = -csm_action.dirW;
            posW_light = new float4(csm_action.transform.position, 0.0f);
        }

        {
            Transform tr = terrain.transform;
            matT = RenderUtil.GetVfromW(tr.position, tr.rotation);
        }

        {
            tMpb.SetMatrix("W", W);
            tMpb.SetMatrix("W_IT", W_IT);

            tMpb.SetVector("dirW_light", dirW_light);
            tMpb.SetVector("posW_light", posW_light);

            tMpb.SetFloat("maxTessFactor", maxTessFactor);
            tMpb.SetVector("color", color);
        }

#if UNITY_EDITOR
        //Debug
        {
            {
                ChangeDebugMode();
            }

            {
                ChangeLayerTexMode();
            }

            {
                ChangeNormalMapMode();
            }

            {
                ChangeQtIdx();
            }
        }
#endif


    }


    int debugMode = 0;
    int debugModeCount = 5;
    void ChangeDebugMode()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bWire = bWire ? false : true;
            pass_csm = bWire ? pass_csm_wire : pass_csm_solid;
            pass_cam = bWire ? pass_cam_wire : pass_cam_solid;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            bDebug = bDebug ? false : true;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            debugMode = (++debugMode) % debugModeCount;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            debugMode = (--debugMode) < 0 ? debugModeCount - 1 : debugMode;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            bNormalLine = bNormalLine ? false : true;
        }

        if (debugMode == 0)
        {
            mode = 0;
            vfIdx = 0;
            csm_action.csmIdx = vfIdx;
        }
        else if (debugMode == 1)
        {
            mode = 1;
            vfIdx = 0;
            csm_action.csmIdx = vfIdx;
        }
        else if (debugMode == 2)
        {
            mode = 1;
            vfIdx = 1;
            csm_action.csmIdx = vfIdx;
        }
        else if (debugMode == 3)
        {
            mode = 1;
            vfIdx = 2;
            csm_action.csmIdx = vfIdx;
        }
        else if (debugMode == 4)
        {
            mode = 1;
            vfIdx = 3;
            csm_action.csmIdx = vfIdx;
        }
    }

    public Transform lightTr;
    public Slider slider_ax;
    public Slider slider_ay;

    void ChangeLightDirection()
    {
        {
            slider_ax.minValue = 0.0f;
            slider_ax.maxValue = 90.0f;
            slider_ax.onValueChanged.AddListener(
                (value) =>
                {
                    //if(Input.GetKey(KeyCode.P))
                    {
                        quaternion rot = lightTr.rotation;

                        float ay = slider_ay.value;
                        float ax = value;

                        rot = math.mul(
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(ay)),
                            quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(ax)));

                        lightTr.rotation = rot;
                    }

                });

            slider_ax.value = 45.0f;
        }

        {
            slider_ay.minValue = 0.0f;
            slider_ay.maxValue = 360.0f;
            slider_ay.onValueChanged.AddListener(
                (value) =>
                {
                    //if (Input.GetKey(KeyCode.P))
                    {
                        quaternion rot = lightTr.rotation;

                        float ay = value;
                        float ax = slider_ax.value;

                        rot = math.mul(
                           quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(ay)),
                           quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(ax)));

                        lightTr.rotation = rot;
                    }
                });

            slider_ay.value = 45.0f;
        }



        {
            float ax = 0.0f;
            float ay = 0.0f;

            quaternion rot = quaternion.identity;

            rot = math.mul(
                quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(ay)),
                quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(ax)));

            //lightTr.rotation = rot;
        }


    }

    public bool useLayerTex = false;

    void ChangeLayerTexMode()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            useLayerTex = useLayerTex ? false : true;
        }

        //LAYERED_DIFFUSE_TEX 
        //BAKED_DIFFUSE_TEX       
        if (useLayerTex)
        {
            tMte.EnableKeyword("LAYERED_DIFFUSE_TEX");
            tMte.DisableKeyword("BAKED_DIFFUSE_TEX");
        }
        else
        {
            tMte.DisableKeyword("LAYERED_DIFFUSE_TEX");
            tMte.EnableKeyword("BAKED_DIFFUSE_TEX");
        }
    }

    bool useLayerdNomalMap = true;

    int normalMode = 2;

    void ChangeNormalMapMode()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            //useLayerdNomalMap = useLayerdNomalMap ? false : true;

            normalMode = (++normalMode) % 3;
            //normalMode = (++normalMode) % 2;
        }

        //NON_NORMAL_MAP
        if (normalMode == 0)
        {
            tMte.EnableKeyword("LAYERED_NORMAL_MAP");
            tMte.DisableKeyword("BAKED_NORMAL_MAP");
            tMte.DisableKeyword("NONE_NORMAL_MAP");
        }
        else if (normalMode == 1)
        {
            tMte.DisableKeyword("LAYERED_NORMAL_MAP");
            tMte.EnableKeyword("BAKED_NORMAL_MAP");
            tMte.DisableKeyword("NONE_NORMAL_MAP");
        }
        else //else if (normalMode == 1)
        {
            tMte.DisableKeyword("LAYERED_NORMAL_MAP");
            tMte.DisableKeyword("BAKED_NORMAL_MAP");
            tMte.EnableKeyword("NONE_NORMAL_MAP");
        }

        //if (useLayerdNomalMap)
        //{
        //    tMte.EnableKeyword ("LAYERED_NORMAL_MAP");
        //    tMte.DisableKeyword("BAKED_NORMAL_MAP");
        //}
        //else
        //{
        //    tMte.DisableKeyword("LAYERED_NORMAL_MAP");
        //    tMte.EnableKeyword ("BAKED_NORMAL_MAP");
        //}
    }

    int qtIdx = 0;

    void ChangeQtIdx()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            qtIdx = (++qtIdx) % qtCount;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            qtIdx = (--qtIdx) < 0 ? qtCount - 1 : qtIdx;
        }
    }

    void OnDestroy()
    {
        ReleaseResource();

        {
            BufferBase<Vector2>.Release(layerSizeBuffer);
        }

        {
            ReleaseTexture(normalHeight_Tex);
            ReleaseTexture(LodData_Tex);

            ReleaseTexture(pPlane_Tex);
            ReleaseTexture(pPos_Tex);
            ReleaseTexture(pNormal_Tex);
            ReleaseTexture(pPosWire_Tex);

            ReleaseTexture(TestCullPVF_Tex);
            ReleaseTexture(TestCullOVF_Tex);

            ReleaseTexture(TileData_qt_Tex);
            ReleaseTexture(TileW_qt_Tex);
            ReleaseTexture(TileWn_qt_Tex);

            ReleaseTexture(TestCullPVF_qt_Tex);
            ReleaseTexture(TestCullOVF_qt_Tex);
        }


    }

    float3 dpCount;
    float3 gtCount;
    float3 tileCount;

    float3 terrainSize;
    float3 tileSize;

    int pvfCount = 1;
    int ovfCount = 4;

    float3 qtStage;
    int qtCount;
    int[] qtCounts;

    void InitTerrain()
    {
        tData = terrain.terrainData;
        hMap = tData.heightmapTexture;

        dpCount = new float3((float)(hMap.width - 1), 1.0f, (float)(hMap.height - 1));
        gtCount = new float3(16.0f, 1.0f, 16.0f);
        tileCount = dpCount / gtCount;

        terrainSize = tData.size;
        terrainSize.y *= 2.0f;
        tileSize = terrainSize / tileCount;

        {
            qtStage = math.log2(tileCount);
            qtCount = (int)qtStage.x + 1;
            qtCounts = new int[qtCount];

            for (int i = 0; i < qtCount; i++)
            {
                qtCounts[i] = (int)math.pow(2.0f, (float)i);
            }
        }

        {
            t1_t0 = new float4(dpCount.xz / terrainSize.xz, 0.0f, 0.0f);
        }

    }

    int ki_lod;
    int ki_quadtree;
    int ki_pvf_vertex;
    int ki_pvf_vertex_wire;
    int ki_pvf_cull;
    int ki_ovf_cull;

    void InitCompute()
    {
        ki_lod = cshader.FindKernel("CS_LOD");
        ki_quadtree = cshader.FindKernel("CS_QuadTree");
        ki_pvf_vertex = cshader.FindKernel("CS_PVF_Vertex");
        ki_pvf_vertex_wire = cshader.FindKernel("CS_PVF_Vertex_Wire");
        ki_pvf_cull = cshader.FindKernel("CS_PVF_Cull");
        ki_ovf_cull = cshader.FindKernel("CS_OVF_Cull");
    }

    //lod
    float4 rot_terrain;
    float4 pos_terrain;

    
    float4[] LodData;
    float4x4[] TileBoxData;
    float4x4[] TileW;
    float4x4[] TileWn;

    RenderTexture LodData_Tex;
    public static RenderTexture normalHeight_Tex
    {
        get; private set;
    }
    
    float4[] nhData;

    //pvf_vertex
    HHCollider box;

    float4[] fisData;
    Vector4[] bPos; //[24]  //[8] wire
    Vector4[] bNormal; //[24]
    Vector4[] bCenter; //[1]
    Vector4[] bPlane;  //[12]

    
    float4[] pCenterData;   //float4    
    float4[] pPlaneData;    
    float4[] pPosData;    
    float4[] pNormalData;

    ROBuffer<float4> fis_Buffer;  //float4
    RWBuffer<float4> pCenter_Buffer;   //float4
    RenderTexture pPlane_Tex;
    RenderTexture pPos_Tex;
    RenderTexture pNormal_Tex;

    //pvf_vertex_wire
    Vector4[] bPosWire; //[8]
   
    float4[] pPosWireData;

    RenderTexture pPosWire_Tex;

    //cull    
    float[] TestCullPVFData;
    float[] TestCullOVFData;

    float4x4[] pvfM;
    float4x4[] ovfM;

    COBuffer<int> hhIndex_Buffer;
    ROBuffer<float4x4> pvfM_Buffer;
    ROBuffer<float4x4> ovfM_Buffer;

    RenderTexture TestCullPVF_Tex;
    RenderTexture TestCullOVF_Tex;

    void InitResource()
    {
        //CS_LOD
        {
            int _count = (int)(tileCount.x * tileCount.z);
            LodData = new float4[_count];
            TileBoxData = new float4x4[_count];
            TileW = new float4x4[_count];
            TileWn = new float4x4[_count];
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = (int)tileCount.x;
                rtd.height = 1;
                rtd.volumeDepth = (int)tileCount.z;
            }

            {
                rtd.height = 1;
                LodData_Tex = new RenderTexture(rtd);
            }
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = (int)dpCount.x;
                rtd.height = 1;
                rtd.volumeDepth = (int)dpCount.z;

                normalHeight_Tex = new RenderTexture(rtd);
            }

            nhData = new float4[(int)(dpCount.x) * 1 * (int)(dpCount.z)];
        }

        {
            cshader.SetVector("dpCount", new float4(dpCount, 0.0f));
            cshader.SetVector("gtCount", new float4(gtCount, 0.0f));
            cshader.SetVector("tileCount;", new float4(tileCount, 0.0f));

            cshader.SetVector("terrainSize", new float4(terrainSize, 0.0f));
            cshader.SetVector("tileSize", new float4(tileSize, 0.0f));

            cshader.SetTexture(ki_lod, "normalHeight_Tex", normalHeight_Tex);
            cshader.SetTexture(ki_lod, "hMap", hMap);
            cshader.SetTexture(ki_lod, "LodData_Tex", LodData_Tex);
        }


        //CS_PVF_Vertex && CS_PVF_Vertex_Wire
        {
            //solid
            fisData = new float4[pvfCount];
            bPos = new Vector4[24];
            bNormal = new Vector4[24];
            bCenter = new Vector4[1];
            bPlane = new Vector4[12];

            pCenterData = new float4[pvfCount];
            pPlaneData = new float4[12];
            pPosData = new float4[24];
            pNormalData = new float4[24];

            //wire
            bPosWire = new Vector4[8];

            pPosWireData = new float4[8];
        }


        {
            box = new HHCollider();
            box.InitBox();

            for (int i = 0; i < 24; i++)
            {
                bPos[i] = new float4(box.pos[i], 0.0f);
                bNormal[i] = new float4(box.nom[i], 0.0f);
            }

            for (int i = 0; i < 1; i++)
            {
                bCenter[i] = new float4(box.center, 0.0f);
            }

            for (int i = 0; i < 6; i++)
            {
                float3x2 plane = box.planes[i];
                bPlane[2 * i + 0] = new float4(plane.c0, 0.0f); //normal
                bPlane[2 * i + 1] = new float4(plane.c1, 0.0f); //position
            }

            //wire
            for (int i = 0; i < BoxWire.vtxCount; i++)
            {
                bPosWire[i] = new float4(BoxWire.sPos[i], 0.0f);
            }
        }

        {
            fis_Buffer = new ROBuffer<float4>(pvfCount);  //float4
            pCenter_Buffer = new RWBuffer<float4>(pvfCount);    //float4

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex2D;
                rtd.width = pvfCount;
                rtd.volumeDepth = 1;
            }

            {
                rtd.height = 12;
                pPlane_Tex = new RenderTexture(rtd);
            }

            {
                rtd.height = 24;
                pPos_Tex = new RenderTexture(rtd);
                pNormal_Tex = new RenderTexture(rtd);
            }

            //wire
            {
                rtd.height = 8;
                pPosWire_Tex = new RenderTexture(rtd);
            }
        }

        {
            cshader.SetVectorArray("bPos", bPos);
            cshader.SetVectorArray("bNormal", bNormal);
            cshader.SetVectorArray("bCenter", bCenter);
            cshader.SetVectorArray("bPlane", bPlane);

            cshader.SetBuffer(ki_pvf_vertex, "fis_Buffer", fis_Buffer.value);
            cshader.SetBuffer(ki_pvf_vertex, "pCenter_Buffer", pCenter_Buffer.value);
            cshader.SetTexture(ki_pvf_vertex, "pPlane_Tex", pPlane_Tex);
            cshader.SetTexture(ki_pvf_vertex, "pPos_Tex", pPos_Tex);
            cshader.SetTexture(ki_pvf_vertex, "pNormal_Tex", pNormal_Tex);

            //wire
            cshader.SetVectorArray("bPosWire", bPosWire);
            cshader.SetBuffer(ki_pvf_vertex_wire, "fis_Buffer", fis_Buffer.value);
            cshader.SetTexture(ki_pvf_vertex_wire, "pPosWire_Tex", pPosWire_Tex);
        }

        //Cull
        {
            int _count = (int)(tileCount.x * tileCount.z);
            TestCullPVFData = new float[_count * pvfCount];
            TestCullOVFData = new float[_count * ovfCount];

            pvfM = new float4x4[pvfCount];
            ovfM = new float4x4[ovfCount];
        }

        {
            hhIndex_Buffer = new COBuffer<int>(box.indices.Length);
            hhIndex_Buffer.data = box.indices;
            hhIndex_Buffer.Write();

            pvfM_Buffer = new ROBuffer<float4x4>(pvfCount);
            ovfM_Buffer = new ROBuffer<float4x4>(ovfCount);

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.RFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = (int)tileCount.x;
                rtd.volumeDepth = (int)tileCount.z; ;
            }

            {
                rtd.height = pvfCount;
                TestCullPVF_Tex = new RenderTexture(rtd);
            }

            {
                rtd.height = ovfCount;
                TestCullOVF_Tex = new RenderTexture(rtd);
            }
        }

        {
            //pvf
            cshader.SetBuffer(ki_pvf_cull, "hhIndex_Buffer", hhIndex_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull, "pvfM_Buffer", pvfM_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull, "pCenter_Buffer", pCenter_Buffer.value);
            cshader.SetTexture(ki_pvf_cull, "pPlane_Tex", pPlane_Tex);
            cshader.SetTexture(ki_pvf_cull, "pPos_Tex", pPos_Tex);
            cshader.SetTexture(ki_pvf_cull, "TestCullPVF_Tex", TestCullPVF_Tex);

            //ovf            
            cshader.SetBuffer(ki_ovf_cull, "ovfM_Buffer", ovfM_Buffer.value);
            cshader.SetTexture(ki_ovf_cull, "TestCullOVF_Tex", TestCullOVF_Tex);
        }

        {
            if (bDebug)
            {
                cshader.EnableKeyword("DEBUG_RENDER");
            }
            else
            {
                cshader.DisableKeyword("DEBUG_RENDER");
            }
        }

        {
            InitResourceQT();
        }
    }
    
    float4x4[] TileData_qt;    
    float4x4[] TileW_qt;    
    float4x4[] TileWn_qt;

    RenderTexture TileData_qt_Tex;
    RenderTexture TileW_qt_Tex;
    RenderTexture TileWn_qt_Tex;
    
    float[] TestCullPVF_qt;    
    float[] TestCullOVF_qt;

    RenderTexture TestCullPVF_qt_Tex;
    RenderTexture TestCullOVF_qt_Tex;


    void InitResourceQT()
    {
        //QuadTree
        {
            {
                int _count = (int)tileCount.x * (qtCount) * (int)tileCount.z;

                TileData_qt = new float4x4[_count];
                TileW_qt = new float4x4[_count];
                TileWn_qt = new float4x4[_count];
            }

            {
                RenderTextureDescriptor rtd = new RenderTextureDescriptor();
                {
                    rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                    rtd.msaaSamples = 1;
                    rtd.depthBufferBits = 0;
                    rtd.enableRandomWrite = true;

                    rtd.dimension = TextureDimension.Tex3D;
                    rtd.width = (int)tileCount.x;
                    rtd.height = qtCount * 4;
                    rtd.volumeDepth = (int)tileCount.z;
                }

                TileData_qt_Tex = new RenderTexture(rtd);
                TileW_qt_Tex = new RenderTexture(rtd);
                TileWn_qt_Tex = new RenderTexture(rtd);
            }


            {
                cshader.SetTexture(ki_lod, "TileBoxData_Tex", TileData_qt_Tex);
                cshader.SetTexture(ki_lod, "TileW_Tex", TileW_qt_Tex);
                cshader.SetTexture(ki_lod, "TileWn_Tex", TileWn_qt_Tex);
            }

            {
                cshader.SetTexture(ki_quadtree, "TileBoxData_Tex", TileData_qt_Tex);
                cshader.SetTexture(ki_quadtree, "TileW_Tex", TileW_qt_Tex);
                cshader.SetTexture(ki_quadtree, "TileWn_Tex", TileWn_qt_Tex);
            }

        }

        //Cull
        {
            {
                int pcount = (int)tileCount.x * (pvfCount * qtCount) * (int)tileCount.z;
                int ocount = (int)tileCount.x * (ovfCount * qtCount) * (int)tileCount.z;

                TestCullPVF_qt = new float[pcount];
                TestCullOVF_qt = new float[ocount];
            }

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.RFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = (int)tileCount.x;
                rtd.volumeDepth = (int)tileCount.z; ;
            }

            {
                rtd.height = pvfCount * (qtCount);
                TestCullPVF_qt_Tex = new RenderTexture(rtd);
            }

            {
                rtd.height = ovfCount * (qtCount);
                TestCullOVF_qt_Tex = new RenderTexture(rtd);
            }

            {
                cshader.SetFloat("pvfCount", (float)pvfCount);
                cshader.SetFloat("ovfCount", (float)ovfCount);
            }

            {
                cshader.SetTexture(ki_pvf_cull, "TestCullPVF_Tex", TestCullPVF_qt_Tex);
                cshader.SetTexture(ki_ovf_cull, "TestCullOVF_Tex", TestCullOVF_qt_Tex);
            }

            {
                cshader.SetTexture(ki_pvf_cull, "TileBoxData_Tex", TileData_qt_Tex);
                cshader.SetTexture(ki_ovf_cull, "TileBoxData_Tex", TileData_qt_Tex);
            }
        }
    }


    Mesh tMesh;
    Mesh bMesh;

    Material tMte;
    Material bMte;
    Material oMte;
    Material pMte;

    MaterialPropertyBlock tMpb;
    MaterialPropertyBlock bMpb;
    MaterialPropertyBlock oMpb;
    MaterialPropertyBlock pMpb;

    int pass_csm;
    int pass_cam;
    int pass_csm_solid;     //"Terrain_CSM"
    int pass_cam_solid;     //"Terrain_CAM"
    int pass_csm_wire;      //"Terrain_CSM_Wire"
    int pass_cam_wire;      //"Terrain_CAM_Wire"
    int pass_csm_debug;     //"Terrain_CSM_Debug"
    int pass_cam_debug;     //"Terrain_CAM_Debug"
    int pass_cam_line;      //"Terrain_CAM_Line"

    //
    float4x4 W;
    float4x4 W_IT;

    Color color = Color.green;

    int mode = 0; //0 Pvf-tileBox //1 Ovf-tileBox
    int vfIdx = 0; //mode == 0 -> vfIdx = 0    //mode == 1 -> vfIdx = 0, 1, 2, 3   

    float4x4 S;
    float4x4 CV;
    Matrix4x4[] CV_csm;
    Matrix4x4[] CV_csm_depth;

    float4 dirW_light;
    float4 posW_light;

    float4 dirW_mainCam;
    float4 posW_mainCam;

    Vector4[] layerSize;

    //public Texture2D testTex;

    COBuffer<Vector2> layerSizeBuffer;
  
    void InitRendering()
    {
        {
            tMesh = RenderUtil.CreateTerrainMeshGrid(terrainSize, (int3)tileCount);
            bMesh = RenderUtil.CreateCubeMeshWire();
        }

        {
            tMte = new Material(gshader);
            bMte = new Material(gshader);
            oMte = new Material(gshader);
            pMte = new Material(gshader);
        }

        {
            tMpb = new MaterialPropertyBlock();
            bMpb = new MaterialPropertyBlock();
            oMpb = new MaterialPropertyBlock();
            pMpb = new MaterialPropertyBlock();
        }

        {
            pass_csm_solid = tMte.FindPass("Terrain_CSM");
            pass_cam_solid = tMte.FindPass("Terrain_CAM");
            pass_csm_wire = tMte.FindPass("Terrain_CSM_Wire");
            pass_cam_wire = tMte.FindPass("Terrain_CAM_Wire");
            pass_csm_debug = tMte.FindPass("Terrain_CSM_Debug");
            pass_cam_debug = tMte.FindPass("Terrain_CAM_Debug");
            pass_cam_line = tMte.FindPass("Terrain_CAM_Line");

            pass_csm = pass_csm_solid;
            pass_cam = pass_cam_solid;
        }

        {
            tMpb.SetTexture("LodData_Tex", LodData_Tex);
            tMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_Tex);
            tMpb.SetTexture("TestCullOVF_Tex", TestCullOVF_Tex);
            tMpb.SetTexture("hMap", hMap);
        }

        {
            //color = new float4(1.0f, 0.0f, 0.0f, 1.0f);
            //maxTessFactor = 8;
            mode = 0; //0 Pvf-tileBox //1 Ovf-tileBox
            vfIdx = 0; //mode == 0 -> vfIdx = 0    //mode == 1 -> vfIdx = 0, 1, 2, 3    

            tMpb.SetVector("color", color);
            tMpb.SetFloat("maxTessFactor", maxTessFactor);
            tMpb.SetInt("mode", mode);
            tMpb.SetInt("vfIdx", vfIdx);
        }

        {
            CV_csm = csm_action.CV;
            tMpb.SetMatrixArray("CV_csm", CV_csm);
        }

        {
            CV_csm_depth = csm_action.CV_depth;
            tMpb.SetMatrixArray("CV_csm_depth", CV_csm_depth);
        }

        {
            tMpb.SetVector("tileSize", new float4(tileSize, 0.0f));
            tMpb.SetVector("terrainSize", new float4(terrainSize, 0.0f));
            tMpb.SetVector("tileCount", new float4(tileCount, 0.0f));
        }

        {
            //testTex.wrapMode = TextureWrapMode.Repeat;
            tMpb.SetTexture("bakedTex", bakedTex);
            tMpb.SetTexture("bakedNormalTex", bakedNormalTex);
        }

        {
            TerrainLayer[] terrainLayers = tData.terrainLayers;
            if (terrainLayers != null)
            {
                Vector2[] layerSize = new Vector2[terrainLayers.Length];
                for (int i = 0; i < terrainLayers.Length; i++)
                {
                    tMpb.SetTexture("diffuseTex" + i.ToString(), terrainLayers[i].diffuseTexture);
                    layerSize[i] = terrainLayers[i].tileSize;

                    tMpb.SetTexture("normalMapTex" + i.ToString(), terrainLayers[i].normalMapTexture);                                 
                }

                {
                    layerSizeBuffer = new COBuffer<Vector2>(terrainLayers.Length);
                    layerSizeBuffer.data = layerSize;
                    layerSizeBuffer.Write();

                    tMpb.SetBuffer("layerSizeBuffer", layerSizeBuffer.value);
                }
            }

            Texture2D[] alphaMap = tData.alphamapTextures;
            if (tData.alphamapTextures != null)
            {
                for (int i = 0; i < tData.alphamapTextureCount; i++)
                {
                    tMpb.SetTexture("alphamap" + i.ToString(), tData.alphamapTextures[i]);
                }
            }

            Texture holeTex = tData.holesTexture;
            {
                tMpb.SetTexture("holeTex", holeTex);
            }

            {
                tMpb.SetTexture("waterTex", waterTex);
            }
        }

        //CSM
        {

            tMpb.SetTexture("csmTexArray", csm_action.renTex);
        }

        bool bArray = true;
        {
            if (bArray)
            {
                tMpb.SetInt("bArray", 1);
            }
            else
            {
                tMpb.SetInt("bArray", 0);
            }

            tMpb.SetInt("csmWidth", csm_action.csmW);
            tMpb.SetInt("csmHeight", csm_action.csmH);
            tMpb.SetFloat("specularPow", 2.0f);
        }

        //QuadTree
        {
            tMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_qt_Tex);
            tMpb.SetTexture("TestCullOVF_Tex", TestCullOVF_qt_Tex);

            tMpb.SetFloat("pvfCount", pvfCount);
            tMpb.SetFloat("ovfCount", ovfCount);
            tMpb.SetInt("qtCount", qtCount);

            tMpb.SetInt("fqtIdx", qtCount - 1);
        }


        ChangeLayerTexMode();
        ChangeNormalMapMode();
    }

    int pass_csm_vf_debug;
    int pass_cam_vf_debug;

    int debugType = 0;

    Matrix4x4[] W_csm;

    Texture2D Box_Vtx_Tex;

    GraphicsBuffer BoxWireIdx_Buffer;

    int boxTileCount;

    void InitRendering_debug()
    {
        {
            pass_csm_vf_debug = tMte.FindPass("Terrain_CSM_Debug");
            pass_cam_vf_debug = tMte.FindPass("Terrain_CAM_Debug");
        }

        {
            W_csm = new Matrix4x4[csm_action.csmCount];
            W_csm = csm_action.W_Cull;
        }

        {
            Box_Vtx_Tex = new Texture2D(1, BoxWire.vtxCount, TextureFormat.RGBAFloat, false);
            for (int i = 0; i < BoxWire.vtxCount; i++)
            {
                Vector4 pos = new float4(BoxWire.sPos[i], 1.0f);
                Box_Vtx_Tex.SetPixel(0, i, new Color(pos.x, pos.y, pos.z, pos.w));
            }
            Box_Vtx_Tex.Apply();

            BoxWireIdx_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, BoxWire.idxCount, sizeof(int));
            BoxWireIdx_Buffer.SetData(BoxWire.sIndices);

            boxTileCount = (int)(tileCount.x * tileCount.z);
        }

        {
            pMpb.SetVector("tileCount", new float4(tileCount, 0.0f));
            oMpb.SetVector("tileCount", new float4(tileCount, 0.0f));
            bMpb.SetVector("tileCount", new float4(tileCount, 0.0f));

            pMpb.SetInt("type", 0);
            oMpb.SetInt("type", 1);
            bMpb.SetInt("type", 2);

            pMpb.SetTexture("Pvf_Vtx_Tex", pPosWire_Tex);
            oMpb.SetTexture("Box_Vtx_Tex", Box_Vtx_Tex);
            bMpb.SetTexture("Box_Vtx_Tex", Box_Vtx_Tex);

            pMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_Tex);
            oMpb.SetTexture("TestCullOVF_Tex", TestCullOVF_Tex);
            bMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_Tex);
            bMpb.SetTexture("TestCullOVF_Tex", TestCullOVF_Tex);

            //bMpb.SetTexture("TileW_Tex", TileW_Tex);
        }

        //QuadTree
        {
            pMpb.SetFloat("pvfCount", pvfCount);
            oMpb.SetFloat("pvfCount", pvfCount);
            bMpb.SetFloat("pvfCount", pvfCount);

            pMpb.SetFloat("ovfCount", ovfCount);
            oMpb.SetFloat("ovfCount", ovfCount);
            bMpb.SetFloat("ovfCount", ovfCount);

            pMpb.SetInt("qtCount", qtCount);
            oMpb.SetInt("qtCount", qtCount);
            bMpb.SetInt("qtCount", qtCount);

            pMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_qt_Tex);
            oMpb.SetTexture("TestCullOVF_Tex", TestCullOVF_qt_Tex);
            bMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_qt_Tex);
            bMpb.SetTexture("TestCullOVF_Tex", TestCullOVF_qt_Tex);

            bMpb.SetTexture("TileW_Tex", TileW_qt_Tex);
        }

    }

    void Compute(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        WriteToResource(context, cmd);
        DispatchCompute(context, cmd);
        //ReadFromResource(context, cmd);
    }

    void WriteToResource(ScriptableRenderContext context, CommandBuffer cmd)
    {
        {
            pos_terrain = new float4((float3)transform.position, 0.0f);
            rot_terrain = ((quaternion)(transform.rotation)).value;

            cshader.SetVector("pos_terrain", pos_terrain);
            cshader.SetVector("rot_terrain", rot_terrain);
        }

        //{
        //    fisData[0] = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
        //    var na = fis_Buffer.BeginWrite<float4>(0, pvfCount);
        //    for (int i = 0; i < pvfCount; i++)
        //    {
        //        na[i] = fisData[i];
        //    }
        //    fis_Buffer.EndWrite<float4>(pvfCount);
        //}

        {
            fisData[0] = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
            var data = fis_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                data[i] = fisData[i];
            }
            fis_Buffer.Write();
        }

        //{
        //    float4x4 M = float4x4.zero;
        //    Transform camTr = mainCam.transform;
        //    M.c0 = new float4(camTr.position, 0.0f);
        //    M.c1 = ((quaternion)camTr.rotation).value;
        //    M.c2 = new float4(camTr.localScale, 0.0f);
        //
        //    var na = pvfM_Buffer.BeginWrite<float4x4>(0, pvfCount);
        //    for (int i = 0; i < pvfCount; i++)
        //    {
        //        na[i] = M;
        //    }
        //    pvfM_Buffer.EndWrite<float4x4>(pvfCount);
        //
        //}

        {
            float4x4 M = float4x4.zero;
            Transform camTr = mainCam.transform;
            M.c0 = new float4(camTr.position, 0.0f);
            M.c1 = ((quaternion)camTr.rotation).value;
            M.c2 = new float4(camTr.localScale, 0.0f);

            var data = pvfM_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                data[i] = M;
            }
            pvfM_Buffer.Write();

        }

        //{
        //    var na = ovfM_Buffer.BeginWrite<float4x4>(0, ovfCount);
        //    for (int i = 0; i < ovfCount; i++)
        //    {
        //        na[i] = csm_action.E_Cull[i];
        //    }
        //    ovfM_Buffer.EndWrite<float4x4>(ovfCount);
        //}

        {
            var data = ovfM_Buffer.data;
            for (int i = 0; i < ovfCount; i++)
            {
                data[i] = csm_action.E_Cull[i];
            }
            ovfM_Buffer.Write();
        }

    }

    public bool bNonBake = true;

    void DispatchCompute(ScriptableRenderContext context, CommandBuffer cmd)
    {
        if (bNonBake)
        {
            {
                cmd.DispatchCompute(cshader, ki_lod, (int)tileCount.x, (int)tileCount.y, (int)tileCount.z);
            }

            {
                cshader.SetInt("qtCount", qtCount);
            }

            //QuadTree_Bake
            for (int i = qtCount - 1; i > 0; i--)
            {
                int _count = qtCounts[i - 1];
                cmd.SetComputeIntParam(cshader, "qtIdx", i - 1);
                cmd.DispatchCompute(cshader, ki_quadtree, _count, 1, _count);
            }

            bNonBake = false;
        }

        //QuadTree_Cull
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex, pvfCount, 1, 1);

            for (int i = 0; i < qtCount; i++)
            {
                int _count = qtCounts[i];
                cmd.SetComputeIntParam(cshader, "qtIdx", i);
                cmd.DispatchCompute(cshader, ki_pvf_cull, _count, pvfCount, _count);
                cmd.DispatchCompute(cshader, ki_ovf_cull, _count, ovfCount, _count);
            }
        }


        if (bDebug)
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex_wire, pvfCount, 1, 1);
        }
    }

    void ReadFromResource(ScriptableRenderContext context, CommandBuffer cmd)
    {
        Action<float4x4[], AsyncGPUReadbackRequest> GetMatFromTex
            = (mat, read) =>
            {
                for (int i = 0; i < read.depth; i++)
                {
                    var na = read.GetData<float4>(i);

                    unsafe
                    {
                        fixed (float4x4* ptrMat = &mat[i * read.width])
                        {
                            float4* ptrVec = (float4*)&ptrMat[0];

                            for (int j = 0; j < read.height; j++)
                            {
                                for (int k = 0; k < read.width; k++)
                                {
                                    ptrVec[k * read.height + j] = na[j * read.width + k];
                                }
                            }
                        }
                    }
                }
            };

        Action<float4x4[], AsyncGPUReadbackRequest> GetMatFromTex1
            = (mat, read) =>
            {
                for (int i = 0; i < read.depth; i++)
                {
                    var na = read.GetData<float4>(i);

                    int _count = read.height / 4;
                    for (int m = 0; m < _count; m++)
                    {
                        unsafe
                        {
                            fixed (float4x4* ptrMat = &mat[i * read.width + m * read.depth * read.width])
                            {
                                float4* ptrVec = (float4*)&ptrMat[0];

                                for (int j = 0; j < 4; j++)
                                {
                                    for (int k = 0; k < read.width; k++)
                                    {
                                        ptrVec[k * 4 + j] = na[m * 4 * read.width + j * read.width + k];
                                    }
                                }
                            }
                        }
                    }
                }
            };

        //NormalHeight
        {
            cmd.RequestAsyncReadback(normalHeight_Tex,
                       (read) =>
                       {
                           for (int i = 0; i < read.depth; i++)
                           {
                               var na = read.GetData<float4>(i);
                               for (int j = 0; j < read.height; j++)
                               {
                                   for (int k = 0; k < read.width; k++)
                                   {
                                       int id0 = j * read.width + k;
                                       int id1 = i * read.height * read.width + id0;
                                       nhData[id1] = na[id0];
                                   }
                               }
                           }
                       });

        }

        //Lod
        {
            cmd.RequestAsyncReadback(LodData_Tex,
                (read) =>
                {
                    for (int i = 0; i < read.depth; i++)
                    {
                        var na = read.GetData<float4>(i);
                        for (int j = 0; j < read.height; j++)
                        {
                            for (int k = 0; k < read.width; k++)
                            {
                                LodData[i * read.height * read.width + j * read.width + k] = na[j * read.width + k];
                            }
                        }
                    }
                });

        }

        //QuadTree
        {
            cmd.RequestAsyncReadback(TileData_qt_Tex,
                (read) =>
                {
                    GetMatFromTex1(TileData_qt, read);
                });

            cmd.RequestAsyncReadback(TileW_qt_Tex,
                (read) =>
                {
                    GetMatFromTex1(TileW_qt, read);
                });

            cmd.RequestAsyncReadback(TileWn_qt_Tex,
                (read) =>
                {
                    GetMatFromTex1(TileWn_qt, read);
                });
        }

        //Pvf_vertex && Pvf_vertex_wire
        {
            pCenter_Buffer.Read(cmd);

            cmd.RequestAsyncReadback(pPlane_Tex,
                (read) =>
                {
                    var na = read.GetData<float4>(0);
                    for (int i = 0; i < read.width; i++)
                    {
                        for (int j = 0; j < read.height; j++)
                        {
                            pPlaneData[i * read.width + j] = na[i * read.width + j];
                        }
                    }
                });

            cmd.RequestAsyncReadback(pPos_Tex,
                (read) =>
                {
                    var na = read.GetData<float4>(0);
                    for (int i = 0; i < read.width; i++)
                    {
                        for (int j = 0; j < read.height; j++)
                        {
                            pPosData[i * read.width + j] = na[i * read.width + j];
                        }
                    }
                });

            cmd.RequestAsyncReadback(pNormal_Tex,
                (read) =>
                {
                    var na = read.GetData<float4>(0);
                    for (int i = 0; i < read.width; i++)
                    {
                        for (int j = 0; j < read.height; j++)
                        {
                            pNormalData[i * read.width + j] = na[i * read.width + j];
                        }
                    }
                });

            //wire
            cmd.RequestAsyncReadback(pPosWire_Tex,
                (read) =>
                {
                    var na = read.GetData<float4>(0);
                    for (int i = 0; i < read.width; i++)
                    {
                        for (int j = 0; j < read.height; j++)
                        {
                            pPosWireData[i * read.width + j] = na[i * read.width + j];
                        }
                    }
                });

        }

        //Cull_QuadTree
        {
            cmd.RequestAsyncReadback(TestCullPVF_qt_Tex,
                (read) =>
                {
                    for (int i = 0; i < read.depth; i++)
                    {
                        var na = read.GetData<float>(i);

                        for (int j = 0; j < read.height; j++)
                        {
                            for (int k = 0; k < read.width; k++)
                            {
                                TestCullPVF_qt[j * read.depth * read.width + i * read.width + k] = na[j * read.width + k];
                            }
                        }
                    }
                });

            cmd.RequestAsyncReadback(TestCullOVF_qt_Tex,
               (read) =>
               {
                   for (int i = 0; i < read.depth; i++)
                   {
                       var na = read.GetData<float>(i);

                       for (int j = 0; j < read.height; j++)
                       {
                           for (int k = 0; k < read.width; k++)
                           {
                               TestCullOVF_qt[j * read.depth * read.width + i * read.width + k] = na[j * read.width + k];
                           }
                       }
                   }
               });
        }
    }

    void RenderCSM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        {
            //float4x4 S = RenderUtil.GetSfromN_toTex(csm_action.csmRect);
            float4x4 S = RenderUtil.GetSfromN(csm_action.csmRect);

            tMpb.SetMatrixArray("CV_csm", CV_csm);
            tMpb.SetMatrixArray("CV_csm_depth", CV_csm_depth);
            tMpb.SetMatrix("S", S);
            //tMpb.SetVector("dirW_mainCam", dirW_mainCam);
            tMpb.SetVector("dirW_cam", dirW_light);
            tMpb.SetVector("posW_mainCam", posW_mainCam);

            tMpb.SetInt("mode", 1);
            tMpb.SetInt("vfIdx", 3);
        }

        cmd.DrawMeshInstancedProcedural(tMesh, 0, tMte, pass_csm, 1, tMpb);
    }

    void RenderCAM(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
        {
            tMpb.SetMatrix("CV_view", math.mul(RenderUtil.GetCfromV(cam, false), perCam.V));
            tMpb.SetMatrixArray("TCV_light", csm_action.TCV_depth);
            tMpb.SetFloatArray("endZ", csm_action.endZ);
        }

        {
            tMpb.SetMatrix("CV", perCam.CV);
            tMpb.SetMatrix("S", perCam.S);
            //tMpb.SetVector("dirW_mainCam", dirW_mainCam);

            if (mode == 0)
            {
                tMpb.SetVector("dirW_cam", dirW_mainCam);
            }
            else
            {
                tMpb.SetVector("dirW_cam", dirW_light);
            }

            tMpb.SetVector("posW_mainCam", posW_mainCam);

            tMpb.SetInt("mode", mode);
            tMpb.SetInt("vfIdx", vfIdx);
        }

        cmd.DrawMeshInstancedProcedural(tMesh, 0, tMte, pass_cam, 1, tMpb);

    }

    void RenderCSM_Debug(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {

#if UNITY_EDITOR
        if (bDebug)
        {
            {
                bMpb.SetMatrixArray("CV_csm", CV_csm);
            }

            //Box
            {
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, bMte, pass_csm_vf_debug, MeshTopology.Lines, BoxWire.idxCount, boxTileCount, bMpb);
            }
        }
#endif

    }

    void RenderCAM_Debug(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {

#if UNITY_EDITOR
        if (bDebug)
        {
            {
                pMpb.SetMatrix("CV", perCam.CV);
                oMpb.SetMatrix("CV", perCam.CV);
                bMpb.SetMatrix("CV", perCam.CV);

                pMpb.SetInt("mode", mode);
                oMpb.SetInt("mode", mode);
                bMpb.SetInt("mode", mode);

                pMpb.SetInt("vfIdx", vfIdx);
                oMpb.SetInt("vfIdx", vfIdx);
                bMpb.SetInt("vfIdx", vfIdx);
            }

            //Pvf
            {
                pMpb.SetMatrix("W", RenderUtil.GetWfromL(mainCam.transform));
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, pMte, pass_cam_vf_debug, MeshTopology.Lines, BoxWire.idxCount, 1, pMpb);
            }

            //Ovf
            {
                oMpb.SetMatrixArray("W_csm", W_csm);
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, oMte, pass_cam_vf_debug, MeshTopology.Lines, BoxWire.idxCount, 1, oMpb);
            }

            //Box
            {
                for (int i = 0; i < qtCount; i++)
                {
                    int _count = qtCounts[i];
                    bMpb.SetInt("qtSize", _count);
                    bMpb.SetInt("qtIdx", i);
                    bMpb.SetTexture("TileW_Tex", TileW_qt_Tex);

                    _count *= _count;
                    cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, bMte, pass_cam_vf_debug, MeshTopology.Lines, BoxWire.idxCount, _count, bMpb);
                }
            }
        }
#endif

    }

    void RenderCAM_Line(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
#if UNITY_EDITOR
        if (bNormalLine)
        {
            {
                tMpb.SetMatrix("CV", perCam.CV);
                tMpb.SetMatrix("S", perCam.S);
                //tMpb.SetVector("dirW_mainCam", dirW_mainCam);

                if (mode == 0)
                {
                    tMpb.SetVector("dirW_cam", dirW_mainCam);
                }
                else
                {
                    tMpb.SetVector("dirW_cam", dirW_light);
                }

                tMpb.SetVector("posW_mainCam", posW_mainCam);

                tMpb.SetInt("mode", mode);
                tMpb.SetInt("vfIdx", vfIdx);
            }

            cmd.DrawMeshInstancedProcedural(tMesh, 0, tMte, pass_cam_line, 1, tMpb);
        }
#endif
    }


    void ReleaseResource()
    {
        BufferBase<float4>.Release(fis_Buffer);
        BufferBase<float4>.Release(pCenter_Buffer);
        BufferBase<int>.Release(hhIndex_Buffer);
        BufferBase<float4x4>.Release(pvfM_Buffer);
        BufferBase<float4x4>.Release(ovfM_Buffer);        
    }

    void ReleaseTexture(RenderTexture tex)
    {
        if (tex != null)
        {
            tex.Release();            
            tex = null;
        }
    }


    //Test
    void ReadFromResource1(ScriptableRenderContext context)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        Action<float4x4[], AsyncGPUReadbackRequest> GetMatFromTex
            = (mat, read) =>
            {
                for (int i = 0; i < read.depth; i++)
                {
                    var na = read.GetData<float4>(i);

                    unsafe
                    {
                        fixed (float4x4* ptrMat = &mat[i * read.width])
                        {
                            float4* ptrVec = (float4*)&ptrMat[0];

                            for (int j = 0; j < read.height; j++)
                            {
                                for (int k = 0; k < read.width; k++)
                                {
                                    ptrVec[k * read.height + j] = na[j * read.width + k];
                                }
                            }
                        }
                    }
                }
            };

        Action<float4x4[], AsyncGPUReadbackRequest> GetMatFromTex1
            = (mat, read) =>
            {
                for (int i = 0; i < read.depth; i++)
                {
                    var na = read.GetData<float4>(i);

                    int _count = read.height / 4;
                    for (int m = 0; m < _count; m++)
                    {
                        unsafe
                        {
                            fixed (float4x4* ptrMat = &mat[i * read.width + m * read.depth * read.width])
                            {
                                float4* ptrVec = (float4*)&ptrMat[0];

                                for (int j = 0; j < 4; j++)
                                {
                                    for (int k = 0; k < read.width; k++)
                                    {
                                        ptrVec[k * 4 + j] = na[m * 4 * read.width + j * read.width + k];
                                    }
                                }
                            }
                        }
                    }
                }
            };

        //QuadTree1
        {
            cmd.RequestAsyncReadback(TileData_qt_Tex,
                (read) =>
                {
                    GetMatFromTex1(TileData_qt, read);
                });

            cmd.RequestAsyncReadback(TileW_qt_Tex,
                (read) =>
                {
                    GetMatFromTex1(TileW_qt, read);
                });

            cmd.RequestAsyncReadback(TileWn_qt_Tex,
                (read) =>
                {
                    GetMatFromTex1(TileWn_qt, read);
                });
        }



        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void RenderCAM_Debug0(ScriptableRenderContext context, Camera cam, RenderGOM.PerCamera perCam)
    {
        if (bDebug)
        {
            {
                pMpb.SetMatrix("CV", perCam.CV);
                oMpb.SetMatrix("CV", perCam.CV);
                bMpb.SetMatrix("CV", perCam.CV);

                pMpb.SetInt("mode", mode);
                oMpb.SetInt("mode", mode);
                bMpb.SetInt("mode", mode);

                pMpb.SetInt("vfIdx", vfIdx);
                oMpb.SetInt("vfIdx", vfIdx);
                bMpb.SetInt("vfIdx", vfIdx);
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //Pvf
            {
                pMpb.SetMatrix("W", RenderUtil.GetWfromL(mainCam.transform));
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, pMte, pass_cam_vf_debug, MeshTopology.Lines, BoxWire.idxCount, 1, pMpb);
            }

            //Ovf
            {
                oMpb.SetMatrixArray("W_csm", W_csm);
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, oMte, pass_cam_vf_debug, MeshTopology.Lines, BoxWire.idxCount, 1, oMpb);
            }

            //Box
            {
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, bMte, pass_cam_vf_debug, MeshTopology.Lines, BoxWire.idxCount, boxTileCount, bMpb);
            }


            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    float4x4[] TestData;
    RenderTexture Test_Tex;
    int testW = 8;
    int testH = 8;
    int testD = 8;

    void InitTestTex()
    {
        int w = testW;
        int h = testH;
        int d = testD;

        {
            TestData = new float4x4[w * h * d];
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = w;
                rtd.height = 4 * h;
                rtd.volumeDepth = d;
            }

            Test_Tex = new RenderTexture(rtd);
        }
    }
}
