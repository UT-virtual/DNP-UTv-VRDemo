// Dissolve effect shared by Dissolve.shader and ModelTransition.shader. The including shader declares the properties
// (inside its UnityPerMaterial buffer) and passes them in, so this file holds functions only.
#ifndef DNPDEMO_DISSOLVE_INCLUDED
#define DNPDEMO_DISSOLVE_INCLUDED

// Noise (0..1) at a world position, read from a tiling noise texture on the three axis planes and blended by the
// normal (triplanar), so it needs no UVs and stays put on a model that does not move. Scale is repeats per metre.
float DissolveNoise(TEXTURE2D_PARAM(noiseMap, noiseSampler), float3 positionWS, float3 normalWS, float scale)
{
    float3 weights = abs(normalWS);
    weights /= max(weights.x + weights.y + weights.z, 1e-4);
    float x = SAMPLE_TEXTURE2D(noiseMap, noiseSampler, positionWS.zy * scale).r;
    float y = SAMPLE_TEXTURE2D(noiseMap, noiseSampler, positionWS.xz * scale).r;
    float z = SAMPLE_TEXTURE2D(noiseMap, noiseSampler, positionWS.xy * scale).r;
    return x * weights.x + y * weights.y + z * weights.z;
}

// Discards the fragment where the noise is below cutoff: 0 shows everything, 1 hides everything. Returns how strongly
// the fragment lies on the burning edge (0..1, within edgeWidth above the cutoff); no edge while nothing is cut.
float ApplyDissolve(float noise, float cutoff, float edgeWidth)
{
    // Kept inside (0, 1) so that cutoff 0 and 1 are exactly "all shown" and "all hidden".
    float value = lerp(0.001, 0.999, saturate(noise));
    clip(value - cutoff);
    float edge = 1.0 - saturate((value - cutoff) / max(edgeWidth, 1e-4));
    return cutoff > 0.0 ? edge : 0.0;
}

#endif
