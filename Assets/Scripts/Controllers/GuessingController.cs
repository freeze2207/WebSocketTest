using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuessingController : Singleton<GuessingController>
{

    // Controller
    [SerializeField] private GameObject mGuessingPanel;

    private bool mCanGuess = false;



    // Start is called before the first frame update
    void Start()
    {
        GameplayController.Instance.GameStateChanged.AddListener(ChangeGuessState);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ChangeGuessState(GameplayController.GameStates _state)
    {
        this.mCanGuess = (_state == GameplayController.GameStates.EGAME_GUESS);
        this.mGuessingPanel.SetActive(this.mCanGuess);
    }
}
