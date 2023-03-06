using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

public class SelectManager : MonoBehaviour
{  
    private void Awake()
    {
          
    }

    int unitCount;

    public CullManager cullMan;

    public void Init()
    {
        unitCount = GameManager.unitCount;

        if(unitCount > 0)
        {
            {
                SetSelectAll(false);
            }

            {
                rectIn = new RectIn();
                rectIn.Init(cshader_rectIn);
            }

            //StartCoroutine(SelectAction());
            //StartCoroutine(MoveAction());
        }
       
    }

    public void SetSelectAll(bool value)
    {
        for (int i = 0; i < GameManager.unitCount; i++)
        {
            GameManager.selectData[i] = value ? 1 : 0;
        }
    }

    public void Begin()
    {
        StartCoroutine(SelectAction());
        StartCoroutine(MoveAction());
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (rectIn != null) rectIn.ReleaseResource();
    }

    public RectTransform rtTrSelect;

    public static KeyCode key_muti_select = KeyCode.E;
    public static KeyCode key_hold = KeyCode.Q;
    

    public static bool isMove = false;

    
    public ComputeShader cshader_rectIn;
    RectIn rectIn;

    IEnumerator SelectAction()
    {
        bool down = false;
        float3 rp0 = float3.zero;
        float3 rp1 = float3.zero;

        LayerMask lmask = LayerMask.GetMask("Unit0", "Unit1");

        {
            rtTrSelect.anchorMin = new Vector2(0.0f, 0.0f);
            rtTrSelect.anchorMax = new Vector2(0.0f, 0.0f);
            rtTrSelect.pivot = new Vector2(0.0f, 0.0f);
        }

        while (true)
        {
            //if (GamePlay.isResume)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    {
                        //Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                        Ray ray = RenderUtil.GetRay_WfromS(Input.mousePosition, Camera.main);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, 500, lmask))
                        {
                            UnitActor hActor = hit.transform.GetComponent<UnitActor>();

                            if (hActor != null)
                            {
                                if (hActor.isActive)
                                {
                                    if (Input.GetKey(key_muti_select))
                                    {
                                        if (hActor.isSelected)
                                        { hActor.isSelected = false; }
                                        else
                                        { hActor.isSelected = true; }
                                    }
                                    else
                                    {
                                        SetSelectAll(false);
                                        hActor.isSelected = true;
                                    }

                                    if(Input.GetKey(KeyCode.R))
                                    {
                                        cullMan.Read_PvfCullData();
                                        var unitMan = hActor.unitMan;
                                        var actors = unitMan.unitActors;                                      
                                        int unitCount = unitMan.count;

                                        for(int i = 0; i < unitCount; i++)
                                        {
                                            var actor = actors[i];
                                            if(!(actor.isCull) && actor.isActive)
                                            {
                                                actor.isSelected = true;
                                            }
                                            else
                                            {
                                                actor.isSelected = false;
                                            }
                                        }
                                        
                                    }
                                }
                            }
                        }
                        else
                        {
                            SetSelectAll(false);
                        }
                    }


                    {
                        down = true;
                        rp0 = Input.mousePosition;
                    }
                }


                if (down)
                {
                    rp1 = Input.mousePosition;
                    if (math.distance(rp0, rp1) > 1.0f)
                    {
                        Rect rt = new Rect(math.min(rp0, rp1).xy, math.abs(rp1 - rp0).xy);
                        //rt = new Rect(new Vector2(100.0f, 100.0f), new Vector2(100.0f, 100.0f));
                        rtTrSelect.offsetMin = rt.min;
                        rtTrSelect.offsetMax = rt.max;

                        rectIn.rect = rt;
                        rectIn.Test();
                    }

                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                down = false;
                rtTrSelect.offsetMin = float2.zero;
                rtTrSelect.offsetMax = float2.zero;
            }
            

            yield return null;
        }
    }

    IEnumerator SelectAction0()
    {
        bool down = false;
        float3 rp0 = float3.zero;
        float3 rp1 = float3.zero;

        LayerMask lmask = LayerMask.GetMask("Unit0", "Unit1");

        {
            rtTrSelect.anchorMin = new Vector2(0.0f, 0.0f);
            rtTrSelect.anchorMax = new Vector2(0.0f, 0.0f);
            rtTrSelect.pivot = new Vector2(0.0f, 0.0f);
        }

        while (true)
        {
            //if (GamePlay.isResume)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    {
                        //Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                        Ray ray = RenderUtil.GetRay_WfromS(Input.mousePosition, Camera.main);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, 100, lmask))
                        {
                            UnitActor hActor = hit.transform.GetComponent<UnitActor>();

                            if (hActor != null)
                            {
                                if (hActor.isActive)
                                {
                                    if (Input.GetKey(key_muti_select))
                                    {
                                        if (hActor.isSelected)
                                        { hActor.isSelected = false; }
                                        else
                                        { hActor.isSelected = true; }
                                    }
                                    else
                                    {
                                        SetSelectAll(false);
                                        hActor.isSelected = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            SetSelectAll(false);
                        }
                    }


                    {
                        down = true;
                        rp0 = Input.mousePosition;
                    }
                }


                if (down)
                {
                    rp1 = Input.mousePosition;
                    if (math.distance(rp0, rp1) > 1.0f)
                    {
                        Rect rt = new Rect(math.min(rp0, rp1).xy, math.abs(rp1 - rp0).xy);
                        //rt = new Rect(new Vector2(100.0f, 100.0f), new Vector2(100.0f, 100.0f));
                        rtTrSelect.offsetMin = rt.min;
                        rtTrSelect.offsetMax = rt.max;

                        rectIn.rect = rt;
                        rectIn.Test();
                    }

                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                down = false;
                rtTrSelect.offsetMin = float2.zero;
                rtTrSelect.offsetMax = float2.zero;
            }


            yield return null;
        }
    }

    static public float3 movePos;

    IEnumerator MoveAction()
    {
        KeyCode key_orbit = CamAction.key_orbit;
        KeyCode key_spin = CamAction.key_spin;

        while (true)
        {
            //if (GamePlay.isResume)
            {
                if (Input.GetMouseButtonDown(1) && !Input.GetKey(key_orbit) && !Input.GetKey(key_spin))
                {
                    //if (mainCam != null)
                    {
                        //Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                        Ray ray = RenderUtil.GetRay_WfromS(Input.mousePosition, Camera.main);
                        RaycastHit hit;

                        //if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("MovePlane")))
                        if (Physics.Raycast(ray, out hit, 500, LayerMask.GetMask("Terrain", "Unit0", "Unit1")))
                        {
                            movePos = hit.point;
                            Transform htr = hit.transform;

                            for (int i = 0; i < GameManager.unitCount; i++)
                            {
                                UnitActor uactor = GameManager.unitActors[i];

                                //Debug.Log("MoveAction()");

                                if (uactor.isActive)
                                {
                                    if (uactor.isSelected)
                                    {
                                        UnitActor hactor = htr.GetComponent<UnitActor>();
                                
                                        if (hactor != null)
                                        {
                                            if (uactor.pNum != hactor.pNum)
                                            {
                                                uactor.SetAttackTr(htr, 0);
                                                uactor.positionTr = null;
                                                //Debug.Log("attack pid");
                                            }
                                            else
                                            {
                                                uactor.ClearAttackTr();
                                                uactor.positionTr = htr;
                                                uactor.targetPos = movePos;
                                            }
                                        }
                                        else
                                        {
                                            uactor.ClearAttackTr();
                                            uactor.positionTr = htr;
                                            uactor.targetPos = movePos;
                                
                                            //inPlane = true;
                                            //StartCoroutine(torusPos.ShowTorus(hitPos));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            yield return null;
        }
    }

    public IEnumerator MoveAction0()
    {
        LayerMask lmask = LayerMask.GetMask("Terrain", "Unit0", "Unit1");

        while (true)
        {
            if (Input.GetMouseButtonDown(1) && !Input.GetKey(CamAction.key_orbit) && !Input.GetKey(CamAction.key_spin))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 500.0f, lmask))
                {
                    for (int i = 0; i < GameManager.unitCount; i++)
                    {
                        var actor = GameManager.unitActors[i];
                        if (actor != null)
                        {
                            if (actor.isSelected)
                            {
                                int id = actor.iid;
                                actor.targetPos = hit.point;
                            }
                        }
                    }

                    //for (int i = 0; i < GameManager.unitCount; i++)
                    //{
                    //    TargetAction.targetPos[i] = hit.point;                        
                    //
                    //    isMove = true;
                    //}
                }
            }

            yield return null;
        }
    }

    class RectIn
    {
        public void Test()
        {
            //WriteToNa();
            ExecuteJob();
            //ReadFromNa();

            WriteToResource();
            DispatchCompute();
            ReadFromResource();
        }

        ComputeShader cshader;
        int ki_RenctIn;
        TransformJob job;

        int unitCount;
        //int maxUnitCount;

        //SelectMode sMode;
        int mIndex;
        int mCount;
        int idxStart;
        int idxEnd;
        int dpCount;

        //
        Transform[] unitTrs;
        Camera mainCam;
        Transform mainCamTr;

        int[] selectData;

        //
        public Rect rect;
        float4x4 S;
        float4x4 CV;

        TransformAccessArray traa;
        NativeArray<float3> naPos;       

        ROBuffer<float4x4> SCV_Buffer;
        ROBuffer<float3> pos_Buffer;
        RWBuffer<int> inRect_Buffer;

        public void Init(ComputeShader cshader)
        {
            this.cshader = cshader;
            ki_RenctIn = cshader.FindKernel("CS_RectIn");
            job = new TransformJob();

            {
                unitCount = GameManager.unitCount;
                //maxUnitCount = GameManager.maxUnitCount;

                //sMode = SelectManager.selectMode;
                //mIndex = SelectManager.mIndex;
                //mCount = SelectManager.mCount;

                idxStart = 0;
                idxEnd = GameManager.unitCount;

                dpCount = unitCount % 64 == 0 ? unitCount / 64 : unitCount / 64 + 1;
            }

            {
                unitTrs = GameManager.unitTrs;
                mainCam = Camera.main;
                mainCamTr = mainCam.transform;

                selectData = GameManager.selectData;
            }

            InitResouce();
        }

        void InitResouce()
        {
            {
                traa = new TransformAccessArray(unitTrs);
                naPos = new NativeArray<float3>(unitCount, Allocator.Persistent);              

                SCV_Buffer =    new ROBuffer<float4x4>(2);
                pos_Buffer =    new ROBuffer<float3>(unitCount);
                inRect_Buffer = new RWBuffer<int>(unitCount);
            }

            {
                job.naPos = naPos;
            }

            {
                cshader.SetBuffer(ki_RenctIn, "SCV_Buffer", SCV_Buffer.value);
                cshader.SetBuffer(ki_RenctIn, "pos_Buffer", pos_Buffer.value);
                cshader.SetBuffer(ki_RenctIn, "active_Buffer", GameManager.active_Buffer.value);
                cshader.SetBuffer(ki_RenctIn, "inRect_Buffer", inRect_Buffer.value);

                cshader.SetInt("idxStart", idxStart);
                cshader.SetInt("idxEnd", idxEnd);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteToNa()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ExecuteJob()
        {
            job.Schedule<TransformJob>(traa).Complete();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReadFromNa()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteToResource()
        {
            RenderUtil.GetMat_SfromW(mainCamTr, mainCam, out S, out CV);

            {
                var data = SCV_Buffer.data;
                data[0] = S;
                data[1] = CV;
                SCV_Buffer.Write();
            }

            {
                var data = pos_Buffer.data;
                for (int i = 0; i < unitCount; i++)
                {
                    data[i] = naPos[i];
                }
                pos_Buffer.Write();
            }

            {
                cshader.SetVector("rect", new float4(rect.x, rect.y, rect.width, rect.height));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DispatchCompute()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            cmd.DispatchCompute(cshader, ki_RenctIn, dpCount, 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReadFromResource()
        {
            {               
                inRect_Buffer.Read();
                var data = inRect_Buffer.data;
                for (int i = 0; i < unitCount; i++)
                {
                    selectData[i] = data[i];
                }
            }
        }

        public void ReleaseResource()
        {
            DisposeTraa(traa);
            DisposeNa<float3>(naPos);

            BufferBase<float4x4>.Release(SCV_Buffer);
            BufferBase<float3>.Release(pos_Buffer);
            BufferBase<int>.Release(inRect_Buffer);
        }
      
        void DisposeNa<T>(NativeArray<T> na) where T : struct
        {
            if (na.IsCreated) na.Dispose();
        }

        void DisposeTraa(TransformAccessArray traa)
        {
            if (traa.isCreated) traa.Dispose();
        }

        [BurstCompile]
        struct TransformJob : IJobParallelForTransform
        {
            [WriteOnly]
            public NativeArray<float3> naPos;

            public void Execute(int i, TransformAccess tra)
            {
                naPos[i] = tra.position;
            }
        }
    }
}


