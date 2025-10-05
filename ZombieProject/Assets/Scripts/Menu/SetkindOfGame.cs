using UnityEngine;

public class SetkindOfGame : MonoBehaviour
{
    public GameObject car;
    public GameObject player;



    public void SetCar()
    {
        car.SetActive(true);
        player.SetActive(false);
    }


    public void SetPlayer()
    {
        car.SetActive(false);
        player.SetActive(true);
    }

    public void Close()
    {

        Application.Quit();
    }
}
