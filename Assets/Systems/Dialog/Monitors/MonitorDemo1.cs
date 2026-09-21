using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MonitorDemo1 : DialogStatMonitorBase
{
    public override int GetStat()
    {
        return DialogAssembler.Instance.cnt;
    }
}
