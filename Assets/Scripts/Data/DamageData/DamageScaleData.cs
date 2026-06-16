using System;
using UnityEngine;

[Serializable]
public class DamageScaleData
{
    [Header("Damage")]
    public float phyiscal = 1f;
    public float elemental = 1f;

    [Header("Chill")]
    public float chillDuration = 3f;
    public float chillSlowMulitplier = .2f;

    [Header("Burn")]
    public float burnDuratin = 3f;
    public float burnDamageScale = 1f;

    [Header("Shock")]
    public float shockDuration = 3f;
    public float shockDamageScale = 1f;
    public float shockCharge = .4f;
}
