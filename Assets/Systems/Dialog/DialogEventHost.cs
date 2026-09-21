using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DialogEventHost : MonoBehaviour, IDialogAction
{
    public string playerScript;

    protected virtual void DialogEndEvent(string param)
    {

    }
    

    public void AddAction()
    {
        DialogList.Instance.act.Add(DialogEndEvent);
    }
}
