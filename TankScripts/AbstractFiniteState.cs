using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//https://www.youtube.com/watch?v=e8m6yXDIx9U
//https://www.youtube.com/watch?v=YdERlPfwUb0

public enum ExecutionState
{
    NONE,
    ACTIVE,
    COMPLETED

    //Idle,
    //Patrol,
    //AttackEnemy,
    //FindCover,
    //CollectConsumables,
    //DefendBase

}
public abstract class AbstractFiniteState : ScriptableObject
{
    public ExecutionState ExecutionState { get; protected set; }

    public abstract void UpdateState();

    public virtual void OnEnable()
    {
        ExecutionState = ExecutionState.NONE;
    }

    public virtual bool EnterState()
    {
        ExecutionState = ExecutionState.ACTIVE;
        return true;
    }

    public virtual bool ExitState()
    {
        ExecutionState = ExecutionState.COMPLETED;
        return true;
    }

    //public virtual void IdleState()
    //{
    //    ExecutionState = ExecutionState.Idle;
    //}

    //public virtual bool PatrolState()
    //{
    //    ExecutionState = ExecutionState.Patrol;
    //    return true;
    //}

    //public virtual bool AttackEnemyState()
    //{
    //    ExecutionState = ExecutionState.AttackEnemy;
    //    return true;

    //}

    //public virtual bool FindCoverState()
    //{
    //    ExecutionState = ExecutionState.FindCover;
    //    return true;
    //}

    //public virtual bool CollectConsumablesState()
    //{
    //    ExecutionState = ExecutionState.CollectConsumables;
    //    return true;
    //}
    //public virtual bool DefendBaseState()
    //{
    //    ExecutionState = ExecutionState.DefendBase;
    //    return true;
    //}
}
