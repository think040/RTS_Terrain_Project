﻿#include "../../../Utility/01_GShader/CSMUtil.hlsl"

struct ArrowConst
{
    bool active;
        
    float u;
    float3 sca;
    
    float3 pi;
    float3 p0;
    float3 p1;
};


struct VertexDynamic
{
    float3 posW;
    float3 normalW;
    float4 tangentW;
    int4 boneI;
};

struct VertexStatic
{
    float2 uv;
    float4 color;
};

StructuredBuffer<VertexDynamic> vtxDynamic;

StructuredBuffer<ArrowConst> arrowConst;

StructuredBuffer<float4> arrowColor_Buffer;
StructuredBuffer<int> pid_Buffer;
//Buffer<float4> arrowColors;
//float4 arrowColor;

int vtxCount;

int cullOffset;
Texture3D<float> cullResult_pvf_Texture;

int enableCull;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_POSITION;
    
    float3 posW : TEXCOORD1;
    float3 normalW : NORMAL;
    float3 tangentW : TANGENT;
    
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
    
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
    uint bid : TEXCOORD2;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    
    float3 posW : TEXCOORD1;
    float3 normalW : NORMAL;
    float3 tangentW : TANGENT;
    
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
    
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
    uint bid : TEXCOORD2;
};

struct PS_Out
{
    float4 color : SV_Target;
};


VS_Out VShader(IA_Out vIn)
{
    VS_Out vOut;
    uint vid = vIn.vid;
    uint iid = vIn.iid;
    
    ArrowConst constData = arrowConst[iid];
    
    bool inPvf = false;
    
    uint isCull = 0;
    if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 0.0f)
    {
        isCull = 1;
    }
    
       
    if (constData.active && isCull == 0)
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];
    
        vOut.posC = mul(CV, float4(vtxd.posW, 1.0f));
    
        vOut.posW = vtxd.posW;
        vOut.normalW = vtxd.normalW;
        vOut.tangentW = vtxd.tangentW;
        
        vOut.bid = vtxd.boneI.x;
    }
    else
    {
        vOut.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
    
        vOut.posW = float3(0.0f, 0.0f, 0.0f);
        vOut.normalW = float3(0.0f, 0.0f, 0.0f);
        vOut.tangentW = float3(0.0f, 0.0f, 0.0f);
        
        vOut.bid = 0;
    }
    
    vOut.isCull = isCull;
    vOut.iid = iid;
        
    return vOut;
}


PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    ArrowConst constData = arrowConst[iid];
    
    c = float3(1.0f, 0.0f, 0.0f);  
    
    c = arrowColor_Buffer[pid_Buffer[iid]].xyz;
    
    if (constData.active && isCull == 0)
    {
        float3 posW = pIn.posW;
        float3 normalW = normalize(pIn.normalW);
        float3 tangentW = normalize(pIn.tangentW);
    
        float NdotL;
        float sf = CSMUtil::GetShadowFactor_CSM(posW, normalW, NdotL);
                 
        float k = 1.75f;
        float m = 0.4f;
        float l = 0.4f;
    
        float specularFactor;
        specularFactor = CSMUtil::GetSpecularFactor(posW, normalW);
        
        //c = k * NdotL * (1.0f - m * sf) * ((1.0f - l) * c + l * specularFactor * float3(1.0f, 1.0f, 1.0f));  
               
        c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
    }
    else
    {
        c = float3(0.0f, 0.0f, 0.0f);
    }
            
            
    pOut.color = float4(c, 1.0f);    
           
    return pOut;
}