using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("--- Resources ---")]
    public int gp = 300;
    public int friends = 5;

    [Header("--- Status ---")]
    public int commuLv = 1;
    public int galLv = 1;
    public int lemonLv = 1;

    [Header("--- Game Progress ---")]
    public int currentGrade = 1;
    public int currentMonth = 4;
    public int currentTurn = 1;

    [Header("--- Counters ---")]
    public int soloPlayConsecutive = 0;
    public int boyfriendCount = 0;
    public int maleFriendCount = 0;
    public int eventForce = 0;

    public int studentIdCount = 0;
    public int present = 0;
    public int albumPrice = 0;
    public List<int> moveCards = new List<int>();

    public int gpIncreaseTileCount = 0;
    public int gpDecreaseTileCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int CalculateSalary(int shinyuCount)
    {
        return shinyuCount * 100;
    }
}