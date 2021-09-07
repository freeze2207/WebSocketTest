using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameplayController : Singleton<GameplayController>
{
    public enum GameStates
    {
        EGAME_NOTREADY,
        EGAME_DRAW,
        EGAME_GUESS,
        EGAME_SPEC,
    }
    // DEBUG_USE toggle between
    public bool mIsMaster = false;
    private GameStates mGameState = GameStates.EGAME_NOTREADY;

    [System.Serializable]
    public class GameStateChangedEvent : UnityEvent<GameStates> { }
    public GameStateChangedEvent GameStateChanged;

    // Start is called before the first frame update
    void Start()
    {
        WSConnectionController.Instance.ConnectionStatusChanged.AddListener(ChageGameState);
    }

    private void ChageGameState()
    {
        this.mGameState = GameStates.EGAME_DRAW;
        GameStateChanged.Invoke(this.mGameState);
    }

    public GameStates GetCurrentGameState()
    {
        return this.mGameState;
    }


}
