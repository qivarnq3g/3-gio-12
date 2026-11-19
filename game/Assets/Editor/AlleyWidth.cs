using UnityEngine;

public static class AlleyWidth
{
    public const float OldHalf = 1.25f;
    public const float Half = 1.75f;
    public const float Shift = Half - OldHalf;
    public const float Core = .35f;

    public static float Weight(Vector3 p)
    {
        if (Mathf.Abs(p.x) > 14.5f) return 0;
        if (p.z > .3f) return Mathf.Clamp01((.9f - p.z) / .6f);
        if (p.z < -48f) return Mathf.Clamp01((p.z + 48.6f) / .6f);
        return 1;
    }

    public static float X(float x) => Mathf.Abs(x) < Core ? x * (Core + Shift) / Core : x + Mathf.Sign(x) * Shift;

    public static Vector3 Offset(Vector3 p) => new Vector3(Weight(p) * (X(p.x) - p.x), 0, 0);

    public static Vector3 Warp(Vector3 p) => p + Offset(p);
}
