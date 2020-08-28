using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit {

    public class Date {
        public int year,cycle,season;
        public float ms;
        public Date(float ms, int year, int cycle, int season) {
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
            return Mathf.RoundToInt(this.year+this.cycle+this.season+this.ms);
        }
        
        public int CompareTo(Date other) {
            Debug.Log("Compare "+this.ToString()+", val: "+this.val()+"to"+other.ToString()+": val:"+other.val()+".");
            return Mathf.RoundToInt(this.val() - other.val());
        }

        public string Description() {
            return "Date: { Year:"+this.year+"Cycle:"+this.cycle+"Season:"+this.season+"Ms:"+this.ms+"}";
        }

        public override bool Equals(object o) {
            Date other = o as Date;
            if (other is null) return false;
            return ((this.year == other.year) && (this.cycle == other.cycle) && (this.season == other.season) && (this.ms == other.ms));

        }

        public override int GetHashCode() {
            return this.val().GetHashCode();
        }
    }

    [System.Serializable]
    public class AIKit_World : MonoBehaviour
    {
        //Day/Season Length in Minutes
        public int StartYear=1641,  StartCycle=2, StartSeason=0, SeasonLength=10*60;
        Date currentDate;
        bool gui = true;

        public Date Now () {
            return currentDate;
        }

        public AIKit_World() {
            currentDate = new Date(0, StartYear, StartCycle, StartSeason);
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
                Entity witness = c.GetComponentInParent<Entity>();
                if (witness!=null) {
                    witness.Witness(s);
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
            GUI.Label(new Rect(100,10,200,50), ", Year:"+currentDate.year+", Cycle:"+currentDate.year+", Season:"+currentDate.year+"Time:"+currentDate.ms);
        }
    }
}

