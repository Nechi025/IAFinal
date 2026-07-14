using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;



public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public Leader leaderA;
    public Leader leaderB;

    public GameObject victoryPanel;
    public TMP_Text victoryText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public void OnLeaderDied(Leader deadLeader)
    {
        if (deadLeader == leaderA)
        {
            Debug.Log("Gano el B");
            EndGame("¡Equipo B gana!");
        }
        else if (deadLeader == leaderB)
        {
            Debug.Log("Gano el A");
            EndGame("¡Equipo A gana!");
        }
    }

    void EndGame(string winner)
    {
        Time.timeScale = 0f;

        victoryPanel.SetActive(true);

        victoryText.text = winner;
    }

}
