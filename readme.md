### ILAwake
ILAwake - is a simple tool for generating boiler plate code.

### How to add
Add to package manifest: 
```
    "com.zen12.ilawake": "git@github.com:Zen12/ILAwake.git",
```

### How to it works
It inserts IL-Code in Awake method (or creates and inserts). 
It doesn't change your source code. 

For Example:

Original code:
```C#
using ILAwake.Runtime;
using UnityEngine;

public class LogTestBehaviour : MonoBehaviour
{
    [AwakeGet] private Transform _tr;
    [AwakeGet] private Collider _collider;
}

```

To (C# equivalent):
```C#
using ILAwake.Runtime;
using UnityEngine;

public class LogTestBehaviour : MonoBehaviour
{
    [AwakeGet] private Transform _tr;
    [AwakeGet] private Collider _collider;
    
    private void Awake()
    {
        _tr = GetComponent<Transform>();
        _collider = GetComponent<Collider>();
    }
}
```

In case if there is Awake already. Here how it will look:

Original code:
```C#
using ILAwake.Runtime;
using UnityEngine;

public class LogTestBehaviour : MonoBehaviour
{
    [AwakeGet] private Transform _tr;
    [AwakeGet] private Collider _collider;
    
    private void Awake()
    {
        Debug.Log("Awake");
    }
}

```

To (C# equivalent):
```C#
using ILAwake.Runtime;
using UnityEngine;

public class LogTestBehaviour : MonoBehaviour
{
    [AwakeGet] private Transform _tr;
    [AwakeGet] private Collider _collider;
    
    private void Awake()
    {
        _tr = GetComponent<Transform>();
        _collider = GetComponent<Collider>();
        Debug.Log("Awake");
    }
}
```