using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace AIKit
{
    using ClassSemanticPair = Tuple<WordClass, object>;
    using SemanticUpdatingFunc = Func<WordClass, List<Tuple<WordClass, object>>, object>;

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
        EMPTY, Name, S, Adj, N, P, V, Vtr, Det, NP, PP, VP, M_if, M_then, Ant, Con, Conj
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
            if (np is null) return;

            AppendWord(np.determiner);
            AppendWord(np.noun);
            SemPP pp = np.pp;
            while (!(pp is null)) {
                AppendWord(pp.preposition);
                AppendNP(pp.np);
            }
        }

        void AppendVP(SemVP vp) {
            if (vp is null) return;

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

    public class GrammarTokenList
    {
        List<WordClass> parts;
        public GrammarTokenList(List<WordClass> parts)
        {
            this.parts = parts;
        }

        public override bool Equals(object o)
        {
            GrammarTokenList other = o as GrammarTokenList;
            if (other is null) return false;
            return this == other;
        }

        public static bool operator ==(GrammarTokenList r1, GrammarTokenList r2)
        {
            return r2.parts.TrueForAll(p => r1.parts.Contains(p)) && r1.parts.TrueForAll(p => r2.parts.Contains(p));
        }

        public static bool operator !=(GrammarTokenList r1, GrammarTokenList r2)
        {
            return !(r1 == r2);
        }

        public override int GetHashCode()
        {
            return string.Join(",", parts).GetHashCode();
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

        //grammar parsing
        public static Dictionary<GrammarTokenList, (WordClass, SemanticUpdatingFunc)> rules;

        public static void ParseGrammarRules()
        {
            rules = new Dictionary<GrammarTokenList, (WordClass, SemanticUpdatingFunc)>();

            //N -> Adj N
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.Adj, WordClass.N })] = (WordClass.N,
                (product, parts) =>
                {
                    //existing adjectives?
                    if (parts[1].Item2 as SemNP is null)
                    {
                        SemNP res = parts[1].Item2 as SemNP;
                        res.adjectives.Add(parts[0].Item2 as LexicalEntry);
                        return res;
                    }
                    else
                    {
                        SemNP res = new SemNP
                        {
                            noun = parts[1].Item2 as LexicalEntry
                        };
                        res.adjectives.Add(parts[0].Item2 as LexicalEntry);
                        return res;
                    }
                }
            );

            //N -> N PP
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.N, WordClass.PP })] = (WordClass.N,
                (product, parts) =>
                {
                    SemNP res = parts[0].Item2 as SemNP;
                    res.pp = parts[1].Item2 as SemPP;
                    return res;
                }
            );

            //NP -> Det N
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.Det, WordClass.N })] = (WordClass.NP,
                (product, parts) =>
                {
                    //bare LE noun?
                    if (parts[1].Item2 as SemNP is null) {
                        SemNP res = new SemNP
                        {
                            determiner = parts[0].Item2 as LexicalEntry,
                            noun = parts[1].Item2 as LexicalEntry
                        };
                        return res;
                    }
                    else
                    {
                        SemNP res = parts[1].Item2 as SemNP;
                        res.determiner = parts[0].Item2 as LexicalEntry;
                        return res;
                    }
                }
            );

            //NP -> Name
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.Name })] = (WordClass.NP,
                (product, parts) => { return new SemNP { noun = parts[0].Item2 as LexicalEntry }; }
            );

            //VP -> Vtr NP
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.Vtr, WordClass.NP })] = (WordClass.VP,
                (product, parts) => {
                    var res = new SemVP {
                        verb = parts[0].Item2 as LexicalEntry
                    };
                    //NP is flexLE?
                    if (parts[1].Item2 as SemNP is null)
                    {
                        res.objects.Add(new SemNP { noun = parts[1].Item2 as LexicalEntry });
                    }
                    else
                    {
                        res.objects.Add(parts[1].Item2 as SemNP);
                    }
                    return res;
                }
            );

            //VP -> Vtr_S S (TODO: implement. Vtr_S are verbs with sentencial objects, e.g. "says", "think", "request"

            //PP -> P NP
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.P, WordClass.NP })] = (WordClass.PP,
                (product, parts) => { return new SemPP { preposition = parts[0].Item2 as LexicalEntry,
                    //NP is FlexLE?
                    np = (parts[1].Item2 as SemNP is null) ? new SemNP{noun = parts[1].Item2 as LexicalEntry} : parts[1].Item2 as SemNP }; }
            );

            //Con -> M(then)S
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.M_then, WordClass.S })] = (WordClass.Con,
                (product, parts) => { return parts[1].Item2 as SemSentence; }
            );

            //Ant -> M(if) S
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.M_if, WordClass.S })] = (WordClass.Ant,
                (product, parts) => { return parts[1].Item2 as SemSentence; }
            );

            //S -> NP VP
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.NP, WordClass.VP })] = (WordClass.S,
                (product, parts) => {
                    return new SemSentence
                    {
                        //NP is FlexLE?
                        np = (parts[0].Item2 as SemNP is null) ? new SemNP { noun = parts[0].Item2 as LexicalEntry } : parts[0].Item2 as SemNP,
                        vp = parts[1].Item2 as SemVP
                    };
                }
            );

            //S -> NP V
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.NP, WordClass.V })] = (WordClass.S,
                (product, parts) =>
                {
                    return new SemSentence
                    {
                        //NP is FlexLE?
                        np = (parts[0].Item2 as SemNP is null) ? new SemNP { noun = parts[0].Item2 as LexicalEntry } : parts[0].Item2 as SemNP,
                        vp = new SemVP { verb = parts[1].Item2 as LexicalEntry }
                    };
                }
            );

            //S -> Ant Con
            //results in a non-literal/meta sentence. an implication.
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.Ant, WordClass.Con })] = (WordClass.S,
                (product, parts) => {
                    SemImplication imp = new SemImplication { antecedent = parts[0].Item2 as SemSentence, consequent = parts[1].Item2 as SemSentence };
                    imp.MakeQuote();
                    return imp;
                }
            );

            //S -> S Conj S
            //results in a non-literal/meta sentence. a compound
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.S, WordClass.Conj, WordClass.S })] = (WordClass.S,
                (product, parts) => {
                    SemCompound conj = new SemCompound { conj = parts[1].Item2 as LexicalEntry, s1 = parts[0].Item2 as SemSentence, s2 = parts[2].Item2 as SemSentence };
                    conj.MakeQuote();
                    return conj;
                }
            );

            //Ant -> Ant Conj S
            //a strange one, but necesary for the compound antecedents to not be ambiguous. TODO: maybe add Con -> Con Conj S
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.Ant, WordClass.Conj, WordClass.S })] = (WordClass.Ant,
                (product, parts) => {
                    SemCompound conj = new SemCompound { conj = parts[1].Item2 as LexicalEntry, s1 = parts[0].Item2 as SemSentence, s2 = parts[2].Item2 as SemSentence };
                    conj.MakeQuote();
                    return conj;
                }
            );
        }

        public static (bool, SemSentence) ValidateGrammar(List<LexicalEntry> words)
        {
            WordClass latestProduct = WordClass.EMPTY;
            Stack<ClassSemanticPair> tokenStack = new Stack<ClassSemanticPair>(words.ConvertAll(w => new ClassSemanticPair(w.wordClass, w as object)));
            Stack<ClassSemanticPair> skipped = new Stack<ClassSemanticPair>();
            Stack<ClassSemanticPair> parts = new Stack<ClassSemanticPair>();

            Debug.Log("Validating: " + string.Join(",", tokenStack));
            bool lastAttempt = false;

            while (tokenStack.Count > 0)//&& ignoreTail < wordClassStack.Count)
            {
                //Debug.Log("Now on: "+ string.Join(",", wordClassStack));
                var thisWord = tokenStack.Pop();
                parts.Push(thisWord);

                GrammarTokenList rule = new GrammarTokenList(parts.ToList().ConvertAll(w => w.Item1));
                bool isnew = false;
                if (rules.ContainsKey(rule))
                {
                    var ruleContent = rules[rule];
                    WordClass producedWordClass = ruleContent.Item1;
                    latestProduct = producedWordClass;

                    //use the parts & object to construct/modify the actual object, store in result.
                    object producedSemanticObject = ruleContent.Item2(producedWordClass, parts.ToList());

                    tokenStack.Push(new ClassSemanticPair(producedWordClass, producedSemanticObject));

                    parts.Clear();
                    isnew = true;
                    lastAttempt = false;

                }

                string wordClassString = thisWord.ToString();
                Debug.Log(wordClassString + ", {" + string.Join(",", parts) + "}, " + latestProduct.ToString() + (isnew ? "*" : ""));

                //if we fail, skip and try again
                if (tokenStack.Count == 0 && !isnew && parts.Count > 0)
                {
                    int n = parts.Count;
                    for (int i = 0; i < n; i++) tokenStack.Push(parts.Pop());
                    skipped.Push(tokenStack.Pop()); // remove tail
                    Debug.Log("Trying again, no tail: " + string.Join(",", tokenStack));
                }

                //if we've skipped everything, pop all skips and try again... or fail, if we've done this before with no results.
                if (tokenStack.Count == 0 && !isnew && parts.Count == 0 && skipped.Count > 0)
                {
                    //fail when we've done all the skipping we can do, but rules didn't work
                    if (lastAttempt) break;
                    else
                    {
                        int n = skipped.Count;
                        for (int i = 0; i < n; i++) tokenStack.Push(skipped.Pop());
                        Debug.Log("Re-added tail: " + string.Join(",", tokenStack));
                        lastAttempt = true;
                    }
                }

                //success! only S remains.
                if (tokenStack.Count == 1 && tokenStack.Peek().Item1 == WordClass.S && skipped.Count == 0 && parts.Count == 0)
                {
                    Debug.Log("Result: " + (tokenStack.Count == 1 && tokenStack.Peek().Item1 == WordClass.S));
                    return (true, tokenStack.Peek().Item2 as SemSentence);
                }
            }

            Debug.Log("Result: " + (tokenStack.Count == 1 && tokenStack.Peek().Item1 == WordClass.S));
            return (false, null);
        }

        //

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

            file = new System.IO.StreamReader("Assets/Scripts/WordLists/conjunctions.txt");
            while ((L = file.ReadLine()) != null)
            {
                L = L.Trim();
                if (L.Length != 0)
                {
                    if (result.ContainsKey(L)) continue;
                    result.Add(L, new LexicalEntry(L, WordClass.Conj, GenerativeWordClass.Conjunctions, Connotation.Neutral));
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

        public static List<LexicalEntry> ExpandToList( LexicalEntry le) {
            List<LexicalEntry> list = new List<LexicalEntry>();
            list.Add(le);
            return list;
        }

        public static Sentence Interpret(List<string> words)
        {
            if (words is null)
                throw new System.Exception("No words provided!");
            if (dictionary is null)
                throw new System.Exception("No dictionary initialized!");

            Debug.Log("Interpreting: '[[[" + string.Join(" ", words) + ".]]]...'");

            List<LexicalEntry> lexicalEntries = new List<LexicalEntry>();

            for (int i = 0; i < words.Count; i++)
            {
                if (!dictionary.ContainsKey(words[i]))
                {
                    Debug.LogError("Unknown word: " + words[i]);
                    words[i] = "???";
                }
                else
                {
                    lexicalEntries.Add(dictionary[words[i]]);
                }
            }

            var res = ValidateGrammar(lexicalEntries);

            if (res.Item1)
            {
                return new Sentence(res.Item2);
            }
            else
            {
                Debug.LogError(string.Join(" ", words.ToArray()));
                throw new System.Exception("Sentence non-grammatical!");
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

            ParseGrammarRules();
            Debug.Log("Loaded " + rules.Count + " grammar rules.");

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
                    
                    //foreach (Memory m in entity.QueryMemories(sentenceParsed.GetLexicalEntryList())) {
                    //    s = "Response: "+m.GetSentence().ToString()+" - "+m.GetSentence().ToLiteralString();
                    //    Debug.Log(s);
                    //    chatWindow.Add(s);
                    //}
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
