using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    [Header("Goal Settings")]
    [SerializeField] private bool requiresAllEnemiesKilled = false;
    [SerializeField] private ParticleSystem goalParticles;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (CanCompleteLevel())
            {
                CompleteLevel();
            }
        }
    }

    private bool CanCompleteLevel()
    {
        if (requiresAllEnemiesKilled)
        {
            Enemy[] remainingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            return remainingEnemies.Length == 0;
        }

        return true;
    }

    private void CompleteLevel()
    {
        if (goalParticles != null)
        {
            goalParticles.Play();
        }

        GameManager.Instance?.CompleteLevel();
    }
}