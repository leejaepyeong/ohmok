using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField]
    private GameObject Ohmok;
    [SerializeField]
    private GameObject Sequence;

    public bool isStart = false;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    public void OnClickStart()
    {
        isStart = true;
        Ohmok.SetActive(true);
        Sequence.SetActive(true);
    }
}
