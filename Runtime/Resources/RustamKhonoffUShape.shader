Shader "RustamKhonoff/UShape"
{
    Properties
    {
        [PerRendererData] _MainTex ("Gradients", 2D) = "white" {}
        _StyleTex ("Styles", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            CompFront [_StencilComp]
            CompBack [_StencilComp]
            PassFront Keep
            PassBack [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 positionInPanel : TEXCOORD0;
                float4 instance : TEXCOORD1; // Slot, half width, half height, writes stencil.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 positionInPanel : TEXCOORD0;
                nointerpolation float4 instance : TEXCOORD1;
                nointerpolation float4 radii : TEXCOORD2;
                nointerpolation float4 effects : TEXCOORD3;
                nointerpolation float4 fill : TEXCOORD4;
                nointerpolation float4 fillSpec : TEXCOORD5;
                nointerpolation float4 pointer : TEXCOORD7;
                nointerpolation float2 innerInset : TEXCOORD8;
                nointerpolation float4 borderStroke : TEXCOORD9;
                nointerpolation float4 outlineStroke : TEXCOORD10;
                float3 blink : TEXCOORD11; // Signed distance to band center, core half width, falloff.
                nointerpolation half4 blinkColor : TEXCOORD12;
                #ifdef UNITY_UI_CLIP_RECT
                float4 mask : TEXCOORD6;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _StyleTex, _MainTex;
            half4 _Color;
            float _UShapeBlinkTime;
            int _UIVertexColorAlwaysGammaSpace;
            float _ColorMask, _StencilOp;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            // Matches StyleWidth and Capacity in PanelAtlas. Unfiltered reads are safe at texel centers.
            float4 style(float slot, float entry)
            {
                return tex2Dlod(_StyleTex, float4((entry + .5) / 34., (slot + .5) / 64., 0, 0));
            }

            // Express each side in coordinates where the pointer extends along positive Y.
            float2 pointOnPointerSide(float2 p, uint side)
            {
                if (side == 2u) return p.yx;
                if (side == 3u) return float2(p.x, -p.y);
                if (side == 4u) return float2(p.y, -p.x);
                return p;
            }

            float4 radiiOnPointerSide(float4 r, uint side)
            {
                if (side == 2u) return r.zyxw;
                if (side == 3u) return r.wzyx;
                if (side == 4u) return r.wxyz;
                return r;
            }

            float2 sizeOnPointerSide(float2 halfSize, uint side)
            {
                return side == 2u || side == 4u ? halfSize.yx : halfSize;
            }

            float4 fitPointer(float4 data, float2 halfSize, float4 radii)
            {
                uint side = (uint)data.x;
                float halfEdge = sizeOnPointerSide(halfSize, side).x;
                float2 edgeRadii = radiiOnPointerSide(radii, side).xy;
                float left = -halfEdge + edgeRadii.x;
                float right = halfEdge - edgeRadii.y;
                float halfWidth = .5 * min(data.z, right - left);
                if (halfWidth <= .0001) return 0.;
                float center = clamp(lerp(-halfEdge, halfEdge, data.y), left + halfWidth, right - halfWidth);
                return float4(data.x, center, halfWidth, data.w);
            }

            float4 animatedStroke(float slot, float entry, uint mode)
            {
                UNITY_BRANCH
                if (mode == 0u) return float4(0., 0., 0., 1.);

                float4 config = style(slot, entry);
                uint motion = mode >> 2u;
                float alpha = 1.;
                if (motion == 1u)
                    config.z = frac(config.z + _Time.y * config.w);
                else if (motion == 2u && config.w != 0.)
                    alpha = .675 + .325 * cos(frac(config.z + _Time.y * config.w) * 6.28318530718);
                return float4(config.xyz, alpha);
            }

            void animatedBlink(float slot, float2 halfSize, float2 p, float4 pointer,
                out float3 blink, out half4 blinkColor)
            {
                blink = 0;
                blinkColor = 0;
                float3 timing = style(slot, 33.).xyz;
                float elapsed = _UShapeBlinkTime - timing.x;
                if (elapsed < 0.) return;
                float phase = fmod(elapsed, timing.y + timing.z);
                if (phase >= timing.y) return;

                float4 band = style(slot, 31.);
                float extent = dot(halfSize, abs(band.xy));
                float low = -extent;
                float high = extent;
                uint side = (uint)pointer.x;
                if (side != 0u)
                {
                    float2 tip = float2(pointer.y, sizeOnPointerSide(halfSize, side).y + pointer.w);
                    // Project the tip and travel direction in the same side coordinates.
                    float projectedTip = dot(tip, pointOnPointerSide(band.xy, side));
                    low = min(low, projectedTip);
                    high = max(high, projectedTip);
                }

                float tail = band.z + band.w;
                float center = lerp(low - tail, high + tail, phase / timing.y);
                blink = float3(dot(p, band.xy) - center, band.zw);
                blinkColor = style(slot, 32.);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.positionInPanel = v.positionInPanel;
                o.instance = v.instance;
                #ifndef UNITY_COLORSPACE_GAMMA
                if (_UIVertexColorAlwaysGammaSpace)
                    v.color.rgb = GammaToLinearSpace(v.color.rgb);
                #endif
                o.color = v.color * _Color;

                float slot = v.instance.x;
                float2 size = v.instance.yz * 2.;
                float4 radii = style(slot, 0.);
                float2 horizontal = max(float2(radii.x + radii.y, radii.w + radii.z), .001);
                float2 vertical = max(float2(radii.x + radii.w, radii.y + radii.z), .001);
                float factor = min(1., min(size.x / max(horizontal.x, horizontal.y), size.y / max(vertical.x, vertical.y)));
                o.radii = radii * factor;
                o.effects = style(slot, 1.);
                o.fill = style(slot, 2.);
                uint flags = (uint)o.effects.z;
                o.borderStroke = animatedStroke(slot, 29., o.effects.x > 0. ? (flags >> 5u) & 15u : 0u);
                o.outlineStroke = animatedStroke(slot, 30., o.effects.y > 0. ? (flags >> 9u) & 15u : 0u);
                o.fillSpec = 0;
                UNITY_BRANCH
                if ((flags & 4u) != 0u)
                    o.fillSpec = style(slot, 3.);

                o.pointer = 0;
                UNITY_BRANCH
                if ((flags & 16u) != 0u)
                    o.pointer = fitPointer(style(slot, 27.), v.instance.yz, o.radii);

                o.blink = 0;
                o.blinkColor = 0;
                UNITY_BRANCH
                if ((flags & 8192u) != 0u)
                    animatedBlink(slot, v.instance.yz, v.positionInPanel, o.pointer, o.blink, o.blinkColor);

                o.innerInset = 0;
                UNITY_BRANCH
                if ((uint)o.effects.w >= 8u)
                {
                    float largestRadius = max(max(o.radii.x, o.radii.y), max(o.radii.z, o.radii.w));
                    o.innerInset = style(slot, 28.).xy + largestRadius;
                }

                #ifdef UNITY_UI_CLIP_RECT
                float4 bounds = clamp(_ClipRect, -2e10, 2e10);
                float2 pixelSize = o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                o.mask = float4(v.vertex.xy * 2. - bounds.xy - bounds.zw,
                    .25 / (.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                #endif
                return o;
            }

            float panelDistance(float2 p, float2 halfSize, float4 radii, bool uniformRadius)
            {
                float2 edge = halfSize - abs(p);
                UNITY_BRANCH
                if (uniformRadius)
                {
                    float2 q = radii.x - edge;
                    return length(max(q, 0.)) + min(max(q.x, q.y), 0.) - radii.x;
                }
                // Use each arc center, since an unequal radius can extend past the panel midpoint.
                float2 topLeft = halfSize - p * float2(-1., 1.);
                float2 topRight = halfSize - p;
                float2 bottomRight = halfSize - p * float2(1., -1.);
                float2 bottomLeft = halfSize + p;
                if (max(topLeft.x, topLeft.y) < radii.x)
                    return length(radii.x - topLeft) - radii.x;
                if (max(topRight.x, topRight.y) < radii.y)
                    return length(radii.y - topRight) - radii.y;
                if (max(bottomRight.x, bottomRight.y) < radii.z)
                    return length(radii.z - bottomRight) - radii.z;
                if (max(bottomLeft.x, bottomLeft.y) < radii.w)
                    return length(radii.w - bottomLeft) - radii.w;
                return -min(edge.x, edge.y);
            }

            float segmentDistanceSquared(float2 p, float2 a, float2 b)
            {
                float2 delta = b - a;
                float2 relative = p - a;
                float2 nearest = relative - delta * saturate(dot(relative, delta) / max(dot(delta, delta), .000001));
                return dot(nearest, nearest);
            }

            float arcDistanceSquared(float2 p, float2 center, float radius, float2 direction)
            {
                float2 q = (p - center) * direction;
                if (min(q.x, q.y) >= 0.)
                {
                    float d = length(q) - radius;
                    return d * d;
                }

                float2 endpoint = q.x > q.y ? float2(radius, 0.) : float2(0., radius);
                float2 delta = q - endpoint;
                return dot(delta, delta);
            }

            float otherPanelEdgesSquared(float2 p, float2 halfSize, float4 radii, float nearest)
            {
                float x = halfSize.x;
                float y = halfSize.y;
                nearest = min(nearest, segmentDistanceSquared(p, float2(-x, -y + radii.w), float2(-x, y - radii.x)));
                nearest = min(nearest, segmentDistanceSquared(p, float2(x, -y + radii.z), float2(x, y - radii.y)));
                nearest = min(nearest, segmentDistanceSquared(p, float2(-x + radii.w, -y), float2(x - radii.z, -y)));
                nearest = min(nearest, arcDistanceSquared(p, float2(-x + radii.x, y - radii.x), radii.x, float2(-1., 1.)));
                nearest = min(nearest, arcDistanceSquared(p, float2(x - radii.y, y - radii.y), radii.y, float2(1., 1.)));
                nearest = min(nearest, arcDistanceSquared(p, float2(x - radii.z, -y + radii.z), radii.z, float2(1., -1.)));
                return min(nearest, arcDistanceSquared(p, float2(-x + radii.w, -y + radii.w), radii.w, float2(-1., -1.)));
            }

            float shapeDistance(Varyings i, float2 p, bool uniformRadius)
            {
                float panel = panelDistance(p, i.instance.yz, i.radii, uniformRadius);
                uint side = (uint)i.pointer.x;
                UNITY_BRANCH
                if (side == 0u) return panel;

                float2 halfSize = sizeOnPointerSide(i.instance.yz, side);
                float2 oriented = pointOnPointerSide(p, side);
                float2 local = oriented - float2(i.pointer.y, halfSize.y);
                float halfWidth = i.pointer.z;
                float height = i.pointer.w;
                bool overBase = abs(local.x) < halfWidth;

                // Below the pointer, an edge closer than its base cannot be replaced by either slope.
                if (local.y < 0. && (!overBase || abs(panel) < -local.y)) return panel;

                float2 tip = float2(0., height);
                float nearest = min(segmentDistanceSquared(local, float2(-halfWidth, 0.), tip),
                    segmentDistanceSquared(local, float2(halfWidth, 0.), tip));
                bool insidePointer = local.y >= 0. && local.y <= height * (1. - abs(local.x) / halfWidth);
                if (!overBase)
                {
                    nearest = min(nearest, panel * panel);
                }
                else if (local.y < 0.)
                {
                    // The flat base is internal. Only the other edges and corner arcs survive.
                    nearest = otherPanelEdgesSquared(oriented, halfSize, radiiOnPointerSide(i.radii, side), nearest);
                }
                return (insidePointer || panel < 0. ? -1. : 1.) * sqrt(nearest);
            }

            float coverage(float distance, float aa)
            {
                return 1. - smoothstep(-aa, aa, distance);
            }

            #include "ShapeStroke.cginc"

            half4 samplePaint(Varyings i, float slot, float lane, float4 tint, float4 spec)
            {
                if (spec.x < .5) return tint;
                float4 transform = style(slot, 4. + lane * 3.);
                float2 q = (i.positionInPanel / (i.instance.yz * 2.) + .5 - transform.xy) / transform.zw;
                q = float2(q.x * spec.y + q.y * spec.z, -q.x * spec.z + q.y * spec.y);
                float type = fmod(spec.x, 8.);
                float t = q.x + .5;
                if (type == 2.) t = length(q) * 2.;
                else if (type == 3.) t = atan2(q.y, q.x) / 6.28318530718 + 1.;
                else if (type == 4.) t = (abs(q.x) + abs(q.y)) * 2.;
                t = type == 3. ? frac(t + spec.w) : saturate(t + spec.w);
                float x = t * 255.;
                if (spec.x >= 8.) x = floor(x + .5);
                return tex2Dlod(_MainTex, float4((x + .5) / 256., (slot * 3. + lane + .5) / 192., 0, 0)) * tint;
            }

            half4 premultiply(half4 color, float mask)
            {
                half alpha = saturate(color.a) * mask;
                return half4(color.rgb * alpha, alpha);
            }

            half4 over(half4 under, half4 top) { return top + under * (1. - top.a); }

            half4 composeShadows(Varyings i, float aa, bool uniformRadius, int count, bool outer)
            {
                // Atlas layout matches OuterShadowStart and InnerShadowStart in PanelAtlas.
                float start = outer ? 11. : 19.;
                half4 stack = 0;
                for (int layer = count - 1; layer >= 0; --layer)
                {
                    float entry = start + layer * 2.;
                    float4 shadow = style(i.instance.x, entry + 1.);
                    float distance = shapeDistance(i, i.positionInPanel - shadow.xy, uniformRadius);
                    float mask = outer
                        ? coverage(distance - shadow.w, max(aa, shadow.z))
                        : 1. - coverage(distance + shadow.w, max(aa, shadow.z));
                    stack = over(stack, premultiply(style(i.instance.x, entry), mask));
                }
                return stack;
            }

            half4 frag(Varyings i, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float slot = i.instance.x;
                uint flags = (uint)i.effects.z;
                bool uniformRadius = (flags & 8u) != 0u;
                uint shadowCounts = (uint)i.effects.w;
                int outerCount = (int)(shadowCounts & 7u);
                int innerCount = (int)(shadowCounts >> 3u);
                float distance = shapeDistance(i, i.positionInPanel, uniformRadius);
                // Local pixel size stays stable across UV discontinuities at adjacent panels.
                float aa = max(.0001, max(length(ddx(i.positionInPanel)), length(ddy(i.positionInPanel))));
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                #endif

                UNITY_BRANCH
                if (i.instance.w > .5)
                {
                    if (!frontFace)
                    {
                        #ifdef UNITY_UI_ALPHACLIP
                        // The silhouette also clears the same bits on the uGUI pop draw.
                        clip(_StencilOp - .5);
                        clip(-distance);
                        #ifdef UNITY_UI_CLIP_RECT
                        clip(clipFactor.x * clipFactor.y - .001);
                        #endif
                        return 0.;
                        #else
                        clip(-1.);
                        #endif
                    }
                    clip(_ColorMask - .5);
                }
                float body = coverage(distance, aa);
                float interior = i.effects.x > 0. ? coverage(distance + i.effects.x, aa) : body;
                float border = body - interior;
                float outline = i.effects.y > 0. && body < 1. ? coverage(distance - i.effects.y, aa) - body : 0.;

                uint borderMode = (flags >> 5u) & 15u;
                uint outlineMode = (flags >> 9u) & 15u;
                uint borderPattern = borderMode & 3u;
                uint outlinePattern = outlineMode & 3u;
                float2 travel = 0.;
                UNITY_BRANCH
                if ((border > 0. && borderPattern != 0u) || (outline > 0. && outlinePattern != 0u))
                    travel = strokeTravel(i, uniformRadius);

                UNITY_BRANCH
                if (border > 0. && borderPattern != 0u)
                {
                    border *= strokePatternMask(borderPattern, i.borderStroke, travel,
                        i.effects.x, distance + i.effects.x * .5, aa);
                    interior = body - border;
                }

                half4 surface = 0;
                if (interior > 0.)
                {
                    half4 fill = premultiply(samplePaint(i, slot, 0., i.fill, i.fillSpec), 1.);
                    // In this core every shifted point is far enough inside the rounded panel.
                    // Its inner-shadow mask is exactly zero for all enabled layers.
                    UNITY_BRANCH
                    if (innerCount > 0 && any(i.instance.yz - abs(i.positionInPanel) < i.innerInset + aa))
                        fill = over(fill, composeShadows(i, aa, uniformRadius, innerCount, false));
                    surface += fill * interior;
                }

                if (border > 0.)
                    surface += premultiply(samplePaint(i, slot, 1., style(slot, 5.), style(slot, 6.)), border * i.borderStroke.w);

                UNITY_BRANCH
                if (i.blinkColor.a > 0. && surface.a > 0.)
                {
                    float inner = max(0., i.blink.y - aa);
                    float outer = i.blink.y + i.blink.z;
                    float mask = 1. - smoothstep(inner, outer, abs(i.blink.x));
                    surface.rgb = lerp(surface.rgb, i.blinkColor.rgb * surface.a, saturate(i.blinkColor.a) * mask);
                }

                if (outline > 0.)
                {
                    UNITY_BRANCH
                    if (outlinePattern != 0u)
                        outline *= strokePatternMask(outlinePattern, i.outlineStroke, travel,
                            i.effects.y, distance - i.effects.y * .5, aa);
                    if (outline > 0.)
                        surface += premultiply(samplePaint(i, slot, 2., style(slot, 8.), style(slot, 9.)), outline * i.outlineStroke.w);
                }

                if (outerCount > 0 && body < 1.)
                {
                    half4 outer = composeShadows(i, aa, uniformRadius, outerCount, true);
                    surface = over(outer * (1. - body), surface);
                }

                surface.rgb *= i.color.rgb * i.color.a;
                surface.a *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                surface *= clipFactor.x * clipFactor.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(surface.a - .001);
                #endif
                return surface;
            }
            ENDCG
        }
    }
}
