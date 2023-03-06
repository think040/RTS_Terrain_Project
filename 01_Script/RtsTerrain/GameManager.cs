using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public CSM_Action csm_action;
    public TerrainManager terrain;
    public UnitManager[] _unitMan;
    public ArrowManager _arrowMan;
    public TargetManager _targetMan;
    public SelectManager _selectMan;
    public TorusManager _torusMan;
    public HpbarManager _hpbarMan;
    public CullManager _cullMan;

    public int[] _unitCounts;

    public int[] _playerNum;
    public float[] _viewRadius;
    public float[] _attackRadius;
    public Color[] _playerColor;
    bool[] _activeData;
    int[] _selectData;
    public float[] _maxHp;
    public float[] _hitHp;

    public static UnitManager[] unitMan
    {
        get; set;
    }

    public static ArrowManager arrowMan
    {
        get; set;
    }

    public static TargetManager targetMan
    {
        get; set;
    }

    public static SelectManager selectMan
    {
        get; set;
    }

    public static TorusManager torusMan
    {
        get; set;
    }

    public static HpbarManager hpbarMan
    {
        get; set;
    }

    public static CullManager cullMan
    {
        get; set;
    }

    public static int[] unitCounts
    {
        get; private set;
    }
    public static int unitCount
    {
        get; set;
    }

    public static bool[] activeData
    {
        get; set;
    }

    public static int[] selectData
    {
        get; private set;
    }

    public static int[] stateData
    {
        get; set;
    }

    public static float4[] terrainArea
    {
        get; private set;
    }

    public static float3[] targetPos
    {
        get; private set;
    }

    public static float3[] refTargetPos
    {
        get; private set;
    }

    public static float4[] minDist
    {
        get; private set;
    }


    public static int2[] baseCount
    {
        get; set;
    }

    public static UnitActor[] unitActors
    {
        get; set;
    }

    public static Transform[] unitTrs
    {
        get; set;
    }

    public static int[] playerNum
    {
        get; set;
    }

    public static Color[] playerColor
    {
        get; private set;
    }

    public static float[] viewRadius
    {
        get; set;
    }

    public static float[] attackRadius
    {
        get; set;
    }

    public static float[] viewRadiusDef
    {
        get; set;
    }

    public static float[] attackRadiusDef
    {
        get; set;
    }

    public static float[] maxHp
    {
        get; set;
    }

    public static float[] hp
    {
        get; private set;
    }

    public static float[] hitHp
    {
        get; set;
    }

    public static float[] refHp
    {
        get; set;
    }

    public static ROBuffer<int> active_Buffer
    {
        get; set;
    }

    public static ROBuffer<int> select_Buffer
    {
        get; set;
    }

    public static ROBuffer<int> state_Buffer
    {
        get; set;
    }

    public static RWBuffer<float4> terrainArea_Buffer
    {
        get; set;
    }

    public static RWBuffer<float3> targetPos_Buffer
    {
        get; set;
    }

    public static RWBuffer<float3> refTargetPos_Buffer
    {
        get; set;
    }

    public static RWBuffer<float4> minDist_Buffer
    {
        get; set;
    }

    public static COBuffer<Color> playerColor_Buffer
    {
        get; set;
    }

    public static RWBuffer<float> refHp_Buffer
    {
        get; set;
    }


    //
    public bool[] _bColDebug;
    public static bool[] bColDebug
    {
        get; set;
    }

    private void Awake()
    {
        {
            unitCounts = _unitCounts;
            baseCount = new int2[unitCounts.Length];

            unitCount = 0;
            for(int i = 0; i < unitCounts.Length; i++)
            {
                baseCount[i].x = (i < 1 ? 0 : baseCount[i - 1].x + baseCount[i - 1].y);
                baseCount[i].y = unitCounts[i];

                unitCount += unitCounts[i];
            }

            unitMan = _unitMan;
            arrowMan = _arrowMan;
            selectMan = _selectMan;
            targetMan = _targetMan;
            torusMan = _torusMan;
            hpbarMan = _hpbarMan;
            cullMan = _cullMan;
        }

        if (unitCount > 0)
        {
            {
                unitActors = new UnitActor[unitCount];
                unitTrs = new Transform[unitCount];
            }

            {
                playerNum = _playerNum;

                viewRadiusDef = _viewRadius;
                attackRadiusDef = _attackRadius;

                viewRadius = new float[unitCount];
                attackRadius    = new float[unitCount];
            }

            {
                active_Buffer = new ROBuffer<int>(unitCount);
                activeData = _activeData = new bool[unitCount];
                for (int i = 0; i < unitCount; i++)
                {
                    activeData[i] = true;
                    //activeData[i] = false;

                    //if(i % 2 == 0)
                    //{
                    //    activeData[i] = true;
                    //}
                    //else
                    //{
                    //    activeData[i] = false;
                    //}
                }
            }

            {
                state_Buffer = new ROBuffer<int>(unitCount);
                stateData = state_Buffer.data;

                for (int i = 0; i < unitCount; i++)
                {
                    stateData[i] = 0;
                    //stateData[i] = 4;

                    //if (i % 2 == 0)
                    //{
                    //    stateData[i] = 0;
                    //}
                    //else
                    //{
                    //    stateData[i] = 4;
                    //}
                }
            }

            {
                select_Buffer = new ROBuffer<int>(unitCount);
                _selectData = selectData = select_Buffer.data;
            }

            {
                terrainArea_Buffer = new RWBuffer<float4>(unitCount);
                terrainArea = terrainArea_Buffer.data;
            }          

            {
                targetPos_Buffer = new RWBuffer<float3>(unitCount);
                targetPos = targetPos_Buffer.data;
            }

            {
                refTargetPos_Buffer = new RWBuffer<float3>(unitCount);
                refTargetPos = refTargetPos_Buffer.data;
            }

            {
                minDist_Buffer = new RWBuffer<float4>(unitCount);
                minDist = minDist_Buffer.data;
            }

            {
                maxHp = _maxHp;
                hitHp = _hitHp;
                hp = new float[unitCount];
            }

            {
                playerColor = _playerColor;
                playerColor_Buffer = new COBuffer<Color>(_playerColor.Length);
                playerColor_Buffer.data = playerColor;
                playerColor_Buffer.Write();
            }

            {
                refHp_Buffer = new RWBuffer<float>(unitCount);
                refHp = refHp_Buffer.data;
            }

            {
                bColDebug = _bColDebug;
            }
        }

        

        //Init()
        {
            csm_action.Init();
            terrain.Init();

            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].unitIdx = i;
                unitMan[i].Init(unitCounts[i]);
            }


            arrowMan.Init();
            targetMan.Init();
            selectMan.Init();
            torusMan.Init();
            hpbarMan.Init();
            cullMan.Init();
        }        
    }

    private void OnEnable()
    {
        if(unitCount > 0)
        {
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Enable();
            }

            arrowMan.Enable();
        }

        
    }

    private void OnDisable()
    {
        if(unitCount > 0)
        {
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Disable();
            }

            arrowMan.Disable();
        }

       
    }

    void Start()
    {
        if(unitCount > 0)
        {
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Begin();
            }
            targetPos_Buffer.Write();

            cullMan.Begin();
            selectMan.Begin();
        }     
    }

    void Update()
    {
        if(unitCount > 0)
        {
            {
                for (int i = 0; i < unitCount; i++)
                {
                    active_Buffer.data[i] = activeData[i] ? 1 : 0;
                }
                active_Buffer.Write();
            }

            {
                select_Buffer.Write();
                state_Buffer.Write();
            }
        }      
    }

    private void OnDestroy()
    {
        if(unitCount > 0)
        {
            BufferBase<int>.Release(active_Buffer);
            BufferBase<int>.Release(select_Buffer);
            BufferBase<float4>.Release(terrainArea_Buffer);
            BufferBase<float3>.Release(targetPos_Buffer);
            BufferBase<float3>.Release(refTargetPos_Buffer);
            BufferBase<float4>.Release(minDist_Buffer);

            BufferBase<Color>.Release(playerColor_Buffer);
            BufferBase<float>.Release(refHp_Buffer);
        }       
    }


}
