using UnityEngine;

public class PlayerPath : MonoBehaviour
{
    public GameObject nextTarger;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if(nextTarger != null)
            {
                nextTarger.SetActive(true);
            }

            gameObject.SetActive(false);
        }
    }
}
