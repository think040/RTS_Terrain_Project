Shader "Unlit/UnitAnimation"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Name "UnitAnimationDepth"

            Cull Back
            ZWrite On
            ZTest LEqual         

            HLSLPROGRAM
            #pragma vertex VShader  
            #pragma geometry GShader
            #pragma fragment PShader      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationDepth.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "UnitAnimationColor"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma fragment PShader      
                      
            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationColor.hlsl"            

            ENDHLSL
        }        
    }
}
