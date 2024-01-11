using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObstacleCollide : MonoBehaviour
{
    public GameObject gameoverPopup;
    void OnTriggerEnter (Collider other)
    {
        Debug.Log("Obstacle hit by" + other.gameObject.name);
        gameoverPopup.GetComponent<GameoverPopup>().popupShown();
        gameoverPopup.SetActive(true);
        Time.timeScale = 0;
    }

    public void restartGame() {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}