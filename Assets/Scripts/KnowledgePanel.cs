using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[System.Serializable]
public class KnowledgePanel : MonoBehaviour
{
    public GameObject EntityObject;
    public TMPro.TextMeshProUGUI ConsoleText;
    public TMPro.TMP_InputField NewSentenceField;
    BeAnEntity EntityIF;
    AIKit.Entity Entity;


    // Start is called before the first frame update
    void Start()
    {
        //Attach to an Entity;
        bool attachSuccessful = false;
        if (EntityIF = EntityObject.GetComponent<BeAnEntity>()) {
            Debug.Log("Knowledge panel attached to Entity " + EntityIF.EntityName);
            Entity = EntityIF.GetSelf();
            attachSuccessful = true;
        }
        else {
            Debug.LogError("Knowledge panel failed to attach to Entity! Target = " + EntityObject);
        }
        if (!attachSuccessful) return;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetEntailedByThis() 
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.GetSentencesEntailedBy(sentenceParsed.GetSemantics());
        if (returnedSentences.Count > 0)
            ConsoleText.text = string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "No sentences entailed by this.";
    }

    public void GetEntailThis() 
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.GetSentencesThatEntail(sentenceParsed.GetSemantics());
        if (returnedSentences.Count > 0)
            ConsoleText.text = string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "No sentences entail this.";
    }

    public void GetPlanTo()
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.PlanTo(sentenceParsed.GetSemantics()).ToList();
        if (returnedSentences.Count > 0)
            ConsoleText.text = string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "Failed to plan how to do this.";
    }

    public void GetTruthOf()
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        string output;
        ConsoleText.text = Entity.knowledgeModule.isTrue(sentenceParsed, out output) ? "This is true.\n" : "This is false.\n";
        ConsoleText.text += output;
    }
}
