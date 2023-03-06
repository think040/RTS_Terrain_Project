using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

using UserAnimSpace;

public class ArcherActor : UnitActor
{
    public override void Init(string[] stNames, float4x4[] stM, bool hasStMesh)
    {
        base.Init(stNames, stM, hasStMesh);
    }

    public override void Begin()
    {
        base.Begin();
    }

    public override void Update()
    {
        base.Update();
    }

    bool bArrowStarted = false;
    public Transform shootTr;

    protected override void ActState_Idle()
    {
        anim.PlayCross("Idle");
        nvAgent.isStopped = true;
        targetPos = transform.position;
        positionTr = null;
        ClearAttackTr();

        float4 _terrainArea = terrainArea;       

        if (_terrainArea.y == 1.0f || _terrainArea.z == 1.0f)
        {
            vRadius = vRadiusDef;
            aRadius = vRadius;
        }
        else
        {
            vRadius = vRadiusDef;
            aRadius = aRadiusDef;
        }
    }

    protected override void ActState_Attack()
    {
        int trId = -1;
        Transform _targetTr = GetAttackTr(out trId);
        float _aRadius = aRadius;
        float4 _terrainArea = terrainArea;

        if (_targetTr != null)
        {
            float3 forward0 = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 forward1 = (float3)_targetTr.position - (float3)transform.position;
            float dist = math.length(forward1);

            targetPos = transform.position;
            positionTr = null;
           
            if (_terrainArea.y == 1.0f || _terrainArea.z == 1.0f)
            {
                vRadius = vRadiusDef;
                aRadius = vRadius;                
            }
            else
            {
                vRadius = vRadiusDef;
                aRadius = aRadiusDef;
            }

            _aRadius = aRadius;

            if (0.1f < dist && dist <= _aRadius)
            {
                nvAgent.isStopped = true;

                forward1 = math.normalize(new float3(forward1.x, 0.0f, forward1.z));
                float cosA = math.dot(forward0, forward1);
                if (cosA < 0.999f)
                {
                    anim.PlayCross("Running");

                    float sinA = math.dot(math.cross(forward0, forward1), new float3(0.0f, 1.0f, 0.0f));

                    float da_dt = 45.0f;
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

                    transform.rotation = quaternion.LookRotation(forward1, new float3(0.0f, 1.0f, 0.0f));

                    UserAnimPlayer player = anim.player;
                    UserAnimState animState = player.cState;
                    float stime = 0.25f;
                    if (animState is UserAnimLoop)
                    {
                        UserAnimLoop animLoop = (animState as UserAnimLoop);
                        if (animLoop.name == "Attacking")
                        {
                            float ut = animLoop.ut;

                            if (math.abs(ut - stime) < 0.05f)
                            {
                                if (bArrowStarted == false)
                                {
                                    ArrowManager.ShootArrow(this, shootTr, _targetTr);
                                    bArrowStarted = true;

                                    //AudioPlay(0);
                                    //AudioAttackPlay();
                                }
                            }
                            else
                            {
                                bArrowStarted = false;
                            }
                        }
                    }
                }
            }
            else if (_aRadius < dist)
            {
                anim.PlayCross("Running");
              
                if (trId == 0)
                {
                    nvAgent.isStopped = false;
                    nvAgent.SetDestination(_targetTr.position);
                }
                else 
                {
                    nvAgent.isStopped = false;
                    float3 pos = (float3)transform.position + math.normalize(forward1) * 1.0f;
                    nvAgent.SetDestination(pos);
                }
              
            }
        }
    }
}
