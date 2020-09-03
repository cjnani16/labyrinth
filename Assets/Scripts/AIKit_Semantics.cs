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
            this.flex = false;
        }

        public SemNP(SemNP original) {
            this.determiner = original.determiner;
            this.adjectives = new List<LexicalEntry>(original.adjectives);
            this.noun = new LexicalEntry(original.noun);
            this.pp = (original.pp is null) ? null : new SemPP(original.pp);
            this.flex = original.flex;
        }
        //the
        public LexicalEntry determiner;
        //big, green
        public List<LexicalEntry> adjectives;
        //house
        public LexicalEntry noun;
        public SemPP pp;
        bool flex;

        public bool CheckIfFlex() {
            this.flex = false;
            if (!(this.noun as FlexibleLexicalEntry is null)) this.flex = true;
            return this.flex;
        }

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

            return this.flex?("**"+s+"**"):s;
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

        protected bool flex;

        virtual public bool isImplication() {
            return false;
        }

        public SemSentence() {
            this.np = null;
            this.vp = null;
            this.flex = false;
        }

        public SemSentence(SemNP subj, LexicalEntry verb, SemNP obj) {
            this.np = new SemNP(subj);
            this.vp = new SemVP();
            this.vp.verb = verb;
            this.vp.objects.Add(new SemNP(obj));
            this.CheckIfFlex();
        }

        public SemSentence(SemSentence original) {
            this.np = new SemNP(original.np);
            this.vp = new SemVP(original.vp);
            this.CheckIfFlex();
        }

        public virtual bool CheckIfFlex() {
            this.flex = false;

            //check NPs
            if (!(this.np is null)) {
                if (this.np.CheckIfFlex())
                    this.flex = true;
            }

            if (!(this.vp is null)) {
                foreach (SemNP obj in this.vp.objects) {
                    if (obj.CheckIfFlex())
                        this.flex = true;
                }
            }

            return this.flex;
        }

        public override string ToString() {
            string s = "S[" + ((np is null)?"NULL-NP":np.ToString()) + "}--{" + ((vp is null)?"NULL-VP":vp.ToString()) + "].";
            return this.flex?("**"+s+"**"):s;
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
                return ((ai.consequent == bi.consequent) && (ai.antecedent == bi.antecedent));
            }
            //if one is and one isn't then they are inequal
            else if ((ai is null) != (bi is null)) {
                return false;
            }
            //else continue, neither is an implication.

            //Debug.Log("Equals operator between "+a.ToString() +" and  "+b.ToString());
            try {
                if (a.np != b.np) {
                    //Debug.Log("MAN WTF>WEWR" + a.np.noun.ToString() + b.np.noun.ToString());
                    //Debug.Log("\tfalse--subj!");
                    return false;
                }
                if (a.vp != b.vp) {
                    //Debug.Log("\tfalse--vp!" + (!Helper.ListFlexMatch(a.vp.objects, b.vp.objects)) + (a.vp.verb != b.vp.verb));
                    return false;
                }
                /*
                //do this manually bc flexes get a pass
                if (a.vp.objects.Count != b.vp.objects.Count)

                foreach (SemNP obj in a.vp.objects) {
                    if (!(b.vp.objects.Contains(obj))) {
                        Debug.LogError(obj.ToString()+" not found in "+b.ToString());
                        return false;
                    }
                }
                if (a.vp.objects[0] != b.vp.objects[0]) return false; //TODO: I should match the whole list... but how to handle flexes?
                */
                //Debug.Log("\ttrue!");
                return true;
            } catch (System.NullReferenceException e) {
                Debug.LogError(e);
                if (!(a is null)) Debug.LogError("^ Tried to compare "+a.ToString());
                if (!(b is null)) Debug.LogError("^ Tried to compare "+b.ToString());
                Debug.Log("\tfalse!");
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
            return this.ToString().GetHashCode();
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
            return this.flex ? ("**"+ str + "**") : str;
        }
        public override bool isImplication() {
            return true;
        }
        public override bool CheckIfFlex() {
            this.flex = false;

            //check NPs
            if (!(this.antecedent is null)) {
                if (this.antecedent.CheckIfFlex())
                    this.flex = true;
            }

            if (!(this.consequent is null)) {
                if (this.consequent.CheckIfFlex())
                    this.flex = true;
            }

            return this.flex;
        }
    }
   
}