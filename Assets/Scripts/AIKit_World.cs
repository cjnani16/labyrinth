using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit {

    public class Date {
        public int year,cycle,season;
        public float ms;
        public Date(int year, int cycle, int season, float ms) {
            if (cycle >= 12 || cycle <0) throw new System.ArgumentException("Cycle out of range");
            if (season >= 4 || season <0) throw new System.ArgumentException("Cycle out of range");
            this.year = year;
            this.cycle = cycle;
            this.season = season;
            this.ms = ms;
        }

        public void tick() {
            this.ms++;
        }

        public static int[] operator -(Date a, Date b) {
            int[] arr = {a.year - b.year, a.cycle - b.cycle, a.season - b.season, Mathf.CeilToInt(a.ms - b.ms)};
            return arr;
        }

        public float val() {
            //Debug.Log("compute val for"+this.ToString()+":"+Mathf.RoundToInt(this.year+this.cycle+this.season+this.ms));
            return Mathf.RoundToInt(this.year*4*12+this.cycle*4+this.season); //don't include ms as this will cause overlaps?
        }
        
        public int CompareTo(Date other) {
            //Debug.Log("Compare "+this.ToString()+", val: "+this.val()+"to"+other.ToString()+": val:"+other.val()+".");
            return Mathf.RoundToInt(this.val() - other.val());
        }

        public string Description() {
            return "Date: { Year:"+this.year+"Cycle:"+this.cycle+"Season:"+this.season+"Ms:"+this.ms+"}";
        }

        public override bool Equals(object o) {
            Date other = o as Date;
            if (other is null) return false;
            Debug.LogFormat("{4} vs {5}... year? {0} cycle? {1} season? {2} ms? {3}", this.year == other.year, this.cycle == other.cycle, this.season == other.season, this.ms == other.ms, this.Description(), other.Description());
            return ((this.year == other.year) && (this.cycle == other.cycle) && (this.season == other.season) && (this.ms == other.ms));

        }

        public bool IsAfter(Date other)
        {
            if (other.year == this.year)
            {
                if (other.cycle == this.cycle)
                {
                    if (other.season == this.season)
                    {
                        return this.ms > other.ms;
                    }
                    else
                    {
                        return this.season > other.season;
                    }
                }
                else
                {
                    return this.cycle > other.cycle;
                }
            }
            else
            {
                return this.year > other.year;
            }
        }

        public static bool operator ==(Date a, Date b)
        {
            //Debug.LogFormat("{0} == {1} ? {2}", a, b, b.Equals(a));
            return b.Equals(a);
        }

        public static bool operator !=(Date a, Date b)
        {
            //Debug.LogFormat("{0} == {1} ? {2}", a, b, b.Equals(a));
            return !b.Equals(a);
        }

        public static bool operator <(Date a, Date b)
        {
            //Debug.LogFormat("{0} after {1} ? {2}", a, b, a.IsAfter(b));
            return b.IsAfter(a);
        }

        public static bool operator >(Date a, Date b)
        {
            return a.IsAfter(b);
        }

        public static bool operator <=(Date a, Date b)
        {
            return b.IsAfter(a) || a.Equals(b);
        }

        public static bool operator >=(Date a, Date b)
        {
            return a.IsAfter(b) || a.Equals(b);
        }

        public override int GetHashCode() {
            Debug.LogError("bad hash");
            return this.val().GetHashCode();
        }

        public LexicalEntry ToLexicalEntry()
        {
            LexicalEntry le = AIKit_Grammar.EntryFor(string.Format("date:{0},{1},{2},{3}", year, cycle, season, Mathf.RoundToInt(ms)));
            if (le is null) throw new System.Exception("Failed to convert " + Description() + " from Date to LexicalEntry!");
            return le;
        }
    }

    [System.Serializable]
    public class AIKit_World : MonoBehaviour
    {
        //Day/Season Length in Minutes
        public int StartYear=1641,  StartCycle=2, StartSeason=0, SeasonLength=10*60;
        public static Date currentDate;
        bool gui = true;

        public static Date Now () {
            return currentDate;
        }

        public AIKit_World() {
            currentDate = new Date(StartYear, StartCycle, StartSeason, 0);
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        //publish events to be perceived
        public static int publishEvent(Vector3 position, float radius, Sentence s) {
            int n=0;
            Collider[] context = Physics.OverlapSphere(position, radius);
            foreach (Collider c in context) {
                BeAnEntity witness = c.GetComponentInParent<BeAnEntity>();
                if (witness!=null) {
                    witness.GetSelf().Witness(s);
                    n++;
                }
            }
            return n;
        }

        //trigger Entities to end their day
        public static int publishEndOfDay() {
            int n=0;
            GameObject[] objs = GameObject.FindGameObjectsWithTag("AIKE");
            foreach(GameObject o in objs) {
                o.GetComponent<BeAnEntity>().GetSelf().degradeMemories();
            }
            return n;
        }

        public void updateTime(float deltaTime) 
        {
            //update current time
            currentDate.ms+=deltaTime;
            if (currentDate.ms>SeasonLength) {
                currentDate.ms -= SeasonLength;
                currentDate.season++;
                if (++currentDate.season>=4) {
                    currentDate.season-=4;
                    currentDate.cycle++;
                    if (++currentDate.cycle>=12) {
                        currentDate.cycle-=12;
                        currentDate.year++;
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            updateTime(Time.deltaTime);
        }

        void OnGUI() {
            if (!this.gui) return;
            GUI.Label(new Rect(900,630,600,50), "Year "+currentDate.year+", Cycle "+currentDate.cycle+", Season "+currentDate.season+", Time:"+currentDate.ms);
        }
    }
}

