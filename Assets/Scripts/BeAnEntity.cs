using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    bool isPlanning = false;

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

    public AIKit.Entity GetSelf()
    {
        if (self is null) this.self = new AIKit.Entity(EntityName.ToLower(), gameObject);
        return this.self;
    }

    // Start is called before the first frame update
    void Start()
    {
        self = GetSelf();
        dt = 0;
        this.perceiveOnInit = new List<AIKit.IsA>();
        StartCoroutine(TryPlanning());
        StartCoroutine(TickState());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!perceiving) return;

        AIKit.IsA obj = other.gameObject.GetComponent<AIKit.IsA>();

        if (obj != null)
        {
            //sometimes we see thigns before the dictionary is ready.
            if (!obj.initialized)
            {
                this.perceiveOnInit.Add(obj);
                return;
            }

            if (Prefs.DEBUG) Debug.Log(EntityName + " notices " + obj.ToString());
            transform.LookAt(other.transform);
            this.GetSelf().GainPerceptOf(obj.ApparentNPs());
        }
    }

    void OnTriggerExit(Collider other)
    {
        AIKit.IsA obj = other.gameObject.GetComponent<AIKit.IsA>();

        if (obj != null)
        {
            if (Prefs.DEBUG) Debug.Log(EntityName + " no longer sees" + obj.ToString());
            this.GetSelf().LosePerceptOf(obj.ApparentNPs());
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < perceiveOnInit.Count; i++)
        {
            if (perceiveOnInit[i].initialized)
            {
                if (Prefs.DEBUG) Debug.Log(EntityName + " awakens and notices " + perceiveOnInit[i].ToString());
                this.GetSelf().GainPerceptOf(perceiveOnInit[i].ApparentNPs());
                perceiveOnInit.RemoveAt(i--);
            }
        }
        this.self.processWitnessQueue();

        if (!(self.currentPlan is null || self.currentPlan.Count < 1))
        {
            //if this is false, the plan was invalidated!
            if (!AIKit.AIKit_Actions.StepOnGoal(self.myGoals.Peek(), ref self.currentPlan, this.self, this.gameObject))
            {
                if (Prefs.DEBUG) Debug.LogError("Plan to " + self.myGoals.Pop() + " was invalidated! Rip");
                //normally wouldn't do this, but lets prevent replanning we popped^
                self.currentPlan = null;
                self.currentPlanTarget = null;
            }
        }
    }

    IEnumerator TryPlanning()
    {
        while (true)
        {
            if (self.myGoals.Count > 0)
            {
                //Debug.Log("Goals for " + this.name + ": " + string.Join(",", self.myGoals));

                if (this.self.knowledgeModule.isTrue(self.myGoals.Peek(), out _, false))
                {
                    if (Prefs.DEBUG) Debug.Log(EntityName + " completed goal of " + self.myGoals.Peek().ToString() + "!");
                    self.myGoals.Pop();
                    self.currentPlan = null;
                    self.currentPlanTarget = null;
                    yield return new WaitForSeconds(1);
                }
                if (isPlanning)
                {
                    //Debug.LogFormat("Still coming up with a plan for goal {0}", self.myGoals.Peek().ToString());
                }
                else
                {
                    if (self.currentPlan is null || self.currentPlan.Count < 1 || self.currentPlanTarget != self.myGoals.Peek())
                    {
                        if (Prefs.DEBUG) Debug.Log("Planning how to " + self.myGoals.Peek().ToString());
                        Debug.LogFormat("Kickoff planning for goal: {0}", self.myGoals.Peek().ToString());
                        isPlanning = true;
                        var planTask = Task<Stack<AIKit.SemSentence>>.Run(() => self.knowledgeModule.PlanTo(self.myGoals.Peek()));
                        yield return new WaitUntil(() => planTask.IsCompleted);
                        Debug.LogFormat("Finished planning for goal: {0}", self.myGoals.Peek().ToString());
                        self.currentPlan = planTask.Result;
                        isPlanning = false;
                        self.currentPlanTarget = self.myGoals.Peek(); //store what this plan was targeting so we don't plan again if necessary
                    }
                    if (self.currentPlan.Count < 1)
                    {
                        if (Prefs.DEBUG) Debug.LogError("Plan to " + self.myGoals.Pop() + " was empty! Rip");
                    }
                }
            }

            else
            {
                //give yourself a goal using motivations
                AIKit.SemSentence goal = self.GetGoalFromMotivation(self.CurrentMotivation());
                if (!(goal is null))
                {
                    self.myGoals.Push(goal);
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator TickState()
    {
        while (true)
        {
            this.self.chillOut();
            self.TickMotivations();
            yield return new WaitForSeconds(5);
        }
    }

    public async void SetPlanAsync()
    {
        Debug.LogFormat("Kickoff planning for goal: {0}", self.myGoals.Peek().ToString());
        isPlanning = true;
        self.currentPlan = await Task.Run(() => self.knowledgeModule.PlanTo(self.myGoals.Peek()));
        isPlanning = false;
        self.currentPlanTarget = self.myGoals.Peek(); //store what this plan was targeting so we don't plan again if necessary
    }
}
