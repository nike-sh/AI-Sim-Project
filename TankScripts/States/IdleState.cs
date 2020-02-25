using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : AbstractFiniteState
{
    [CreateAssetMenu(fileName ="IdleState"), menuName=]
    public override bool EnterState()
    {
         base.EnterState();
        Debug.Log("ENTERED IDLE STATE");
        return true;
    }
    public override void UpdateState()
    {
        Debug.Log("UPDATING IDLE STATE");
    }
    public override bool ExitState()
    {
        base.ExitState();
        Debug.Log("EXITING IDLE STATE");
        return true;

    }
}
