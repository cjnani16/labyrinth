using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit 
{
    
    public class AIKit_Actions
    {
        //Check if the given entity has the ability to do x (the entity's name should be the subject of the sentence in ALL of these, but i dont check for that rn)
        public static bool AbilityCheck(SemSentence action, KnowledgeModule module) {
            SemSentence abilityCheck = new SemSentence(action);
            abilityCheck.vp.verb = AIKit_Grammar.dictionary["can"+action.vp.verb.ToString()];
            if (action.vp.verb.ToString().StartsWith("can") || !module.isTrue(abilityCheck)) {
                Debug.LogError("Ability check is false:" + abilityCheck.ToString());
                return false;
            }
            return true;
        }

        public static bool StepOnGoal(SemSentence goal, ref Stack<SemSentence> plan, Entity entity, GameObject entityGameObject) {
            

            //check if this is already true (may be wonky that i need to do this)
            if (entity.knowledgeModule.isTrue(plan.Peek())) {
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

            if (verb == AIKit_Grammar.dictionary["find"]) {
                StepOnFind(ref plan, entity, entityGameObject);
                return true;
            }
            if (verb == AIKit_Grammar.dictionary["take"]) {
                StepOnTake(ref plan, entity, entityGameObject);
                return true;
            }
            if (verb == AIKit_Grammar.dictionary["eat"]) {
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
            SemSentence perform = new SemSentence(plan.Peek());

            // Move our position a step closer to the target.
            Transform target = perform.vp.objects[0].noun.GetReferent().transform;

            float speed = 2.0f;
            float step =  speed * Time.deltaTime; // calculate distance to move
            entityGameObject.transform.position = Vector3.MoveTowards(entityGameObject.transform.position, target.position, step);

            //lookat target
            entityGameObject.transform.LookAt(target);

            //entity.addMemory(new Sentence(plan.Peek()));
            //plan.Pop();
        }
    }

}
