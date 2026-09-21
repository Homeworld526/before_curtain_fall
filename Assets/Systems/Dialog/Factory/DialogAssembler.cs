using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogAssembler : SingleCase<DialogAssembler>
{
    public int cnt;

    public void Init()
    {
        DialogList.Instance.AddChanger<ChangerDemo1>("Money", this);
        DialogList.Instance.AddMonitor<MonitorDemo1>("Money", this);
    }

}
