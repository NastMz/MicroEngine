# Enum MessageArgumentKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

How a message uses one of its arguments.

```csharp
public enum MessageArgumentKind
```

## Fields

`Plural = 1` 

The number chooses a plural form: <code>{count, plural, one {...} other {...}}</code>.



`Select = 2` 

The text chooses a branch: <code>{who, select, hero {...} other {...}}</code>.



`Value = 0` 

The value is shown: <code>{name}</code>.

</div>
