using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit
{
    public class Memory 
    {
        float salience;
        Sentence sentence;
        int health;

        public Memory(Sentence sentence, float salience) 
        {
            this.salience = salience;
            this.health = 100;
            this.sentence = sentence;
        }

        public string StatsString() {
            return "Salience: "+ this.salience + ", Health: "+this.health;
        }

        public Sentence GetSentence()
        {
            return sentence;
        }

        public bool isAboutAll(List<LexicalEntry> words) {
            foreach (LexicalEntry w in words) {
                if (!sentence.containsWord(w)) return false;
            }
            return true;
        }

        public bool isAboutAny(List<LexicalEntry> words) {
            foreach (LexicalEntry w in words) {
                if (sentence.containsWord(w)) return true;
            }
            return false;
        }

        public bool isAbout(LexicalEntry word) {
            return sentence.containsWord(word);
        }

        public void Degrade() {
            this.health -= Mathf.RoundToInt(25f * (Mathf.Pow(salience,2)+1));
        }

        public bool toForget() {
            return this.health<0;
        }
    }

    public class Knowledge 
    {
        float salience;
        Sentence sentence;
    }

    [System.Serializable]
    public class AIKit_Perception : MonoBehaviour
    {

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}