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
            SemNP other = obj as SemNP;
            if (this.flex || other.flex) return this.noun == other.noun;
            else {
                return this.ToString() == other.ToString();
            }
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

        bool flex;

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

        public bool CheckIfFlex() {
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
            //Debug.Log("Equals operator between "+a.ToString() +" and  "+b.ToString());
            try {
                if (!Helper.ListFlexMatch(b.np.adjectives, a.np.adjectives) || a.np.noun != b.np.noun) {
                    //Debug.Log("MAN WTF>WEWR" + a.np.noun.ToString() + b.np.noun.ToString());
                    //Debug.Log("\tfalse--subj!");
                    return false;
                }
                if (!Helper.ListFlexMatch(a.vp.objects, b.vp.objects) || a.vp.verb != b.vp.verb) {
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
            return (this.antecedent is null ? "{NULL}" : antecedent.ToString()) + " implies " + (this.consequent is null ? "{NULL}" : consequent.ToString());
        }
        public override bool isImplication() {
            return true;
        }
    }
   
}