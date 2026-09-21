using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangerDemo1 : DialogStatChangerBase
{
    public override void ChangeStat(int val)
    {
        DialogAssembler.Instance.cnt++;
    }
}
