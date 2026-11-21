# Enum PluralCategory

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

The form a language uses for a number: English has two ([One](MicroEngine.Localization.PluralCategory.md) for 1, [Other](MicroEngine.Localization.PluralCategory.md) for the rest),
Russian four, Arabic six, Japanese one. A plural message gives a text per category
(<code>{count, plural, one {# gem} other {# gems}}</code>).

```csharp
public enum PluralCategory
```

## Fields

`Few = 3` 

The form for a few (2 to 4 in most Slavic languages).



`Many = 4` 

The form for many (5 and more in most Slavic languages).



`One = 1` 

The singular form.



`Other = 5` 

The general form; every language has it and every plural message must give it.



`Two = 2` 

The form for two, in languages that have one (Arabic, Hebrew, Slovenian).



`Zero = 0` 

The form for zero, in languages that have one (Arabic, Latvian).

</div>
