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
            this.qt = QuoteType.Literal;
            this.antecedent = null;
        }

        public SemNP(SemNP original) {
            this.determiner = original.determiner;
            this.adjectives = new List<LexicalEntry>(original.adjectives);
            this.noun = new LexicalEntry(original.noun);
            this.qt = original.qt;
            this.antecedent = original.antecedent;
        }
        //the
        public LexicalEntry determiner;
        //big, green
        public List<LexicalEntry> adjectives;
        //house
        public LexicalEntry noun;
        public QuoteType qt; //is this literal? Important for Semantic Web
        public SemNP antecedent;

        public override string ToString() {
            string s = "( ";
            if (!(determiner is null)) s += determiner.ToString();
            s += " ( ";
            foreach (LexicalEntry le in adjectives) {
                s += le.ToString() + ", ";
            }
            s += ")- " + ((noun is null)?"NULL-N":noun.ToString());
            s += (antecedent is null) ? "" : "~" + antecedent.ToString();
            s += " )";

            switch (qt)
            {
                case QuoteType.Start: s = "<Q_S>" + s + "</Q_S>"; break;
                case QuoteType.Mid: s = "<Q_M>" + s + "</Q_M>"; break;
                case QuoteType.End: s = "<Q_E>" + s + "</Q_E>"; break;
                case QuoteType.Invalid: s = "<Q_XXX!>" + s + "</Q_XXX!>"; break;
                case QuoteType.Literal: break;
                default: throw new System.Exception("invalid qt in np");
            }

            return s;
        }
        
        public override bool Equals(object obj) {
            if (obj is null || obj as SemNP is null) return false;
            //Debug.LogWarning("1");
            SemNP other = obj as SemNP;
            //Debug.LogWarning("2");
            if (this.qt != other.qt) return false;
            if (other.noun is null || this.noun is null) return false;
            //Debug.LogWarning("3");
            if (this.noun != other.noun) return false;
            //Debug.LogWarning("4");

            if (other.determiner is null != this.determiner is null) return false;
            if (!(this.determiner is null) && this.determiner != other.determiner) return false;

            if (other.antecedent is null != this.antecedent is null) return false;
            if (!(this.antecedent is null) && this.antecedent != other.antecedent) return false;

            if ((other.adjectives is null) != (this.adjectives is null)) return false;
            //Debug.LogWarning("5");
            return Helper.ListFlexMatch(adjectives, other.adjectives);
        }

        public static bool operator ==(SemNP a, SemNP b) {
            return (a.Equals(b));
        }
        public static bool operator !=(SemNP a, SemNP b) {
            return !(a.Equals(b));
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
            this.pps = new List<SemPP>();
        }

        public SemVP(SemVP original) {
            this.verb = original.verb;
            this.objects = original.objects.ConvertAll((obj) => new SemNP(obj));
            this.sentenceObjects = new List<SemSentence>(original.sentenceObjects);
            this.pps = new List<SemPP>(original.pps);
        }
        //throw
        public LexicalEntry verb;
        //the ball, a fit
        public List<SemNP> objects;
        public List<SemSentence> sentenceObjects;
        //to me
        public List<SemPP> pps;

        public override string ToString() {
            string s = verb.ToString() + " => {";
            foreach (SemNP np in objects) {
                s += ((np is null) ? "{NULL}" : np.ToString()) + "& ";
            }
            foreach (SemSentence sen in sentenceObjects) {
                s += sen.ToString() + "& ";
            }
            s += "}";
            if (pps.Count > 0)
            {
                s += "<- (";
                foreach (SemPP pp in pps)
                {
                    s += pp.ToString() + "& ";
                }
                s += ")";
            }
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
            return !(a.Equals(b));
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

        public override bool Equals(object obj)
        {
            if (obj is null || obj as SemPP is null) return false;

            SemPP other = obj as SemPP;

            if (other.preposition is null || this.preposition is null) return false;

            if (this.np != other.np) return false;

            return true;
        }
        public static bool operator ==(SemPP a, SemPP b)
        {
            return (a.Equals(b));
        }
        public static bool operator !=(SemPP a, SemPP b)
        {
            return !(a.Equals(b));
        }
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public bool ImpliedBy(SemPP other)
        {
            LexicalEntry otherPreposition = other.preposition;

            //Debug.LogFormat("Is {0} necesarily implied by {1}?", this, other);
            //does other imply this?
            //e.g. since tuesday 3/2 implies since monday 3/1

            if (Equals(other)) return true;

            Date thisDate = np.noun.ToDate();
            Date otherDate = other.np.noun.ToDate();

            if (thisDate is null || otherDate is null) return false;

            switch (preposition.GetWord())
            {
                case "since":
                case "after":
                    switch (other.preposition.GetWord())
                    {
                        case "since":
                        case "after":
                            //after monday < after tuesday
                            //Debug.Log("1  thisDate < otherDate" );
                            return thisDate < otherDate;
                        case "before":
                            //after monday < before tuesday? no
                            //Debug.Log("2  false");
                            return false;
                        case "on":
                            //after monday < on tuesday
                            //Debug.Log("3  thisDate < otherDate");
                            return thisDate < otherDate;
                        default:
                            Debug.LogError("unsupported prep");
                            return false;
                    }

                case "before":
                    switch (other.preposition.GetWord())
                    {
                        case "since":
                        case "after":
                            //before tuesday < after monday? no
                            //Debug.Log("4  false");
                            return false;
                        case "before":
                            //before tuesday < before monday? yes
                            //Debug.Log("5  thisDate > otherDate");
                            return thisDate > otherDate;
                        case "on":
                            //before tuesday < on monday? yes
                            //Debug.LogFormat("6  thisDate > otherDate{0}", thisDate > otherDate);
                            return thisDate > otherDate;
                        default:
                            Debug.LogError("unsupported prep");
                            return false;
                    }

                case "on":
                    switch (other.preposition.GetWord())
                    {
                        case "since":
                        case "after":
                            //on tuesday < after monday? no
                            //Debug.Log("7  false");
                            return false;
                        case "before":
                            //on tuesday < before wednesday? no
                            //Debug.Log("8  false");
                            return false;
                        case "on":
                            //on tuesday < on tuesday? yes
                            //Debug.Log("9  thisDate == otherDate");
                            return thisDate == otherDate;
                        default:
                            Debug.LogError("unsupported prep");
                            return false;
                    }

                default:
                    Debug.LogError("unsupported prep");
                    return false;
            }
        }

        public static bool operator <(SemPP a, SemPP b)
        {
            return a.ImpliedBy(b);
        }

        public static bool operator >(SemPP a, SemPP b)
        {
            return b.ImpliedBy(a);
        }
    }
    public class SemSentence {
        //{the little kid}
        public SemNP np;

        //{excitedly throw <-> a ball} }
        public SemVP vp;

        //interret literally? or is it meta
        public bool quoted;

        virtual public bool IsQuoted()
        {
            return quoted;
        }
        virtual public bool IsImplication() {
            return false;
        }
        virtual public bool IsCompound()
        {
            return false;
        }
        virtual public void MakeQuote()
        {
            MakeQuote(QuoteType.Start);
            MakeQuote(QuoteType.End);
        }
        virtual public void MakeQuote(QuoteType qt)
        {
            this.quoted = true;

            if (qt == QuoteType.Start || qt == QuoteType.Mid)
            {
                foreach (SemNP np in this.vp.objects)
                {
                    np.qt = QuoteType.Mid;
                }
                foreach (SemSentence s in this.vp.sentenceObjects)
                {
                    s.MakeQuote(QuoteType.Mid);
                }
            }

            this.np.qt = qt == QuoteType.Start ? QuoteType.Start : QuoteType.Mid;

            //add the end if thre are no sentence objects
            if (qt == QuoteType.End) {
                if (vp.sentenceObjects.Count > 0)
                {
                    vp.sentenceObjects[vp.sentenceObjects.Count - 1].MakeQuote(QuoteType.End);
                }
                else if (vp.objects.Count > 0)
                {
                    vp.objects[vp.objects.Count - 1].qt = QuoteType.End;
                }
                else
                {
                    this.np.qt = QuoteType.End;
                }
            }
        }

        virtual public void MakeLiteral()
        {
            this.quoted = false;
            this.np.qt = QuoteType.Literal;
            foreach (SemNP np in this.vp.objects)
            {
                np.qt = QuoteType.Literal;
            }
            foreach (SemSentence s in this.vp.sentenceObjects)
            {
                s.MakeLiteral();
            }
            foreach (SemPP pp in this.vp.pps)
            {
                pp.np.qt = QuoteType.Literal;
            }
        }

        //useful for the web
        virtual public SemNP GetLastNP()
        {
            if (this.vp.sentenceObjects.Count > 0)
            {
                return vp.sentenceObjects[vp.sentenceObjects.Count - 1].GetLastNP();
            }
            else if (this.vp.objects.Count > 0)
            {
                return this.vp.objects[this.vp.objects.Count - 1];
            }

            return new SemNP(this.np);
        }
        virtual public SemNP GetFirstNP()
        {
            return new SemNP(this.np);
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

        public static SemSentence NewCopy(SemSentence original) {
            if (original.IsImplication())
            {
                return new SemImplication(original as SemImplication);
            }
            if (original.IsCompound())
            {
                return new SemCompound(original as SemCompound);
            }

            SemSentence s = new SemSentence()
            {
                np = new SemNP(original.np),
                vp = new SemVP(original.vp),
                quoted = original.quoted
            };
            return s;
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
            //Debug.Log("Comparing" + a.ToString() + " and " + b.ToString());

            //if they're implications, then do something different
            if (a.IsImplication() && b.IsImplication()) {
                SemImplication ai = a as SemImplication;
                SemImplication bi = b as SemImplication;

                //Debug.Log("\tboth implications!");
                if (ai.consequent != bi.consequent) {
                    //Debug.Log("false-- consequent mismatch");
                    return false;
                }
                if (ai.antecedent != bi.antecedent) {
                    //Debug.Log("false-- antecedent mismatch");
                    return false;
                }
                return true;
            }
            //if one is and one isn't then they are inequal
            else if (a.IsImplication() != b.IsImplication()) {
                //Debug.Log("\tfalse--implicaiton vs non-implication!");
                return false;
            }
            //else continue, neither is an implication.

            //if theyr'e both compounds, do something different
            if (a.IsCompound() && b.IsCompound())
            {
                SemCompound ac = a as SemCompound;
                SemCompound bc = b as SemCompound;

                //Debug.Log("\tboth implications!");
                if (ac.s1 != bc.s1)
                {
                    //Debug.Log("false-- s1 mismatch");
                    return false;
                }
                if (ac.s2 != bc.s2)
                {
                    //Debug.Log("false-- s2 mismatch");
                    return false;
                }
                return true;
            }
            //if one is and one isn't then they are inequal
            else if (a.IsCompound() != b.IsCompound())
            {
                //Debug.Log("\tfalse--compoun vs non-compound!");
                return false;
            }
            //else continue, neither is a compound

            //Debug.Log("Equals operator between "+a.ToString() +" and  "+b.ToString());
            try {
                if (a.np != b.np) {
                    //Debug.Log("MAN WTF>WEWR" + a.np.noun.ToString() + b.np.noun.ToString());
                    //Debug.Log("\tfalse--subj!");
                    //Debug.Log("subj1 = " + a.np + ", subj2 = " + b.np);
                    return false;
                }
                if (a.vp != b.vp) {
                    //Debug.Log("\tfalse--vp! objects match? " + (Helper.ListFlexMatch(a.vp.objects, b.vp.objects)) + " verbs match? " + (a.vp.verb == b.vp.verb));
                    return false;
                }
                return true;
            } catch (System.NullReferenceException e) {
                Debug.LogError(e);
                //if (!(a is null)) Debug.LogError("^ Tried to compare "+a.ToString());
                //if (!(b is null)) Debug.LogError("^ Tried to compare "+b.ToString());
                //Debug.Log("\tfalse -- null error!");
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
            //Debug.Log("SemSentence " + this.ToString() + " hashed to " + hash);
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
            this.quoted = false;
        }
        public SemImplication(SemImplication other)
        {
            this.antecedent = SemSentence.NewCopy(other.antecedent);
            this.consequent = SemSentence.NewCopy(other.consequent);
            this.quoted = other.quoted;
        }
        public override string ToString() {
            string str = (this.antecedent is null ? "{NULL}" : antecedent.ToString()) + " implies " + (this.consequent is null ? "{NULL}" : consequent.ToString());
            return str;
        }
        public override bool IsQuoted()
        {
            return this.quoted;
        }
        public override bool IsImplication() {
            return true;
        }
        public override bool IsCompound()
        {
            return false;
        }
        public override void MakeQuote()
        {
            MakeQuote(QuoteType.Start);
            MakeQuote(QuoteType.End);
        }
        public override void MakeQuote(QuoteType qt)
        {
            this.quoted = true;
            if (qt != QuoteType.End)
            {
                antecedent.MakeQuote(QuoteType.Mid);
                if (qt == QuoteType.Start)
                {
                    antecedent.MakeQuote(QuoteType.Start);
                }
                consequent.MakeQuote(QuoteType.Mid);
            }

            if (qt == QuoteType.End)
            {
                consequent.MakeQuote(QuoteType.End);
            }
        }
        public override void MakeLiteral()
        {
            this.quoted = false;
            antecedent.MakeLiteral();
            consequent.MakeLiteral();
        }
        public override SemNP GetFirstNP()
        {
            return antecedent.GetFirstNP();
        }
        public override SemNP GetLastNP()
        {
            return consequent.GetLastNP();
        }
    }

    public class SemCompound : SemSentence
    {
        public SemSentence s1, s2;
        public LexicalEntry conj;
        public SemCompound()
        {
            this.s1 = null;
            this.s2 = null;
            this.conj = null;
        }
        public SemCompound(SemCompound other)
        {
            this.s1 = SemSentence.NewCopy(other.s1);
            this.s2 = SemSentence.NewCopy(other.s2);
            this.conj = other.conj;
            this.quoted = other.quoted;
        }
        public override string ToString()
        {
            string str = (this.s1 is null ? "{NULL}" : s1.ToString()) + " " + (this.conj is null ? "{NULL}" : conj.ToString()) + " " + (this.s2 is null ? "{NULL}" : s2.ToString());
            return str;
        }
        public override bool IsQuoted()
        {
            return this.quoted;
        }
        public override bool IsImplication()
        {
            return false;
        }
        public override bool IsCompound()
        {
            return true;
        }
        public override void MakeQuote()
        {
            MakeQuote(QuoteType.Start);
            MakeQuote(QuoteType.End);
        }
        public override void MakeQuote(QuoteType qt)
        {
            this.quoted = true;
            if (qt != QuoteType.End)
            {
                s1.MakeQuote(QuoteType.Mid);
                if (qt == QuoteType.Start)
                {
                    s1.MakeQuote(QuoteType.Start);
                }
                s2.MakeQuote(QuoteType.Mid);
            }

            if (qt == QuoteType.End)
            {
                s2.MakeQuote(QuoteType.End);
            }
        }
        public override void MakeLiteral()
        {
            this.quoted = false;
            s1.MakeLiteral();
            s2.MakeLiteral();
        }
        public override SemNP GetFirstNP()
        {
            return s1.GetFirstNP();
        }
        public override SemNP GetLastNP()
        {
            return s2.GetLastNP();
        }
    }

}