using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Tilemaps;
using System.Text.RegularExpressions;
using UnityEngine;


[RequireComponent(typeof(IEffect))]
public class EffectTracker : ContentTracker
{
    public List<string> effectType;
    
    protected override void TrackContent(DialogNode xx)
    {
        if (type == DialogColumn.backup)
        {
            string[] x = xx.note.Split('+');
            for (int i = 0; i < x.Length; i++)
            {
                //Debug.Log(x[i]);
                if (x[i].Contains(target))
                {
                    foreach (string t in effectType)
                    {
                        Match match = Regex.Match(x[i], t);
                        
                        if (match.Success)
                        {
                            foreach(IEffect e in GetComponents<IEffect>())
                            {
                                e.StartEffect(match.Value);
                            }
                            break;
                        }
                    }
                }
                
            }
        }
        if(type == DialogColumn.potrait)
        {
            string x = xx.illustration_opponent;
            if (x == target)
            {
                foreach (string t in effectType)
                {
                    foreach (IEffect e in GetComponents<IEffect>())
                    {
                        e.StartEffect(t);
                    }
                }
            }
        }
        if(type == DialogColumn.music)
        {
            string x = xx.music;
            var clip = Resources.Load<AudioClip>(x);
            if (clip != null) SoundsManager.Instance.SetMusicFile(clip);

        }
        
    }
}
