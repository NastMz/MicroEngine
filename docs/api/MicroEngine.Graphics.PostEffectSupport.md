# Enum PostEffectSupport

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

What a post-processing effect does on a graphics device ([SupportOn](MicroEngine.Graphics.PostEffect.md#MicroEngine_Graphics_PostEffect_SupportOn_MicroEngine_Graphics_IGraphicsDevice_)).

```csharp
public enum PostEffectSupport
```

## Fields

`Full = 0` 

The effect as its type describes it.



`None = 2` 

Nothing: the picture passes through the effect unchanged.



`Reduced = 1` 

A simpler form the effect's type states, made of what every device has (render targets, the four blend states,
linear sampling): what an effect with a shader draws where shaders do not run.

</div>
