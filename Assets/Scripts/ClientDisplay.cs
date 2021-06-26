using gamingCloud.Network.Voip;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientDisplay : MonoBehaviour
{

    public Sprite PTTEnable, PTTDisable;

    Text displayName;
    Image ptt;
    
    void Awake()
    {
        ptt = transform.GetChild(0).GetComponent<Image>();
        displayName = transform.GetChild(1).GetComponent<Text>();

        ptt.sprite = PTTDisable;
        displayName.color = Color.blue;
    }

    public void ChangePTTStatus(PTTStatus status)
    {
        if(status == PTTStatus.Enabled)
        {
            ptt.sprite = PTTEnable;
            displayName.color = new Color(0,97,0);
        }
        else
        {
            ptt.sprite = PTTDisable;
            displayName.color = Color.black;
        }
    }

    public void SetDisplayName(string name)
    {
        displayName.text = name;
    }
}
