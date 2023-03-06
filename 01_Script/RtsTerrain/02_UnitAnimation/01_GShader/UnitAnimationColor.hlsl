#include "../../../Utility/01_GShader/CSMUtil.hlsl"

struct VertexDynamic
{
    float3 posW;
    float3 normalW;
    float4 tangentW;
};

struct VertexStatic
{
    float2 uv0;
    float2 uv1;
    float4 color;
};

StructuredBuffer<VertexDynamic> vtxDynamic;
StructuredBuffer<VertexStatic> vtxStatic;
int vtxCount;

Texture2D tex_diffuse0;
SamplerState sampler_tex_diffuse0;

Texture2D tex_diffuse1;
SamplerState sampler_tex_diffuse1;

StructuredBuffer<int> active_Buffer;
int offsetIdx;

StructuredBuffer<int> state_Buffer;
float4 unitColor;

int cullOffset;
Texture3D<float> cullResult_pvf_Texture;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_POSITION;
    
    float3 posW : PosW;
    float3 normalW : NomW;
    float3 tangentW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    
    float4 color : COLOR0;       
    
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    
    float3 posW : PosW;
    float3 normalW : NomW;
    float3 tangentW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    
    float4 color : COLOR0;    
    
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
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
        
    uint isCull = 0;
    
    if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 0.0f)
    {
        isCull = 1;
    }
    
    uint isActive = 0;
    if (active_Buffer[offsetIdx + iid] == 1)
    {
        isActive = 1;
    }
    
    int state = state_Buffer[offsetIdx + iid];
    
    if(state < 4 && isCull == 0)
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];
        VertexStatic vtxs = vtxStatic[vid];
    
        vOut.posC = mul(CV, float4(vtxd.posW, 1.0f));
    
        vOut.posW = vtxd.posW;
        vOut.normalW = vtxd.normalW;
        vOut.tangentW = vtxd.tangentW;
            
        vOut.uv0 = vtxs.uv0;
        vOut.uv1 = vtxs.uv1;
        
        vOut.color = vtxs.color;
    }  
    else
    {
        vOut.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
    
        vOut.posW =     float3(0.0f, 0.0f, 0.0f);
        vOut.normalW =  float3(0.0f, 0.0f, 0.0f);                                                
        vOut.tangentW = float3(0.0f, 0.0f, 0.0f);
            
        vOut.uv0 = float2(0.0f, 0.0f);                                     
        vOut.uv1 = float2(0.0f, 0.0f);
        
        vOut.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.iid = iid;
        vOut.isActive = isActive;
        vOut.isCull = isCull;
        vOut.state = state;
    }
        
    return vOut;
}

VS_Out VShader_Debug(IA_Out vIn)
{
    VS_Out vOut;
    uint vid = vIn.vid;
    uint iid = vIn.iid;
        
    uint isCull = 0;
    
    //if (cullResult_pvf_Texture.Load(int4(cullOffset + iid, 0, 0, 0)) == 0.0f)
    //{
    //    isCull = 1;
    //}
    
    uint isActive = 0;
    if (active_Buffer[offsetIdx + iid] == 1)
    {
        isActive = 1;
    }
    
    int state = state_Buffer[offsetIdx + iid];
    
   
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];
        VertexStatic vtxs = vtxStatic[vid];
    
        vOut.posC = mul(CV, float4(vtxd.posW, 1.0f));
    
        vOut.posW = vtxd.posW;
        vOut.normalW = vtxd.normalW;
        vOut.tangentW = vtxd.tangentW;
            
        vOut.uv0 = vtxs.uv0;
        vOut.uv1 = vtxs.uv1;
        
        vOut.color = vtxs.color;
    }   
    
    {
        vOut.iid = iid;
        vOut.isActive = isActive;
        vOut.isCull = isCull;
        vOut.state = state;
    }
        
    return vOut;
}


PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;

    int state = pIn.state;
    
    if (state < 4 && isCull == 0)
    {
        float3 pos = pIn.posW;
        
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
                   
        if (state < 3)
        {
            c = pIn.color.xyz;
              
            bool isMan = pIn.color.x == 0.0f ? true : false;
        
            if (isMan)
            {
                c = tex_diffuse0.Sample(sampler_tex_diffuse0, pIn.uv0).xyz;
            }
            else
            {
                c = tex_diffuse1.Sample(sampler_tex_diffuse1, pIn.uv0).xyz;
            }
            c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
        
            pOut.color = float4(c.xyz, 1.0f);
        }
        else //state 3 Die
        {
            c = unitColor.xyz;
            c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
                
            pOut.color = float4(c.xyz, 0.75f);
        }
    }
    else //state 4 Sleep
    {
        pOut.color = float4(0.0f, 1.0f, 0.0f, 0.0f);
    }
    
                          
    return pOut;
}



//Test
PS_Out PShader0(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    {
        float3 posW = pIn.posW;
        float3 normalW = normalize(pIn.normalW);
        float3 tangentW = normalize(pIn.tangentW);
        
        c = pIn.color.xyz;
        //c = float3(0.0f, 1.0f, 0.0f);
              
        bool isMan = pIn.color.x == 0.0f ? true : false;
        
        if (isMan)
        {
            c = tex_diffuse0.Sample(sampler_tex_diffuse0, pIn.uv0).xyz;
        }
        else
        {
            c = tex_diffuse1.Sample(sampler_tex_diffuse1, pIn.uv0).xyz;
        }
        
        float NdotL = max(0.5f, dot(normalW, dirW_light));
        c *= NdotL;
        
        pOut.color = float4(c, 1.0f);
            
    }
                          
    return pOut;
}

PS_Out PShader1(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;

    if (isActive == 1)
    {
        int state = state_Buffer[offsetIdx + iid];
        
        {
            float3 pos = pIn.posW;
        
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
        
            if (state == 3)
            {
                c = unitColor.xyz;
                c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
                
                pOut.color = float4(c.xyz, 0.75f);
            }
            else
            {
                c = pIn.color.xyz;
              
                bool isMan = pIn.color.x == 0.0f ? true : false;
        
                if (isMan)
                {
                    c = tex_diffuse0.Sample(sampler_tex_diffuse0, pIn.uv0).xyz;
                }
                else
                {
                    c = tex_diffuse1.Sample(sampler_tex_diffuse1, pIn.uv0).xyz;
                }
                c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
        
                pOut.color = float4(c.xyz, 1.0f);
            }
        }
    }
    else
    {
        pOut.color = float4(0.0f, 1.0f, 0.0f, 1.0f);
    }
    
                          
    return pOut;
}

PS_Out PShader2(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;

    if (isActive == 1)
    {
        {
            float3 pos = pIn.posW;
        
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
                   
            {
                c = pIn.color.xyz;
              
                bool isMan = pIn.color.x == 0.0f ? true : false;
        
                if (isMan)
                {
                    c = tex_diffuse0.Sample(sampler_tex_diffuse0, pIn.uv0).xyz;
                }
                else
                {
                    c = tex_diffuse1.Sample(sampler_tex_diffuse1, pIn.uv0).xyz;
                }
                c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
        
                pOut.color = float4(c.xyz, 1.0f);
            }
        }
    }
    else
    {
        int state = state_Buffer[offsetIdx + iid];
        if (state == 3)
        {
            float3 pos = pIn.posW;
        
            float3 posW = pIn.posW;
            float3 normalW = normalize(pIn.normalW);
            float3 tangentW = normalize(pIn.tangentW);
            
            float NdotL;
            float sf = CSMUtil::GetShadowFactor_CSM(posW, normalW, NdotL);
        
            float specularFactor;
            specularFactor = CSMUtil::GetSpecularFactor(posW, normalW);
                        
            c = unitColor.xyz;
            c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
                
            pOut.color = float4(c.xyz, 0.75f);
        }
        else
        {
            pOut.color = float4(0.0f, 1.0f, 0.0f, 1.0f);
        }
        
        //pOut.color = float4(0.0f, 1.0f, 0.0f, 1.0f);
    }
    
                          
    return pOut;
}

PS_Out PShader3(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;

    int state = pIn.state;
    
    if (state < 4)
    {
        float3 pos = pIn.posW;
        
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
                   
        if (state < 3)
        {
            c = pIn.color.xyz;
              
            bool isMan = pIn.color.x == 0.0f ? true : false;
        
            if (isMan)
            {
                c = tex_diffuse0.Sample(sampler_tex_diffuse0, pIn.uv0).xyz;
            }
            else
            {
                c = tex_diffuse1.Sample(sampler_tex_diffuse1, pIn.uv0).xyz;
            }
            c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
        
            pOut.color = float4(c.xyz, 1.0f);
        }
        else //state 3 Die
        {
            c = unitColor.xyz;
            c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
                
            pOut.color = float4(c.xyz, 0.75f);
        }
    }
    else //state 4 Sleep
    {
        pOut.color = float4(0.0f, 1.0f, 0.0f, 0.0f);
    }
    
                          
    return pOut;
}