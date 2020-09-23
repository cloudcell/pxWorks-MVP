using UnityEngine;

public class DoNotDestroyOnLoad : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}