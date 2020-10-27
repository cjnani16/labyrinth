using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit
{
    [System.Serializable]
    public class IsA : MonoBehaviour
    {
        public List<string> InitialIdentity;
        public bool IsPlayer;
        public bool initialized = false;
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
            LexicalEntry le = AIKit_Grammar.EntryFor(thing);
            if (le.wordClass != WordClass.N) le.AffixReferent(gameObject);
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
                noun.AffixReferent(this.gameObject);
                np.noun = noun;
                if (noun.wordClass == WordClass.N)
                    np.determiner = AIKit_Grammar.EntryFor("a");
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

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            //initialize after dictionary becomes ready
            if (this.initialized == false && AIKit_Grammar.dictionary!=null) {
                //everything is something
                //BecomeA("something"); //this is in KMs so it shouldn't be necessary rlly

                foreach (string s in InitialIdentity) {
                    BecomeA(s);
                }

                //everyone is someone
                if (gameObject.GetComponent<BeAnEntity>() != null) {
                    Entity e = gameObject.GetComponent<BeAnEntity>().GetSelf();
                    BecomeA(e.GetName());
                    BecomeA("whomever");
                    BecomeA("someone");
                    //TODO: Entities should fully identify themselves with IsA edges??

                }
                this.initialized = true;
            }
        }
    }
}
