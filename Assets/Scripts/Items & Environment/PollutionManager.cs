using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PollutionManager : MonoBehaviour
{
    [Header("Damage to Player")]
    [SerializeField] float timePerImpact = 1;
    [SerializeField] uint damagePerImpact = 1;

    [Header("Pollution Texture")]
    [SerializeField] Image[] textureAreas;
    [SerializeField] Vector2 scrollSpeed = new Vector2(0.05f, 0.05f);

    // properties
    public static PollutionManager Instance { get; private set; }
    public float TimePerImpact => timePerImpact;
    public uint DamagePerImpact => damagePerImpact;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (Image texture in textureAreas)
        {
            texture.material = new Material(texture.material);
        }
    }

    private void FixedUpdate()
    {
        foreach (Image texture in textureAreas)
        {
            texture.material.mainTextureOffset += scrollSpeed * Time.fixedDeltaTime;
        }
    }

}
