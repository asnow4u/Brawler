using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum LevelType { Desert, Forest, Mountain, Jungle };

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public LevelType CurLevelType;
        

    //TODO: Need to track level progression, difficulty


    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }
}
