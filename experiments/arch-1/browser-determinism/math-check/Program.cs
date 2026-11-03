using System.Numerics;
// Replays Meadow Run's squash damping (StageView: _squash = MathX.Damp(_squash, One, 14, dt)) from a landing and
// prints each step's X with the library Lerp, a plain multiply-add and a fused one, plus MathF.Exp's bits.
float dt = 1f / 60f;
float e = MathF.Exp(-14f * dt);
float eDouble = (float)Math.Exp((double)(-14f * dt));
Console.WriteLine($"MathF.Exp={e:R} ({BitConverter.SingleToUInt32Bits(e):x8}); (float)Math.Exp={eDouble:R} ({BitConverter.SingleToUInt32Bits(eDouble):x8}); FMA hardware: {System.Runtime.Intrinsics.X86.Fma.IsSupported}");
float t = 1f - e;
var lib = new Vector2(1.35f, 0.7f);
float plain = 1.35f, fused = 1.35f;
for (int i = 1; i <= 16; i++)
{
    lib = Vector2.Lerp(lib, Vector2.One, t);
    plain = plain * (1 - t) + 1f * t;
    fused = MathF.FusedMultiplyAdd(fused, 1 - t, 1f * t);
    Console.WriteLine($"{i}: Lerp={lib.X:R} plain={plain:R} fused={fused:R}");
}
float pw = MathF.Pow(0.8f, 1f / 60f), pwd = (float)Math.Pow(0.8f, (double)(1f / 60f));
Console.WriteLine($"MathF.Pow(0.8,1/60)={pw:R} ({BitConverter.SingleToUInt32Bits(pw):x8}) double rounded={pwd:R} ({BitConverter.SingleToUInt32Bits(pwd):x8})");
int sinDiff = 0, cosDiff = 0, powDiff = 0, expDiff = 0, n = 0;
var rng = new Random(1);
for (int i = 0; i < 1_000_000; i++)
{
    float x = (float)(rng.NextDouble() * 2 * Math.PI);
    n++;
    if (MathF.Sin(x) != (float)Math.Sin(x)) sinDiff++;
    if (MathF.Cos(x) != (float)Math.Cos(x)) cosDiff++;
    float d = (float)rng.NextDouble();
    if (MathF.Pow(d, 1f / 60f) != (float)Math.Pow(d, (double)(1f / 60f))) powDiff++;
    float y = -(float)rng.NextDouble() * 5;
    if (MathF.Exp(y) != (float)Math.Exp(y)) expDiff++;
}
Console.WriteLine($"of {n}: MathF.Sin {sinDiff}, Cos {cosDiff}, Pow {powDiff}, Exp {expDiff} differ from the double result rounded to float");
