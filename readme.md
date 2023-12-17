## Strong in development, Expect dragons! 

### ILAwake
ILAwake - is a simple tool for generating boilerplate code.

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


### Feature list
Supported Methods:
- [AwakeGet] 
  - [x] GetComponent<T> for Reference Types
  - [x] GetComponents<T> for Array
  - [ ] GetComponents<T> for List<T>
- [AwakeGetChild]
  - [x] GetComponentInChildren<T> for Reference Types
  - [x] GetComponentsInChildren<T> for array
  - [ ] GetComponentsChildren<T> for List<T>.  With includeInactive argument
- [AwakeFind]
  - [x] FindObjectOfType<T> for Reference Types. With includeInactive argument
  - [x] FindObjectsOfType<T> for array. With includeInactive argument
  - [ ] FindObjectsOfType<T> for List<T>. With includeInactive argument
- ILViewer (Window->ILView)
  - [x] Search by class name 
  - [ ] Search by method name
  - [ ] Show C# equivalent 
