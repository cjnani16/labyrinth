using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIKit {

    public class Dialogue 
    {
        //Called when I'm being told a sentence directly
        public void conversationalListen(Entity speaker, Entity listener, Sentence s) {
            
        }

        //Called when I'm trying to enter a conversation with a target -- "Hello"
        public void attemptConversationalEntry(Entity speaker, Entity target, Sentence s) {

        }

        //Called when I'm trying to exit a conversation with a target -- "Goodbye"
        public void conversationalExit(Entity speaker, Entity target, Sentence s) {

        }

        //Called when I'm receiving a request for conversation from a speaker
        public void conversationalInvite(Entity speaker, Entity target, Sentence s) {
            
        }

        //Called whenever I overhear someone say something
        public void Hear(Entity speaker, Entity listener, Sentence s) {

        }
    }

    public class AIKit_Dialogue : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}