using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit
{
    [System.Serializable]
    public class IsA : MonoBehaviour
    {
        public string Name;
        public List<string> InitialIdentity;
        public bool IsPlayer;
        List<List<LexicalEntry>> Iam;

        public IsA() {
            Iam = new List<List<LexicalEntry>>();
        }

        public bool AreYouPlayer() {
            return IsPlayer;
        }

        //TODO: handle determiners here?
        public bool AreYouA(List<LexicalEntry> query) {
            foreach (List<LexicalEntry> thing in Iam) {
                if (thing == query) return true;
            }
            return false;
        }

        public void BecomeA(List<LexicalEntry> thing) {
            Iam.Add(thing);
        }

        public void BecomeA(SemNP thing) {
            Iam.Add(AIKit_Grammar.ExpandToList(thing.noun));
        }

        public void BecomeA(string thing) {
            LexicalEntry le = new LexicalEntry(AIKit_Grammar.EntryFor(thing));
            //if (le.wordClass != WordClass.N) le.AffixReferent(gameObject);
            Iam.Add(AIKit_Grammar.ExpandToList(le));
        }

        public void NoLongerA(List<LexicalEntry> thing) {
            if (!Iam.Contains(thing)) return;
            Iam.Remove(thing);
        }

        public List<SemNP> ApparentNPs() {
            List<SemNP> NPs = new List<SemNP>();
            List<LexicalEntry> nouns = this.Iam.ConvertAll((list) => list[0]);
            foreach(LexicalEntry noun in nouns) {
                SemNP np = new SemNP();
                //noun.AffixReferent(this.gameObject);
                np.noun = noun;
                if (noun.wordClass == WordClass.N)
                {
                    np.determiner = AIKit_Grammar.EntryFor("a");
                    SemNP np2 = new SemNP(np) { determiner = AIKit_Grammar.EntryFor("some") };
                    NPs.Add(np2);
                }
                NPs.Add(np);
            }
            return NPs;
        }

        public override string ToString() {
            string s = "";
            foreach (List<LexicalEntry> l in Iam) {
                foreach(LexicalEntry le in l) {
                    s+=le.ToString();
                    s+="/";
                }
            }
            return s;
        }
    }
}
