#define v4c0 _m00_m10_m20_m30
#define v4c1 _m01_m11_m21_m31
#define v4c2 _m02_m12_m22_m32
#define v4c3 _m03_m13_m23_m33

cbuffer perObject
{
    float4x4 W;
    float4x4 W_csm[4];
          
    int type; //0 Pvf //1 Ovf //2 tile_Box
    int mode; //0 Pvf-tileBox //1 Ovf-tileBox
    int vfIdx; //mode == 0 -> vfIdx = 0    //mode == 1 -> vfIdx = 0, 1, 2, 3
}
		
cbuffer perView
{
    float4x4 CV;
    float4x4 CV_csm[4];
}

Texture2D<float4> Pvf_Vtx_Tex;
Texture2D<float4> Box_Vtx_Tex;

Texture3D<float> TestCullPVF_Tex;
Texture3D<float> TestCullOVF_Tex;
Texture3D<float4> TileW_Tex;

float3 tileCount;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posL : SV_POSITION;
    uint iid : SV_InstanceID;
};

struct GS_Out
{
    float4 posC : SV_POSITION;
    int state : STATE;
    
    uint rid : SV_RenderTargetArrayIndex;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    int state : STATE;
    
    uint rid : SV_RenderTargetArrayIndex;
};

struct PS_Out
{
    float4 color : SV_Target;
};

int qtIdx;
int qtSize;

float pvfCount;
float ovfCount;

int qtCount;


VS_Out VShader(IA_Out vIn)
{
    VS_Out vOut;
    
    float3 posL;
    
    if (type == 0)
    {
        posL = Pvf_Vtx_Tex[int2(0, vIn.vid)];
    }
    else
    {
        posL = Box_Vtx_Tex[int2(0, vIn.vid)];
    }
    
    vOut.posL = float4(posL, 1.0f);
    vOut.iid = vIn.iid;
    
    return vOut;
}

[maxvertexcount(8)]
void GShader_CSM(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    
    int cx = (int) tileCount.x;
    int cz = (int) tileCount.z;
            
    int state = 0;
        
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        for (i = 0; i < 4; i++)
        {
            GS_Out v;
            if (TestCullOVF_Tex[int3(px, (qtIdx) * ovfCount + i, pz)] == 1)
            {
                state = 1;
            }
            
            if (state == 1)
            {
                float4x4 _W;
                _W.v4c0 = TileW_Tex[int3(px, qtIdx * 4 + 0, pz)].xyzw;
                _W.v4c1 = TileW_Tex[int3(px, qtIdx * 4 + 1, pz)].xyzw;
                _W.v4c2 = TileW_Tex[int3(px, qtIdx * 4 + 2, pz)].xyzw;
                _W.v4c3 = TileW_Tex[int3(px, qtIdx * 4 + 3, pz)].xyzw;
                
                for (j = 0; j < 2; j++)
                {
                    float4 posW = mul(_W, gIn[j].posL);
                    
                    v.posC = mul(CV_csm[i], posW);
                    v.state = state;
                    v.rid = i;
                
                    gOut.Append(v);
                }
                gOut.RestartStrip();
            }
        }
    }
}

[maxvertexcount(2)]
void GShader_CAM(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    int m = 0;
    
    //int cx = (int) tileCount.x;
    //int cz = (int) tileCount.z;
    
    int cx = (int) qtSize;
    int cz = (int) qtSize;       
    
    int state = 2;
    if (type == 0)       //Pvf
    {
        GS_Out v;
        if (mode == 0)
        {
            state = 0;
            for (m = 0; m < qtCount; m++)
            {
                cz = cx = (int) pow(2.0f, m);
                
                for (j = 0; j < cx; j++)
                {
                    for (k = 0; k < cz; k++)
                    {
                        if (TestCullPVF_Tex[int3(j, m * pvfCount + vfIdx, k)] == 1)
                        {
                            state = 1;
                            j = cx;
                            k = cz;
                            m = qtCount;
                        }
                    }
                }
            }                       
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW = mul(W, gIn[j].posL);
            
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else if (type == 1)  //Ovf
    {
        GS_Out v;
        if (mode == 1)
        {
            state = 0;            
            for (m = 0; m < qtCount; m++)
            {
                cz = cx = (int) pow(2.0f, m);
                
                for (j = 0; j < cx; j++)
                {
                    for (k = 0; k < cz; k++)
                    {
                        //if (TestCullOVF_Tex[int3(j, (float) (qtCount - 1) * ovfCount + vfIdx, k)] == 1)
                        //if (TestCullOVF_Tex[int3(j, (qtIdx) * ovfCount + vfIdx, k)] == 1)
                        if (TestCullOVF_Tex[int3(j, m * ovfCount + vfIdx, k)] == 1)                        
                        {
                            state = 1;
                            j = cx;
                            k = cz;
                            m = qtCount;
                        }
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW = mul(W_csm[vfIdx], gIn[j].posL);
                        
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
                
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else //if(mode == 2)        //TileBox
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        GS_Out v;
        state = 0;
        
        float4x4 _W;
        _W.v4c0 = TileW_Tex[int3(px, qtIdx * 4 + 0, pz)].xyzw;
        _W.v4c1 = TileW_Tex[int3(px, qtIdx * 4 + 1, pz)].xyzw;
        _W.v4c2 = TileW_Tex[int3(px, qtIdx * 4 + 2, pz)].xyzw;
        _W.v4c3 = TileW_Tex[int3(px, qtIdx * 4 + 3, pz)].xyzw;               
                
        if (mode == 0)   //TileBox - Pvf
        {
            if (TestCullPVF_Tex[int3(px, (qtIdx) * pvfCount + vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
        else //TileBox - Ovf
        {
            if (TestCullOVF_Tex[int3(px, (qtIdx) * ovfCount + vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
                       
        if (state == 1)
        {
            for (j = 0; j < 2; j++)
            {
                float4 posW = mul(_W, gIn[j].posL);
                
                //Debug
                //posW = gIn[j].posL;
                    
                v.posC = mul(CV, posW);
                v.state = state;
                v.rid = 0;
                
                gOut.Append(v);
            }
            gOut.RestartStrip();
        }
    }
        
   
}

PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
    float3 color = float3(0.0f, 1.0f, 0.0f);
    
    int state = pIn.state;
    
    if (state == 0)
    {
        color = float3(0.0f, 0.0f, 1.0f);
    }
    else if (state == 1)
    {
        color = float3(1.0f, 0.0f, 0.0f);
    }
    
    pOut.color = float4(color, 1.0f);
    
    return pOut;
}



//Test
[maxvertexcount(2)]
void GShader_CAM2(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    
    int cx = (int) tileCount.x;
    int cz = (int) tileCount.z;
            
    int state = 2;
    if (type == 0)       //Pvf
    {
        GS_Out v;
        if (mode == 0)
        {
            state = 0;
            for (j = 0; j < cx; j++)
            {
                for (k = 0; k < cz; k++)
                {
                    if (TestCullPVF_Tex[int3(j, vfIdx, k)] == 1)
                    {
                        state = 1;
                        j = cx;
                        k = cz;
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW = mul(W, gIn[j].posL);
            
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else if (type == 1)  //Ovf
    {
        GS_Out v;
        if (mode == 1)
        {
            state = 0;
            for (j = 0; j < cx; j++)
            {
                for (k = 0; k < cz; k++)
                {
                    if (TestCullOVF_Tex[int3(j, vfIdx, k)] == 1)
                    {
                        state = 1;
                        j = cx;
                        k = cz;
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW = mul(W_csm[vfIdx], gIn[j].posL);
                        
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
                
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else //if(mode == 2)        //TileBox
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        GS_Out v;
        state = 0;
        
        float4x4 _W;
        _W.v4c0 = TileW_Tex[int3(px, 0, pz)].xyzw;
        _W.v4c1 = TileW_Tex[int3(px, 1, pz)].xyzw;
        _W.v4c2 = TileW_Tex[int3(px, 2, pz)].xyzw;
        _W.v4c3 = TileW_Tex[int3(px, 3, pz)].xyzw;
                
                
        if (mode == 0)   //TileBox - Pvf
        {
            if (TestCullPVF_Tex[int3(px, vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
        else //TileBox - Ovf
        {
            if (TestCullOVF_Tex[int3(px, vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
        
        //if (state == 1)
        {
            for (j = 0; j < 2; j++)
            {
                float4 posW = mul(_W, gIn[j].posL);
                
                //Debug
                //posW = gIn[j].posL;
                    
                v.posC = mul(CV, posW);
                v.state = state;
                v.rid = 0;
                
                gOut.Append(v);
            }
            gOut.RestartStrip();
        }
    }
        
   
}

[maxvertexcount(2)]
void GShader_CAM1(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    
    int cx = (int) tileCount.x;
    int cz = (int) tileCount.z;
            
    int state = 2;
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        GS_Out v;
        
        float4x4 _W;
        _W.v4c0 = TileW_Tex[int3(px, 0, pz)].xyzw;
        _W.v4c1 = TileW_Tex[int3(px, 1, pz)].xyzw;
        _W.v4c2 = TileW_Tex[int3(px, 2, pz)].xyzw;
        _W.v4c3 = TileW_Tex[int3(px, 3, pz)].xyzw;
                
                       
        {
            for (j = 0; j < 2; j++)
            {
                float4 posW = mul(_W, gIn[j].posL);
                
                //Debug
                posW = gIn[j].posL;
                    
                v.posC = mul(CV, posW);
                v.state = state;
                v.rid = 0;
                
                gOut.Append(v);
            }
            gOut.RestartStrip();
        }
    }
        
   
}

[maxvertexcount(2)]
void GShader_CAM0(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    
    //int cx = (int) tileCount.x;
    //int cz = (int) tileCount.z;
    
    int cx = (int) qtSize;
    int cz = (int) qtSize;
    
            
    int state = 2;
    if (type == 0)       //Pvf
    {
        GS_Out v;
        if (mode == 0)
        {
            state = 0;
            for (j = 0; j < cx; j++)
            {
                for (k = 0; k < cz; k++)
                {
                    if (TestCullPVF_Tex[int3(j, vfIdx, k)] == 1)
                    {
                        state = 1;
                        j = cx;
                        k = cz;
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW = mul(W, gIn[j].posL);
            
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else if (type == 1)  //Ovf
    {
        GS_Out v;
        if (mode == 1)
        {
            state = 0;
            for (j = 0; j < cx; j++)
            {
                for (k = 0; k < cz; k++)
                {
                    if (TestCullOVF_Tex[int3(j, vfIdx, k)] == 1)
                    {
                        state = 1;
                        j = cx;
                        k = cz;
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW = mul(W_csm[vfIdx], gIn[j].posL);
                        
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
                
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else //if(mode == 2)        //TileBox
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        GS_Out v;
        state = 0;
        
        float4x4 _W;
        _W.v4c0 = TileW_Tex[int3(px, qtIdx * 4 + 0, pz)].xyzw;
        _W.v4c1 = TileW_Tex[int3(px, qtIdx * 4 + 1, pz)].xyzw;
        _W.v4c2 = TileW_Tex[int3(px, qtIdx * 4 + 2, pz)].xyzw;
        _W.v4c3 = TileW_Tex[int3(px, qtIdx * 4 + 3, pz)].xyzw;
        
        //_W.v4c0 = TileW_Tex[int3(px, 2 * 4 + 0, pz)].xyzw;
        //_W.v4c1 = TileW_Tex[int3(px, 2 * 4 + 1, pz)].xyzw;
        //_W.v4c2 = TileW_Tex[int3(px, 2 * 4 + 2, pz)].xyzw;
        //_W.v4c3 = TileW_Tex[int3(px, 2 * 4 + 3, pz)].xyzw;
                
                
        if (mode == 0)   //TileBox - Pvf
        {
            if (TestCullPVF_Tex[int3(px, vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
        else //TileBox - Ovf
        {
            if (TestCullOVF_Tex[int3(px, vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
        
        //if (state == 1)
        {
            for (j = 0; j < 2; j++)
            {
                float4 posW = mul(_W, gIn[j].posL);
                
                //Debug
                //posW = gIn[j].posL;
                    
                v.posC = mul(CV, posW);
                v.state = state;
                v.rid = 0;
                
                gOut.Append(v);
            }
            gOut.RestartStrip();
        }
    }
        
   
}

PS_Out PShader1(RS_Out pIn)
{
    PS_Out pOut;
    float3 color = float3(0.0f, 1.0f, 0.0f);
       
    
    pOut.color = float4(color, 1.0f);
    
    return pOut;
}