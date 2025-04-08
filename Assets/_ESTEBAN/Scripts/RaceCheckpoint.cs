using UnityEngine;

public class RaceCheckpoint : MonoBehaviour
{
    [SerializeField]
    private Transform m_checkpointFXspawnPos;

    [SerializeField]
    private GameObject m_checkpointFX;

    private bool m_hasBeenCrossed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_hasBeenCrossed)
            return;

        var newFX = Instantiate(
            m_checkpointFX,
            m_checkpointFXspawnPos.position,
            Quaternion.identity);

        Destroy(newFX, 2f);

        m_hasBeenCrossed = true;
    }
}
