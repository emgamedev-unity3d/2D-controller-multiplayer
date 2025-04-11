using UnityEngine;
using UnityEngine.Events;

public class RaceCheckpoint : MonoBehaviour
{
    [SerializeField]
    private Transform m_checkpointFXspawnPos;

    [SerializeField]
    private GameObject m_checkpointFX;

    private bool m_hasBeenCrossed = false;

    [SerializeField]
    [Tooltip("Note: can be left null, to be the 1st checkpoint in the race.")]
    private RaceCheckpoint m_previousCheckpoint = null;

    public UnityEvent OnCrossedCheckpoint = new();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_hasBeenCrossed || !HasCrossedAllPreviousCheckpoints())
            return;

        InstantiateCrossCheckpointFX();

        m_hasBeenCrossed = true;
    }

    private bool HasCrossedAllPreviousCheckpoints()
    {
        // Note: using recursion to detect if all previous checkpoints have been crossed

        // base case
        if (m_previousCheckpoint == null)
            return true;

        // recursive call
        return
            m_previousCheckpoint.m_hasBeenCrossed && 
            m_previousCheckpoint.HasCrossedAllPreviousCheckpoints();
    }

    private void InstantiateCrossCheckpointFX()
    {
        if (m_checkpointFX == null)
            return;

        // NOTE:
        // The rest is not efficient code! Object pooling VFX objects is best practice
        var newFX = Instantiate(
            m_checkpointFX,
            m_checkpointFXspawnPos.position,
            Quaternion.identity);

        Destroy(newFX, 2f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(m_previousCheckpoint == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            m_previousCheckpoint.transform.position,
            transform.position);
    }
#endif
}