# Interface ISaveStorage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Saving data](../manual/persistence.md)  

Where a [SaveStore](MicroEngine.Core.SaveStore.md) keeps its slots: the platform's storage contract
([IStorage](MicroEngine.Platform.IStorage.md): named texts that are written whole, failures as `IOException` or
`UnauthorizedAccessException`) under the name saves have always used.

```csharp
public interface ISaveStorage : IStorage
```

#### Implements

IStorage

</div>
