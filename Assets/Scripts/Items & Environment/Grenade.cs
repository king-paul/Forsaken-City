using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] float damageRadius = 3.0f;
    [SerializeField] uint damageDealt = 5;    
    [SerializeField] float decellerationRate = 0.01f;
    [SerializeField] bool automaticallyDetonate = false;
    [SerializeField] float timeBeforeExplosion = 3.0f;

    [SerializeField] GameObject explosionPrefab;
    [SerializeField] SoundEffect hitObjectSound;

    private Rigidbody2D body;
    private float activeTime = 0;
    private bool hitObject;
    private new AudioSource audio;

    public Vector2 Direction { get; set; }
    public float ThrowPower { get; set; }

    // Start is called before the first frame update
    void Start()
    {       
        body = GetComponent<Rigidbody2D>();
        body.AddForce(Direction * ThrowPower);
        audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if(hitObject)
            body.velocity /= (1+ decellerationRate);

        if (automaticallyDetonate)
        {
            activeTime += Time.deltaTime;

            if (activeTime >= timeBeforeExplosion)
                Detonate();
        }
    }

    public void Detonate()
    {
        if (this == null)
            return;

        Vector3 explosionPos = new Vector3(transform.position.x, transform.position.y, -0.1f);
        var explosion = Instantiate(explosionPrefab, explosionPos, Quaternion.identity);
        //explosion.transform.localScale = new Vector3(damageRadius * 2, damageRadius * 2, 1);

        // damage all characters in area
        var collisions = Physics2D.OverlapCircleAll(transform.position, damageRadius);

        foreach (var collision in collisions)
        {
            if(collision.gameObject.layer == 3 && // Ground layer
               collision.gameObject.tag == "Breakable")
            {
                GameObject.Destroy(collision.gameObject);
            }

            // check if game object is the player
            if (collision.gameObject.layer == 6)
            {
                collision.gameObject.GetComponent<PlayerController>().TakeDamage(damageDealt);
            }

            // check if game object is an enemy
            if (collision.gameObject.layer == 7)
            {
                collision.gameObject.GetComponent<EnemyController>().TakeDamage(damageDealt);
            }
        }

        GameObject.Destroy(gameObject);        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        hitObject = true;

        if (hitObjectSound.audioClip != null)
            audio.PlayOneShot(hitObjectSound.audioClip, hitObjectSound.volume);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}