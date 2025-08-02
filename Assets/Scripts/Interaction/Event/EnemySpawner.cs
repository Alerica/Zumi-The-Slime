using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public GameObject enemyPrefab;

    public void SpawnEnemy()
    {
        if (enemyPrefab != null && spawnPoint != null)
        {
            Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }
}