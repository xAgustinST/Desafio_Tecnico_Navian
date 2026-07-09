// Transparent surface shader for the anatomy meshes (Skin/GrayMatter/WhiteMatter/Veins).
// Same _Color property as Standard, so StructureVisibilityController's opacity slider
// (which sets Material.color) keeps working unmodified.
//
// Adds an optional world-space clip plane driven by global shader properties, pushed from
// CrossSectionController so the meshes cut away in sync with the same plane that already
// cuts the MRI volume (Volume Inspection's "Enable plane" toggle + Anatomical Planes axis/
// position sliders). No per-material wiring needed: any material using this shader picks
// up the global clip state automatically.
Shader "NavianChallenge/ClippedTransparent"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.2
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Cull Off
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:blend
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // xyz = world-space plane normal, w = signed distance (dot(planePoint, normal)).
        // Matches the vendor DVR shader's cross-section convention: the side the normal
        // points toward gets clipped away.
        float4 _NavianClipPlane;
        float _NavianClipEnabled;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            if (_NavianClipEnabled > 0.5)
            {
                float d = dot(IN.worldPos, _NavianClipPlane.xyz) - _NavianClipPlane.w;
                clip(-d);
            }

            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
