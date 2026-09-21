using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ST_Name : SmallTalkElements
{
    private TextMeshProUGUI text { get => GetComponent<TextMeshProUGUI>(); }

    protected override void OnMoveNext(DialogNode node)
    {
        text.text = node.logger_name;
    }
}
