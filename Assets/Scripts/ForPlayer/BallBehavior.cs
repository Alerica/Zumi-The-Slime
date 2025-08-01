using UnityEngine;
using System.Collections;

public class BallBehavior : MonoBehaviour
{
    [Header("Ball Properties")]
    public int colorIndex;
    public float lifetime = 10f;

    [Header("Break Effect")]
    [Tooltip("Assign your ParticleSystem prefab here (Play On Awake).")]
    public ParticleSystem breakEffectPrefab;

    private bool hasCollided = false;

    void Start()
    {
        // Instead of Destroy(gameObject, lifetime),
        // run our own coroutine so we can spawn the effect:
        StartCoroutine(AutoDestroy());
    }

    IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(lifetime);
        HandleBreak(transform.position, Vector3.up);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleBreak(collision.contacts[0].point, collision.contacts[0].normal);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleBreak(transform.position, Vector3.up);
    }

    private void HandleBreak(Vector3 position, Vector3 normal)
    {
        if (hasCollided) return;
        hasCollided = true;

        if (breakEffectPrefab != null)
        {
            Instantiate(
                breakEffectPrefab,
                position,
                Quaternion.LookRotation(normal)
            );
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // If something else destroyed us (e.g. your shooter code)
        // and we never collided or timed out yet, still play the effect:
        if (!hasCollided && breakEffectPrefab != null)
        {
            Instantiate(
                breakEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }
    }
}