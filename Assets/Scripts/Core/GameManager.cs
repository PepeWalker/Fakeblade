using UnityEngine;

// GameManager.cs 
//Ser� el controlador principal del juego

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    //Estos valoresa se podr�n personalizar al iniciar partida.
    [Header("Game Settings")]
    public float matchDuration = 180f; //por ahora valor placeholder
    public int maxPlayers = 4;
    public GameState currentState;



    // Gesti�n de estados del juego, inicializaci�n, victoria/derrota, etcs






    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
