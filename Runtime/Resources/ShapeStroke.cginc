#ifndef RUSTAMKHONOFF_U_SHAPE_STROKE_INCLUDED
#define RUSTAMKHONOFF_U_SHAPE_STROKE_INCLUDED

float travelOnRoundedPanel(float2 p, float2 h, float4 r, float4 straight, float4 starts)
{
    float2 topLeft = r.x - (h - p * float2(-1., 1.));
    float2 topRight = r.y - (h - p);
    float2 bottomRight = r.z - (h - p * float2(1., -1.));
    float2 bottomLeft = r.w - (h + p);

    if (r.y > 0. && min(topRight.x, topRight.y) > 0.)
        return straight.x + r.y * atan2(topRight.x, topRight.y);
    if (r.z > 0. && min(bottomRight.x, bottomRight.y) > 0.)
        return starts.y + straight.y + r.z * atan2(bottomRight.y, bottomRight.x);
    if (r.w > 0. && min(bottomLeft.x, bottomLeft.y) > 0.)
        return starts.z + straight.z + r.w * atan2(bottomLeft.x, bottomLeft.y);
    if (r.x > 0. && min(topLeft.x, topLeft.y) > 0.)
        return starts.w + straight.w + r.x * atan2(topLeft.y, topLeft.x);

    float2 edge = h - abs(p);
    if (edge.x < edge.y)
    {
        if (p.x >= 0.) return starts.y + clamp(h.y - r.y - p.y, 0., straight.y);
        return starts.w + clamp(p.y + h.y - r.w, 0., straight.w);
    }
    if (p.y >= 0.) return clamp(p.x + h.x - r.x, 0., straight.x);
    return starts.z + clamp(h.x - r.z - p.x, 0., straight.z);
}

float projectedFraction(float2 p, float2 from, float2 to)
{
    float2 segment = to - from;
    return saturate(dot(p - from, segment) / dot(segment, segment));
}

float2 strokeTravel(Varyings i, bool uniformRadius)
{
    float2 h = i.instance.yz;
    float4 r = i.radii;
    float4 straight = max(0., h.xyxy * 2. - r - r.yzwx);
    float4 sections = straight + r.yzwx * 1.5707963267948966;
    float4 starts = float4(0., sections.x, sections.x + sections.y, sections.x + sections.y + sections.z);
    float total = starts.w + sections.w;
    float travel = travelOnRoundedPanel(i.positionInPanel, h, r, straight, starts);

    uint side = (uint)i.pointer.x;
    UNITY_BRANCH
    if (side != 0u)
    {
        float2 sideSize = sizeOnPointerSide(h, side);
        float halfWidth = i.pointer.z;
        float height = i.pointer.w;
        float direction = side == 2u || side == 3u ? -1. : 1.;
        float2 local = pointOnPointerSide(i.positionInPanel, side) - float2(i.pointer.y, sideSize.y);
        local.x *= direction;
        float baseStart = starts[side - 1u] + sideSize.x - r[side - 1u] + i.pointer.y * direction - halfWidth;
        float slopeLength = length(float2(halfWidth, height));
        float extra = 2. * (slopeLength - halfWidth);
        total += extra;

        if (local.y >= 0. || abs(local.x) < halfWidth)
        {
            float2 left = float2(-halfWidth, 0.);
            float2 tip = float2(0., height);
            float2 right = float2(halfWidth, 0.);
            float first = projectedFraction(local, left, tip);
            float second = projectedFraction(local, tip, right);
            float2 a = local - lerp(left, tip, first);
            float2 b = local - lerp(tip, right, second);
            float firstSquared = dot(a, a);
            float secondSquared = dot(b, b);
            float panel = panelDistance(i.positionInPanel, h, r, uniformRadius);
            bool baseIsHidden = abs(local.x) < halfWidth && abs(panel) >= abs(local.y);
            if (baseIsHidden || min(firstSquared, secondSquared) <= panel * panel)
            {
                float along = firstSquared <= secondSquared ? first : 1. + second;
                return float2(baseStart + along * slopeLength, total);
            }
        }

        if (travel > baseStart + halfWidth * 2.)
            travel += extra;
    }

    return float2(travel, total);
}

float strokePatternMask(uint pattern, float4 config, float2 travel, float width,
    float distanceFromMidline, float aa)
{
    if (pattern == 0u || (pattern == 1u && config.y <= .001)) return 1.;

    float mark = pattern == 2u ? width : config.x;
    float requested = mark + config.y;
    // Fit whole repetitions to the perimeter to avoid a broken mark at the wrap.
    float repeats = max(1., floor(travel.y / max(.1, requested) + .5));
    float step = travel.y / repeats;
    float fromCenter = abs(frac(travel.x / max(.001, step) - config.z + .5) - .5) * step;
    if (pattern == 2u)
        return coverage(length(float2(fromCenter, distanceFromMidline)) - min(width, step) * .5, aa * .65);

    float halfMark = .5 * step * mark / max(.1, requested);
    return coverage(fromCenter - halfMark, aa * .65);
}

#endif
