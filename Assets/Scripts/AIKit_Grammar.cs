using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit
{
    public struct Connotation 
    {
        public static Connotation Neutral = new Connotation();
        public int Joy; public int Fear; public int Sadness; public int Humor; public int Disgust;

        public static Connotation operator +(Connotation a, Connotation b)
        {
            Connotation c = new Connotation();
            c.Joy = a.Joy+b.Joy;
            c.Fear = a.Fear+b.Fear;
            c.Sadness = a.Sadness+b.Sadness;
            c.Humor = a.Humor+b.Humor;
            c.Disgust = a.Disgust+b.Disgust;
            return c;
        }

        public static Connotation operator *(Connotation a, Connotation b)
        {
            Connotation c = new Connotation();
            c.Joy = a.Joy*b.Joy;
            c.Fear = a.Fear*b.Fear;
            c.Sadness = a.Sadness*b.Sadness;
            c.Humor = a.Humor*b.Humor;
            c.Disgust = a.Disgust*b.Disgust;
            return c;
        }

        public static Connotation operator *(Connotation a, float b)
        {
            Connotation c = new Connotation();
            c.Joy = Mathf.RoundToInt(a.Joy*b);
            c.Fear = Mathf.RoundToInt(a.Fear*b);
            c.Sadness = Mathf.RoundToInt(a.Sadness*b);
            c.Humor = Mathf.RoundToInt(a.Humor*b);
            c.Disgust = Mathf.RoundToInt(a.Disgust*b);
            return c;
        }

        public int Magnitude() {
            int mag = 0;
            mag+=Mathf.Abs(this.Joy);
            mag+=Mathf.Abs(this.Fear);
            mag+=Mathf.Abs(this.Sadness);
            mag+=Mathf.Abs(this.Humor);
            mag+=Mathf.Abs(this.Disgust);
            return mag;
        }

    }

    public enum WordClass 
    {
        Name, S, Adj, N, P, V, Vtr, Det, NP, PP, VP, M_if, M_then, Ant, Con
    }

    public class FlexibleLexicalEntry : LexicalEntry
    {
        public int type;
        
        public FlexibleLexicalEntry(string word, GenerativeWordClass gwc):base(word,WordClass.Name,gwc,Connotation.Neutral) {
            type = 0;
            generativeWordClass = gwc;
        }

        public FlexibleLexicalEntry(string word, WordClass wc):base(word,wc,GenerativeWordClass.Names,Connotation.Neutral) {
            type = 1;
            wordClass = wc;
        }

        public override string ToString() {
            return this.word;//type==0? "["+this.word+"/"+generativeWordClass.ToString()+"]" : "["+this.word+"/"+wordClass.ToString()+"]";
        }

        public override bool Equals(object o) {
            LexicalEntry ole = o as LexicalEntry;
            if (ole != null) 
                if (this.type == 0)
                    return this.generativeWordClass == ole.generativeWordClass;
                else
                    return this.wordClass == ole.wordClass;
            else
                return false;
        }

        public override int GetHashCode() {
            if (this.type == 0) {
                return 25+(int)this.generativeWordClass;
            } else {
                return (int)this.wordClass;
            }
        }

        public static bool operator ==(LexicalEntry a, FlexibleLexicalEntry b) {
            if (a is null || b is null) return false;
            return b.type==0? a.generativeWordClass == b.generativeWordClass : a.wordClass == b.wordClass;
        }
        public static bool operator !=(LexicalEntry a, FlexibleLexicalEntry b) {
            if (a is null || b is null) return true;
            return b.type==0? a.generativeWordClass != b.generativeWordClass : a.wordClass != b.wordClass;
        }
    }

    public class LexicalEntry 
    {
        protected string word;
        public WordClass wordClass;
        public GenerativeWordClass generativeWordClass;
        public Connotation connotation;
        GameObject referent;

        public LexicalEntry(LexicalEntry le) {
            this.word = le.word; this.wordClass = le.wordClass; this.connotation = le.connotation; this.generativeWordClass = le.generativeWordClass;
            this.referent = le.GetReferent(); //TODO: always require?
        }
        public LexicalEntry(string s, WordClass wc, GenerativeWordClass gwc, Connotation c) {
            this.word = s; this.wordClass = wc; this.connotation = c; this.generativeWordClass=gwc;
            this.referent = null; //TODO: always require?
        }

        public override string ToString() {
            return this.word + ((this.referent is null)?"":("=("+this.referent.ToString()+")"));
        }

        public void AffixReferent(GameObject o) {
            this.referent = o; 
        }

        public GameObject GetReferent() {
            return this.referent;
        }

        public static bool operator ==(LexicalEntry a, LexicalEntry b) {
            try {
                if (a is null != b is null) return false; //only one null? false
                else if (a is null) return true; //both null? true

                bool referentMatch = (a.referent is null && b.referent is null)?true:(a.referent == b.referent);
                return (a.word == b.word) && referentMatch;
            } catch (System.NullReferenceException e) {
                Debug.LogError(e);
                if (!(a is null)) Debug.Log("^ Tried to compare "+a.word);
                if (!(b is null)) Debug.Log("^ Tried to compare "+b.word);
                return false;
            }
        }
        public static bool operator !=(LexicalEntry a, LexicalEntry b) {
            return !(a==b);
        }

        public override int GetHashCode() {
            return this.word.GetHashCode() + ((this.referent is null)?0:this.referent.GetHashCode());
        }

        public override bool Equals(object obj) {
            LexicalEntry ole = obj as LexicalEntry;
            if (ole is null) return false;
            bool referentMatch = (this.referent is null && ole.referent is null)?true:(this.referent == ole.referent);
            return (this.word == ole.word) && referentMatch;
        }
        public bool WordEquals(string str) {
            return this.word == str;
        }
    }
 
    public class Sentence 
    {
        HashSet<LexicalEntry> lexicalEntries;
        List<LexicalEntry> lexicalEntryList;
        List<WordClass> syntax;
        public Date utterance;
        List<LexicalEntry> subjectsToVerbs;
        SemSentence semantics;

        public bool negated;

        //Todo deprecate?
        public Sentence(List<LexicalEntry> words) {
            utterance = GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now();
            lexicalEntries = new HashSet<LexicalEntry>();
            syntax = new List<WordClass>();
            lexicalEntryList = new List<LexicalEntry>();
            negated = false;
            semantics = null; //TODO: parse immediately?

            foreach (LexicalEntry word in words) {
                lexicalEntries.Add(word);
                lexicalEntryList.Add(word);
                syntax.Add(word.wordClass);
            }
        }

        public Sentence(SemSentence sem) {
            utterance = GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now();
            lexicalEntries = new HashSet<LexicalEntry>();
            syntax = new List<WordClass>();
            lexicalEntryList = new List<LexicalEntry>();
            negated = false;
            semantics = sem;

            AppendNP(sem.np);
            AppendVP(sem.vp);
        }

        void AppendNP(SemNP np) {
            AppendWord(np.determiner);
            AppendWord(np.noun);
            SemPP pp = np.pp;
            while (!(pp is null)) {
                AppendWord(pp.preposition);
                AppendNP(pp.np);
            }
        }

        void AppendVP(SemVP vp) {
            AppendWord(vp.verb);
            foreach (SemNP np in vp.objects) {
                AppendNP(np);
            }
            foreach (SemSentence sent in vp.sentenceObjects) {
                AppendNP(sent.np);
                AppendVP(sent.vp);
            }
        }

        void AppendWord(LexicalEntry word) {
            if (word is null) return;

            lexicalEntries.Add(word);
            lexicalEntryList.Add(word);
            syntax.Add(word.wordClass);
        }

        public SemSentence GetSemantics() {
            return semantics;
        }

        public Sentence(List<LexicalEntry> words, SemSentence ss) {
            utterance = GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now();
            lexicalEntries = new HashSet<LexicalEntry>();
            syntax = new List<WordClass>();
            lexicalEntryList = new List<LexicalEntry>();
            negated = false;
            semantics = ss;

            foreach (LexicalEntry word in words) {
                lexicalEntries.Add(word);
                lexicalEntryList.Add(word);
                syntax.Add(word.wordClass);
            }
        }

        public void Negate() {
            negated = !negated;
        }
        public bool containsWord(LexicalEntry w) {
            return lexicalEntries.Contains(w);
        }
        public bool containsWord(string s) {
            return true;
        }

        public HashSet<LexicalEntry> GetLexicalEntries() {
            return lexicalEntries;
        }

        public List<LexicalEntry> GetLexicalEntryList() {
            return lexicalEntryList;
        }

        public override string ToString() {
            string s = "";
            foreach (LexicalEntry le in this.lexicalEntryList) {
                string wordAsString = (le is FlexibleLexicalEntry) ? le.ToString(): le.wordClass.ToString();
                s+=wordAsString+" ";
            }
            s+=".";
            return s;
        }

        public string ToLiteralString() {
            string s = "";
            foreach (LexicalEntry le in this.lexicalEntryList) {
                s+=le.ToString()+" ";
            }
            s+=".";
            return s;
        }

    }

    public class AIKit_Grammar : MonoBehaviour 
    {
        public static Dictionary<string, LexicalEntry> dictionary;
        [SerializeField]
        public bool showGrammarConsole;
        [SerializeField]
        public GameObject EntityToTalkTo;
        [SerializeField]
        public string[] startingSentences;
        Entity entity;
        BeAnEntity beAnEntity;
        List<string> chatWindow;
        static bool dictReady = false;

        public static LexicalEntry EntryFor(string s) 
        {
            if (!dictReady) ParseDictionary();
            return dictionary[s];
        }

        static void ParseDictionary()
        {
            Dictionary<string, LexicalEntry> result = new Dictionary<string,LexicalEntry>();
            System.IO.StreamReader file;
            string L ="";

            //Get everyone's name
            GameObject[] objs = GameObject.FindGameObjectsWithTag("AIKE");
            foreach(GameObject o in objs) {
                LexicalEntry name = new LexicalEntry(o.GetComponent<BeAnEntity>().EntityName.ToLower(),WordClass.Name,GenerativeWordClass.Names,Connotation.Neutral);
                name.AffixReferent(o);
                result.Add(o.GetComponent<BeAnEntity>().EntityName.ToLower(), name);
            }

            //Read each file for specific syntactic category
            file = new System.IO.StreamReader("Assets/Scripts/WordLists/demonstratives.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.NP,GenerativeWordClass.Demonstratives,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/determiners.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.Det,GenerativeWordClass.Determiners,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/inquiries.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.NP,GenerativeWordClass.Inquiries,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/intransitive verbs.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.V,GenerativeWordClass.ItrVerbs,Connotation.Neutral));
                    result.Add("can"+L,new LexicalEntry("can"+L,WordClass.V,GenerativeWordClass.ItrVerbs,Connotation.Neutral));
                    result.Add("no"+L,new LexicalEntry("no"+L,WordClass.V,GenerativeWordClass.ItrVerbs,Connotation.Neutral));
                    result.Add("nocan"+L,new LexicalEntry("nocan"+L,WordClass.V,GenerativeWordClass.ItrVerbs,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/transitive verbs.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.Vtr,GenerativeWordClass.TrVerbs,Connotation.Neutral));
                    result.Add("can"+L,new LexicalEntry("can"+L,WordClass.Vtr,GenerativeWordClass.TrVerbs,Connotation.Neutral));
                    result.Add("no"+L,new LexicalEntry("no"+L,WordClass.Vtr,GenerativeWordClass.TrVerbs,Connotation.Neutral));
                    result.Add("nocan"+L,new LexicalEntry("nocan"+L,WordClass.Vtr,GenerativeWordClass.TrVerbs,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/pronouns.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.NP,GenerativeWordClass.Deictic,Connotation.Neutral)); //used t be names, now NPs
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/subjects.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.NP,GenerativeWordClass.Subjects,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/nouns.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.N,GenerativeWordClass.Nouns,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/possessive noun phrases.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.NP,GenerativeWordClass.PosessiveNounPhrases,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/prepositions.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new LexicalEntry(L,WordClass.P,GenerativeWordClass.Prepositions,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/adjectives.txt");
            while ((L = file.ReadLine()) != null)
            {
                string[] S=L.Trim().Split(':');
                L=S[0];
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    if (S.Length>1) {
                        Connotation c = new Connotation();
                        c.Joy = System.Int32.Parse(""+S[1][0]);
                        c.Fear = System.Int32.Parse(""+S[1][1]);
                        c.Sadness = System.Int32.Parse(""+S[1][2]);
                        c.Humor = System.Int32.Parse(""+S[1][3]);
                        c.Disgust = System.Int32.Parse(""+S[1][4]);
                    }
                    result.Add(L,new LexicalEntry(L,WordClass.Adj,GenerativeWordClass.Adjectives,Connotation.Neutral));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/flex noun phrases.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L,new FlexibleLexicalEntry(L, WordClass.NP));
                }
            }
            file.Close();

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/inquiries.txt");
            while ((L = file.ReadLine()) != null)
            {
                L=L.Trim();
                if (L.Length!=0)
                {
                    string[] S = L.Split(':');
                    WordClass wc;
                    GenerativeWordClass gwc;
                    int type;
                    if (result.ContainsKey(S[0])) continue;
                    switch (S[1]) {
                        case "Name": wc=WordClass.Name; gwc=GenerativeWordClass.Names; type=0; break;
                        case "N": wc=WordClass.N; gwc=GenerativeWordClass.Subjects; type=1; break;
                        case "NP": wc=WordClass.NP; gwc=GenerativeWordClass.Subjects; type=1; break;
                        case "PP": wc=WordClass.PP; gwc=GenerativeWordClass.Demonstratives; type=1; break;
                        case "S": wc=WordClass.S; gwc=GenerativeWordClass.Subjects; type=1; break;
                        case "Dem": wc=WordClass.S; gwc=GenerativeWordClass.Demonstratives; type=1; break;
                        default: wc=WordClass.Name; gwc=GenerativeWordClass.Names; type=0; break;
                    }
                    FlexibleLexicalEntry l = type==0? new FlexibleLexicalEntry(S[0], gwc) : new FlexibleLexicalEntry(S[0], wc);
                    result.Add(S[0],l);
                }
            }
            file.Close();

            //special case words: if, then
            LexicalEntry le_if = new LexicalEntry("if", WordClass.M_if, GenerativeWordClass.Markers, Connotation.Neutral);
            result.Add("if", le_if);
            LexicalEntry le_then = new LexicalEntry("then", WordClass.M_then, GenerativeWordClass.Markers, Connotation.Neutral);
            result.Add("then",le_then);

            dictionary = result;
            dictReady = true;
        }

        public static List<WordClass> CollapseGrammar(List<WordClass> seen) {
            int n1 = seen.Count;

            //Debug.Log("\tFrom: "+string.Join(" ",seen.ConvertAll<string>(word => word.ToString())));
            bool popcon = false;
            if (seen[seen.Count-1] == WordClass.Con) {
                seen.RemoveAt(seen.Count-1);   
                popcon = true;
            }

            //Grammatical rules

            //this a lil hacky, but N -> N V allows the N to keep collapsing with a V on the end.
            bool popverb = false;
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.N && seen[seen.Count-1] == WordClass.V) {
                seen.RemoveAt(seen.Count-1);
                popverb = true;
            }

            //S -> NP VP | NP V | Name VP | Name V | Ant Con
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.NP && seen[seen.Count-1] == WordClass.VP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.S);
            }

            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.NP && seen[seen.Count-1] == WordClass.V) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.S);
            }

            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.Name && seen[seen.Count-1] == WordClass.VP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.S);
            }

            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.Name && seen[seen.Count-1] == WordClass.V) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.S);
            }

            if (seen.Count >=1 && seen[seen.Count-1] == WordClass.Ant && popcon) {
                seen.RemoveAt(seen.Count-1);
                popcon = false;
                seen.Add(WordClass.S);
            }

            //N -> Adj N | N PP
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.Adj && seen[seen.Count-1] == WordClass.N) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.N);
            }

            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.N && seen[seen.Count-1] == WordClass.PP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.N);
            }
            //NP -> Det N | Name
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.Det && seen[seen.Count-1] == WordClass.N) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.NP);
            }
            if (seen.Count >=1 && seen[seen.Count-1] == WordClass.Name) {
                seen.RemoveAt(seen.Count-1);
                seen.Add(WordClass.NP);
            }
            //VP -> Vtr NP | V PP | Vtr PP
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.Vtr && seen[seen.Count-1] == WordClass.NP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.VP);
            }

            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.V && seen[seen.Count-1] == WordClass.PP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.VP);
            }

            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.Vtr && seen[seen.Count-1] == WordClass.PP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.VP);
            }

            //PP -> P NP
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.P && seen[seen.Count-1] == WordClass.NP) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.PP);
            }

            //A -> M(if) S
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.M_if && seen[seen.Count-1] == WordClass.S) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.Ant);
            }

            //C -> M(then) S
            if (seen.Count >=2 && seen[seen.Count-2] == WordClass.M_then && seen[seen.Count-1] == WordClass.S) {
                seen.RemoveRange(seen.Count-2,2);
                seen.Add(WordClass.Con);
            }

            if (popverb) {
                seen.Add(WordClass.V);
            }

            if (popcon) {
                seen.Add(WordClass.Con);
            }

            if (seen.Count==n1) {
                //Debug.Log("Switching to parse SUBJ grammar");
                seen = CollapseGrammarFurther(seen);
            }

            //Debug.Log("\tTo: "+string.Join(" ",seen.ConvertAll<string>(word => word.ToString())));
            return seen;
        }

        public static List<WordClass> CollapseGrammarFurther(List<WordClass> seen) {
            bool popcon = false;
            if (seen[seen.Count-1] == WordClass.Con) {
                seen.RemoveAt(seen.Count-1);   
                popcon = true;
            }

            //peel off vp and collapse the subject towards an np?
            if (seen[seen.Count-1] == WordClass.VP) {
                seen.RemoveAt(seen.Count-1);
                if (seen.Count>1)
                    seen = CollapseGrammar(seen);
                seen.Add(WordClass.VP);
            }

            if (popcon) {
                seen.Add(WordClass.Con);
            }
            return seen;
        }

        public static object CollapseSemantics(ref List<LexicalEntry> words, object sem) {
            if (words.Count == 0) return sem;
            //Debug.Log("Words: "+ string.Join(".",words.ConvertAll(word => word.ToString())));
            //start with the tail
            LexicalEntry w = words[words.Count-1];
            words.RemoveAt(words.Count-1);

            //isolate antecedent from an implication if we need to keep collapsing
            SemImplication prev_im = sem as SemImplication;
            if (!(prev_im is null)) {
                //pretty much guaranteed we're working on the antecedent
                //Debug.Log("Now working on antecedent.");
                sem = prev_im.antecedent;
            }

            //isolate np from a sentence if we need to keep collapsing
            SemSentence prev_s = sem as SemSentence;
            if (!(prev_s is null)) {
                //Debug.Log("Preexisting Sentence: "+prev_s.ToString());
                //if we have existing work on the subject
                if (!(prev_s.np is null)) {
                    //if we haven't reached the noun phrase part then we we're in the middle of a PP
                    if (prev_s.np.noun is null){
                        sem = prev_s.np.pp;
                    }
                    //otherwise we are building a np
                    else {
                        sem = prev_s.np;
                    }
                } 
                //if we don't then start with a null subj
                else {
                    sem = null;
                }        
            }

            SemImplication imp = null;
            SemSentence s = null;
            SemNP np = null;
            SemPP pp = null;
            SemVP vp = null;

            //Debug.Log("W"+words.Count+":"+w.ToString());
            
            switch (w.wordClass) {
                case WordClass.Adj:
                    //should be followed by an NP
                    np = sem as SemNP;
                    if (!(np is null))  {
                        np.adjectives.Add(w);
                    }

                    break;

                case WordClass.Det:
                    //should be followed by an NP
                    np = sem as SemNP;
                    if (!(np is null)) {
                        np.determiner = w;
                    }
                    break;

                //flexible lexical entries like "it" come as NP... also pronouns
                case WordClass.NP:
                    //Debug.Log("Noticed a flexible le: "+w.ToString());

                    //if there's no following vp
                    if (sem is null) {
                        //Debug.Log("Null sem before: " + w.ToString());
                        np = new SemNP();
                        np.noun = w;
                        sem = np;
                        break;
                    }

                    //if there's a following PP (or a PP hidden in an NP...) then attach to this np
                    pp = sem as SemPP;
                    if (pp is null){
                        np = sem as SemNP;
                        if (!(np is null) && (np.noun is null)) {
                            //Debug.Log("recover pp");
                            pp = np.pp;
                        }
                        else np = null;
                    }

                    if (!(pp is null)) {
                        np = new SemNP();
                        np.noun = w;
                        np.pp = pp;
                        sem = np;
                        break;
                    }

                    //if there's following vp then this is a subject
                    vp = sem as SemVP;
                    if (!(vp is null)) {
                        s = new SemSentence();
                        s.vp = vp;
                        s.np = new SemNP();
                        s.np.noun = w;
                        sem = s;
                    }

                    break;

                case WordClass.N: 
                    //Debug.Log("Triggered Noun: " + w.ToString());
                    //if there's no following vp
                    if (sem is null) {
                        //Debug.Log("Null sem before: " + w.ToString());
                        np = new SemNP();
                        np.noun = w;
                        sem = np;
                        break;
                    }

                    //if there's a following PP (or a PP hidden in an NP...) then attach to this np
                    pp = sem as SemPP;
                    if (pp is null){
                        np = sem as SemNP;
                        if (!(np is null) && (np.noun is null)) {
                            //Debug.Log("recover pp");
                            pp = np.pp;
                        }
                        else np = null;
                    }
                    
                    if (!(pp is null)) {
                        np = new SemNP();
                        np.noun = w;
                        np.pp = pp;
                        sem = np;
                        break;
                    }

                    //if there's following vp then this is a subject
                    vp = sem as SemVP;
                    if (!(vp is null)) {
                        s = new SemSentence();
                        s.vp = vp;
                        s.np = new SemNP();
                        s.np.noun = w;
                        sem = s;
                    }

                    break;

                case WordClass.V:
                    //there should be no following object
                    if (sem is null) {
                        s = new SemSentence();
                        s.vp = new SemVP();
                        s.vp.verb = w;
                        sem = s;
                    }

                    break;

                case WordClass.Vtr: 
                    //thre should be following object
                    np = sem as SemNP;
                    if (!(np is null))  {
                        vp = new SemVP();
                        vp.verb = w;
                        vp.objects.Add(np);
                        sem = vp;
                    }
                    break;

                case WordClass.Name:
                    //if there's no following vp
                    if (sem is null) {
                        np = new SemNP();
                        np.noun = w;
                        sem = np;
                        break;
                    }

                    //if there's following vp then this is a subject
                    vp = sem as SemVP;
                    if (!(vp is null)) {
                        s = new SemSentence();
                        s.vp = vp;
                        s.np = new SemNP();
                        s.np.noun = w;
                        sem = s;
                    }
                    break;

                case WordClass.P: 
                    //there should be a following NP
                    np = sem as SemNP;
                    if (!(np is null))  {
                        pp = new SemPP();
                        pp.preposition = w;
                        pp.np = np;
                        sem = pp;
                    }
                    break;

                case WordClass.M_if:
                    //there should be a following S
                    s = sem as SemSentence;
                    if (!(s is null)) {
                        //Debug.Log("Capped off implication with IF.");
                        //we neeed not do anything, this complete sentence will be added to the prev_im antecedent below...
                    }
                    break;

                case WordClass.M_then:
                    //there should be a following S, but we isolated it
                    if (!(prev_s is null))  {
                        //Debug.Log("Found THEN + S, making an impliction now...");
                        imp = new SemImplication();
                        imp.consequent = prev_s;
                        prev_s = null;
                        sem = imp;
                    }
                    break;

                default:
                    //Debug.Log("Failed to SemCollapse word: "+w.ToString());
                    break;
            } 

            //Debug.Log("Lil piece is: "+(sem is null? "[sem is NULL]" : sem.ToString())+", p_sent is "+(prev_s is null ? "NULL" : prev_s.ToString() )+"and p_im is "+(prev_im is null ? "NULL" : prev_im.ToString()));

            //reattach the np to the sentence if we isolated it for collapsing
            if (!(prev_s is null)) {
                //Debug.Log("Rebuilding sentence "+prev_s.ToString()+".");
                if (!(sem as SemNP is null)) {
                    prev_s.np = sem as SemNP;
                }
                else if (!(sem as SemPP is null)) {
                    //don't worry abt overwrite the old np becuse it's part of a pp now
                    prev_s.np = new SemNP();
                    prev_s.np.pp = sem as SemPP;
                }
                else if (!(sem as SemVP is null)) {
                    //we will completely replace sentence bc its np became a vp
                    prev_s.np = null;
                    prev_s.vp = sem as SemVP;
                }

                sem = prev_s;
            }

            //reattach this antecedent to an implication if we had one
            if (!(prev_im is null)) {
                //Debug.Log("Rebuilding implication "+prev_im.ToString()+".");
                s = sem as SemSentence;

                if (s is null) {
                    np = sem as SemNP;
                    pp = sem as SemPP;
                    if (!(np is null)) {
                        s = new SemSentence();
                        s.np = np;
                    } else if (!(pp is null)) {
                        s = new SemSentence();
                        s.np = new SemNP();
                        s.np.pp = pp;
                    }
                }
                
                prev_im.antecedent = s;
                sem = prev_im;
            } 

            //Debug.Log("Collapsed to" + sem.ToString());
            return sem;
        }

        public static List<LexicalEntry> ExpandToList( LexicalEntry le) {
            List<LexicalEntry> list = new List<LexicalEntry>();
            list.Add(le);
            return list;
        }

        public static Sentence Interpret(List<string> words) {
            if (words is null) 
                throw new System.Exception("No words provided!");
            if (dictionary is null) 
                throw new System.Exception("No dictionary initialized!");

            bool negate = false;
            Debug.Log("Interpreting: '[[["+string.Join(" ", words)+".]]]...'");
            
            //negate sentences if not is detected (use this for negation o/w)
            if (words.Contains("not")) {
                words.Remove("not");
                negate = true;
            }
            List<WordClass> seen = new List<WordClass>();
            List<LexicalEntry> les = new List<LexicalEntry>();

            for (int i = 0; i < words.Count; i++) {
                if (!dictionary.ContainsKey(words[i])) {
                    Debug.LogError("Unknown word: " + words[i]);
                    words[i] = "???";
                } else {
                    seen.Add(dictionary[words[i]].wordClass);
                    les.Add(dictionary[words[i]]);
                }
            }

            //Collapse the sentence using ug rules until we reach a stable point. Hopefully thats an S
            int n1, n2;
            object o = null;
            do {
                n2 = les.Count;
                n1 = seen.Count;

                seen = CollapseGrammar(seen);
                o = CollapseSemantics(ref les, o);

                if (les.Count==n2) {
                    //Debug.Log("Switching to parse SUBJ semantics");
                    o = CollapseSemantics(ref les, o);
                }

            } while (seen.Count<n1 || les.Count<n2);
            SemSentence sem_s = o as SemSentence;
            if (sem_s is null) {
                Debug.LogError("Semantic Collapse Failed! Last word:"+les[les.Count-1]);
            } else {
                //Debug.Log("Semantic collapse successful:"+sem_s.ToString());
            }

            if (seen.Count==1 && seen[0]==WordClass.S) {
                Sentence s = new Sentence(words.ConvertAll<LexicalEntry>(word => dictionary[word]), sem_s);
                if (negate) s.Negate();
                return s;
            }
            else 
            {
                Debug.LogError(string.Join(" ", words.ToArray()));
                throw new System.Exception("Sentence non-grammatical! Seen.Count is "+seen.Count+", and seen[0] is " + ((seen.Count > 0) ? seen[0].ToString() : "none"));
            }
        }

        public static SemNP ReplaceVia(SemNP fill, SemNP context) {
            //no error checking on arguments! dont mes up!

            //"any" becomes "some" if the fill pronoun is singular, but remains if the pronoun is plural
            if (!(context.determiner is null) && context.determiner.WordEquals("any")) {
                //list of SINGULAR pronouns (nested for readability)
                //will have to figure out how to determine singular vs plural they..
                if (fill.noun.WordEquals("it") || fill.noun.WordEquals("he") || fill.noun.WordEquals("she")) {
                    fill = new SemNP(context);
                    fill.determiner = AIKit_Grammar.dictionary["some"];
                }
            } 
            else fill = new SemNP(context);

            return fill;
        }

        public static SemSentence FillPronouns(SemSentence context, SemSentence sentenceWithPronoun) {
            SemSentence filled = new SemSentence(sentenceWithPronoun);
        
            //check if subj is pronoun
            if (filled.np.noun.generativeWordClass == GenerativeWordClass.Deictic) {
                filled.np = ReplaceVia(filled.np, context.np);
            }

            //check if obj is pronoun
            for (int i = 0; i < filled.vp.objects.Count; i++) {
                if (filled.vp.objects[i].noun.generativeWordClass == GenerativeWordClass.Deictic) {
                    if (context.vp.objects.Count > i)
                        filled.vp.objects[i] = ReplaceVia(filled.vp.objects[i], context.vp.objects[i]);
                    else
                        filled.vp.objects[i] = ReplaceVia(filled.vp.objects[i], context.vp.objects[context.vp.objects.Count - 1]);
                }
            }

            return filled;
        }

        public static SemSentence TakePronouns(SemSentence context, SemSentence sentenceToPlace) {
            SemSentence filled = new SemSentence(sentenceToPlace);
        
            //check if subj is pronoun
            if (context.np.noun.generativeWordClass == GenerativeWordClass.Deictic) {
                filled.np = context.np;
            }

            //check if obj is pronoun
            for (int i = 0; i < context.vp.objects.Count; i++) {
                if (context.vp.objects[i].noun.generativeWordClass == GenerativeWordClass.Deictic) {
                    if (filled.vp.objects.Count > i)
                        filled.vp.objects[i] = context.vp.objects[i];
                    else
                        filled.vp.objects[filled.vp.objects.Count - 1] = context.vp.objects[i];
                }
            }

            return filled;
        }

        public static SemSentence FillDemonstratives(SemSentence context, SemSentence sentenceWithDemonstrative) {
            SemSentence filled = sentenceWithDemonstrative;

            //check if subj is demonstrative
            if (sentenceWithDemonstrative.np.determiner.generativeWordClass == GenerativeWordClass.Demonstratives) {
                LexicalEntry noun = sentenceWithDemonstrative.np.noun;
                //grab the demonstrative phrase's antecedent by whichever has a matching noun
                if (context.np.noun == noun) {
                    filled.np = context.np;
                } else {
                    foreach (SemNP obj in context.vp.objects) {
                        if (obj.noun == noun) {
                            filled.np = obj;
                            break;
                        }
                    }
                }
            }

            //check if obj is demonstrative
            for (int i = 0; i < sentenceWithDemonstrative.vp.objects.Count; i++) {
                SemNP obj = sentenceWithDemonstrative.vp.objects[i];
                if (obj.noun.generativeWordClass == GenerativeWordClass.Demonstratives) {
                    LexicalEntry noun = sentenceWithDemonstrative.vp.objects[i].noun;

                    //grab the demonstrative phrase's antecedent by whichever has a matching noun
                    if (context.np.noun == noun) {
                        filled.vp.objects[i] = context.np;
                    } else {
                        foreach (SemNP context_obj in context.vp.objects) {
                            if (context_obj.noun == noun) {
                                filled.vp.objects[i] = context_obj;
                                break;
                            }
                        }
                    }
                }
            }

            return filled;
        }

        /*public static SemSentence ReplaceFlex(SemSentence template, SemSentence filled, SemSentence target) {
            if (!template.flex || !target.flex) return target;
            if (filled.flex) return target;

            SemNP grabbed = null;
            int objIndex = -1;

            //check subject
            if (template.np.flex) {
                grabbed = filled.np;
            } 

            //check object
            //TODO: fill a template with multuple objects!?
            else {
                for (int i = 0; i < template.vp.objects.Count; i++) {
                    if (target.vp.objects[i].flex) {
                        grabbed = filled.vp.objects[i];
                        objIndex = i;
                        break;
                    }
                }
            }

            if (grabbed is null) Debug.LogError("Somehow the template's flex wasn't found!");

            //apply
            SemSentence targetFilled = target;
            if (objIndex != -1) {
                targetFilled.np = grabbed;
            } else {
                targetFilled.vp.objects[objIndex] = grabbed;
            }

            return targetFilled;
        }*/

        // Start is called before the first frame update
        void Start()
        {
            if (!dictReady) 
            {
                dictionary = new Dictionary<string, LexicalEntry>();
                Debug.Log("Parsing word lists into Dicitonary...");
                ParseDictionary();
                Debug.Log("Dicitonary complete.");
            }

            entity = EntityToTalkTo.GetComponent<BeAnEntity>().GetSelf();
            Debug.Log("Loaded entity: " + entity.GetName());
            beAnEntity = EntityToTalkTo.GetComponent<BeAnEntity>();
            
            chatWindow = new List<string>();

            foreach (string s in startingSentences) {
                try{
                Sentence sentenceParsed = Interpret(new List<string>(s.ToLower().Split(' ')));
                string deb = "Parsed '"+s+"' to: "+sentenceParsed.ToString();
                Debug.Log(deb);
                chatWindow.Add(deb);
                entity.addMemory(sentenceParsed);
                } catch {

                }
            }

            //Debug.Log("known as:" + string.Join("/",entity.knowledgeModule.GetHyponymsOf(entity.knowledgeModule.lexicalMemory.GetOrInsert(entity.GetName()))));
            /*
            string str = "the woman kill the monster";
            Sentence s1 = Interpret(new List<string>(str.ToLower().Split(' ')));
            
            str = "the woman kill it";
            Sentence s2 = Interpret(new List<string>(str.ToLower().Split(' ')));
            
            str = "the woman eat it";
            Sentence s3 = Interpret(new List<string>(str.ToLower().Split(' ')));
            Debug.LogError("Match between "+s1.GetSemantics().ToString()+" and "+s2.GetSemantics().ToString()+"?:"+(s1.GetSemantics()==s2.GetSemantics()));
            
            SemSentence s4 = ReplaceFlex(s2.GetSemantics(), s1.GetSemantics(), s3.GetSemantics());
            Debug.LogError("Filled template: "+s4.ToString());*/
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public string givenSentence, givenWord, givengoal, GivenStatement;
        Sentence sentenceParsed;
        LexicalEntry wordParsed;
        

        void OnGUI() {
            if (!showGrammarConsole) return;

            givenSentence = GUI.TextField(new Rect(100, 50, 200, 20), givenSentence);
            if (GUI.Button(new Rect(100, 90, 100, 60), "Parse Sentence")) 
            {
                if (givenSentence.Contains("?")){
                    givenSentence = givenSentence.Substring(0,givenSentence.Length-1).ToLower();
                    try {
                        sentenceParsed = Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
                    } catch (System.Exception e) {
                        //if the first wasn't grammatical but has "is", try the flipped version
                        if (givenSentence.Contains(" is ")) {
                            string [] splitVersion = givenSentence.Split(new [] { " is " }, 2, System.StringSplitOptions.RemoveEmptyEntries);
                            string flippedVerison = splitVersion[1] + " is " + splitVersion[0];
                            Debug.Log("Failed, now trying to flip: ");
                            sentenceParsed = Interpret(new List<string>(flippedVerison.ToLower().Split(' ')));
                        } 
                        else 
                        {
                            throw e;
                        }

                    }

                    string s = "Parsed Query '"+givenSentence+"?' to: "+sentenceParsed.ToString();
                    Debug.Log(s);
                    chatWindow.Add(s);
                    
                    foreach (Memory m in entity.QueryMemories(sentenceParsed.GetLexicalEntryList())) {
                        s = "Response: "+m.GetSentence().ToString()+" - "+m.GetSentence().ToLiteralString();
                        Debug.Log(s);
                        chatWindow.Add(s);
                    }
                }else {
                    sentenceParsed = Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
                    string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.ToString();
                    Debug.Log(s);
                    chatWindow.Add(s);
                    
                    entity.addMemory(sentenceParsed);
                }
                
            }
            
            if(sentenceParsed!=null)
                GUI.Label(new Rect(100,170,200,20), sentenceParsed.ToString());
            
            givenWord = GUI.TextField(new Rect(100, 200, 200, 20), givenWord).ToLower();
            if (GUI.Button(new Rect(100, 240, 100, 60), "Parse Word")) 
            {
                if (dictionary.ContainsKey(givenWord)) {
                    wordParsed = dictionary[givenWord];
                    string s = "Parsed "+givenWord+" to: "+wordParsed.ToString();
                    Debug.Log(s);
                    chatWindow.Add(s);
                } else {
                    string s = "Word "+givenWord+" not recognized.";
                    Debug.Log(s);
                    chatWindow.Add(s);
                }
                
            }

            //if(false && wordParsed!=null)
            //    GUI.Label(new Rect(100,270,200,20), wordParsed.ToString());
            
            if(chatWindow!=null) {
                for (int i = chatWindow.Count-1; i >=0; i--) {
                    GUI.Label(new Rect(300,200-((chatWindow.Count - i)*20),400,20), chatWindow[i]);
                }
                GUI.Label(new Rect(300,10,400,20), entity.GetName()+"'s Dialogue");
            }
                
            if(entity!=null) {
                for (int i = 0; i < entity.GetMemories().Count; i++) {
                    GUI.Label(new Rect(700,50+(i*20),1000,20), entity.GetMemories()[i].GetSentence().ToString() + " - " +entity.GetMemories()[i].GetSentence().ToLiteralString() + " - "+entity.GetMemories()[i].StatsString());
                }
                GUI.Label(new Rect(700,10,400,20), entity.GetName()+"'s Memories");
            }

            if(beAnEntity!=null) {
                //GameObject[] context = new GameObject[beAnEntity.PerceptualContext.Count];
                SemSentence[] pFacts = new SemSentence[entity.knowledgeModule.perceptualFacts.Count];
                entity.knowledgeModule.perceptualFacts.CopyTo(pFacts);

                for (int i = 0; i < pFacts.Length; i++) {
                    GUI.Label(new Rect(1500,200+(i*20),1000,20), pFacts[i].ToString());
                    //chatWindow.Add(beAnEntity.EntityName+" has noticed "+context[i].GetComponent<IsA>().ToString()+" entering perceptual range.");
                }
                GUI.Label(new Rect(1500,170,700,20), entity.GetName()+"'s Perceptual Facts");

                Goal[] goals = new Goal[entity.goals.Count];
                entity.goals.CopyTo(goals,0);
                for (int i = 0; i < goals.Length; i++) {
                    GUI.Label(new Rect(1500,50+(i*20),1000,20), goals[i].ToString());
                }
                GUI.Label(new Rect(1500,10,400,20), entity.GetName()+"'s Goals");

                SemImplication[] rules = new SemImplication[entity.knowledgeModule.ruleSet.Count];
                entity.knowledgeModule.ruleSet.CopyTo(rules);
                for (int i = 0; i < rules.Length; i++) {
                    GUI.Label(new Rect(2000,50+(i*20),1000,20), rules[i].ToString());
                }
                GUI.Label(new Rect(2000,10,400,20), entity.GetName()+"'s Ruleset");

                /*
                SemImplication[] rules = new SemImplication[entity.knowledgeModule.];
                entity.knowledgeModule.ruleSet.CopyTo(rules);
                for (int i = 0; i < rules.Length; i++) {
                    GUI.Label(new Rect(1800,50+(i*20),1000,20), rules[i].ToString());
                }
                GUI.Label(new Rect(1800,10,400,20), entity.GetName()+"'s Ruleset");
                */

                /*givengoal = GUI.TextField(new Rect(100, 500, 200, 20), givengoal).ToLower();
                if (GUI.Button(new Rect(100, 540, 100, 60), "PlanTo Goal")) 
                {
                    Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
                    Sentence goalparsed = Interpret(new List<string>(givengoal.ToLower().Split(' ')));
                    string s = "Parsed '"+givengoal+"' to: "+goalparsed.GetSemantics().ToString();
                    Debug.Log(s);
                    chatWindow.Add(s);
                    
                    Stack<SemSentence> plan = entity.knowledgeModule.PlanTo(goalparsed.GetSemantics());
                    if (plan is null || plan.Count<1) {
                        Debug.Log("planning failed!");
                    } else {
                        SemSentence[] plan_arr = plan.ToArray();
                        string str = "";
                        foreach (SemSentence step in plan_arr) {
                            str += step.ToString() + ",then..";
                        }
                        Debug.Log("Given plan:"+str);
                    }
                    
                }*/

                givengoal = GUI.TextField(new Rect(100, 500, 200, 20), givengoal).ToLower();
                if (GUI.Button(new Rect(100, 540, 150, 40), "Add Goal to Entity")) 
                {
                    //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
                    Sentence goalparsed = Interpret(new List<string>(givengoal.ToLower().Split(' ')));
                    string s = "Parsed '"+givengoal+"' to: "+goalparsed.GetSemantics().ToString();
                    //Debug.Log(s);
                    chatWindow.Add(s);
                    
                    Debug.Log("Pushed new goal to "+entity.GetName().ToString()+": "+goalparsed.ToLiteralString()+" // "+goalparsed.GetSemantics().ToString());
                    entity.myGoals.Push(goalparsed.GetSemantics());
                    
                }

                GivenStatement = GUI.TextField(new Rect(100, 600, 200, 20), GivenStatement).ToLower();
                if (GUI.Button(new Rect(100, 640, 100, 60), "Evaluate TRuth")) 
                {
                    Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
                    Sentence statementParsed = Interpret(new List<string>(GivenStatement.ToLower().Split(' ')));
                    string s = "Parsed '"+GivenStatement+"' to: "+statementParsed.GetSemantics().ToString();
                    Debug.Log(s);
                    chatWindow.Add(s);
                    
                    Debug.Log("Truth value: "+entity.knowledgeModule.isTrue(statementParsed, out _));
                    
                }

            }
        }
    }
}
