using System.Collections;
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    public float timeBeforeDisable = 1.0f;
    public bool destroy = false;
    public bool fadeDestroy = true;

    private float m_Timer = 0.0f;

    private void OnEnable()
    {
        m_Timer = timeBeforeDisable;
    }

    private void Update()
    {
        m_Timer -= Time.deltaTime;

        if (m_Timer < 0.0f)
        {

            if (destroy)
            {
                if (fadeDestroy)
                    StartCoroutine(FadingDestroy());
                else
                    Destroy(gameObject);
            }
            else
                gameObject.SetActive(false);
        }
    }

    private IEnumerator FadingDestroy()
    {
        var renderers = transform.GetComponentsInChildren<Renderer>();
        var time = 2;
        foreach (var r in renderers)
            StartCoroutine(FadeTo(r, 0, time));

        yield return new WaitForSeconds(time + 0.1f);

        Destroy(gameObject);
    }

    IEnumerator FadeTo(Renderer renderer, float aValue, float aTime)
    {
        if (!renderer.material.HasProperty("_Color"))
            yield break;

        float alpha = renderer.material.color.a;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / aTime)
        {
            Color newColor = new Color(1, 1, 1, Mathf.Lerp(alpha, aValue, t));
            renderer.material.color = newColor;
            yield return null;
        }
    }
}
