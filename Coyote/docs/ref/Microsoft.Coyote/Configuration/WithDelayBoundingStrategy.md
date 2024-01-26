# Configuration.WithDelayBoundingStrategy method

Updates the configuration to use the delay-bounding exploration strategy during systematic testing. You can specify if you want to enable liveness checking, which is disabled by default, and an upper bound of possible delays, which by default can be up to 10.

```csharp
public Configuration WithDelayBoundingStrategy(bool isFair = false, uint delayBound = 10)
```

| parameter | description |
| --- | --- |
| isFair | If true, enable liveness checking by using fair scheduling. |
| delayBound | Upper bound of possible priority delays per test iteration. |

## Remarks

Note that explicitly setting this strategy disables the default exploration mode that uses a tuned portfolio of strategies.

## See Also

* class [Configuration](../Configuration.md)
* namespace [Microsoft.Coyote](../Configuration.md)
* assembly [Microsoft.Coyote](../../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->