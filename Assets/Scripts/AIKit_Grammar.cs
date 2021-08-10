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
        string referentString;
        int referentHash;
        public string GetWord() { return word; }

        public LexicalEntry(LexicalEntry le) {
            this.word = le.word; this.wordClass = le.wordClass; this.connotation = le.connotation; this.generativeWordClass = le.generativeWordClass;
            this.referent = le.GetReferent(); //TODO: always require?
            this.referentString = le.referentString;
            this.referentHash = le.referentHash;
        }
        public LexicalEntry(string s, WordClass wc, GenerativeWordClass gwc, Connotation c) {
            this.word = s; this.wordClass = wc; this.connotation = c; this.generativeWordClass=gwc;
            this.referent = null; //TODO: always require?
            this.referentString = "";
            this.referentHash = 0;
        }

        public override string ToString() {
            return this.word + (this.referent is null ? "" : "=("+this.referentString+")");
        }

        public void AffixReferent(GameObject o) {
            this.referent = o;
            this.referentString = (o is null) ? "" : o.ToString();
            this.referentHash = (o is null) ? 0 : o.GetHashCode();
        }

        public GameObject GetReferent() {
            return this.referent;
        }

        public static bool operator ==(LexicalEntry a, LexicalEntry b) {
            try {
                if (a is null != b is null) return false; //only one null? false
                else if (a is null) return true; //both null? true

                bool referentMatch = (a.referent is null && b.referent is null)?true:(a.referentHash == b.referentHash);
                return (a.word == b.word) && referentMatch;
            } catch (System.NullReferenceException e) {
                if (Prefs.DEBUG) Debug.LogError(e);
                if (!(a is null)) if (Prefs.DEBUG) Debug.Log("^ Tried to compare "+a.word);
                if (!(b is null)) if (Prefs.DEBUG) Debug.Log("^ Tried to compare "+b.word);
                return false;
            }
        }
        public static bool operator !=(LexicalEntry a, LexicalEntry b) {
            return !(a==b);
        }

        public override int GetHashCode() {
            return this.word.GetHashCode() + this.referentHash;
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
            utterance = AIKit_World.Now();
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
            utterance = AIKit_World.Now();
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
            utterance = AIKit_World.Now();
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
        public List<GameObject> EntitiesToTalkTo;
        [SerializeField]
        public string[] startingSentences;
        Entity entity;
        BeAnEntity beAnEntity;
        List<string> chatWindow;
        static bool dictReady = false;

        //grammar parsing
        public static Dictionary<GrammarTokenList, (WordClass, SemanticUpdatingFunc)> rules;

        public static bool IsDictionaryReady() { return dictReady; } 

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
                (product, parts) => { return new SemNP { noun = parts[0].Item2 as LexicalEntry, qt = QuoteType.Invalid }; }
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
                    SemSentence s = new SemSentence
                    {
                        //NP is FlexLE?
                        np = (parts[0].Item2 as SemNP is null) ? new SemNP { noun = parts[0].Item2 as LexicalEntry } : parts[0].Item2 as SemNP,
                        vp = parts[1].Item2 as SemVP
                    };
                    s.MakeLiteral();
                    return s;
                }
            );

            //S -> NP V
            rules[new GrammarTokenList(new List<WordClass>() { WordClass.NP, WordClass.V })] = (WordClass.S,
                (product, parts) =>
                {
                    SemSentence s = new SemSentence
                    {
                        //NP is FlexLE?
                        np = (parts[0].Item2 as SemNP is null) ? new SemNP { noun = parts[0].Item2 as LexicalEntry } : parts[0].Item2 as SemNP,
                        vp = new SemVP { verb = parts[1].Item2 as LexicalEntry }
                    };
                    s.MakeLiteral();
                    return s;
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

            //if (Prefs.DEBUG) Debug.Log("Validating: " + string.Join(",", tokenStack));
            bool lastAttempt = false;

            while (tokenStack.Count > 0)//&& ignoreTail < wordClassStack.Count)
            {
                //if (Prefs.DEBUG) Debug.Log("Now on: "+ string.Join(",", wordClassStack));
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
                //if (Prefs.DEBUG) Debug.Log(wordClassString + ", {" + string.Join(",", parts) + "}, " + latestProduct.ToString() + (isnew ? "*" : ""));

                //if we fail, skip and try again
                if (tokenStack.Count == 0 && !isnew && parts.Count > 0)
                {
                    int n = parts.Count;
                    for (int i = 0; i < n; i++) tokenStack.Push(parts.Pop());
                    skipped.Push(tokenStack.Pop()); // remove tail
                    //if (Prefs.DEBUG) Debug.Log("Trying again, no tail: " + string.Join(",", tokenStack));
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
                        //if (Prefs.DEBUG) Debug.Log("Re-added tail: " + string.Join(",", tokenStack));
                        lastAttempt = true;
                    }
                }

                //success! only S remains.
                if (tokenStack.Count == 1 && tokenStack.Peek().Item1 == WordClass.S && skipped.Count == 0 && parts.Count == 0)
                {
                    //if (Prefs.DEBUG) Debug.Log("Result: " + (tokenStack.Count == 1 && tokenStack.Peek().Item1 == WordClass.S));
                    return (true, tokenStack.Peek().Item2 as SemSentence);
                }
            }

            //if (Prefs.DEBUG) Debug.Log("Result: " + (tokenStack.Count == 1 && tokenStack.Peek().Item1 == WordClass.S));
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
            string L = "";

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

            //initialize IsA scripts after dictionary becomes ready

            //Get everything's name
            dictionary = result;
            dictReady = true;

            IsA[] objs = GameObject.FindObjectsOfType<IsA>();
            foreach (IsA obj in objs)
            {
                //entities get named based on BeAnEntity script, override IsA script's name
                if (obj.gameObject.GetComponent<BeAnEntity>() != null)
                {
                    obj.Name = obj.GetComponent<BeAnEntity>().GetSelf().GetName().noun.GetWord();
                    Debug.LogFormat("Becoming an entity named: {0}", obj.Name);
                    //BecomeA("whomever");
                    //BecomeA("someone");
                    //TODO: Entities should fully identify themselves with IsA edges??

                }

                //random object gets a generic name
                else if (obj.Name is null || obj.Name == "")
                {
                    obj.Name = obj.InitialIdentity[0] + "#" + obj.gameObject.GetHashCode();
                }


                //Add name to dictionary
                LexicalEntry nameLE = new LexicalEntry(obj.Name, WordClass.Name, GenerativeWordClass.Names, Connotation.Neutral);
                nameLE.AffixReferent(obj.gameObject);
                Debug.LogFormat("Added obj named {0} to the dictionary", obj.Name);
                result.Add(obj.Name, nameLE);

                //Identify with name
                obj.BecomeA(obj.Name);
                obj.gameObject.name = obj.gameObject.name + " ( named '" +obj.Name+ "')";

                foreach (string s in obj.InitialIdentity)
                {
                    obj.BecomeA(s);
                }
            }

            //run initla perception fo entities (they needed the dictionary for this)
            GameObject.FindObjectsOfType<BeAnEntity>().ToList().ForEach(e => { if (e.Perceiving) e.RunInitialPerception(); });
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

            if (Prefs.DEBUG) Debug.Log("Interpreting: '[[[" + string.Join(" ", words) + ".]]]...'");

            List<LexicalEntry> lexicalEntries = new List<LexicalEntry>();

            for (int i = 0; i < words.Count; i++)
            {
                if (!dictionary.ContainsKey(words[i]))
                {
                    if (Prefs.DEBUG) Debug.LogError("Unknown word: " + words[i]);
                    words[i] = "???";
                }
                else
                {
                    Debug.LogFormat("{0} => {1}", words[i], dictionary[words[i]]);
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
                if (Prefs.DEBUG) Debug.LogError(string.Join(" ", words.ToArray()));
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
            SemSentence filled = SemSentence.NewCopy(sentenceWithPronoun);
            if (filled.IsCompound() || filled.IsImplication() || context.IsCompound() || context.IsImplication()) return filled; // TODO: make this work for impl and comp one day
        
            //check if subj is pronoun
            if (sentenceWithPronoun.np.noun.generativeWordClass == GenerativeWordClass.Deictic) {
                filled.np = ReplaceVia(filled.np, context.np);
            }

            //check if obj is pronoun
            for (int i = 0; i < sentenceWithPronoun.vp.objects.Count; i++) {
                if (sentenceWithPronoun.vp.objects[i].noun.generativeWordClass == GenerativeWordClass.Deictic) {
                    if (context.vp.objects.Count > i)
                        filled.vp.objects[i] = ReplaceVia(filled.vp.objects[i], context.vp.objects[i]);
                    else
                        filled.vp.objects[i] = ReplaceVia(filled.vp.objects[i], context.vp.objects[context.vp.objects.Count - 1]);
                }
            }

            return filled;
        }

        public static SemSentence FillTemplatePronouns(SemSentence sentenceToFill, SemSentence sentenceWithPronouns, SemSentence sentenceWithAntecedents)
        {
            if (Prefs.DEBUG) Debug.LogError("Filling " + sentenceToFill.ToString() + " with words from " + sentenceWithAntecedents + " from templates " + sentenceWithPronouns);

            SemSentence sToFill = SemSentence.NewCopy(sentenceToFill);
            if (sentenceToFill.IsCompound() || sentenceToFill.IsImplication() || sentenceWithPronouns.IsCompound() || sentenceWithPronouns.IsImplication() || sentenceWithAntecedents.IsCompound() || sentenceWithAntecedents.IsImplication()) return sToFill; // TODO: make this work for impl and comp one day

            if (Prefs.DEBUG) Debug.LogError("Filling sentences all valid!");

            //check if subj is pronoun
            if (sentenceWithPronouns.np.noun.generativeWordClass == GenerativeWordClass.Deictic)
            {
                sToFill.np = ReplaceVia(sentenceWithPronouns.np, sentenceWithAntecedents.np);
            }

            //check if obj is pronoun
            for (int i = 0; i < sentenceWithPronouns.vp.objects.Count; i++)
            {
                if (sentenceWithPronouns.vp.objects[i].noun.generativeWordClass == GenerativeWordClass.Deictic)
                {
                    if (sentenceWithAntecedents.vp.objects.Count > i)
                        sToFill.vp.objects[i] = ReplaceVia(sentenceWithPronouns.vp.objects[i], sentenceWithAntecedents.vp.objects[i]);
                    else
                        sToFill.vp.objects[i] = ReplaceVia(sentenceWithPronouns.vp.objects[i], sentenceWithAntecedents.vp.objects[sentenceWithAntecedents.vp.objects.Count - 1]);
                }
            }

            return sToFill;
        }

        public static SemSentence TakePronouns(SemSentence context, SemSentence sentenceToPlace) {
            SemSentence filled = SemSentence.NewCopy(sentenceToPlace);
            if (filled.IsCompound() || filled.IsImplication() || context.IsCompound() || context.IsImplication()) return filled; // TODO: make this work for impl and comp one day

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
            if (filled.IsCompound() || filled.IsImplication() || context.IsCompound() || context.IsImplication()) return filled; // TODO: make this work for impl and comp one day

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

            if (grabbed is null) if (Prefs.DEBUG) Debug.LogError("Somehow the template's flex wasn't found!");

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
                if (Prefs.DEBUG) Debug.Log("Parsing word lists into Dicitonary...");
                ParseDictionary();
                if (Prefs.DEBUG) Debug.Log("Dicitonary complete.");
            }

            ParseGrammarRules();
            if (Prefs.DEBUG) Debug.Log("Loaded " + rules.Count + " grammar rules.");
            
            chatWindow = new List<string>();

            foreach (string s in startingSentences) {
                foreach (GameObject entityGameObject in EntitiesToTalkTo)
                {
                    var entity = entityGameObject.GetComponent<BeAnEntity>().GetSelf();
                    if (entity is null) continue;
                    try
                    {
                        Sentence sentenceParsed = Interpret(new List<string>(s.ToLower().Split(' ')));
                        entity.addMemory(sentenceParsed);
                    }
                    catch
                    {
                        Debug.LogErrorFormat("Failed to parse the sentence {0}!", s);
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public string givenSentence, givenWord, givengoal, GivenStatement;
        Sentence sentenceParsed;
        LexicalEntry wordParsed;
}
}
