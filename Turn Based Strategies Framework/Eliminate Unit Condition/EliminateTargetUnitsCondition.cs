using System;
using System.Collections;
using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Grid.GameResolvers;
using TbsFramework.Units;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class UnitRecord
{
    public Unit Target;
    public int Goal;
    [ReadOnly] public int KilledTimes;
}

public class EliminateTargetUnitsCondition : GameEndCondition
{


    [Tooltip("The player who wins when clears this condition.")]
    public int TargetPlayerNumber = 0;
    public UnitRecord[] UnitRecords;

    void Awake()
    {
        GetComponent<CellGrid>().UnitAdded += OnUnitAdded;

        foreach (UnitRecord record in UnitRecords)
        {
            record.KilledTimes = 0;
        }

        UpdateDisplay();
    }

    protected void OnUnitAdded(object sender, UnitCreatedEventArgs e)
    {
        Unit unit = e.unit.GetComponent<Unit>();
        unit.UnitDestroyed += OnUnitDestroyed;
    }

    protected void OnUnitDestroyed(object sender, AttackEventArgs e)
    {
        string deadUnitName = e.Defender.gameObject.name;
        UnitRecord deadUnitRecord = UnitRecords.ToList().Find(r => FormatUnitName(r.Target.gameObject.name) == FormatUnitName(deadUnitName));
        if (deadUnitRecord == null) return;
        deadUnitRecord.KilledTimes += 1;
        UpdateDisplay();
        
        if(CheckEliminatedEnoughUnits()) GetComponent<CellGrid>().CheckGameFinished();
    }

    protected virtual void UpdateDisplay()
    {
        // MyGUIManager.Instance.UpdateClearConditionDisplay(UnitRecords);
    }

    protected string FormatUnitName(string unitName)
    {
        return unitName.Replace("(Clone)", "").Trim();
    }
    public override GameResult CheckCondition(CellGrid cellGrid)
    {
        bool result = CheckEliminatedEnoughUnits();

        if (result)
        {
            var playersAlive = new List<int>();
            playersAlive.Add(TargetPlayerNumber);
            var playersDead = cellGrid.Players.FindAll(p => p.PlayerNumber != TargetPlayerNumber)
                                                      .Select(p => p.PlayerNumber)
                                                      .ToList();
            return new GameResult(true, playersAlive, playersDead);

        }
        else
        {
            return new GameResult(false, null, null);

        }
    }

    protected bool CheckEliminatedEnoughUnits()
    {
        foreach (UnitRecord record in UnitRecords)
        {
            if (record.KilledTimes < record.Goal) return false;
        }
        return true;
    }
}
