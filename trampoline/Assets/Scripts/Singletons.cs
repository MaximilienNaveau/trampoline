using UnityEngine;

public class Singletons : MonoBehaviour
{
    private FrenchDictionary frenchDictionary_ = new FrenchDictionary();

    public void Start()
    {
        frenchDictionary_.initialize();
    }

    public FrenchDictionary GetFrenchDictionary()
    {
        return frenchDictionary_;
    }
}
