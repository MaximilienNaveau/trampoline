using UnityEngine;

public class Singletons : MonoBehaviour
{
    private FrenchDictionary frenchDictionary_ = new FrenchDictionary();

    public void Start()
    {
        frenchDictionary_.initialize(async: true);
    }

    public FrenchDictionary GetFrenchDictionary()
    {
        return frenchDictionary_;
    }
}
