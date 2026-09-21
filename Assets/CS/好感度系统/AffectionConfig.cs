using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 好感度配置
[CreateAssetMenu(fileName = "AffectionConfig", menuName = "Visual Novel/Affection Config")]
public class AffectionConfig : ScriptableObject
{
    public string characterName;
    public Sprite characterIcon;
    
}

