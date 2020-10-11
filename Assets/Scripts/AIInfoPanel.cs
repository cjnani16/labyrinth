using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[System.Serializable]
public class AIInfoPanel : MonoBehaviour
{
    public GameObject EntityObject;
    public TMPro.TextMeshProUGUI NameText, IsAText, GoalText, MemoryText, PerceptualFactsText;
    public TMPro.TMP_InputField NewGoalField, NewSentenceField;

    AIKit.IsA EntityIsA;
    BeAnEntity EntityIF;
    AIKit.Entity Entity;

    // Start is called before the first frame update
    void Start()
    {
        //Attach to an Entity;
        bool attachSuccessful = false;
        if (EntityIF = EntityObject.GetComponent<BeAnEntity>()) {
            Debug.Log("AI Info panel attached to Entity " + EntityIF.EntityName);
            Entity = EntityIF.GetSelf();
            EntityIsA = EntityObject.GetComponent<AIKit.IsA>();
            attachSuccessful = true;
        }
        else {
            Debug.LogError("AI Info panel failed to attach to Entity! Target = " + EntityObject);
        }
        if (!attachSuccessful) return;

        //Write static info to display
        NameText.text = EntityIF.EntityName;
        IsAText.text = EntityIsA.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        //Write dynamic info to display
        if (true)
        {
            //goals
            Stack<AIKit.Goal> goals = Entity.goals;
            if (goals.Count > 0)
                GoalText.text = string.Join("\n", Entity.goals.Select(m => m.ToString()).ToArray());
            else
                GoalText.text = "No goals";

            //memory
            MemoryText.text = string.Join("\n", Entity.GetMemories().Select(m => m.GetSentence().GetSemantics().ToString()).ToArray());
            //perceptual facts
            PerceptualFactsText.text = string.Join("\n", Entity.knowledgeModule.perceptualFacts.Select(m => m.ToString()).ToArray());
        }
    }

    public void AddEventToEntity() 
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        AIKit.Sentence sentenceParsed; 

        if (givenSentence.Contains("?")){
            givenSentence = givenSentence.Substring(0,givenSentence.Length-1).ToLower();
            try {
                sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
            } catch (System.Exception e) {
                //if the first wasn't grammatical but has "is", try the flipped version
                if (givenSentence.Contains(" is ")) {
                    string [] splitVersion = givenSentence.Split(new [] { " is " }, 2, System.StringSplitOptions.RemoveEmptyEntries);
                    string flippedVerison = splitVersion[1] + " is " + splitVersion[0];
                    Debug.Log("Failed, now trying to flip: ");
                    sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(flippedVerison.ToLower().Split(' ')));
                } 
                else 
                {
                    throw e;
                }

            }

            string s = "Parsed Query '"+givenSentence+"?' to: "+sentenceParsed.ToString();
            //Debug.Log(s);
            
            //foreach (AIKit.Memory m in Entity.QueryMemories(sentenceParsed.GetLexicalEntryList())) {
            //    s = "Response: "+m.GetSentence().ToString()+" - "+m.GetSentence().ToLiteralString();
            //    Debug.Log(s);
            //}
        }else {
            sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
            string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.ToString();
            Debug.Log(s);
            Entity.addMemory(sentenceParsed);
        }
    }

    public void AddGoalToEntity() 
    {
        if (Entity is null) return;

        string givengoal = NewGoalField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence goalparsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givengoal.ToLower().Split(' ')));
        string s = "Parsed '"+givengoal+"' to: "+goalparsed.GetSemantics().ToString();
        //Debug.Log(s);
        Debug.Log("Pushed new goal to "+Entity.GetName().ToString()+": "+goalparsed.ToLiteralString()+" // "+goalparsed.GetSemantics().ToString());
        Entity.myGoals.Push(goalparsed.GetSemantics());
    }
}
