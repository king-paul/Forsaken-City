using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableOnCollision : MonoBehaviour
{
    private void OnCollisionEnter2D()
    {
        gameObject.SetActive(false);
    }
}
