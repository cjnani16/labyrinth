using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AIKit {
    public class Goal {
        Sentence sentence;
        public bool completed;
        public Goal (Sentence s) {
            this.sentence = s;
            this.completed = false;
        }
        public Sentence GetSentence() {
            return sentence;
        }

        public override string ToString() {
            return sentence.ToLiteralString();
        }

    }
    public enum GenerativeWordClass {
        Demonstratives, Determiners, Inquiries, ItrVerbs, TrVerbs, Names, PossessiveDeterminers, PosessiveNounPhrases, Subjects, Nouns, Places, Prepositions, Adjectives, Markers, Deictic, Antecedents, Consequents, Conjunctions
    }
    public enum Motivation
    {
        Hunger, Security, Social, Esteem, Actualization //Mazlow (ew)
    }
    public class Entity {
        SemNP name;
        List<Memory> memory;
        List<Knowledge> baseInfo;
        List<Knowledge> knowledge;
        List<LexicalEntry> conversationalContext;
        Dictionary<LexicalEntry,float> interests;
        Dictionary<LexicalEntry,Connotation> emotionalOverrides;
        Connotation emotionalState;
        public KnowledgeModule knowledgeModule;
        public Stack<SemSentence> myGoals;
        public Stack<SemSentence> currentPlan;
        public SemSentence currentPlanTarget;
        public int[] motivationState;

        Queue<Sentence> witnessedEvents;

        public Motivation CurrentMotivation()
        {
            for (int i = 0; i < motivationState.Length; i++)
            {
                if (motivationState[i] < 5) //this takes priority if it reaches 0
                {
                    return (Motivation)i;
                }
            }

            return Motivation.Actualization;
        }

        public void ModifyMotivation (Motivation m, int val)
        {
            motivationState[(int)m] += val;
        }

        public void TickMotivations()
        {
            motivationState[(int)Motivation.Hunger] -= 10;
            motivationState[(int)Motivation.Security] -= 5;
            motivationState[(int)Motivation.Social] -= 3;
            motivationState[(int)Motivation.Esteem] -= 2;
            motivationState[(int)Motivation.Actualization] -= 1;
        }

        public SemSentence GetGoalFromMotivation(Motivation motive)
        {
            switch (motive)
            {
                case Motivation.Hunger: //hungry? eat something
                    var goal = new SemSentence()
                    {
                        np = new SemNP(this.name),
                        vp = new SemVP()
                        {
                            verb = AIKit_Grammar.EntryFor("eat"),
                            objects = new List<SemNP>() {
                                new SemNP() {
                                    noun = AIKit_Grammar.EntryFor("thing"),
                                    determiner = AIKit_Grammar.EntryFor("some")
                                } 
                            }
                        }
                    };
                    goal.MakeLiteral();
                    return goal;
                case Motivation.Security: //insecure? see home lmao
                    goal = new SemSentence()
                    {
                        np = new SemNP(this.name),
                        vp = new SemVP()
                        {
                            verb = AIKit_Grammar.EntryFor("see"),
                            objects = new List<SemNP>() {
                                new SemNP() {
                                    noun = AIKit_Grammar.EntryFor("house"),
                                    determiner = AIKit_Grammar.EntryFor("a")
                                }
                            }
                        }
                    };
                    goal.MakeLiteral();
                    return goal;
                case Motivation.Social: //lonely? see someone
                    goal = new SemSentence()
                    {
                        np = new SemNP(this.name),
                        vp = new SemVP()
                        {
                            verb = AIKit_Grammar.EntryFor("see"),
                            objects = new List<SemNP>() {
                                new SemNP() {
                                    noun = AIKit_Grammar.EntryFor("person"),
                                    determiner = AIKit_Grammar.EntryFor("a")
                                }
                            }
                        }
                    };
                    goal.MakeLiteral();
                    return goal;
                case Motivation.Esteem: //feeling down? make something, help someone
                    goal = new SemSentence()
                    {
                        np = new SemNP(this.name),
                        vp = new SemVP()
                        {
                            verb = AIKit_Grammar.EntryFor("help"),
                            objects = new List<SemNP>() {
                                new SemNP() {
                                    noun = AIKit_Grammar.EntryFor("person"),
                                    determiner = AIKit_Grammar.EntryFor("some")
                                }
                            }
                        }
                    };
                    goal.MakeLiteral();
                    return goal;
                case Motivation.Actualization: //purposeless? idk yet bro pursue some ultimate life goal
                    return null;
                default:
                    Debug.LogError("Invalid Motivation value");
                    return null;
            }
        }

        public Entity(string n, GameObject referent) 
        {
            LexicalEntry na = new LexicalEntry(n, WordClass.Name, GenerativeWordClass.Names, Connotation.Neutral);
            na.AffixReferent(referent);
            this.name = new SemNP();
            this.name.noun = na;
            this.memory = new List<Memory>();
            this.knowledge = new List<Knowledge>();
            this.interests = new Dictionary<LexicalEntry, float>();
            this.emotionalOverrides = new Dictionary<LexicalEntry, Connotation>();
            this.emotionalState = new Connotation();
            this.witnessedEvents = new Queue<Sentence>();
            this.knowledgeModule = new KnowledgeModule(na);

            this.myGoals = new Stack<SemSentence>();
            this.currentPlan = new Stack<SemSentence>();
            this.motivationState = new int[5] { 20, 100, 100, 100, 100 };
        }

        public void GainPerceptOf(List<SemNP> apparentNPs) {
            Debug.Log("Gained Percept of " + string.Join("/", apparentNPs));

            //only need one subject moniker and object moniker here, others will be derived later (meaning hypernymy and allat) in KM!
            foreach (SemNP obj in apparentNPs) {
                //this.addMemory
                SemSentence sentence = new SemSentence();
                sentence.np = this.name;
                sentence.vp = new SemVP();
                sentence.vp.verb = AIKit_Grammar.dictionary["see"];
                sentence.vp.objects.Add(obj);

                this.knowledgeModule.perceptualFacts.Add(sentence);
                this.addMemory(new Sentence(sentence));
            }
            
        }

        public void LosePerceptOf(List<SemNP> apparentNPs) {
            List<SemNP> allObjectMonikers = new List<SemNP>();
            foreach (SemNP moniker in apparentNPs) {
                SemanticWebNode recognizedAs = this.knowledgeModule.lexicalMemory.GetOrInsert(moniker);
                List<SemNP> monikers = this.knowledgeModule.GetHypernyms(recognizedAs);
                allObjectMonikers.AddRange(monikers);
            }
            allObjectMonikers = allObjectMonikers.Distinct().ToList();

            SemNP name_np = new SemNP();
            SemanticWebNode selfNode = this.knowledgeModule.lexicalMemory.GetOrInsert(this.name);
            List<SemNP> allSubjectMonikers = this.knowledgeModule.GetHypernyms(selfNode);
            foreach (SemNP subject in allSubjectMonikers) {
                foreach (SemNP obj in allObjectMonikers) {
                    //this.addMemory
                    SemSentence sentence = new SemSentence();
                    sentence.np = subject;
                    sentence.vp = new SemVP();
                    sentence.vp.verb = AIKit_Grammar.dictionary["see"];
                    sentence.vp.objects.Add(obj);

                    this.knowledgeModule.perceptualFacts.Remove(sentence);
                }
            }
        }

        public Connotation getEmotionalState() {
            return emotionalState;
        }

        //Calculate salience and emotional content of memory, then pack it up
        public void addMemory(Sentence s) 
        {
            Debug.Log("Interpreting "+s.GetSemantics().ToString()+"...");
            float sal = 0;
            Connotation con = new Connotation();

            foreach (LexicalEntry l in s.GetLexicalEntries() ) {
                if (this.interests.ContainsKey(l)) {
                    sal += this.interests[l];
                    if (emotionalOverrides.ContainsKey(l))
                        con += emotionalOverrides[l];
                    else 
                        con += l.connotation;
                    sal += con.Magnitude();
                }
            }
            sal = ((1/Mathf.PI) * Mathf.Atan(sal)) + 0.5f;
            this.memory.Add(new Memory(s, sal));

            //TODO: not sure exactly where I want this but for now..
            this.knowledgeModule.interpretEvent(s, sal);

            this.emotionalState *= con;
        }

        public void degradeMemories() {
            foreach (Memory m in memory) {
                m.Degrade();
                if (m.toForget()) {
                    memory.Remove(m);
                }
            }
        }

        public List<Memory> GetMemories(){
            return memory;
        }

        public SemNP GetName() {
            return this.name;
        }

        public void processWitnessQueue() {
            if (witnessedEvents.Count==0) return;
            
            Sentence s = witnessedEvents.Dequeue();
            this.addMemory(s);
        }

        public void chillOut() {
            this.emotionalState*=0.5f;
        }

        public void Witness(Sentence s) {
            witnessedEvents.Enqueue(s);
        }

        public Connotation emotionalReaction(Sentence s) {
            throw new System.NotImplementedException();
        }

        public Connotation emotionalReaction(LexicalEntry le) {
            return emotionalOverrides.ContainsKey(le) ? emotionalOverrides[le] : Connotation.Neutral;
        }

        public List<Memory> GetMemoriesAboutAny(List<LexicalEntry> words) {
            List<Memory> memories = new List<Memory>();

            foreach (Memory m in memory) {
                if (m.isAboutAny(words)) memories.Add(m);
            }

            return memories;
        }

        public List<Memory> GetMemoriesAboutAll(List<LexicalEntry> words) {
            List<Memory> memories = new List<Memory>();

            foreach (Memory m in memory) {
                if (m.isAboutAll(words)) memories.Add(m);
            }

            return memories;
        }

    //    public List<Memory> QueryMemories(List<LexicalEntry> query) {
    //        List<Memory> matches = new List<Memory>();

    //        //get the non-flex portion of the query
    //        List<LexicalEntry> query_no_flex = new List<LexicalEntry>();
    //        List<FlexibleLexicalEntry> query_flex_only = new List<FlexibleLexicalEntry>();
    //        foreach (LexicalEntry le in query) {
    //            if (le is FlexibleLexicalEntry) {
    //                query_flex_only.Add(le as FlexibleLexicalEntry);
    //                Debug.Log("\tFlex in query: "+ (le as FlexibleLexicalEntry).ToString());
    //            }
    //            else {
    //                query_no_flex.Add(le);
    //                Debug.Log("\tNon-flex in query: "+ le.ToString());
    //            }
    //        }

            
    //        foreach (Memory m in memory)
    //        {
    //            if (false && !m.isAboutAll(query_no_flex)) continue; //make sure the flex match quickly
    //            else {
    //                //match no-flex more closely and build collapse_to_flex
    //                List<LexicalEntry> mem_le = m.GetSentence().GetLexicalEntryList();
    //                List<WordClass> collapse_to_flex = new List<WordClass>();
    //                List<GenerativeWordClass> collapse_to_flex_gen = new List<GenerativeWordClass>();
    //                int j = 0;
    //                for (int i = 0; i < mem_le.Count && j<query_no_flex.Count; i++) {
    //                    if (mem_le[i] == query_no_flex[j]) j++;
    //                    else {
    //                        collapse_to_flex.Add(mem_le[i].wordClass);
    //                        collapse_to_flex_gen.Add(mem_le[i].generativeWordClass);
    //                    }
    //                }
    //                if (j != query_no_flex.Count) continue; //discard the memory if no_flex don't match
                    
    //                //try to collapse and match flex
    //                int n1;
    //                do {
    //                    n1 = collapse_to_flex.Count;//collect prior length to detect collapsing
    //                    //check for a match between collapse_to_flex/gen and query_flex_only
    //                    int jj = 0;
    //                    for (int i = 0; i < collapse_to_flex.Count && jj < query_flex_only.Count; i++) {
    //                        if (query_flex_only[jj].type == 1 && collapse_to_flex[i] == query_flex_only[jj].wordClass) jj++;
    //                        if (query_flex_only[jj].type == 0 && collapse_to_flex_gen[i] == query_flex_only[jj].generativeWordClass) jj++;
    //                    }
    //                    if (jj != query_flex_only.Count) {
    //                        //we have a match!
    //                        matches.Add(m);
    //                        continue;
    //                    }

    //                    //collapse and try again
    //                    collapse_to_flex = AIKit_Grammar.CollapseGrammar(collapse_to_flex);
    //                    if (collapse_to_flex.Count==n1)
    //                        collapse_to_flex = AIKit_Grammar.CollapseGrammarFurther(collapse_to_flex);


    //                } while (collapse_to_flex.Count<n1);//give up if no collapsing happened
    //                //finally, give up bc u can't collapse and match the flex
    //            }
    //        }

    //        //debugging:print query and any matched mems
    //        string s = "Query: ";
    //        foreach (LexicalEntry le in query) {
    //            s+=le.ToString()+" ";
    //        }
    //        s+="?";
    //        Debug.Log(s);

    //        return matches;
    //    }
    }

    [System.Serializable]
    public class AIKit_Entity : MonoBehaviour
    {
        public static GameObject world;

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
