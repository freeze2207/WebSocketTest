using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameplayController : Singleton<GameplayController>
{
    public enum GameStates
    {
        EGAME_DRAW,
        EGAME_GUESS,
        EGAME_SPEC,
    }
    // DEBUG_USE toggle between
    public bool mIsMaster = false;
    public GameStates gameState = GameStates.EGAME_SPEC;

    [System.Serializable]
    public class GameStateChangedEvent : UnityEvent<GameStates> { }
    public GameStateChangedEvent GameStateChanged;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameStates GetCurrentGameState()
    {
        return this.gameState;
    }
}
