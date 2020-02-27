using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//https://www.youtube.com/watch?v=e8m6yXDIx9U
//https://www.youtube.com/watch?v=YdERlPfwUb0

public enum tankState
{
    Idle,
    Patrol,
    AttackEnemy,
    FindCover,
    CollectConsumables,
    DefendBase

}
public abstract class AbstractFiniteState : ScriptableObject
{
    public ExecutionState executionState { get; protected set; }

    public virtual void IdleState()
    {
        executionState = ExecutionState.Idle;
    }

    public bool PatrolState()
    {
        executionState = ExecutionState.Patrol;
        return true;
    }

    public void AttackEnemyState()
    {
        executionState = ExecutionState.AttackEnemy;
        return true;

    }

    public virtual bool FindCoverState()
    {
        executionState = ExecutionState.FindCover;
        return true;
    }

    public virtual bool CollectConsumablesState()
    {
        executionState = ExecutionState.CollectConsumables;
        return true;
    }
    public virtual bool DefendBaseState()
    {
        executionState = ExecutionState.DefendBase;
        return true;
    }
}
