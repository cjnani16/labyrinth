using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AIInfoPanel : MonoBehaviour
{
    public GameObject EntityObject;
    public Text TextName;

    BeAnEntity Entity;

    // Start is called before the first frame update
    void Start()
    {
        //Attach to an Entity;
        bool attachSuccessful = false;
        if (Entity = EntityObject.GetComponent<BeAnEntity>()) {
            Debug.Log("Panel attached to Entity " + Entity);
            attachSuccessful = true;
        }
        else {
            Debug.LogError("Panel failed to attach to Entity! Target = " + EntityObject);
        }
        if (!attachSuccessful) return;

        //Write static info to display
        TextName.text = Entity.EntityName;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
