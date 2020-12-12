using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit 
{
    
    public class AIKit_Actions
    {
        //Check if the given entity has the ability to do x (the entity's name should be the subject of the sentence in ALL of these, but i dont check for that rn)
        public static bool AbilityCheck(SemSentence action, KnowledgeModule module) {
            SemSentence abilityCheck = SemSentence.NewCopy(action);
            abilityCheck.vp.verb = AIKit_Grammar.dictionary["can"+action.vp.verb.ToString()];
            if (action.vp.verb.ToString().StartsWith("can") || !module.isTrue(abilityCheck, out _, false)) {
                Debug.LogError("Ability check is false:" + abilityCheck.ToString());
                return false;
            }
            //some actions require a definite target. like take...
            if (action.vp.verb.WordEquals("find") ||
                action.vp.verb.WordEquals("take") ||
                action.vp.verb.WordEquals("eat"))
            {
                if (action.vp.objects.Count < 1 || !action.vp.objects.TrueForAll((obj) => { return !(obj.noun.GetReferent() is null); }))
                {
                    Debug.LogError("Ability check is false, no referent:" + abilityCheck.ToString());
                    return false;
                }
            }

            return true;
        }

        public static bool StepOnGoal(SemSentence goal, ref Stack<SemSentence> plan, Entity entity, GameObject entityGameObject) {
            

            //check if this is already true (may be wonky that i need to do this)
            if (entity.knowledgeModule.isTrue(plan.Peek(), out _, false)) {
                plan.Pop();
                return true;
            }

            //check if I CAN do this
            if (!AbilityCheck(plan.Peek(), entity.knowledgeModule)) {
                //if we can't, plan is no longer valid. Re-plan later!
                return false;
            }

            //Each verb calls its own function for actual per-step behavior;
            LexicalEntry verb = plan.Peek().vp.verb;

            if (verb.WordEquals("find")) {
                StepOnFind(ref plan, entity, entityGameObject);
                return true;
            }
            if (verb.WordEquals("take")) {
                StepOnTake(ref plan, entity, entityGameObject);
                return true;
            }
            if (verb.WordEquals("eat")) {
                StepOnEat(ref plan, entity, entityGameObject);
                return true;
            }

            Debug.LogError("I don't know how to:"+plan.Peek().vp.verb+", although I have the ability.");
            return false;
        }

        public static void StepOnEat(ref Stack<SemSentence> plan, Entity entity, GameObject entityGameObject) {
            Debug.Log("Entity perorms EAT: "+plan.Peek());
            entity.addMemory(new Sentence(plan.Peek()));
            plan.Pop();
        }

        public static void StepOnFind(ref Stack<SemSentence> plan, Entity entity, GameObject entityGameObject) {
            Debug.Log("Entity perorms FIND: "+plan.Peek());
            entity.addMemory(new Sentence(plan.Peek()));
            plan.Pop();
        }

        public static void StepOnTake(ref Stack<SemSentence> plan, Entity entity, GameObject entityGameObject) {
            Debug.Log("Entity perorms TAKE: "+plan.Peek());
            SemSentence perform = SemSentence.NewCopy(plan.Peek());

            // Move our position a step closer to the target.
            Transform target = perform.vp.objects[0].noun.GetReferent().transform;

            float speed = 2.0f;
            float step =  speed * Time.deltaTime; // calculate distance to move
            entityGameObject.transform.position = Vector3.MoveTowards(entityGameObject.transform.position, target.position, step);

            //lookat target
            entityGameObject.transform.LookAt(target);

            //take once within x distance
            if (Vector3.Distance(entityGameObject.transform.position, target.position) < 2)
            {
                GameObject.Destroy(target);
                entity.addMemory(new Sentence(plan.Peek()));
                plan.Pop();
            }


        }
    }

}
