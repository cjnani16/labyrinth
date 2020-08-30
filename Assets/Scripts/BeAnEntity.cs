using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeAnEntity : MonoBehaviour
{
    AIKit.Entity self;
    public string EntityName;
    public bool perceiving = false;

    List<AIKit.IsA> perceiveOnInit;

    //public HashSet<GameObject> PerceptualContext;
    float dt;

    void processGoalsStack() {
        AIKit.Goal currentGoal = self.CurrentGoal();
    }

    /*
    GameObject FindInContext(List<AIKit.LexicalEntry> thing) {
        foreach (GameObject g in this.PerceptualContext) {
            AIKit.IsA isa = g.GetComponent<AIKit.IsA>();
            if (isa!=null) {
                if (isa.AreYouA(thing)) {
                    return g;
                }
            }
        }
        return null;
    }*/

    public AIKit.Entity GetSelf() {
        if (self is null) this.self = new AIKit.Entity(EntityName.ToLower(), gameObject);
        return this.self;
    }

    // Start is called before the first frame update
    void Start()
    {
        self = GetSelf();
        dt=0;
        this.perceiveOnInit = new List<AIKit.IsA>();
        //this.PerceptualContext = new ObservableCollection<string>();
        //this.PerceptualContext.CollectionChanged += this.PerceptualContextChanged;
    }

    void OnTriggerEnter(Collider other) {
        if (!perceiving) return;

        AIKit.IsA obj = other.gameObject.GetComponent<AIKit.IsA>();

        if (obj!=null){
            //sometimes we see thigns before the dictionary is ready.
            if (!obj.initialized) {
                this.perceiveOnInit.Add(obj);
                return;
            }

            Debug.Log(EntityName+" notices "+obj.ToString());
            transform.LookAt(other.transform);
            this.GetSelf().GainPerceptOf(obj.ApparentNPs());
        }
    }

    void OnTriggerExit(Collider other) {
        AIKit.IsA obj = other.gameObject.GetComponent<AIKit.IsA>();

        if (obj!=null){
            Debug.Log(EntityName+" no longer sees"+obj.ToString());
            this.GetSelf().LosePerceptOf(obj.ApparentNPs());
        }
    }

    /*
    void PerceptualContextChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
            //list changed - an item was added.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                //Do what ever you want to do when an item is added here...
                //the new items are available in e.NewItems
                Debug.Log(EntityName+" notices "+obj.ToString());
                this.gameObject.transform.LookAt(c.gameObject.transform);
                this.self.GainPerceptOf(obj.ApparentNPs());
            }

            //list changed - an item was removed.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                //Do what ever you want to do when an item is added here...
                //the new items are available in e.NewItems
            }
    }*/

    // Update is called once per frame
    void Update()
    {
        /*
        HashSet<GameObject> newArrivals = new HashSet<GameObject>();
        Collider[] context = Physics.OverlapSphere(this.transform.TransformVector(Vector3.zero), PerceptualRadius);
        foreach (Collider c in context) {
            if (c.gameObject==gameObject) continue; //ignore self lol

            AIKit.IsA obj = c.gameObject.GetComponent<AIKit.IsA>();

            if (obj!=null){
                //if this wasn't *already* in our perceptual context, we announce its arrival
                if (!PerceptualContext.Contains(c.gameObject)) {
                    
                }

                newArrivals.Add(c.gameObject);
            }
        }
        foreach (GameObject gameObject in PerceptualContext) {
            AIKit.IsA obj = gameObject.GetComponent<AIKit.IsA>();
            if (!newArrivals.Contains(gameObject)) {
                Debug.Log(EntityName+" no longer sees"+obj.ToString());
                this.self.LosePerceptOf(obj.ApparentNPs());
            }
        }
        PerceptualContext = newArrivals;
        */

        
        for (int i = 0; i < perceiveOnInit.Count; i++) {
            if (perceiveOnInit[i].initialized) {
                Debug.Log(EntityName+" awakens and notices "+perceiveOnInit[i].ToString());
                this.GetSelf().GainPerceptOf(perceiveOnInit[i].ApparentNPs());
                perceiveOnInit.RemoveAt(i--);
            }
        }
        

        dt+=Time.deltaTime;
        if ((int)dt % 1 == 0 && this.perceiving)  //replan every 1 second
        {
            this.self.processWitnessQueue();
            if (self.myGoals.Count > 0) {
                if (this.self.knowledgeModule.isTrue(self.myGoals.Peek(), out _)) {
                    Debug.Log(EntityName + " completed goal of "+self.myGoals.Peek().ToString() +"!");
                    self.myGoals.Pop();
                    self.curentPlan = null;
                    return;
                }

                if (self.curentPlan is null || self.curentPlan.Count<1 || self.curentPlan.ToArray()[self.curentPlan.Count - 1] != self.myGoals.Peek()) {
                    Debug.Log("Planning how to "+self.myGoals.Peek().ToString());
                    self.curentPlan = self.knowledgeModule.PlanTo(self.myGoals.Peek());
                }

                if (self.curentPlan.Count < 1) 
                {
                    Debug.LogError("Plan to "+self.myGoals.Pop()+" was empty! Rip");
                }
                else 
                {
                    //if this is false, the plan was invalidated!
                    if (!AIKit.AIKit_Actions.StepOnGoal(self.myGoals.Peek(), ref self.curentPlan, this.self, this.gameObject)) {
                        Debug.LogError("Plan to "+self.myGoals.Pop()+" was invalidated! Rip");
                        //normally wouldn't do this, but lets prevent replanning we popped^
                        self.curentPlan = null;
                    }
                }
                    
            }
            
            /*this.decideNextGoal();
            this.ActOnGoals();*/
        }

        if ((int)dt % 10 == 0) 
        {
            this.self.chillOut();
        }

        if (dt > 100) dt=0;
    }
}
