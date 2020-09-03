using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit 
{
    public class Helper {
        public static bool ListFlexMatch<T>(List<T> a, List<T> b) {
            return (a.Count == b.Count && a.TrueForAll((elem) => b.Contains(elem)));
        } 
    }

    public class SemNP {
        public SemNP() {
            this.determiner = null;
            this.adjectives = new List<LexicalEntry>();
            this.noun = null;
            this.pp = null;
        }

        public SemNP(SemNP original) {
            this.determiner = original.determiner;
            this.adjectives = new List<LexicalEntry>(original.adjectives);
            this.noun = new LexicalEntry(original.noun);
            this.pp = (original.pp is null) ? null : new SemPP(original.pp);
        }
        //the
        public LexicalEntry determiner;
        //big, green
        public List<LexicalEntry> adjectives;
        //house
        public LexicalEntry noun;
        public SemPP pp;

        public override string ToString() {
            string s = "( ";
            if (!(determiner is null)) s += determiner.ToString();
            s += " ( ";
            foreach (LexicalEntry le in adjectives) {
                s += le.ToString() + ", ";
            }
            s += ")- " + ((noun is null)?"NULL-N":noun.ToString());
            s += " )";
            if (!(pp is null)) s += "<-" + pp.ToString();

            return s;
        }
        
        public override bool Equals(object obj) {
            if (obj is null || obj as SemNP is null) return false;
            //Debug.LogWarning("1");
            SemNP other = obj as SemNP;
            //Debug.LogWarning("2");
            if (other.noun is null || this.noun is null) return false;
            //Debug.LogWarning("3");
            if (this.noun != other.noun) return false;
            //Debug.LogWarning("4");
            if ((other.adjectives is null) != (this.adjectives is null)) return false;
            //Debug.LogWarning("5");
            return Helper.ListFlexMatch(adjectives, other.adjectives);
        }

        public static bool operator ==(SemNP a, SemNP b) {
            return (a.Equals(b));
        }
        public static bool operator !=(SemNP a, SemNP b) {
            return (a.Equals(b));
        }
        //note: this doesn't work with flex like equals
        public override int GetHashCode() {
            return this.ToString().GetHashCode();
        }
    }
    public class SemVP {
        public SemVP() {
            this.verb = null;
            this.objects = new List<SemNP>();
            this.sentenceObjects = new List<SemSentence>();
        }

        public SemVP(SemVP original) {
            this.verb = original.verb;
            this.objects = original.objects.ConvertAll((obj) => new SemNP(obj));
            this.sentenceObjects = new List<SemSentence>(original.sentenceObjects);
        }
        //throw
        public LexicalEntry verb;
        //the ball, a fit
        public List<SemNP> objects;
        public List<SemSentence> sentenceObjects;

        public override string ToString() {
            string s = verb.ToString() + " => {";
            foreach (SemNP np in objects) {
                s += np.ToString() + "& ";
            }
            foreach (SemSentence sen in sentenceObjects) {
                s += sen.ToString() + "& ";
            }
            s += "}";
            return s;
        }

        public override bool Equals(object obj) {
            if (obj is null || obj as SemVP is null) return false;
            
            SemVP other = obj as SemVP;

            if (other.verb is null || this.verb is null) return false;

            if (this.verb != other.verb) return false;

            if ((other.objects is null) != (this.objects is null)) return false;
            return Helper.ListFlexMatch(objects, other.objects);
        }

        public static bool operator ==(SemVP a, SemVP b) {
            return (a.Equals(b));
        }
        public static bool operator !=(SemVP a, SemVP b) {
            return (a.Equals(b));
        }

        public override int GetHashCode() {
            return this.ToString().GetHashCode();
        }
    }
    public class SemPP {
        public SemPP(){
            this.preposition = null;
            this.np = null;
        }
        public SemPP(SemPP original) {
            this.preposition = original.preposition;
            this.np = new SemNP(original.np);
        }
        public LexicalEntry preposition;
        public SemNP np;

        public override string ToString() {
            return preposition.ToString() + " => {" + np.ToString() + "}";
        }
        
    }
    public class SemSentence {
        //{the little kid}
        public SemNP np;

        //{excitedly throw <-> a ball} }
        public SemVP vp;

        virtual public bool isImplication() {
            return false;
        }

        public SemSentence() {
            this.np = null;
            this.vp = null;
        }

        public SemSentence(SemNP subj, LexicalEntry verb, SemNP obj) {
            this.np = new SemNP(subj);
            this.vp = new SemVP();
            this.vp.verb = verb;
            this.vp.objects.Add(new SemNP(obj));
        }

        public SemSentence(SemSentence original) {
            this.np = new SemNP(original.np);
            this.vp = new SemVP(original.vp);
        }

        public override string ToString() {
            string s = "S[" + ((np is null)?"NULL-NP":np.ToString()) + "}--{" + ((vp is null)?"NULL-VP":vp.ToString()) + "].";
            return s;
        }

        public override bool Equals(object obj)
        {
            //Debug.Log("Equals within "+this.ToString());
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            SemSentence other = obj as SemSentence;

            return this == other;
        }

        public static bool operator ==(SemSentence a, SemSentence b) {
            //if they're implications, then do something different
            SemImplication ai = a as SemImplication;
            SemImplication bi = b as SemImplication;
            if (!(ai is null) && !(bi is null)) {
                Debug.Log("\tboth implications!");
                if (ai.consequent != bi.consequent) {
                    Debug.Log("false-- consequent mismatch");
                    return false;
                }
                if (ai.antecedent != bi.antecedent) {
                    Debug.Log("false-- antecedent mismatch");
                    return false;
                }
                return true;
            }
            //if one is and one isn't then they are inequal
            else if ((ai is null) != (bi is null)) {
                Debug.Log("\tfalse--implicaiton vs non-implication!");
                return false;
            }
            //else continue, neither is an implication.

            //Debug.Log("Equals operator between "+a.ToString() +" and  "+b.ToString());
            try {
                if (a.np != b.np) {
                    //Debug.Log("MAN WTF>WEWR" + a.np.noun.ToString() + b.np.noun.ToString());
                    Debug.Log("\tfalse--subj!");
                    return false;
                }
                if (a.vp != b.vp) {
                    Debug.Log("\tfalse--vp! objects match? " + (Helper.ListFlexMatch(a.vp.objects, b.vp.objects)) + " verbs match? " + (a.vp.verb == b.vp.verb));
                    return false;
                }
                return true;
            } catch (System.NullReferenceException e) {
                Debug.LogError(e);
                if (!(a is null)) Debug.LogError("^ Tried to compare "+a.ToString());
                if (!(b is null)) Debug.LogError("^ Tried to compare "+b.ToString());
                Debug.Log("\tfalse -- null error!");
                return false;
            }
        }

        public static bool operator !=(SemSentence a, SemSentence b) {
            return !(a==b);
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            //Debug.LogWarning("Hashed " +this.ToString()+" to "+this.ToString().GetHashCode());
            int hash = this.ToString().GetHashCode();
            Debug.Log("SemSentence " + this.ToString() + " hashed to " + hash);
            return hash;
        }
    }

    public class SemImplication : SemSentence {
        //If I am blue
        public SemSentence antecedent;
        //then I am hot
        public SemSentence consequent;
        public SemImplication() {
            this.antecedent = null;
            this.consequent = null;
        }
        public override string ToString() {
            string str = (this.antecedent is null ? "{NULL}" : antecedent.ToString()) + " implies " + (this.consequent is null ? "{NULL}" : consequent.ToString());
            return str;
        }
        public override bool isImplication() {
            return true;
        }
    }
   
}