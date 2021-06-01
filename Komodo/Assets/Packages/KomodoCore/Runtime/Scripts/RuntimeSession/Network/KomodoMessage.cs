using UnityEngine;
using System.Runtime.InteropServices;

namespace Komodo.Runtime
{
    // Message System: WIP
    // to send a message
    // 1. pack a struct with the data you need
    // 2. serialize that struct
    // 3. pass the message `type` and the serialized struct in the constructor
    // 4. call the .Send() method
    // 5. write a handler and register it in the ProcessMessage function below
    // 6. this is still a hacky way to do it, so feel free to change/improve as you see fit. 
    [System.Serializable]
    public struct KomodoMessage
    {
        public string type;

        public string data;

        public KomodoMessage(string type, string messageData)
        {
            this.type = type;
            this.data = messageData;
        }

        public void Send()
        {
            var message = JsonUtility.ToJson(this);

#if UNITY_WEBGL && !UNITY_EDITOR
            NetworkUpdateHandler.BrowserEmitMessage(message);
#else
            //TODO(Brandon): find a way to use SocketIOEditorSimulator from here
#endif
        }
    }
}