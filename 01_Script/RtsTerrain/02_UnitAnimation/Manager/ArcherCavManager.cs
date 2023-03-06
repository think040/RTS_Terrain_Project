using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherCarvManager : UnitManager
{
    //public override void OnEnable()
    //{
    //    base.OnEnable();
    //}
    //
    //public override void OnDisable()
    //{
    //    base.OnDisable();
    //}
  

    public override void Init(int _count)
    {
        base.Init(_count);
        Spawn<ArcherCavActor>();       
    }

    public override void Spawn<T>() 
    {
        base.Spawn<T>();

        if (count > 0)
        {
            InitColliderRendering();
            {
                StartCoroutine(UpdateColRender());
            }
        }

    }
    
    protected override void InitColliderRendering()
    {
        base.InitColliderRendering();

        if (count > 0)
        {
            colRenders[3] = new ColliderRender(hitTrs, ColliderRender.Type.Sphere);
            colRenders[3].Init(gshader_col, cshader_col);
        }       
    }

    public override void Begin()
    {
        base.Begin();
    }
      
    public override void Update()
    {
        base.Update();
    }
}
