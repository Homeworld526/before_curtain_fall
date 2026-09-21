using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStatChanger
{
    public void ChangeStat(int val);
}

public abstract class DialogStatChangerBase : IStatChanger
{
    public MonoBehaviour _host;

    public virtual void ChangeStat(int val)
    {
        
    }

}
