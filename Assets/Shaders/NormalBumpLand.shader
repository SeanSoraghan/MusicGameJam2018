Shader "Custom/NormalBumpLand" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        _NAmount		("Bump Mapping Amount", Range (0, 1)) = 0.5
        _Granularity  ("Bump Mapping Granularity", Range (0, 10)) = 1.0
        _NSpeed       ("Bump Mapping Animation Speed", Range (0, 10)) = 1.0
        _NShift ("Texture Shift Amount", Range (0, 0.5)) = 0.1
        _TextureShiftGranularity ("Texture Shift Granularity", Range (0, 10)) = 1.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

        int perm(int d)
        {
            d = d % 256;
            float2 t = float2(d%16,d/16)/15.0;
            //return tex2D(permutation,t).r *255;
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

        float _NAmount;
      	float _Granularity;
      	float _NSpeed;

        void vert (inout appdata_full v) 
        {
            // lerp at edges to get rid of seam.
            float halfV = 100.0f;
            float normedZ = (v.vertex.y + halfV) / (halfV * 2.0f);
            float seamEdgeWidth = 0.05f;
            float start = smoothstep(0.0f, seamEdgeWidth, normedZ);
            float end = 1.0f - smoothstep(1.0f - seamEdgeWidth, 1.0f, normedZ);

            //float horizontalSeamEdgeWidth = 0.1f;
            //float normedX = (v.vertex.x + halfV) / (halfV * 2.0f);
            //float left = smoothstep(0.0f, horizontalSeamEdgeWidth, normedX);
            //float right = 1.0f - smoothstep(1.0f - horizontalSeamEdgeWidth, 1.0f, normedX); 

            float effect = (start * end /** left * right*/);

        	v.normal.xyz += noise ((sin(v.vertex.x * _Granularity) + _Time * _NSpeed), 
        						   (sin(v.vertex.y * _Granularity) + _Time * _NSpeed), 
        						   (sin(v.vertex.z * _Granularity) + _Time * _NSpeed))  * _NAmount * effect;

            //v.vertex.z += end * 5.0f;

        }

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

        float _NShift;
        float _TextureShiftGranularity;
		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
            float yScrollShift = _Time * sin(sin(_Time)) * 3.0;
            float xScrollShift = _Time * sin(sin(_Time * 2.0));
            float yScroll = (IN.uv_MainTex.y + yScrollShift) % 1.0;
            float xScroll = (IN.uv_MainTex.x + xScrollShift) % 1.0;
            float2 scrolledUV = IN.uv_MainTex;// + float2(xScroll, yScroll);
			fixed4 c = tex2D (_MainTex, scrolledUV) * _Color;

            float shift = noise(scrolledUV.x * _TextureShiftGranularity + _Time * 10.3, 
                                scrolledUV.y * _TextureShiftGranularity + _Time * 11.4 * sin(_Time), 
                                _TextureShiftGranularity + _Time * 10.7) * _NShift;
            float uMod = scrolledUV.x + shift % 1;
            float vMod = scrolledUV.y + (shift*2) % 1;
            fixed4 c2 = tex2D(_MainTex, float2(uMod, vMod)) * _Color;
            
            // lerp at edges to get rid of seam.
            float seamEdgeWidth = 0.1f;
            float start = smoothstep(0.0f, seamEdgeWidth, scrolledUV.y);
            const float maxY = 4.0f;
            float end = 1.0f - smoothstep(maxY - seamEdgeWidth, maxY, scrolledUV.y);

            float left = smoothstep(0.0f, seamEdgeWidth, scrolledUV.x);
            const float maxX = 4.0f;
            float right = 1.0f - smoothstep(maxX - seamEdgeWidth, maxX, scrolledUV.x);

            float noEffect = 1.0f - (start * end * left * right);
			o.Albedo = (noEffect * c.rgb) + (1.0f - noEffect) * (c.rgb * 0.2 + c2.rgb * 0.8);
			//o.Albedo = float3(noEffect, 0, 0);
            // Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
