using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static System.Collections.IList;

namespace AIKit 
{
    public enum LogicClass //used for FOL
    {
        Atomic, Universal, Existential
    }
    public enum QuoteType
    {
        Invalid, Literal, Start, Mid, End
    }

    public class KnowledgeModule {
        public LexicalMemory lexicalMemory;
        public EpisodicMemory episodicMemory;
        public HashSet<SemImplication> ruleSet;
        public Dictionary<SemSentence,List<SemSentence>> waysTo;
        public Dictionary<SemSentence,List<SemSentence>> resultsFrom;
    
        public HashSet<SemSentence> perceptualFacts; //these things operate so quickly and on the fly that i want them in a more rapid and non-persistent location?
        public LexicalEntry myName;

        public KnowledgeModule(LexicalEntry myName) {
            this.episodicMemory = new EpisodicMemory();
            this.lexicalMemory = new LexicalMemory();
            this.ruleSet = new HashSet<SemImplication>();
            this.waysTo = new Dictionary<SemSentence, List<SemSentence>>();
            this.resultsFrom = new Dictionary<SemSentence, List<SemSentence>>();
            this.myName = myName;

            this.perceptualFacts = new HashSet<SemSentence>();
        }

        void interpretSentence(Sentence s, float salience)
        {
            interpretSentence(s, s.GetSemantics(), salience);
        }

        void interpretSentence(Sentence s, SemSentence sem, float salience) {
            Debug.Log("Interpreting Sentence "+s.ToString()+"...");
            //Debug.Log("Is this a rule? "+s.GetSemantics().ToString()+" : "+ (!(s.GetSemantics() as SemImplication is null)));

            //break down sentence, modify lexical memory & semantic web apropriately
            Queue<SemSentence> roots = new Queue<SemSentence>();
            roots.Enqueue(sem);

            //if requested, give a ref to the first np's node.

            int timeout = 100; //okay, this might be mean but i need to limit the procesing time on these & avoid loops sooo...

            //We will interpret this sentence AND any sentences entailed by this sentence
            while (roots.Count > 0 && timeout > 0) {
                timeout--;
                SemSentence root = roots.Dequeue();

                //is it an implication?
                if (root.IsImplication())
                {
                    Debug.LogError("BEFORE LEARNRULE: "+ s.GetSemantics().ToString());
                    this.LearnRule(s);
                    Debug.LogError("AFTER LEARNRULE: " + s.GetSemantics().ToString());

                    SemImplication imp = root as SemImplication;

                    interpretSentence(s, imp.antecedent, salience);
                    interpretSentence(s, imp.consequent, salience);

                    SemanticWebNode antLast = lexicalMemory.GetOrInsert(imp.antecedent.GetLastNP());
                    SemanticWebNode consFirst = lexicalMemory.GetOrInsert(imp.consequent.GetFirstNP());

                    antLast.AddEdgeTo(AIKit_Grammar.EntryFor("implies"), consFirst, s, salience);
                    continue;
                }

                //is it a compound?
                if (root.IsCompound())
                {
                    SemCompound com = root as SemCompound;

                    interpretSentence(s, com.s1, salience);
                    interpretSentence(s, com.s2, salience);

                    SemanticWebNode s1Last = lexicalMemory.GetOrInsert(com.s1.GetLastNP());
                    SemanticWebNode s2First = lexicalMemory.GetOrInsert(com.s2.GetFirstNP());

                    s1Last.AddEdgeTo(com.conj, s2First, s, salience);
                    continue;
                }

                //Handle normal sentences
                //might cause infinite loops? but implication elimination happens here :c (so i added the timout counter)
                foreach (SemSentence conclusion in GetResultsFrom(root).ConvertAll((result) => AIKit_Grammar.FillPronouns(s.GetSemantics(), result))) {
                    roots.Enqueue(conclusion);
                    Debug.Log("Derived rule-based result:\t" + conclusion.ToString());
                }

                SemNP np = root.np;
                List<SemanticWebNode> subjectNodes = GetWebNodeByNP(np, s, salience, 1);
                SemVP vp = root.vp;

                foreach (SemanticWebNode subjectNode in subjectNodes) {
                    foreach (SemNP obj in vp.objects) {
                        List<SemanticWebNode> objNodes = GetWebNodeByNP(obj, s, salience, 1);//vp.verb.ToString().StartsWith("no")?0:1);
                        foreach (SemanticWebNode objNode in objNodes) {
                            //Debug.Log("\tDerived conclusion:\t"+ subjectNode.GetString() +" "+ vp.verb.ToString() +" "+ objNode.GetString());
                            SemSentence newRoot = new SemSentence(subjectNode.GetAliases()[0], vp.verb, objNode.GetAliases()[0]);
                            if (newRoot != root) roots.Enqueue(newRoot); //to avoid infinite loops dont push what i just derived. 
                            //maybe even make a set of these old roots for avoidin larger loops?
                            subjectNode.AddEdgeTo(vp.verb, objNode, s, salience);
                        }
                    }

                    /* TODO: handle quoted sentences
                    foreach (SemSentence obj in vp.sentenceObjects) {
                        List<SemanticWebNode> objNodes = interpretObject(obj, s, salience, vp.verb.ToString().StartsWith("no")?0:1);
                        foreach (SemanticWebNode objNode in objNodes) {
                            //Debug.Log("Derived conclusion:\t"+ subjectNode.GetString() +" "+ vp.verb.ToString() +" "+ objNode.GetString());
                            SemSentence newRoot = new SemSentence(subjectNode.GetAliases()[0], vp.verb, objNode.GetAliases()[0]);
                            if (newRoot != root) roots.Enqueue(newRoot); //to avoid infinite loops dont push what i just derived. 
                            //maybe even make a set of these old roots for avoidin larger loops?
                            subjectNode.AddEdgeTo(vp.verb, objNode, s, salience);
                        }
                    }*/

                    //intransitive verb vp's have no objects!
                    if (vp.objects.Count == 0 && vp.sentenceObjects.Count == 0)
                    {
                        subjectNode.AddEdgeTo(vp.verb, null, s, salience);
                    }
                }
            } 
        }

        //get Node by np, always using QuoteType literal. maybe handle choosing form multi matches HERE (instead of GetOrInsert)
        List<SemanticWebNode> GetWebNodeByNP(SemNP np, Sentence s, float salience, int flexibility) {
            //flexibility settings:
            //  0 - GET HYPONYMS/reverse IS'es (things which entail this) a carrot -is-> a vegetable
            //  1 - GET HYPERNYMS/forward IS'es (things which are entailed BY this)
            // any other number has no flexibility//doesnt traverse any IS edges. probably a bug.

            SemanticWebNode n = lexicalMemory.GetOrInsert(np); //if there's an existing node for this np we just update it, o.w. make one...
            List<SemanticWebNode> matchingNodes = new List<SemanticWebNode> {n};
            switch (flexibility) {
                case 0: {
                    if (n.some) break; //try not to get entailment from 'some'
                    List<SemanticWebNode> e = n.TraverseEdgeRev(AIKit_Grammar.EntryFor("is"));
                    if (!(e is null))
                        matchingNodes.AddRange(e); 
                    break;
                }
                case 1: {
                    if (n.some) break; //try not to get entailment from 'some'
                    List<SemanticWebNode> e = n.TraverseEdge(AIKit_Grammar.EntryFor("is"));
                    if (!(e is null))
                        matchingNodes.AddRange(e);
                    break;
                }
                default: Debug.LogWarning("Inflexible object interpretation."); break;
            }

            SemPP pp = np.pp;
            if (!(pp is null)) {
                List<SemanticWebNode> ppObjects = GetWebNodeByNP(pp.np, s, salience, 1);

                foreach (SemanticWebNode node in matchingNodes) {
                    //if (np.determiner.ToString() == "the") Do something? -- the above line already carries over referents, so..
                    foreach (SemanticWebNode ppObj in ppObjects) {
                        node.AddEdgeTo(pp.preposition, ppObj, s, salience);
                    }
                }

            }
            

            return matchingNodes;
        }

        ////Given a quoted semS, adds semanticwebnodes for it and returns the node for the last or first semNP in the quote's chain
        //SemanticWebNode GetWebNodeBySentence(SemSentence s, Sentence originalSentence, float salience, bool last)
        //{
        //    if (!s.IsQuoted()) { Debug.LogError("Trying to interpret non-Quote as Quote!"); }

        //    //is it an implication?
        //    if (s.IsImplication())
        //    {
        //        SemImplication imp = s as SemImplication;
        //        SemanticWebNode antLast = GetWebNodeBySentence(imp.antecedent, originalSentence, salience, true);
        //        SemanticWebNode consFirst = GetWebNodeBySentence(imp.consequent, originalSentence, salience, false);
        //        antLast.AddEdgeTo(AIKit_Grammar.EntryFor("implies"), consFirst, originalSentence, salience);
        //    }

        //    //is it a compound?
        //    if (s.IsCompound())
        //    {
        //        SemCompound com = s as SemCompound;
        //        SemanticWebNode s1Last = GetWebNodeBySentence(com.s1, originalSentence, salience, true);
        //        SemanticWebNode s2First = GetWebNodeBySentence(com.s2, originalSentence, salience, false);
        //        s1Last.AddEdgeTo(com.conj, s2First, originalSentence, salience);
        //    }

        //    //normal sentences, link np node to objects then return last (or first) node.
        //    SemanticWebNode subjectNode = lexicalMemory.GetOrInsert(s.np);

        //}

        public void interpretEvent(Sentence s, float salience ) {
            //take in sentence, modify lexical and episodic memories apropriately
            EpisodicMemoryEntry eme = new EpisodicMemoryEntry(s, salience);
            //episodicMemory.Insert(s, salience); TODO: fix data structure later
            interpretSentence(s, salience);
        }

        //FOL 
        public LogicClass GetLogicClass(SemNP np) {
            LexicalEntry d = np.determiner;
            if (d is null || !(np.noun.GetReferent() is null) || d.WordEquals("the")) return LogicClass.Atomic; //if it has a referent, its probably atomic
            //TODO: The should have its referent set in context of utterance.... or not be valid at all!
            // But since im using ghostly words ("the --" w/o referent) rn i'll allow that last if condition

            if ( d.WordEquals("any") || d.WordEquals("some") || d.WordEquals("a")) return LogicClass.Existential;
            if ( d.WordEquals("every") || d.WordEquals("all") ) return LogicClass.Universal;

            Debug.LogWarning("Passthru on determining Logic Class/Quantification of NP:"+ np.ToString());
            return LogicClass.Atomic;
        }

        //Look for an edge representing a sentence, OR any edges which entail that edge's existence
        //Rev is appropriate here.
        bool DoNodesConnect(SemanticWebNode from, SemanticWebNode to, LexicalEntry verb, out string console) {
            console = "";
            List<SemanticWebNode> possibleFroms = new List<SemanticWebNode> ();
            //possibleFroms.AddRange(GetHyponymsOf(from).ConvertAll((np) => lexicalMemory.GetOrInsert(np))); //not enough detail? shared aliases?

            /*possibleFroms.AddRange( from.TraverseEdgeRev(AIKit_Grammar.EntryFor("is")).FindAll(n => !n.some) );
            possibleFroms.Add( lexicalMemory.GetAnythingNode() );
            //add the anything node for this specific noun (TODO: probably remove tese unneccesary steps and just save this node's ref to each node)
            SemNP fromNP = from.GetAliases()[0];
            if (!(fromNP.determiner is null) && !fromNP.determiner.WordEquals("any")) {
                fromNP.determiner = AIKit_Grammar.EntryFor("any");
                fromNP.noun.AffixReferent(null);
                possibleFroms.Add( lexicalMemory.GetOrInsert(fromNP) );
            }*/
            //^this has to go further!! if it doesnt recurse, transitivity will be ignored!

            //possibleFroms.AddRange(GetHyponymNodesOf(from));
            //possibleFroms.RemoveAll((node) => node.some);
            possibleFroms.Add(from);


            List<SemanticWebNode> possibleTos = new List<SemanticWebNode> ();
            /*possibleTos.AddRange( to.TraverseEdgeRev(AIKit_Grammar.EntryFor("is")).FindAll(n => !n.some) );
            possibleTos.Add( lexicalMemory.GetAnythingNode() );
            //add the anything node for this specific noun
            SemNP toNP = from.GetAliases()[0];
            if (!(toNP.determiner is null) && !toNP.determiner.WordEquals("any")) {
                toNP.determiner = AIKit_Grammar.EntryFor("any");
                toNP.noun.AffixReferent(null);
                possibleTos.Add( lexicalMemory.GetOrInsert(toNP) );
            }*/

            //possibleTos.AddRange(GetHyponymNodesOf(to));
            //possibleTos.RemoveAll((node) => node.some);
            if (!(to is null)) possibleTos.Add(to);

            //don't just check for one connection: weigh the yeses and nos!
            foreach(SemanticWebNode f in possibleFroms) {
                foreach( SemanticWebNode t in possibleTos) {
                    List<SemanticWebNode> returnedTos = f.TraverseEdge(verb);
                    string antiVerbS = verb.ToString().StartsWith("no") ? verb.ToString().Substring(2) : "no"+verb.ToString();
                    LexicalEntry antiVerb = AIKit_Grammar.EntryFor(antiVerbS);
                    List<SemanticWebNode> returnedAntiTos = f.TraverseEdge(antiVerb);

                    //Debug.LogError("\tAttempting connection via \t"+f.GetString()+" "+verb.ToString()+" "+t.GetString());
                    float recentNo = 0;
                    int nocount = 0;
                    if (returnedAntiTos.Contains(t)) {
                        foreach (SemanticWebEdge e in f.GetEdges(antiVerbS)) {
                            nocount++;
                            recentNo += Mathf.Exp(GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now().val() - e.why.utterance.val()); 
                        }
                        Debug.Log("\tDeconfirmed x"+nocount+": "+f.GetString()+" "+antiVerb.ToString()+" "+t.GetString());
                    }
                    
                    float recentYes = 0;
                    int yescount = 0;
                    if (returnedTos.Contains(t)) {
                        foreach (SemanticWebEdge e in f.GetEdges(verb.ToString())) {
                            yescount++;
                            recentYes += Mathf.Exp(GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now().val() - e.why.utterance.val()); 
                        }
                        Debug.Log("\tConfirmed x"+yescount+": "+f.GetString()+" "+verb.ToString()+" "+t.GetString());
                    }

                    console+=f.GetString() + " "+verb.ToString()+" "+t.GetString()+"? "+ recentYes + " for, " + recentNo + " against.\n";
                    if ((recentYes - recentNo) > 0) {
                        console += "True!\n";
                        return true;
                    }
                    console += "Fail.\n";
                }

                //if this is intransitive, we don't have possibleTos. Do a different kind of polling
                if (possibleTos.Count == 0)
                {
                    string antiVerbS = verb.ToString().StartsWith("no") ? verb.ToString().Substring(2) : "no" + verb.ToString();
                    LexicalEntry antiVerb = AIKit_Grammar.EntryFor(antiVerbS);

                    float recentNo = 0;
                    int nocount = 0;
                    foreach (SemanticWebEdge e in f.GetEdges(antiVerbS))
                    {
                        nocount++;
                        recentNo += Mathf.Exp(GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now().val() - e.why.utterance.val());
                    }
                    Debug.Log("\tDeconfirmed x" + nocount + ": " + f.GetString() + " " + antiVerb.ToString());

                    float recentYes = 0;
                    int yescount = 0;
                    foreach (SemanticWebEdge e in f.GetEdges(verb.ToString()))
                    {
                        yescount++;
                        recentYes += Mathf.Exp(GameObject.FindGameObjectWithTag("AIKW").GetComponent<AIKit_World>().Now().val() - e.why.utterance.val());
                    }
                    Debug.Log("\tConfirmed x" + yescount + ": " + f.GetString() + " " + verb.ToString());

                    console += f.GetString() + " " + verb.ToString() + "? " + recentYes + " for, " + recentNo + " against.\n";
                    if ((recentYes - recentNo) > 0)
                    {
                        console += "True!\n";
                        return true;
                    }
                    console += "Fail.\n";
                }
            }


            Debug.Log("\tCould not prove: "+from.GetString()+" "+verb.ToString()+" "+ ((to is null) ? "" : to.GetString()));
            return false;
        }

        bool BranchIsTrue(SemanticWebNode rootNode, SemVP branch, out string console) {
            console = "Checking branch " + branch.ToString() +"...\n";

            //does quantification checking and validation on branch objects of a given root (root quantification is handled by parent)
            foreach (SemNP branchObject in branch.objects) {
                console+="For object "+ branchObject.ToString() +":\n";
                switch (GetLogicClass(branchObject)) {
                    case LogicClass.Atomic:
                        console+="An atomic object -- only one check.\n";
                        //if the branch obj is atomic, simple node check
                        //edge DNE or the edge's node doesn't match the goal node form sentence = fail
                        SemanticWebNode definiteBranchObjectNode = lexicalMemory.GetOrInsert(branchObject);
                        string s;
                        bool r = DoNodesConnect(rootNode, definiteBranchObjectNode, branch.verb, out s);
                        console += s;
                        if (!r) {
                            return false;
                        }
                        break;

                    case LogicClass.Existential:
                        //existential: at least one node with this alias must match
                        console+="An existential object -- need at least one hit.\n";
                        SemNP singularBranchObject = new SemNP(branchObject);
                        singularBranchObject.determiner = AIKit_Grammar.EntryFor("a");
                        foreach(SemanticWebNode matchingObjectNode in lexicalMemory.NodesWithAlias(singularBranchObject)) {
                            //TODO: allow ANY of the multiple edges with this verb.
                            bool res = DoNodesConnect(rootNode, matchingObjectNode, branch.verb, out string addtl);
                            console += addtl;
                            if (res) {
                                console+="Found a true example for: " + matchingObjectNode.GetString() + ".\n";
                                return true;
                            }
                        }
                        console+="All false!\n";
                        return false;

                    case LogicClass.Universal:
                        //universal: all nodes with this alias must match
                        console+="A universal object -- all must match.\n";
                        SemNP singularBranchObject2 = new SemNP(branchObject);
                        singularBranchObject2.determiner = AIKit_Grammar.EntryFor("a");
                        foreach(SemanticWebNode matchingObjectNode in lexicalMemory.NodesWithAlias(singularBranchObject2)) {
                            bool res = DoNodesConnect(rootNode, matchingObjectNode, branch.verb, out string addtl);
                            console += addtl;
                            if (!res) {
                                console+="Found a false example for: " + matchingObjectNode.GetString() + ".\n";
                                return false;
                            }
                        }
                        console+="All true!\n";
                        return true;

                    default:
                        Debug.LogError("Broken Logic Class on object");
                        return false;
                }

                //if this is intransitive we don't need to check branch objects.
                console += "Intransitive verb! (no objects) Just checking for edge existence\n";
                bool ret = DoNodesConnect(rootNode, null, branch.verb, out string a); //No object node!
                console += a;
                return ret;
            }

            /* TODO: handle sentencial objects differently, the new way
            foreach (SemSentence branchObject in branch.sentenceObjects) {
                //sentencial branch obj ALWAYS simple node check
                SemanticWebNode branchObjectNode = lexicalMemory.GetOrInsert(branchObject);
                
                //edge DNE or the edge's node doesn't match the goal node form sentence = fail
                string addtl;
                bool res = DoNodesConnect(rootNode, branchObjectNode, branch.verb, out addtl);
                console += addtl;
                if (!res) return false;
            }
            */

            //if all the objects were satisfied, return true;
            return true;
        }

        public bool isTrue(SemSentence s, out string console){
            console = "";

            //implications, compounds, and quotes are validated differently.
            if (s.IsQuoted() || s.IsCompound() || s.IsImplication()) return false;

            SemNP root = new SemNP(s.np);
            SemVP branch = new SemVP(s.vp);
            SemSentence why;

            if (this.perceptualFacts.Contains(s)) {
                console+="This is a perceptual fact.\n";
                return true;
            }
            console+="This is NOT a perceptual fact.\n";

            //referents are either described by noun LEs or are entire Sentences (sentencial objects).
                //if its universal, check all referents of the noun/sentence. must be true for ALL.
                //if its existential, check all referents of the noun/sentence. must be true for ONE.
                //if its definite a.k.a atomic, check the single referent of the noun/sentence.

            
            switch (GetLogicClass(root)) 
            {

                //if the root is atomic, simply use that single node for checking
                case LogicClass.Atomic:
                    console+="An atomic subject -- only one check.\n";
                    SemanticWebNode rootNode = lexicalMemory.GetOrInsert(root);
                    string a;
                    bool res = BranchIsTrue(rootNode, branch, out a);
                    console += a;
                    return res;

                //existential: at least one root with this alias must match
                case LogicClass.Existential:
                    console+="An existential subject -- any one node will do.\n";
                    SemNP singularRoot = new SemNP(root);
                    singularRoot.determiner = AIKit_Grammar.EntryFor("a");
                    foreach(SemanticWebNode matchingRootNode in lexicalMemory.NodesWithAlias(singularRoot)) {
                        //TODO: allow ANY of the multiple edges with this verb.
                        string addtl;
                        if (BranchIsTrue(matchingRootNode, branch, out addtl)) {
                            console += addtl;
                            console+="Found a true example for: " + matchingRootNode.GetString() + ".\n";
                            return true;
                        }
                    }
                    console+="All false!\n";
                    return false;

                //universal: all roots with this alias must match
                case LogicClass.Universal:
                    console+="A universal subject -- all nodes must match\n";
                    SemNP singularRoot2 = new SemNP(root);
                    singularRoot2.determiner = AIKit_Grammar.EntryFor("a");
                    foreach(SemanticWebNode matchingRootNode in lexicalMemory.NodesWithAlias(singularRoot2)) {
                        //TODO: allow ANY of the multiple edges with this verb.
                        string addtl;
                        if (!BranchIsTrue(matchingRootNode, branch, out addtl)) {
                            console += addtl;
                            console+="Found a false example for: " + matchingRootNode.GetString() + ".\n";
                            return false;
                        }
                    }
                    console+="All true!\n";
                    return true;
            }
            Debug.LogError("Fellthrough on root "+root.ToString()+" LogicClasses!");
            return false;
        }

        public bool isTrue(Sentence s, out string str){
            if (isTrue(s.GetSemantics(), out str)) return true;

            str += "Core sentence failed, trying entailing sentences...\n";

            List<SemSentence> entailingSentences = GetSentencesThatEntail(s.GetSemantics());
            foreach (SemSentence eS in entailingSentences) {
                str += "Trying entailing sentence: " + eS.ToString() +"\n";
                string add;
                bool res = isTrueR(eS, out add, new List<SemSentence>{s.GetSemantics()});
                str += add;
                if (res) {
                    str += "Entailed sentence true, so this is true! :)\n";
                    return true;
                }
            }

            str += "Entailing sentences failed! Trying implications...\n";

            List<SemSentence> implyingSentences = GetSentencesThatImply(s.GetSemantics());
            foreach (SemSentence iS in implyingSentences) {
                str += "Trying implying sentence: " + iS.ToString() +"\n";
                string add;
                bool res = isTrueR(iS, out add, new List<SemSentence>{s.GetSemantics()});
                str += add;
                if (res) {
                    str += "Implying sentence true, so this is true! :)\n";
                    return true;
                }
            }

            str += "Implying sentences failed!\n";
            return false;
        }  

        public bool isTrueR(SemSentence s, out string str, List<SemSentence> alreadyChecked){
            //avoid loops
            if (alreadyChecked.Contains(s)) {
                str = "";
                return false;
            }

            if (isTrue(s, out str)) return true;

            str += "Core sentence failed, trying entailing sentences...\n";
            alreadyChecked.Add(s);

            List<SemSentence> entailingSentences = GetSentencesThatEntail(s);
            foreach (SemSentence eS in entailingSentences) {
                //avoidloops
                if (alreadyChecked.Contains(eS)) continue;

                str += "Trying entailing sentence: " + eS.ToString() +"\n";
                string add;
                bool res = isTrueR(eS, out add, alreadyChecked);
                str += add;
                if (res) {
                    str += "Entailed sentence true, so this is true! :)\n";
                    return true;
                }
                alreadyChecked.Add(eS);
            }

            str += "Entailing sentences failed! Trying implications...\n";

            List<SemSentence> implyingSentences = GetSentencesThatImply(s);
            foreach (SemSentence iS in implyingSentences) {
                //avoid reflexive b  loops? what is Sa <-> Sb ? oops
                if (alreadyChecked.Contains(iS)) continue;

                str += "Trying implying sentence: " + iS.ToString() +"\n";
                string add;
                bool res = isTrueR(iS, out add, alreadyChecked);
                str += add;
                if (res) {
                    str += "Implying sentence true, so this is true! :)\n";
                    return true;
                }
                alreadyChecked.Add(iS);
            }

            str += "Implying sentences failed!\n";
            return false;
        }    

        //GOAP

        public void LearnRule(Sentence s) {
            SemImplication rule = s.GetSemantics() as SemImplication;
            rule.MakeLiteral();

            if (rule is null) return;

            Debug.LogError ("Learning Rule: " + rule.ToString());

            this.ruleSet.Add(rule); //save the rule along with pronouns...

            //AND... put a map from antecedent to consequent (for "drawing conclusions")
            SemSentence newAntecedent = AIKit_Grammar.FillPronouns(rule.consequent, rule.antecedent);
            Debug.LogError("Pronoun filled antecedent:" + newAntecedent.ToString());
            SemSentence matchingConsequent = AIKit_Grammar.TakePronouns(rule.antecedent, rule.consequent);
            Debug.LogError("Matching consequent:" + matchingConsequent.ToString());
            if (this.resultsFrom.ContainsKey(newAntecedent)) {
                this.resultsFrom[newAntecedent].Add(matchingConsequent);
            } else {
                this.resultsFrom.Add(newAntecedent, new List<SemSentence>() {matchingConsequent});
            }

            //also put a map from consequent to antecedent
            SemSentence newConsequent = AIKit_Grammar.FillPronouns(rule.antecedent, rule.consequent);
            Debug.LogError("Pronoun filled consequent:" + newConsequent.ToString());
            SemSentence matchingAntecedent = AIKit_Grammar.TakePronouns(rule.consequent, rule.antecedent);
            Debug.LogError("Matching antecedent:" + matchingAntecedent.ToString());
            if (this.waysTo.ContainsKey(newConsequent)) {
                this.waysTo[newConsequent].Add(matchingAntecedent);
            } else {
                this.waysTo.Add(newConsequent, new List<SemSentence>() {matchingAntecedent});
            }

            rule.MakeQuote();
        }

        public List<SemSentence> GetWaysTo2(SemSentence goal) {
            List<SemSentence> allWaysTo = new List<SemSentence>();

            //save originals
            SemNP subject = goal.np;
            List<SemNP> objects = goal.vp.objects;

            //for aliasing
            List<SemNP> originals = new List<SemNP> {objects[0], subject};

            //get entailing sentences
            List<SemSentence> entailingSentences = GetSentencesThatEntail(goal);
            List<SemSentence> aliasedEntailingSentences = entailingSentences; //Necessary? .ConvertAll((sentence) => AliasedSentence(sentence, new List<SemNP> {sentence.vp.objects[0], }, originals));
            aliasedEntailingSentences.Add(goal);

            foreach (SemSentence sentence in aliasedEntailingSentences) {
                if (waysTo.ContainsKey(sentence)) {
                    allWaysTo.AddRange(waysTo[sentence]);
                }
            }

            return allWaysTo;
        }

        public List<SemSentence> GetResultsFrom2(SemSentence fact) {
            List<SemSentence> allResultsFrom = new List<SemSentence>();

            //save originals
            SemNP subject = fact.np;
            List<SemNP> objects = fact.vp.objects;

            //for aliasing
            List<SemNP> originals = new List<SemNP> {objects[0], subject};

            //get entailing sentences
            List<SemSentence> entailedSentences = GetSentencesEntailedBy(fact);
            List<SemSentence> aliasedEntailedSentences = entailedSentences; //Necessary? .ConvertAll((sentence) => AliasedSentence(sentence, new List<SemNP> {sentence.vp.objects[0], }, originals));
            aliasedEntailedSentences.Add(fact);

            foreach (SemSentence sentence in aliasedEntailedSentences) {
                if (resultsFrom.ContainsKey(sentence)) {
                    allResultsFrom.AddRange(resultsFrom[sentence]);
                }
            }

            return allResultsFrom;
        }

        public List<SemSentence> GetWaysTo(SemSentence goal) {
            Debug.LogWarning("Looking for ways to:\t"+goal.ToString());
            List<SemSentence> allWaysTo = new List<SemSentence>();

            Debug.Log("waysTo Dictionary content:" + string.Join(", ", this.waysTo.Keys));
            
            SemanticWebNode subjNode = this.lexicalMemory.GetOrInsert(goal.np);
            List<SemanticWebNode> objNodes = goal.vp.objects.ConvertAll((obj) => lexicalMemory.GetOrInsert(obj));
            
            List<SemNP> subjMonikers = GetHyponymsOf(subjNode);
            List<List<SemNP>> objMonikers = objNodes.ConvertAll((node) => GetHyponymsOf(node));//goal.vp.verb.ToString().StartsWith("no")?objNodes.ConvertAll((node) => GetHypernymsOf(node)):objNodes.ConvertAll((node) => GetHyponymsOf(node));

            //add the og names
            subjMonikers.Add(goal.np);
            for (int i = 0; i < objMonikers.Count; i++) {
                objMonikers[i].Add(goal.vp.objects[i]);
            }

            Debug.Log("subjects: " + string.Join("/", subjMonikers) + ", objects: " + string.Join(" - ", objMonikers.ConvertAll((list) => string.Join("/", list))));

            foreach (SemNP subjectMoniker in subjMonikers) {
                //if we only have sentencial objects in our goal just try one sentence with that
                if (objMonikers.Count == 0) {
                    if (this.waysTo.ContainsKey(goal))
                        allWaysTo.AddRange(this.waysTo[goal]);
                }

                for (int obj_index = 0; obj_index < objMonikers.Count; obj_index++) {
                    foreach (SemNP object_i_Moniker in objMonikers[obj_index]) {
                        SemSentence aliasedGoal = new SemSentence(goal);
                        aliasedGoal.np = subjectMoniker;
                        aliasedGoal.vp.objects[obj_index] = object_i_Moniker;

                        Debug.LogError("Checking dict for \t" + aliasedGoal.ToString());

                        if (this.waysTo.ContainsKey(aliasedGoal)) {
                            //Debug.LogWarning("1.Replacing all occurrences of "+object_i_Moniker.ToString()+" with "+goal.vp.objects[obj_index].ToString()+"in the sentence: "+aliasedGoal.ToString());
                            List<SemSentence> results = AliasedWaysTo(aliasedGoal, new List<SemNP> { object_i_Moniker, subjectMoniker }, new List<SemNP> { goal.vp.objects[obj_index], goal.np });
                            allWaysTo.AddRange(results);
                            Debug.Log("Added " + results.Count + " waysTo.");
                        }
                        else
                        {
                            //Debug.LogError("No rules to\t"+aliasedGoal.ToString());
                        }

                        //if we're talking about ourselves, then one way to make this true is to just DO something. In which case we only need can___
                        if ((goal.np.noun == myName || subjectMoniker.noun == myName) && !aliasedGoal.vp.verb.ToString().StartsWith("can")) { //<--cheap!
                            SemSentence canAliasedGoal = new SemSentence(aliasedGoal);
                            canAliasedGoal.vp.verb = AIKit_Grammar.EntryFor("can" + aliasedGoal.vp.verb.ToString());
                            Debug.LogWarning("Since subject is self, Also checking for ways to "+canAliasedGoal.ToString());
                            if (this.waysTo.ContainsKey(canAliasedGoal)) {
                                List<SemSentence> result = AliasedWaysTo(canAliasedGoal, new List<SemNP> { object_i_Moniker, subjectMoniker }, new List<SemNP> { goal.vp.objects[obj_index], goal.np });
                                Debug.LogError("Found " + result.Count + " waysTo with can: \t"+canAliasedGoal.ToString());
                                allWaysTo.AddRange(result);
                            }
                        }

                        Debug.Log("Now have " + allWaysTo.Count + " waysTo.");
                    }
                }
            }

            allWaysTo = allWaysTo.Distinct().ToList();
            Debug.Log("Found "+allWaysTo.Count+" ways to "+goal.ToString());
            return allWaysTo;
        }

        public SemSentence AliasedSentence(SemSentence ogS, List<SemNP> monikers, List<SemNP> originals) {
            SemSentence originalSentence = new SemSentence(ogS);

            for (int i = 0; i < monikers.Count; i++) {
                SemNP moniker = monikers[i]; 
                SemNP original = originals[i];

                //replace any NP that match the moniker with the original
                //Debug.LogWarning("Replacing all occurrences of "+moniker.ToString()+" with "+original.ToString()+"in the sentence: "+aliasedGoal.ToString());
                if (originalSentence.np == moniker)
                    originalSentence.np = original;
                for (int j = 0; j < originalSentence.vp.objects.Count; j++) {
                    if (originalSentence.vp.objects[j] == moniker) {
                        originalSentence.vp.objects[j] = original;
                    }
                }
            }

            return originalSentence;
        }

        public List<SemSentence> AliasedWaysTo(SemSentence aliasedGoal, List<SemNP> monikers, List<SemNP> originals) {
            List<SemSentence> waysTo = new List<SemSentence>();
            foreach (SemSentence method in this.waysTo[aliasedGoal]) {
                waysTo.Add(AliasedSentence(method, monikers, originals));
            }
            return waysTo;
        }

        public List<SemNP> GetHypernymsOf(SemanticWebNode original) {
            //prior to looking at the oringal SemNP, just use the web edges
            List<SemNP> hypernyms = new List<SemNP>();
            Stack<SemanticWebNode> nodes = new Stack<SemanticWebNode>();
            //Debug.LogError("Finding hypernyms of " + lexicalMemory.NodeInfo(original, null));
            
            //dont process is edges on "some"
            if (!original.some) {
                nodes.Push(original);
            }

            //take hypernyms of "any" hyponyms -- kinda rough bc i only really want the primary name but meh
            SemNP thisNodesName = original.GetAliases()[0];
            if (!(thisNodesName.determiner is null) && !thisNodesName.determiner.WordEquals("any")) {
                thisNodesName.determiner = AIKit_Grammar.EntryFor("any");
                thisNodesName.noun.AffixReferent(null);
                nodes.Push( lexicalMemory.GetOrInsert(thisNodesName) );
            }

            //Add a "some" version of the original noun to the list (use an implied "IS" edge)
            if (!(thisNodesName.determiner is null) && !thisNodesName.determiner.WordEquals("some")) {
                thisNodesName.determiner = AIKit_Grammar.EntryFor("some");
                thisNodesName.noun.AffixReferent(null);
                hypernyms.Add(thisNodesName);
            }

            int limit = 5;

            while (nodes.Count > 0 && limit > 0) {
                limit--;
                //would like to make const but meh
                List<SemanticWebEdge> allIsEdges = nodes.Pop().GetEdges("is");
                allIsEdges.ForEach((edge) => { if (!edge.to.some) nodes.Push(edge.to);});
                
                //Debug.Log("Found IS edges going to: " + string.Join(" & ", allIsEdges.ConvertAll((edge) => edge.to.GetString())));
                hypernyms.AddRange(allIsEdges.ConvertAll((edge) => edge.to.GetAliases()).SelectMany(x => x).ToList());
            }

            

            hypernyms = hypernyms.Distinct().ToList();

            //Debug.LogError("Found " + string.Join("/",hypernyms));
            hypernyms.ForEach((np) => { np.qt = thisNodesName.qt; });
            return hypernyms;
        }


        public List<SemanticWebNode> GetHyponymNodesOf(SemanticWebNode original) {
            //prior to looking at the oringal SemNP, just use the web edges
            List<SemanticWebNode> hyponyms = new List<SemanticWebNode>();
            Stack<SemanticWebNode> nodes = new Stack<SemanticWebNode>();
            //Debug.LogError("Finding hyponym ndoes of " + lexicalMemory.NodeInfo(original, null));

            //dont process incoming is edges on "any" -- fine now because we removed edges from these nodes
            nodes.Push(original);

            int limit = 5;
            while (nodes.Count > 0 && limit > 0) {
                limit--;
                List<SemanticWebEdge> allIsEdges = nodes.Pop().GetEdgesRev("is");
                allIsEdges.ForEach((edge) => { if (!edge.from.some) nodes.Push(edge.from); }); //add some here bc trans shouldn't work on these kinda of words?
                hyponyms.AddRange(allIsEdges.ConvertAll((edge) => edge.from));
            }

            //Get any hypernym and change it to "any" -- that becomes a hypOnym. (...i think)
            foreach (SemNP hyper in GetHypernymsOf(original)) {
                if (!(hyper.determiner is null) && !hyper.determiner.WordEquals("any")) {
                    hyper.determiner = AIKit_Grammar.EntryFor("any");
                    hyponyms.Add(lexicalMemory.GetOrInsert(hyper));
                }
            }

            //add "anything" to the list
            SemNP anything = new SemNP();
            anything.noun = AIKit_Grammar.EntryFor("anything");
            anything.qt = original.GetAliases()[0].qt;
            hyponyms.Add(lexicalMemory.GetOrInsert(anything));

            //add "any" ___ to the list, if this is a noun (we do this instead of using an "is" edge.)
            SemNP thisNodesName = original.GetAliases()[0];
            if (!(thisNodesName.determiner is null) && !thisNodesName.determiner.WordEquals("any")) {
                thisNodesName.determiner = AIKit_Grammar.EntryFor("any");
                thisNodesName.noun.AffixReferent(null);
                hyponyms.Add(lexicalMemory.GetOrInsert(anything));
            }

            hyponyms = hyponyms.Distinct().ToList();

            //Debug.LogError("Found " + string.Join("/",hyponym nodes));
            return hyponyms;
        }

        public List<SemNP> GetHyponymsOf(SemanticWebNode original) {
            //prior to looking at the oringal SemNP, just use the web edges
            List<SemNP> hyponyms = new List<SemNP>();
            Stack<SemanticWebNode> nodes = new Stack<SemanticWebNode>();
            //Debug.LogError("Finding hyponyms of " + lexicalMemory.NodeInfo(original, null));

            //dont process incoming is edges on "any"
            if (!original.any) {
                nodes.Push(original);
            }

            int limit = 5;
            while (nodes.Count > 0 && limit > 0) {
                limit--;
                List<SemanticWebEdge> allIsEdges = nodes.Pop().GetEdgesRev("is");
                allIsEdges.ForEach((edge) => { if (!edge.from.some) nodes.Push(edge.from); });
                hyponyms.AddRange(allIsEdges.ConvertAll((edge) => edge.from.GetAliases()).SelectMany(x => x).ToList());
            }

            //Get any hypernym and change it to "any" -- that becomes a hypOnym. (...i think)
            foreach (SemNP hyper in GetHypernymsOf(original)) {
                if (!(hyper.determiner is null) && !hyper.determiner.WordEquals("any")) {
                    hyper.determiner = AIKit_Grammar.EntryFor("any");
                    hyponyms.Add(hyper);
                }
            }

            //add "anything" to the list
            SemNP anything = new SemNP();
            anything.noun = AIKit_Grammar.EntryFor("anything");
            anything.qt = original.GetAliases()[0].qt;
            hyponyms.Add(anything);

            //add "any" ___ to the list, if this is a noun (we do this instead of using an "is" edge.)
            SemNP thisNodesName = original.GetAliases()[0];
            if (!(thisNodesName.determiner is null) && !thisNodesName.determiner.WordEquals("any")) {
                thisNodesName.determiner = AIKit_Grammar.EntryFor("any");
                thisNodesName.noun.AffixReferent(null);
                hyponyms.Add(thisNodesName);
            }

            hyponyms = hyponyms.Distinct().ToList();

            //Debug.LogError("Found " + string.Join("/",hyponyms));
            hyponyms.ForEach((np) => { np.qt = thisNodesName.qt; });
            return hyponyms;
        }

        public List<SemSentence> GetSentencesThatEntail(SemSentence original) {
            if (original.IsImplication())
            {
                SemImplication imp = original as SemImplication;
                List<SemSentence> sentencesThatEntailAnt = GetSentencesThatEntail(imp.antecedent);
                List<SemSentence> sentencesThatEntailCon = GetSentencesThatEntail(imp.consequent);
                List<SemSentence> sentences = new List<SemSentence>();
                foreach (SemSentence eant in sentencesThatEntailAnt)
                {
                    foreach (SemSentence econ in sentencesThatEntailCon)
                    {
                        sentences.Add(new SemImplication { antecedent=eant, consequent=econ });
                    }
                }
                sentences.ForEach((s) => s.MakeQuote());
                return sentences;
            }
            else if (original.IsCompound())
            {
                SemCompound comp = original as SemCompound;
                List<SemSentence> sentencesThatEntailS1 = GetSentencesThatEntail(comp.s1);
                List<SemSentence> sentencesThatEntailS2 = GetSentencesThatEntail(comp.s2);
                List<SemSentence> sentences = new List<SemSentence>();
                foreach (SemSentence es1 in sentencesThatEntailS1)
                {
                    foreach (SemSentence es2 in sentencesThatEntailS2)
                    {
                        sentences.Add(new SemCompound { s1 = es1, s2 = es2, conj = comp.conj });
                    }
                }

                //TODO:use logic rules instead of this
                if (comp.conj.WordEquals("or"))
                {
                    sentences.AddRange(sentencesThatEntailS1);
                    sentences.AddRange(sentencesThatEntailS2);
                }

                sentences.ForEach((s) => s.MakeQuote());
                return sentences;
            }

            bool q = original.IsQuoted();
            if (q)
            {
                original.MakeLiteral();
            }

            List<SemSentence> entailingSentences = new List<SemSentence>();

            SemNP subject = original.np;
            SemanticWebNode originalNode = lexicalMemory.GetOrInsert(subject);
            List<SemNP> entailingSubjects = GetHyponymsOf(originalNode);
            entailingSubjects.Add(subject);

            List<SemNP> objects = original.vp.objects;
            List<SemanticWebNode> originalObjectNodes = objects.ConvertAll((obj) => lexicalMemory.GetOrInsert(obj));
            List<List<SemNP>> entailingObjects = originalObjectNodes.ConvertAll((objNode) => GetHyponymsOf(objNode));
            for (int i = 0; i < entailingObjects.Count; i++) {
                entailingObjects[i].Add(objects[i]);
            }
                    

            foreach (SemNP current_subject in entailingSubjects) {
                //no objects for intransitive verbs
                if (entailingObjects.Count == 0)
                {
                    if (current_subject == subject) continue; //no need to add og sentence?

                    SemSentence entailingSentence = new SemSentence();
                    entailingSentence.np = current_subject;
                    entailingSentence.vp = new SemVP();
                    entailingSentence.vp.verb = original.vp.verb;

                    entailingSentences.Add(entailingSentence);
                }
                else
                {
                    foreach (SemNP current_object in entailingObjects[0])
                    {
                        if (current_subject == subject && current_object == objects[0]) continue; //no need to add og sentence?

                        SemSentence entailingSentence = new SemSentence();
                        entailingSentence.np = current_subject;
                        entailingSentence.vp = new SemVP();
                        entailingSentence.vp.verb = original.vp.verb;
                        entailingSentence.vp.objects.Add(current_object);

                        entailingSentences.Add(entailingSentence);
                    }
                }
            }

            if (q)
            {
                entailingSentences.ForEach((s) => s.MakeQuote());
            }

            return entailingSentences;
        }

        public List<SemSentence> GetSentencesThatImply(SemSentence original) {
            return GetWaysTo(original);
        }

        public List<SemSentence> GetSentencesEntailedBy(SemSentence original) {
            if (original.IsImplication())
            {
                SemImplication imp = original as SemImplication;
                List<SemSentence> sentencesEntailedByAnt = GetSentencesEntailedBy(imp.antecedent);
                List<SemSentence> sentencesEntailedByCon = GetSentencesEntailedBy(imp.consequent);
                List<SemSentence> sentences = new List<SemSentence>();
                foreach (SemSentence eant in sentencesEntailedByAnt)
                {
                    foreach (SemSentence econ in sentencesEntailedByCon)
                    {
                        sentences.Add(new SemImplication { antecedent = eant, consequent = econ });
                    }
                }

                sentences.ForEach((s) => s.MakeQuote()); //todo: only top layer needs to do this
                return sentences;
            }
            else if (original.IsCompound())
            {
                SemCompound comp = original as SemCompound;
                List<SemSentence> sentencesEntailedByS1 = GetSentencesEntailedBy(comp.s1);
                List<SemSentence> sentencesEntailedByS2 = GetSentencesEntailedBy(comp.s2);
                List<SemSentence> sentences = new List<SemSentence>();
                foreach (SemSentence es1 in sentencesEntailedByS1)
                {
                    foreach (SemSentence es2 in sentencesEntailedByS2)
                    {
                        sentences.Add(new SemCompound { s1 = es1, s2 = es2, conj = comp.conj });
                    }
                }

                //TODO:use logic rules instead of this
                if (comp.conj.WordEquals("and")) {
                    sentences.AddRange(sentencesEntailedByS1);
                    sentences.AddRange(sentencesEntailedByS2);
                }

                sentences.ForEach((s) => s.MakeQuote());
                return sentences;
            }

            bool q = original.IsQuoted();
            if (q)
            {
                original.MakeLiteral();
            }

            List<SemSentence> entailedSentences = new List<SemSentence>();

            SemNP subject = original.np;
            SemanticWebNode originalNode = lexicalMemory.GetOrInsert(subject);
            List<SemNP> entailedSubjects = GetHypernymsOf(originalNode);
            entailedSubjects.Add(subject);

            List<SemNP> objects = original.vp.objects;
            List<SemanticWebNode> originalObjectNodes = objects.ConvertAll((obj) => lexicalMemory.GetOrInsert(obj));
            //negation via "no" flips entailment
            //List<List<SemNP>> entailedObjects = original.vp.verb.ToString().StartsWith("no") ?originalObjectNodes.ConvertAll((objNode) => GetHyponymsOf(objNode)):originalObjectNodes.ConvertAll((objNode) => GetHypernymsOf(objNode));
            List<List<SemNP>> entailedObjects = originalObjectNodes.ConvertAll((objNode) => GetHypernymsOf(objNode));
            for (int i = 0; i < entailedObjects.Count; i++) {
                entailedObjects[i].Add(objects[i]);
            }

            //try EVERY combination of entailing subjects and objects
            foreach (SemNP current_subject in entailedSubjects) {
                //no objects for intransitive verbs.
                if (entailedObjects.Count == 0)
                {
                    if (current_subject == subject) continue; //no need to add og sentence?

                    SemSentence entailedSentence = new SemSentence();
                    entailedSentence.np = current_subject;
                    entailedSentence.vp = new SemVP();
                    entailedSentence.vp.verb = original.vp.verb;

                    entailedSentences.Add(entailedSentence);
                }
                else
                {
                    foreach (SemNP current_object in entailedObjects[0])
                    {
                        if (current_subject == subject && current_object == objects[0]) continue; //no need to add og sentence?

                        SemSentence entailedSentence = new SemSentence();
                        entailedSentence.np = current_subject;
                        entailedSentence.vp = new SemVP();
                        entailedSentence.vp.verb = original.vp.verb;
                        entailedSentence.vp.objects.Add(current_object);

                        entailedSentences.Add(entailedSentence);
                    }
                }
            }

            if (q)
            {
                entailedSentences.ForEach((s) => s.MakeQuote());
            }

            return entailedSentences;
        }

        public bool TrueOrDoable(SemSentence actionOrStatement) {
            //TODO: not sure about doability for people who arent me...

            //doable
            if ( !actionOrStatement.vp.verb.ToString().StartsWith("can") && AIKit_Actions.AbilityCheck(actionOrStatement, this) ) return true;
            
            return isTrue(actionOrStatement, out _);
        }

        public Stack<SemSentence> PlanTo(SemSentence goal) {
            Stack<SemSentence> plan = new Stack<SemSentence>();
            Stack<int> alternatives = new Stack<int>();
            Stack<List<SemSentence>> methods = new Stack<List<SemSentence>>();

            plan.Push(goal);
            alternatives.Push(0);
            
            List<SemSentence> initialMethods = GetWaysTo(goal).ConvertAll((method) => AIKit_Grammar.FillPronouns(plan.Peek(), method));
            initialMethods.Sort( (a,b) => CostTo(a) - CostTo(b) );
            methods.Push(initialMethods);
            string lastFailedStepMessage = "";

            while (plan.Count>0 && !TrueOrDoable(plan.Peek())) 
            {
                Debug.Log(plan.Peek().ToString()+" is not True or Doable and plan's not empty, so still planning...");

                //if we've exhausted all possible ways to do this (negative alternatives)
                if (alternatives.Peek() < 0) 
                {
                    Debug.Log("Exhausted ways to " + plan.Peek().ToString());
                    
                    plan.Pop();
                    methods.Pop();

                    alternatives.Pop();
                    if (alternatives.Count>0) {
                        alternatives.Push(alternatives.Pop() - 1);
                    }
                    continue;
                }

                List<SemSentence> m = methods.Peek();

                Debug.Log("Found " + m.Count + " methods to " + plan.Peek().ToString());

                //if there were no methods found to do this
                if (m.Count < 1) 
                {
                    lastFailedStepMessage = "No methods to " + plan.Peek().ToString();
                    Debug.Log(lastFailedStepMessage);

                    plan.Pop();
                    methods.Pop();

                    alternatives.Push(alternatives.Pop() - 1);
                    continue;
                }

                //if this is our first attempt at this, add an alternatives #
                if (alternatives.Count == plan.Count) 
                {
                    Debug.Log("This is our first attempt. Leaving room for "+ (m.Count - 1) +" alternatives");

                    plan.Push(m[m.Count - 1]);

                    //also fill any pronouns in each method
                    List<SemSentence> foundMethods = GetWaysTo(m[m.Count - 1]).ConvertAll((method) => AIKit_Grammar.FillPronouns(plan.Peek(), method));
                    foundMethods.Sort( (a,b) => CostTo(a) - CostTo(b) );
                    methods.Push(foundMethods);

                    alternatives.Push(m.Count - 1);
                } 
                
                //if this is not our first attempt at this, simply add the methods given by alt #
                else 
                {
                    Debug.Log("This is not our first attempt. Trying next method.");

                    plan.Push(m[alternatives.Peek()]);

                    List<SemSentence> foundMethods = GetWaysTo(m[alternatives.Peek()]).ConvertAll((method) => AIKit_Grammar.FillPronouns(plan.Peek(), method));;
                    foundMethods.Sort( (a,b) => CostTo(a) - CostTo(b) );
                    methods.Push(foundMethods);
                }
            }

            if (plan.Count>0 && TrueOrDoable(plan.Peek())) {
                Debug.LogWarning("Successfully planned how to "+goal.ToString()+": "+string.Join(", then ", new List<SemSentence>(plan.ToArray())));
            }
            else {
                Debug.LogError("Failed to plan how to "+goal.ToString()+"... \tLast Fail:" + lastFailedStepMessage);
            }

            return plan;
        }


        int CostTo(SemSentence s) {
            return 1;
        }

        public List<SemSentence> GetResultsFrom(SemSentence fact) {
            List<SemSentence> allResultsFrom = new List<SemSentence>();

            Debug.Log("resultsFrom Dictionary content:" + string.Join(", ", this.resultsFrom));
            
            SemanticWebNode subjNode = this.lexicalMemory.GetOrInsert(fact.np);
            List<SemanticWebNode> objNodes = fact.vp.objects.ConvertAll((obj) => lexicalMemory.GetOrInsert(obj));
            
            List<SemNP> subjMonikers = GetHypernymsOf(subjNode);
            List<List<SemNP>> objMonikers = objNodes.ConvertAll((node) => GetHypernymsOf(node));//fact.vp.verb.ToString().StartsWith("no")?objNodes.ConvertAll((node) => GetHyponymsOf(node)):objNodes.ConvertAll((node) => GetHypernymsOf(node));

            //add ogs
            subjMonikers.Add(fact.np);
            for (int i = 0; i < objMonikers.Count; i++) {
                objMonikers[i].Add(fact.vp.objects[i]);
            }

            Debug.Log("subjects: " + string.Join("/", subjMonikers) + ", objects: " + string.Join(" - ", objMonikers.ConvertAll((list) => string.Join("/", list))));

            foreach (SemNP subjectMoniker in subjMonikers) {
                //if we only have sentencial objects in our goal just try one sentence with that
                //TODO: intransitive covered here too?
                if (objMonikers.Count == 0) {
                    if (this.resultsFrom.ContainsKey(fact))
                        allResultsFrom.AddRange(this.resultsFrom[fact]);
                }

                for (int obj_index = 0; obj_index < objMonikers.Count; obj_index++) {
                    foreach (SemNP object_i_Moniker in objMonikers[obj_index]) {
                        SemSentence aliasedFact = new SemSentence(fact);
                        aliasedFact.np = subjectMoniker;
                        aliasedFact.vp.objects[obj_index] = object_i_Moniker;

                        Debug.Log("Checking dict for \t" + aliasedFact.ToString());

                        if (this.resultsFrom.ContainsKey(aliasedFact)) {
                            List<SemSentence> results = AliasedResultsFrom(aliasedFact, new List<SemNP> { object_i_Moniker, subjectMoniker }, new List<SemNP> { fact.vp.objects[obj_index], fact.np });
                            allResultsFrom.AddRange(results);
                            Debug.Log("Added " + results.Count + " results.");
                        }
                        Debug.Log("Now have " + allResultsFrom.Count + " results.");
                    }
                }
            }

            //Debug.Log("Derived "+allResultsFrom.Count+" rule-based results from "+fact.ToString()+":\n\t"+string.Join(",\n\t", allResultsFrom));
            return allResultsFrom;
        }

        public List<SemSentence> AliasedResultsFrom(SemSentence aliasedFact, List<SemNP> monikers, List<SemNP> originals) {
            List<SemSentence> resultsFrom = this.resultsFrom[aliasedFact];
            foreach (SemSentence method in resultsFrom) {
                for (int i = 0; i < monikers.Count; i++) {
                    SemNP moniker = monikers[i]; 
                    SemNP original = originals[i];

                    //replace any NP that match the moniker with the original
                    //Debug.LogWarning("Replacing all occurrences of "+moniker.ToString()+" with "+original.ToString()+"in the sentence: "+aliasedGoal.ToString());
                    if (method.np == moniker)
                        method.np = original;
                    for (int j = 0; j < method.vp.objects.Count; j++) {
                        if (method.vp.objects[j] == moniker) {
                            method.vp.objects[j] = original;
                        }
                    }
                }
            }
            return resultsFrom;
        }

        public SemSentence TryImplicationElimination(SemSentence sentence, SemImplication implication) {
            if (implication.antecedent == sentence) {
                return implication.consequent;
            }
            else return null;
        }
    }

    public class SemanticWebEdge 
    {
        public Sentence why;
        public SemanticWebNode from, to;
        public LexicalEntry word;
        public float salience;
        public SemanticWebEdge(SemanticWebNode from, LexicalEntry word, SemanticWebNode to, Sentence why, float salience) {
            this.from = from;
            this.to = to;
            this.why = why;
            this.word = word;
            this.salience = salience;
        }
    }

    public class SemanticWebNode {
        GameObject referent;

        string nounOrSentence;
        List<LexicalEntry> adjectives;
        List<SemNP> aliases;
        Dictionary<LexicalEntry, List<SemanticWebEdge>> incomingEdges;
        Dictionary<LexicalEntry, List<SemanticWebEdge>> outgoingEdges;
        public bool some, any;
        public SemanticWebNode(SemNP np) {
            this.incomingEdges = new Dictionary<LexicalEntry, List<SemanticWebEdge>>();
            this.outgoingEdges = new Dictionary<LexicalEntry, List<SemanticWebEdge>>();
            this.aliases = new List<SemNP>();
            this.adjectives = new List<LexicalEntry>();
            this.some = false;
            this.any = false;
            this.referent = np?.noun.GetReferent();
            this.nounOrSentence = np.ToString();
            if (np.noun.WordEquals("something") || (!(np.determiner is null) && np.determiner.WordEquals("some"))) {
                this.some = true;
            }
            if (np.noun.WordEquals("anything") || (!(np.determiner is null) && np.determiner.WordEquals("any"))) {
                this.any = true;
            }
            this.AddAdjectives(np.adjectives);
            this.AddAlias(np);
        }

        public List<SemanticWebNode> TraverseEdge(LexicalEntry s) {
            List<SemanticWebNode> returnedNodes = new List<SemanticWebNode>();
            if (s.wordClass == WordClass.Vtr && this.outgoingEdges.ContainsKey(s) && !(this.outgoingEdges[s] is null))
            {
                returnedNodes = this.outgoingEdges[s].ConvertAll((e) => e.to).Distinct().ToList();
            }
            return returnedNodes;
        }
        public List<SemanticWebNode> TraverseEdgeRev(LexicalEntry s) {
            List<SemanticWebNode> returnedNodes = new List<SemanticWebNode>();
            if (s.wordClass == WordClass.Vtr && this.incomingEdges.ContainsKey(s) &&!(this.incomingEdges[s] is null))
            {
                returnedNodes = this.incomingEdges[s].ConvertAll((e) => e.from).Distinct().ToList();
            }
            return returnedNodes;
        }
        public void AddEdgeTo(LexicalEntry word, SemanticWebNode to, Sentence s, float salience) {
            SemanticWebEdge e = new SemanticWebEdge(this, word, to, s, salience);
            if (!outgoingEdges.ContainsKey(word)) {
                outgoingEdges.Add(word, new List<SemanticWebEdge> {e});
            } else {
                outgoingEdges[word].Add(e);
            }
            if (!(to is null)) to.AddEdgeFrom(e);
        }
        public void AddEdgeFrom(SemanticWebEdge edge) {
            if (!incomingEdges.ContainsKey(edge.word)) {
                incomingEdges.Add(edge.word, new List<SemanticWebEdge> {edge});
            } else {
                incomingEdges[edge.word].Add(edge);
            }
        }
        public void AddAdjectives(List<LexicalEntry> newAdjectives) {
            this.adjectives.AddRange(newAdjectives);
        }
        public void RemoveAdjectives(List<LexicalEntry> targetAdjectives) {
            foreach (LexicalEntry adj in targetAdjectives) this.adjectives.Remove(adj);
        }
        public bool MatchesAdjectives(List<LexicalEntry> givenAdjectives) {
            return Helper.ListFlexMatch(this.adjectives, givenAdjectives);
        }
        public List<SemanticWebEdge> GetEdges(string w) {
            LexicalEntry word = AIKit_Grammar.EntryFor(w);
            List<SemanticWebEdge> edges = new List<SemanticWebEdge> ();
            if (this.outgoingEdges.ContainsKey(word))
                edges.AddRange(this.outgoingEdges[word]);
            return edges;
        }

        public List<SemanticWebEdge> GetEdgesRev(string w) {
            LexicalEntry word = AIKit_Grammar.EntryFor(w);
            List<SemanticWebEdge> edges = new List<SemanticWebEdge> ();
            if (this.incomingEdges.ContainsKey(word))
                edges.AddRange(this.incomingEdges[word]);
            return edges;
        }

        public List<SemanticWebEdge> GetEdges() {
            return this.outgoingEdges.Values.SelectMany(x => x).ToList();
        }

        public List<SemanticWebEdge> GetEdgesRev() {
            return this.incomingEdges.Values.SelectMany(x => x).ToList();
        }

        public List<SemNP> GetAliases() {
            return aliases.ConvertAll((np) => new SemNP(np)); //a deeper copy so that these aliases aren't modified by caller
        }

        public void AddAlias(SemNP item) {
            this.aliases.Add(item);
        }

        public void RemoveAlias(SemNP oldName) {
            this.aliases.Remove(oldName);
        }

        public string GetString() {
            return this.nounOrSentence;
        }
    }

    public class EpisodicMemoryEntry : IComparable<Date> {
        public Sentence sentence;
        float salience;
        public EpisodicMemoryEntry(Sentence s, float salience) {
            this.sentence = s;
            this.salience = salience;
        }
        public int CompareTo(Date key) {
            return this.sentence.utterance.CompareTo(key);
        }
    }

    public class EpisodicMemory {
        SortedDictionary<Date, EpisodicMemoryEntry> entries;
        public EpisodicMemory() {
            entries = new SortedDictionary<Date, EpisodicMemoryEntry>();
        }
        EpisodicMemoryEntry GetEntry(Date key) {
            EpisodicMemoryEntry eme = null;
            entries.TryGetValue(key, out eme);
            return eme;
        }

        public void Insert(Sentence s, float salience) {
            EpisodicMemoryEntry eme = new EpisodicMemoryEntry(s, salience);
            if (entries.ContainsKey(eme.sentence.utterance)) {
                Debug.Log("Episodic Memory already contained key: " + eme.sentence.utterance.Description());
                eme.sentence.utterance.tick();
                Debug.Log("Ticked version: " + eme.sentence.utterance.Description());
            }
            entries.Add(eme.sentence.utterance, eme);
        }
    }

    public class LexicalMemoryEntry : IComparable<string> {
        public string np;
        public SemanticWebNode node;
        public LexicalMemoryEntry(SemanticWebNode n) {
            this.np = n.GetString();
            this.node = n;
        }
        public int CompareTo(string key) {
            return this.np.CompareTo(key);
        }
        public void Ingest(LexicalMemoryEntry lme) {
            //Nothing atm.
        }
    }

    public class LexicalMemory {
        SortedList<string,List<LexicalMemoryEntry>> entries;
        public LexicalMemory() {
            entries = new SortedList<string,List<LexicalMemoryEntry>>();

            //concept of "thing" always exists
            SetupNodes();

            //extra work so that anything is something?
        }

        SemanticWebNode anythingNode;

        void SetupNodes() {
            //concept of "some thing" always exists
            SemNP thing = new SemNP();
            thing.noun = AIKit_Grammar.EntryFor("thing");
            thing.determiner = AIKit_Grammar.EntryFor("a");
            SemanticWebNode thingNode = this.GetOrInsert(thing);
        }

        //given semNP, return matching node (TODO: handle multi matches better)
        public SemanticWebNode GetOrInsert(SemNP np) {

            List<LexicalMemoryEntry> matches = null;
            if (entries.TryGetValue(np.ToString(), out matches)) {
                //merge?

                //return the first match. FUTURE TODO: return the most salient? or the whole list?
                foreach(LexicalMemoryEntry m in matches) {
                    if (m.node.MatchesAdjectives(np.adjectives))
                    {
                        //Debug.LogWarning("Node found: "+np.ToString()+" matched " +m.node.GetString());
                        return m.node;
                    }
                }
                //lme.Ingest(le);
            }

            SemanticWebNode newNode = new SemanticWebNode(np); //new node created and it has this as an alias

            if (!(np.determiner is null) && !np.determiner.WordEquals("some") && !np.determiner.WordEquals("any")) {
                //concept of "some thing" should be created if it doesn't exist. Then add the edge
                if (!np.determiner.WordEquals("some")) {
                    SemNP something = new SemNP();
                    something.noun = new LexicalEntry(np.noun);
                    something.noun.AffixReferent(null); //remove referent as this is abstract
                    something.determiner = AIKit_Grammar.EntryFor("some");
                    something.qt = np.qt;
                    SemanticWebNode someNode = this.GetOrInsert(something);

                    //our explanation: "a ___ is some ____"
                    SemSentence why = new SemSentence();
                    why.np = np;
                    why.vp = new SemVP();
                    why.vp.verb = AIKit_Grammar.EntryFor("is");
                    why.vp.objects.Add(something);

                    //Debug.LogWarning("should add edge indicating that "+np.ToString()+" is "+something.ToString()+".");

                    newNode.AddEdgeTo(why.vp.verb, someNode, new Sentence(why), 0.5f);
                }

                //concept of "any thing" should be created if it doesn't exist. [[Then add the edge.]]] actuallyy...... this has the same porblem as the other anything edges... so... maybe not?
                if (!np.determiner.WordEquals("any")) {
                    SemNP anything = new SemNP();
                    anything.noun = new LexicalEntry(np.noun);
                    anything.noun.AffixReferent(null); //remove referent as this is abstract
                    anything.determiner = AIKit_Grammar.EntryFor("any");
                    SemanticWebNode anyNode = this.GetOrInsert(anything);

                    //our explanation: "any ___ is a ____/any ____ is some ____"
                    SemSentence why = new SemSentence();
                    why.np = anything;
                    why.vp = new SemVP();
                    why.vp.verb = AIKit_Grammar.EntryFor("is");
                    why.vp.objects.Add(np);

                    //Debug.LogWarning("should add edge indicating that "+anything.ToString()+" is "+np.ToString()+".");

                    //I'll have to infer this... I can't add it. Otherwise I'll see infinite loops
                    //anyNode.AddEdgeTo(why.vp.verb, newNode, new Sentence(why), 0.5f);
                }
            }

            //"anything" is EVERYTHING
            //this creates a bug where this is interpreted as actually "anything is everything" in which case all "is" statements i.e. "a monster is a person" were true!
            //if all I want is for "anything" to be a hyponym of everything then I will implement this in the GetHyponyms function instead of using edges!
            /*
            if (!np.noun.WordEquals("thing") && !np.noun.WordEquals("anything") && !np.noun.WordEquals("something")) {
                Debug.LogWarning("should add edge indicating that anything is "+np.ToString()+".");
                SemNP anything = new SemNP();
                anything.noun = AIKit_Grammar.EntryFor("anything");
                SemanticWebNode anythingNode = this.GetAnythingNode();

                //our explanation: "everything is some thing"
                SemSentence why = new SemSentence();
                why.np = anything;
                why.vp = new SemVP();
                why.vp.verb = AIKit_Grammar.EntryFor("is");
                why.vp.objects.Add(new SemNP());
                why.vp.objects[0].noun = AIKit_Grammar.EntryFor("everything");

                anythingNode.AddEdgeTo(why.vp.verb, newNode, new Sentence(why), 0.5f);
            }*/

            //EVERYTHING is "something"
            if (!np.noun.WordEquals("thing") && !np.noun.WordEquals("something") && !np.noun.WordEquals("anything")) {
                //Debug.LogWarning("should add edge indicating that "+np.ToString()+" is something.");
                SemNP something = new SemNP();
                something.noun = AIKit_Grammar.EntryFor("something");
                something.qt = np.qt;
                SemanticWebNode somethingNode = this.GetOrInsert(something);

                //our explanation: "everything is some thing"
                SemSentence why = new SemSentence();
                why.np = new SemNP();
                why.np.noun = AIKit_Grammar.EntryFor("everything");
                why.vp = new SemVP();
                why.vp.verb = AIKit_Grammar.EntryFor("is");
                why.vp.objects.Add(something); //TODO: should be literal.

                newNode.AddEdgeTo(why.vp.verb, somethingNode, new Sentence(why), 0.5f);
            }

            LexicalMemoryEntry lme = new LexicalMemoryEntry(newNode);
            if (entries.ContainsKey(np.ToString())) {
                entries[np.ToString()].Add(lme);
            }
            else {
                entries.Add(np.ToString(),new List<LexicalMemoryEntry>() {lme});
            }

            Debug.LogWarning("First occurrence of "+np.ToString()+", created node: "+NodeInfo(newNode, null));

            return lme.node;
        }

        public string AllEntriesInfo() {
            string output = "---- Lexical Memory Info ----";
            List<SemanticWebNode> AllNodes = new List<SemanticWebNode>();
            foreach (List<LexicalMemoryEntry> val in this.entries.Values.ToList()) {
                AllNodes.AddRange(val.ConvertAll((lme) => lme.node));
            }
            AllNodes = AllNodes.Distinct().ToList();

            foreach (String key in this.entries.Keys.ToList()) {
                output += "\n\t" + key + ":";
                foreach (LexicalMemoryEntry entry in this.entries[key]) {
                    output += " Node "+AllNodes.IndexOf(entry.node);
                }
                output += "\n";
            }
            return output+"-----------------------\n";;
        }

        public string AllNodesInfo() {
            string output = "---- NODES INFO ----\n";
            List<SemanticWebNode> AllNodes = new List<SemanticWebNode>();
            foreach (List<LexicalMemoryEntry> val in this.entries.Values.ToList()) {
                AllNodes.AddRange(val.ConvertAll((lme) => lme.node));
            }
            AllNodes = AllNodes.Distinct().ToList();
            foreach(SemanticWebNode N in AllNodes) {
                output += "\n\n"+NodeInfo(N, AllNodes);
            }

            return output+"-----------------------\n";
        }

        public List<SemanticWebNode> GetAllNodes()
        {
            List<SemanticWebNode> allNodes = new List<SemanticWebNode>();
            foreach (List<LexicalMemoryEntry> val in this.entries.Values.ToList())
            {
                allNodes.AddRange(val.ConvertAll((lme) => lme.node));
            }
            allNodes = allNodes.Distinct().ToList();
            return allNodes;
        }

        public string NodeInfo(SemanticWebNode N, List<SemanticWebNode> AllNodes) {
            if (AllNodes is null) {
                AllNodes = new List<SemanticWebNode>();
                foreach (List<LexicalMemoryEntry> val in this.entries.Values.ToList()) {
                    AllNodes.AddRange(val.ConvertAll((lme) => lme.node));
                }
                AllNodes = AllNodes.Distinct().ToList();
            }

            string output = "Node "+AllNodes.IndexOf(N)+": [ ";
            foreach (SemNP alias in N.GetAliases()) {
                output += alias.ToString()+" ";
            }
            output += "],\n O-edges:\n";
            foreach (SemanticWebEdge edge in N.GetEdges()) {
                output += "\t" + edge.word.ToString();
                if (!(edge.to is null)) output +=" -> \tNode "+AllNodes.IndexOf(edge.to)+":\t"+edge.to.GetString()+"\n";
            }
            output += " I-edges:\n";
            foreach (SemanticWebEdge edge in N.GetEdgesRev()) {
                output += "\t<- \tNode "+AllNodes.IndexOf(edge.from)+":\t"+edge.from.GetString()+" \t"+edge.word.ToString()+"\n";
            }
            output += " Also generalizable from ('any' nodes):\n";
            foreach (SemNP alias in N.GetAliases().FindAll((np) => (!(np.determiner is null) && !np.determiner.WordEquals("any")))) {
                alias.determiner = AIKit_Grammar.EntryFor("any");
                alias.noun.AffixReferent(null);
                SemanticWebNode anyNode = this.GetOrInsert(alias);
                output += "\t<- Node "+AllNodes.IndexOf(anyNode)+":\t"+anyNode.GetString()+"\t includes\n";
            }
            return output;
        }

        public List<SemanticWebNode> NodesWithAlias(SemNP alias) {
            if (!this.entries.ContainsKey(alias.ToString())) return new List<SemanticWebNode>() {this.GetOrInsert(alias)};
            return this.entries[alias.ToString()].ConvertAll((lme) => lme.node );
        }
    }
}
public class AIKit_Knowledge : MonoBehaviour
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
