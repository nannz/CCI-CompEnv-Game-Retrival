using Gamekit3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyControllerStateSetterSMB : StateMachineBehaviour
{
    public string State;//will link this state with the EnemyController Script.

    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var controller = animator.gameObject.GetComponent<EnemyController>();
        if (controller == null) return;
        controller.State = State;
    }

    //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var controller = animator.gameObject.GetComponent<EnemyController>();
        if (controller == null) return;
        controller.State = "";
    }
}
