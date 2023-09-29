using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField] bool destroyAfterTime = false;
    [SerializeField] float timeBeforeDestruction = 1.0f;

    private float timeActive = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (destroyAfterTime)
        {
            timeActive += Time.deltaTime;

            if (timeActive >= timeBeforeDestruction)
                GameObject.Destroy(gameObject);
        }
    }

    public void Destroy()
    {
        GameObject.Destroy(gameObject);
    }
}
