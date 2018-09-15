Shader "Custom/WaterEffect" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        _NAmount("Bump Mapping Amount", Range(0, 1)) = 0.5
        _Granularity("Bump Mapping Granularity", Range(0, 10)) = 1.0
        _NSpeed("Bump Mapping Animation Speed", Range(0, 10)) = 1.0
        _NShift("Texture Shift Amount", Range(0, 0.5)) = 0.1
        _TextureShiftGranularity("Texture Shift Granularity", Range(0, 10)) = 1.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
        
        float4 TrailPoints;
		
        sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

        float _NShift;
        float _TextureShiftGranularity;

        int perm(int d)
        {
            d = d % 256;
            float2 t = float2(d%16,d/16)/15.0;
            return t * 255;
        }

        float fade(float t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

        float lerp(float t,float a,float b) { return a + t * (b - a); }

        float grad(int hash,float x,float y,float z)
        {
            int h	= hash % 16;										// & 15;
            float u = h<8 ? x : y;
            float v = h<4 ? y : (h==12||h==14 ? x : z);
            return ((h%2) == 0 ? u : -u) + (((h/2)%2) == 0 ? v : -v); 	// h&1, h&2 
        }
        float noise(float x, float y,float z)
        {	
            int X = (int)floor(x) % 256;	// & 255;
            int Y = (int)floor(y) % 256;	// & 255;
            int Z = (int)floor(z) % 256;	// & 255;
               	
            x -= floor(x);
            y -= floor(y);
            z -= floor(z);
                     
            float u = fade(x);
            float v = fade(y);
            float w = fade(z);
               	
                int A	= perm(X  	)+Y;
                int AA	= perm(A	)+Z;
            int AB	= perm(A+1	)+Z; 
            int B	= perm(X+1	)+Y;
            int BA	= perm(B	)+Z;
            int BB	= perm(B+1	)+Z;

            return lerp(w, lerp(v, lerp(u, grad(perm(AA  ), x  , y  , z   ),
                                            grad(perm(BA  ), x-1, y  , z   )),
                                    lerp(u, grad(perm(AB  ), x  , y-1, z   ),
                                            grad(perm(BB  ), x-1, y-1, z   ))),
                            lerp(v, lerp(u, grad(perm(AA+1), x  , y  , z-1 ),
                                            grad(perm(BA+1), x-1, y  , z-1 )),
                                    lerp(u, grad(perm(AB+1), x  , y-1, z-1 ),
                                            grad(perm(BB+1), x-1, y-1, z-1 ))));
        }

        float _Granularity;
        float _NAmount;
        float _NSpeed;

        void vert(inout appdata_full v)
        {
            // lerp at edges to get rid of seam.
            float halfV = 100.0f;
            float normedZ = (v.vertex.y + halfV) / (halfV * 2.0f);
            float seamEdgeWidth = 0.05f;
            float start = smoothstep(0.0f, seamEdgeWidth, normedZ);
            float end = 1.0f - smoothstep(1.0f - seamEdgeWidth, 1.0f, normedZ);

            float effect = (start * end);

            v.normal.xyz += noise ((sin(v.vertex.x * _Granularity) + _Time * _NSpeed), 
               					    (sin(v.vertex.y * _Granularity) + _Time * _NSpeed), 
               					    (sin(v.vertex.z * _Granularity) + _Time * _NSpeed))  * _NAmount * effect;
        }

        float2 GetTrailWeightAndPosition(float2 trailStart, float2 trailEnd, float2 uv)
        {
            float2 pa = uv - trailEnd;
            float2 ba = trailStart - trailEnd;
            float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
            float d = length(pa - ba * h);
            float positionOnLine = clamp(length(uv - trailStart) / length(ba), 0.0, 1.0);
            float thickness = positionOnLine * 0.6 + 0.1;
            float trailEffect = 1.0 - smoothstep(0.0, thickness, d);
            return float2(trailEffect, positionOnLine);
        }

        float GetTextureShiftForTrailEffect(float2 weightAndPositionOnLine, float2 uv)
        {
            float g = 50.0f * weightAndPositionOnLine.y + 1.0f;
            float speed = (200.0f/* * weightAndPositionOnLine.x*/);
            float trailShift = noise(uv.x * g + _Time * speed,
                                     uv.y * g + _Time * speed,
                                     g + _Time * speed);
            return trailShift;
        }

        float2 TrailEffectTextureShiftAndWeight(float2 a, float2 b, float2 uv)
        {
            float2 weightAndPosOnLine = GetTrailWeightAndPosition(a, b, uv);
            float trailWeight = weightAndPosOnLine.x;
            float trailShift = GetTextureShiftForTrailEffect(weightAndPosOnLine, uv);
            return float2(trailShift, trailWeight);
        }

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
            float2 uv = IN.uv_MainTex;// + float2(xScroll, yScroll);
            fixed4 c = tex2D (_MainTex, uv) * _Color;
            float shift = noise(uv.x * _TextureShiftGranularity + ((_Time * 10.3) % 200.0),
                                uv.y * _TextureShiftGranularity + ((_Time * 11.4) % 200.0) * sin(_Time),
                                _TextureShiftGranularity + ((_Time * 10.7) % 200.0)) * _NShift;
            float uMod = uv.x + shift % 1;
            float vMod = uv.y + (shift * 2) % 1;
            fixed4 c2 = tex2D(_MainTex, float2(uMod, vMod)) * _Color;

            // lerp at edges to get rid of seam.
            float seamEdgeWidth = 0.1f;
            float start = smoothstep(0.0f, seamEdgeWidth, uv.y);
            const float maxY = 4.0f;
            float end = 1.0f - smoothstep(maxY - seamEdgeWidth, maxY, uv.y);

            float left = smoothstep(0.0f, seamEdgeWidth, uv.x);
            const float maxX = 4.0f;
            float right = 1.0f - smoothstep(maxX - seamEdgeWidth, maxX, uv.x);

            float noEffect = 1.0f - (start * end * left * right);

			o.Albedo = (noEffect * c.rgb) + (1.0f - noEffect) * (c.rgb * 0.2 + c2.rgb * 0.8);

            float2 a = float2(TrailPoints.x*maxX, TrailPoints.y*maxY);
            float2 b = float2(TrailPoints.z*maxX, TrailPoints.w*maxY);
            float2 trailShiftAndWeight = TrailEffectTextureShiftAndWeight(a, b, uv);
            float trailShift = trailShiftAndWeight.x * _NShift;
            float uTrail = (uv.x - trailShift) % maxX;
            float vTrail = (uv.y - trailShift) % maxY;
            const float trailBrightness = 3.0f;
            fixed4 cTrail = tex2D(_MainTex, float2(uTrail, vTrail)) * _Color * trailShiftAndWeight.y * trailBrightness;

            /*const int numTrailPoints = 4;
            for (int i = 0; i < numTrailPoints - 1; i++)
            {
                float2 a = float2(TrailPoints[i].x * maxX, TrailPoints[i].y * maxY);
                float2 b = float2(TrailPoints[i + 1].x * maxX, TrailPoints[i + 1].y * maxY);
                float2 trailShiftAndWeight = TrailEffectTextureShiftAndWeight(a, b, uv);
                float trailShift = trailShiftAndWeight.x * _NShift;
                float uTrail = (uv.x - trailShift) % maxX;
                float vTrail = (uv.y - trailShift) % maxY;
                const float trailBrightness = 3.0f;
                fixed4 cTrail = tex2D(_MainTex, float2(uTrail, vTrail)) * _Color * trailShiftAndWeight.y * trailBrightness;

                o.Albedo += cTrail.rgb;
            }*/

            o.Albedo += cTrail.rgb;

			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
