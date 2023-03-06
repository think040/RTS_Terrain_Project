using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.AI;

using UserAnimSpace;

public class UnitActor : MonoBehaviour
{
    public virtual void Init(string[] stNames, float4x4[] stM, bool hasStMesh)
    {        
        this.hasStMesh = hasStMesh;
        if (hasStMesh)
        {
            this.stNames = stNames;
            this.stM = stM;

            this.stCount = transform.childCount;
            stTr = new Transform[stCount];
            for (int i = 0; i < stCount; i++)
            {
                stTr[i] = transform.GetChild(i);
                stTr[i].gameObject.name = stNames[i];
            }
        }

        InitAnim();
    }

    IEnumerator et_die;

    public virtual void Begin()
    {
        {
            targetPos = transform.position;
        }

        {
            attackTr = new Transform[3];
            ClearAttackTr();
        }

        {
            bodyRadius = nvAgent.radius * 1.10f;
        }

        {
            et_die = EndTime(2.0f);
        }      
    }

    private void OnEnable()
    {

    }

    void Start()
    {

    }

    public float _vRadius;
    public float _aRadius;

    public virtual void Update()
    {
        {
            UpdateBone();
            Behave();
        }

        {
            TestAnim();
            //TestEndTime();
        }

        
        //{
        //    vRadius = _vRadius;
        //}
    }

    public UserAnimation anim;
    protected UserAnimPlayer player;
    protected Dictionary<string, UserAnimState> dicStates;
    public NavMeshAgent nvAgent;
    public Collider bodyCollider;
    public Collider hitCollider;

    protected string[] stNames;
    protected float4x4[] stM;
    protected Transform[] stTr;
    protected int stCount;
    protected bool hasStMesh;

    public UnitManager unitMan
    {
        get; set;
    }

    public int offsetIdx
    {
        get; set;
    }
    public int iid
    {
        get; set;
    }
    public int unitIdx
    {
        get; set;
    }

    public int pNum
    {
        get
        {
            return GameManager.playerNum[unitIdx];
        }
    }

    public float vRadiusDef
    {
        get
        {
            return GameManager.viewRadiusDef[unitIdx];
        }
    }
    
    public float aRadiusDef
    {
        get
        {
            return GameManager.attackRadiusDef[unitIdx];
        }
    }

    public float vRadius
    {
        get
        {
            return GameManager.viewRadius[offsetIdx + iid];
        }
        set
        {
            GameManager.viewRadius[offsetIdx + iid] = value;
        }
    } 

    public float aRadius
    {
        get
        {
            return GameManager.attackRadius[offsetIdx + iid];
        }
        set
        {
            GameManager.attackRadius[offsetIdx + iid] = value;
        }
    }

    public float Hp
    {
        get { return GameManager.hp[offsetIdx + iid]; }
        set { GameManager.hp[offsetIdx + iid] = value; }
    }

    public float maxHp
    {
        get { return GameManager.maxHp[unitIdx]; }        
    }

    public float rHp
    {
        get { return Hp / GameManager.maxHp[unitIdx]; }
    }

    public float hitHp
    {
        get { return GameManager.hitHp[unitIdx]; }
    }

    public bool isSelected
    {
        get { return GameManager.selectData[offsetIdx + iid] == 1 ? true : false; }
        set { GameManager.selectData[offsetIdx + iid] = value ? 1 : 0; }
    }

    public bool isActive
    {
        get { return GameManager.activeData[offsetIdx + iid]; }
        set
        {
            GameManager.activeData[offsetIdx + iid] = value;
            if (!value)
            {
                isSelected = value;
                //selectGroup = -1;
            }
        }
    }

    public int stateData
    {
        get { return GameManager.stateData[offsetIdx + iid]; }
        set
        {            
            GameManager.stateData[offsetIdx + iid] = value;            
        }
    }

    public float4 terrainArea
    {
        //get
        //{
        //    return GameManager.terrainArea[offsetIdx + iid];
        //}

        get
        {
            float4 area = GameManager.terrainArea[offsetIdx + iid];

            if(area.x == 1.0f)
            {
                area = new float4(1.0f, 0.0f, 0.0f, 0.0f);
            }
            else if (area.y == 1.0f)
            {
                area = new float4(0.0f, 1.0f, 0.0f, 0.0f);
            }
            else if (area.z == 1.0f)
            {
                area = new float4(0.0f, 0.0f, 1.0f, 0.0f);
            }
            else if (area.w == 1.0f)
            {
                area = new float4(0.0f, 0.0f, 0.0f, 1.0f);
            }

            return area;
        }
    }

    public int minTargetIdx
    {
        get
        {
            float4 minDist = GameManager.minDist[offsetIdx + iid];
            int idx = -1;

            if (minDist.z > 0.0f)
            {
                idx = (int)minDist.x;
                return idx;
            }

            _minTargetIdx = idx;
            return idx;
        }
    }

    public int _minTargetIdx;

    Transform[] attackTr;

    public Transform positionTr
    {
        get; set;
    }

    public Transform targetTr
    {
        get
        {
            int minIdx = minTargetIdx;
            if (minIdx >= 0)
            {
                return GameManager.unitTrs[minIdx];
            }

            return null;
        }
    }

    public float3 targetPos
    {
        get
        {
            return GameManager.targetPos[offsetIdx + iid];
        }
        set
        {
            int idx = offsetIdx + iid;
            GameManager.targetPos[idx] = value;
            GameManager.refTargetPos[idx] = value;
        }
    }

    public void SetAttackTr(Transform tr, int order)
    {
        if (order < attackTr.Length)
        {
            attackTr[order] = tr;          
        }
    }

    public Transform GetAttackTr()
    {
        int count = attackTr.Length;
        for (int i = 0; i < count; i++)
        {
            if (attackTr[i] != null)
            {
                if (attackTr[i].GetComponent<UnitActor>().isActive)
                {
                    return attackTr[i];
                }
                else
                {
                    attackTr[i] = null;
                }
            }
        }

        return null;
    }

    public Transform GetAttackTr(out int idx)
    {
        int count = attackTr.Length;
        idx = -1;

        for (int i = 0; i < count; i++)
        {
            if (attackTr[i] != null)
            {
                if (attackTr[i].GetComponent<UnitActor>().isActive)
                {
                    idx = i;
                    return attackTr[i];
                }
                else
                {
                    attackTr[i] = null;
                }
            }
        }


        return null;
    }

    public void ClearAttackTr()
    {
        int count = attackTr.Length;
        for (int i = 0; i < count; i++)
        {
            attackTr[i] = null;
        }
    }

    public int cullOffset
    {
        get; set;
    }

    public bool isCull
    {
        get
        {
            return CullManager.cullResult_pvf[cullOffset + iid] == 0.0f ? true : false;
        }
    }


    public void InitAnim()
    {
        this.enabled = true;
        anim = GetComponent<UserAnimation>();
        player = anim.player;
        dicStates = anim.dicStates;


        dicStates["Idle_Running"] = new UserAnimCross("Idle_Running", dicStates["Idle"], dicStates["Running"], player);
        (dicStates["Idle_Running"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Idle_Running"].isRightNow = true;

        dicStates["Running_Idle"] = new UserAnimCross("Running_Idle", dicStates["Running"], dicStates["Idle"], player);
        (dicStates["Running_Idle"] as UserAnimCross).InitTime(0.05f, 0.0f, 0.075f, 0.5f, 1.0f, 0.0f);
        dicStates["Running_Idle"].isRightNow = true;

        dicStates["Running_Attacking"] = new UserAnimCross("Running_Attacking", dicStates["Running"], dicStates["Attacking"], player);
        (dicStates["Running_Attacking"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Running_Attacking"].isRightNow = true;

        dicStates["Attacking_Running"] = new UserAnimCross("Attacking_Running", dicStates["Attacking"], dicStates["Running"], player);
        (dicStates["Attacking_Running"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Attacking_Running"].isRightNow = true;

        dicStates["Attacking_Idle"] = new UserAnimCross("Attacking_Idle", dicStates["Attacking"], dicStates["Idle"], player);
        (dicStates["Attacking_Idle"] as UserAnimCross).InitTime(0.05f, 0.0f, 0.2f, 0.5f, 1.0f, 0.0f);
        dicStates["Attacking_Idle"].isRightNow = true;

        dicStates["Idle_Attacking"] = new UserAnimCross("Idle_Attacking", dicStates["Idle"], dicStates["Attacking"], player);
        (dicStates["Idle_Attacking"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Idle_Attacking"].isRightNow = true;


        player.SetDirection(AnimDirection.forward);
        //player.SetDirection(AnimDirection.backward);
        player.cState = dicStates["Idle"];
    }      
  
    IEnumerator EndTime(float _et)
    {
        float et = _et;
        float ct = 0.0f;        
reset:
        while (ct < et)
        {           
            yield return false;
            ct = ct + Time.deltaTime;
            //Debug.Log(ct.ToString());
        }
        ct = 0.0f;

        yield return true;
        goto reset;
    }  

    void TestAnim()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            anim.PlayCross("Idle");
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            anim.PlayCross("Running");
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            anim.PlayCross("Attacking");
        }

        //if (Input.GetKeyDown(KeyCode.U))
        //{
        //    anim.PlayLoop("Idle");
        //}
        //else if (Input.GetKeyDown(KeyCode.I))
        //{
        //    anim.PlayLoop("Running");
        //}
        //else if (Input.GetKeyDown(KeyCode.O))
        //{
        //    anim.PlayLoop("Attacking");
        //}

        if (Input.GetKeyDown(KeyCode.K))
        {
            player.SetDirection(AnimDirection.forward);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            player.SetDirection(AnimDirection.backward);
        }
    }

    protected virtual void UpdateBone()
    {
        if (hasStMesh)
        {
            for (int i = 0; i < stCount; i++)
            {
                float4x4 M = stM[i];

                stTr[i].position = M.c0.xyz;
                stTr[i].rotation = new quaternion(M.c1);
                stTr[i].localScale = M.c2.xyz;
            }
        }
    }  

   

    void Behave()
    {
        {            
            Test_ActionState();
            switch (state)
            {
                case ActionState.Idle:
                    ActState_Idle();
                    break;
                case ActionState.Run:
                    ActState_Run();
                    break;
                case ActionState.Attack:
                    ActState_Attack();
                    break;
                case ActionState.Die:
                    ActState_Die();
                    break;
                case ActionState.Sleep:
                    ActState_Sleep();
                    break;
            }
        }
    }
      
    void Test_ActionState()
    {
        attackTr[1] = targetTr;

        bool active = isActive;
        float _rHp = rHp;

        state = (ActionState)stateData;
       
        if (active && _rHp <= 0.0f)
        {
            SetState_Die();
        }      
        else if (active)
        {            
            if (Input.GetKeyDown(KeyCode.Q) && isSelected)
            {
                SetState_Idle();
            }
            else
            {
                if (GetAttackTr() != null && positionTr == null)
                {
                    SetState_Attack();
                }
                else
                {
                    float3 tPos = targetPos;
                    float3 cPos = transform.position;
                    if (math.distance(tPos, cPos) < 1.0f)
                    {
                        SetState_Idle();
                    }
                    else
                    {
                        SetState_Run();
                    }
                }
            }
        }
        else if (state == ActionState.Die)
        {
            if ((bool)(et_die.Current))
            {
                SetState_Sleep();
            }
        }

        stateData = (int)state;
    }

    protected virtual void ActState_Idle()
    {
        anim.PlayCross("Idle");     
        nvAgent.isStopped = true;
        targetPos = transform.position;
        positionTr = null;
        ClearAttackTr();
        
        {
            aRadius = aRadiusDef;
            vRadius = vRadiusDef;
        }       
    }

    protected virtual void ActState_Run()
    {      
        anim.PlayCross("Running");
        nvAgent.isStopped = false;
        nvAgent.SetDestination(targetPos);
    }    

    public float _da_dt = 45.0f;   
    public float bodyRadius;

    protected virtual void ActState_Attack()
    {       
        Transform _targetTr = GetAttackTr();
        float _aRadius = aRadius;
        
        if (_targetTr != null)
        {
            float3 forward0 = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 forward1 = (float3)_targetTr.position - (float3)transform.position;
            float dist = math.length(forward1);

            targetPos = transform.position;
            positionTr = null;

            float r0 = bodyRadius;           
            float r1 = 0.0f;
            if (targetTr != null)
            {                
                r1 = targetTr.GetComponent<UnitActor>().bodyRadius;
            }

            _aRadius = (r0 + r1);           
            if (0.1f < dist && dist <= _aRadius)
            {
                nvAgent.isStopped = true;
                
                forward1 = math.normalize(new float3(forward1.x, 0.0f, forward1.z));
                float cosA = math.dot(forward0, forward1);
                if (cosA < 0.98f)
                {
                    anim.PlayCross("Running");

                    float sinA = math.dot(math.cross(forward0, forward1), new float3(0.0f, 1.0f, 0.0f));

                    float da_dt = _da_dt * 50.0f * Time.deltaTime;
                    if (sinA > 0.0f)
                    {
                        da_dt *= +1.0f;
                    }
                    else
                    {
                        da_dt *= -1.0f;
                    }
                    transform.rotation = math.mul(transform.rotation,
                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da_dt * Time.fixedDeltaTime)));
                }
                else
                {
                    anim.PlayCross("Attacking");
                }
            }
            else if (_aRadius < dist)
            {
                anim.PlayCross("Running");

                {
                    vRadius = dist * 1.5f;
                }

                {
                    nvAgent.isStopped = false;
                    nvAgent.SetDestination(_targetTr.position);
                }              
            }
        }
    }


    protected virtual void ActState_Die()
    {
        float3 pos = transform.position;
        anim.PlayCross("Idle");

        bodyCollider.enabled = false;
        hitCollider.enabled = false;
        nvAgent.enabled = true;

        nvAgent.isStopped = true;
        targetPos = pos;
        positionTr = null;       
        isActive = false;

        et_die.MoveNext();        
    }

    protected virtual void ActState_Sleep()
    {       
        float3 pos = transform.position;
        anim.PlayCross("Idle");
      
        bodyCollider.enabled = false;
        hitCollider.enabled = false;        
        nvAgent.enabled = false;    
        
        targetPos = pos;
        positionTr = null;       
    }
   

    [Serializable]
    public enum ActionState : int
    {
        Idle = 0, Run = 1, Attack = 2, Die = 3, Sleep = 4, ReSpawn = 5
    }

    public ActionState state;

    public void SetState_Idle()
    {
        state = ActionState.Idle;
    }

    public void SetState_Run()
    {
        state = ActionState.Run;
    }

    public void SetState_Attack()
    {
        state = ActionState.Attack;
    }

    public void SetState_Die()
    {
        state = ActionState.Die;
    }

    public void SetState_Sleep()
    {
        state = ActionState.Sleep;
    }


    private void OnTriggerEnter(Collider other)
    {
        var hitGo = other.gameObject;

#if UNITY_EDITOR
        Debug.Log($"{gameObject.name} to {other.gameObject.name}");
#endif

        if (isActive)
        {
            HitActor hitActor = other.GetComponent<HitActor>();
            if (hitActor != null)
            {
                UnitActor actor = hitActor.unitActor;
                if (actor.isActive && actor.state == ActionState.Attack)
                {
                    {                       
                        DamageHp(actor.hitHp);                        
                    }
                }
            }
        }
    }

    public void DamageHp(float dHp)
    {
        float hp = Hp - dHp;
        if (hp < 0.0f)
        {
            hp = 0.0f;
            //hp = maxHp;
        }

        Hp = hp;
    }


    //Test
    void TestEndTime()
    {
        et_die.MoveNext();
        if ((bool)(et_die.Current))
        {
            Debug.Log("et_die");
            //et_die.Reset();
        }
    }

    void Test_ActionState0()
    {
        {
            if (Input.GetKeyDown(KeyCode.Q) && isSelected)
            {
                SetState_Idle();
            }
            else
            {
                if (Input.GetKey(KeyCode.R) && isSelected)
                {
                    SetState_Attack();
                }
                else
                {
                    float3 tPos = targetPos;
                    float3 cPos = transform.position;
                    if (math.distance(tPos, cPos) < 1.0f)
                    {
                        SetState_Idle();
                    }
                    else
                    {
                        SetState_Run();
                    }
                }
            }
        }
    }

    void Test_ActionState1()
    {
        attackTr[1] = targetTr;

        bool active = isActive;
        float _rHp = rHp;

        state = (ActionState)stateData;

        if (active && _rHp <= 0.0f)
        {
            SetState_Die();
        }
        else if (active)
        {
            if (Input.GetKeyDown(KeyCode.Q) && isSelected)
            {
                SetState_Idle();
            }
            else
            {
                if (GetAttackTr() != null && positionTr == null)
                {
                    SetState_Attack();
                }
                else
                {
                    float3 tPos = targetPos;
                    float3 cPos = transform.position;
                    if (math.distance(tPos, cPos) < 1.0f)
                    {
                        SetState_Idle();
                    }
                    else
                    {
                        SetState_Run();
                    }
                }
            }
        }

        stateData = (int)state;
    }


    protected virtual void ActState_Attack0()
    {
        anim.PlayCross("Attacking");
        nvAgent.isStopped = true;
        targetPos = transform.position;
    }
}