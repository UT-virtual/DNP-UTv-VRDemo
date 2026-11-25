// Scan line effect shared by ScanLine.shader and ModelTransition.shader. Functions only; the including shader declares
// the properties.
#ifndef DNPDEMO_SCANLINE_INCLUDED
#define DNPDEMO_SCANLINE_INCLUDED

// Brightness (0..1) of a thin band that runs from the top (height01 = 1) to the bottom (0) and starts again, speed times
// a second. width is the band's half height in the same 0..1 units.
float ScanLineBand(float height01, float time, float speed, float width)
{
    float position = 1.0 - frac(time * speed);
    return 1.0 - smoothstep(0.0, max(width, 1e-4), abs(height01 - position));
}

// Height of a world position inside the box boundsMin..boundsMax, 0 at the bottom and 1 at the top.
float ScanLineHeight(float3 positionWS, float3 boundsMin, float3 boundsMax)
{
    return saturate((positionWS.y - boundsMin.y) / max(boundsMax.y - boundsMin.y, 1e-3));
}

#endif
