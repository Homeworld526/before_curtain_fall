using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStatMonitor{
    public int GetStat();
}

public abstract class DialogStatMonitorBase : IStatMonitor
{
    public MonoBehaviour _host;


    public virtual int GetStat()
    {
        return 0;
    }
}
