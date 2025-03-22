using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveMe : MonoBehaviour
{
    // Start is called before the first frame update
    public void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
