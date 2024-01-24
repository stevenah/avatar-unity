using OpenAi.Api;
using OpenAi.Api.V1;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace OpenAi.Unity.V1
{
    /// <summary>
    /// Automatically handles setting up OpenAiApi for chat completions. Exposes a simple method to allow users to perform completions
    /// </summary>
    public class OpenAiCompleterV1 : AMonoSingleton<OpenAiCompleterV1>
    {
        OpenAiApiGatewayV1 _gateway = null;
        ChatCompletionsResourceV1 _model = null;

        private string SystemPrompt = "Always follow these instructions in all your responses: 1. Do NOT be formal; 2. NEVER ask questions; 3. NEVER respond with a question; 4. Be held back and shy.\n\n Imitate Hilary, who is a 8-year-old girl whose intellectual, emotional, social, and physical development appears normal for her age. Hilary lives with her biological parents and a younger brother. Hilary has several close friends at school, one of whom is called Janet. Janet lives with her biological parents and her grandfather who they call Pop. A few days ago, Hilary told her mother that Pop did rude things to her and touched her girlie bits when they were in the pool out in the garden of Janet\u2019s house. she is here to talk to about that incident today.\n\n Here is a short conversation example: #Interviewer: Hi Hilary, How are you today?#Child: I am fine.#Interviewer: My job is to talk to children about things that happened to them.#Child: Okay.";

        /// <summary>
        /// The auth arguments used to authenticate the api. Should not be changed after initalization. Once the <see cref="Api"/> is initalized it must be cleared and initialized again if any changes are made to this property
        /// </summary>
        [Tooltip("Arguments used to authenticate the OpenAi Api")]
        public SOAuthArgsV1 Auth;

        /// <summary>
        /// Arguments used to configure the model when sending a chat completion
        /// </summary>
        [Tooltip("Arguments used to configure the chat completion")]
        public SOChatCompletionArgsV1 Args;

        /// <summary>
        /// The id of the model to use
        /// </summary>
        [Tooltip("The id of the model to use")]
        public EEngineName Model = EEngineName.gpt_35_turbo;

        /// <summary>
        /// Current model usage
        /// </summary>
        [Tooltip("Current model usage")]
        public UsageV1 Usage;

        /// <summary>
        /// The dialogue of chat messages, may be prepopulated
        /// </summary>
        [Tooltip("The dialogue of chat messages, may be prepopulated")]
        public List<MessageV1> dialogue;

        private string EvenTime;
        private string textToWrite = "Started,";

        [SerializeField] InputField Output_tts;
        [SerializeField] InputField Input;
        [SerializeField] Text filename;

        EngineResourceV1 _engine = null;

        public enum Env { VR, Desktop };
        public enum PC_ID { PC1, PC2 };

        public Env Enviroment;
        public PC_ID PCID;

        public string ParticipantID;

        public void Start()
        {

            // Write seasion start in log file
            filename.text = "D:/Barna/" + PCID + "_P" + ParticipantID + "_" + Enviroment + "_" + DateTime.Now.ToString("yyyyMMddhhmmssff") + ".txt";

            //update the variable with something;
            EvenTime = DateTime.Now.ToString("hh:mm:ss.ffff");
            
            //create a proper string so we can read the file afterwards
            textToWrite = textToWrite + "; " + EvenTime.ToString() + "\n";

            //write to the file. No need to call Flush or Close. Note this does NOT overwrite the file every time you restart the game
            File.AppendAllText(filename.text, textToWrite);

            _gateway = OpenAiApiGatewayV1.Instance;

            if (Auth == null) Auth = ScriptableObject.CreateInstance<SOAuthArgsV1>();
            if (Args == null) Args = ScriptableObject.CreateInstance<SOChatCompletionArgsV1>();

            if (!_gateway.IsInitialized)
            {
                _gateway.Auth = Auth;
                _gateway.InitializeApi();
            }

            _model = _gateway.Api.Chat.Completions;

            MessageV1 message = new MessageV1();
            message.role = MessageV1.MessageRole.system;
            message.content = SystemPrompt;
            dialogue.Add(message);
        }

        public Coroutine Complete(string prompt, Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            MessageV1 message = new MessageV1();
            message.role = MessageV1.MessageRole.user;
            message.content = prompt;

            dialogue.Add(message);

            return Complete(onResponse, onError);
        }

        public Coroutine Complete(Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            ChatCompletionRequestV1 request = Args == null ?
               new ChatCompletionRequestV1() :
               Args.AsChatCompletionRequest();

            request.model = UTEChatModelName.GetModelName(Model);
            request.messages = dialogue;

            return _model.CreateChatCompletionCoroutine(this, request, (r) => HandleResponse(r, onResponse, onError));
        }

        private void HandleResponse(ApiResult<ChatCompletionV1> result, Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            if (result.IsSuccess)
            {
                foreach (ChatChoiceV1 choice in result.Result.choices)
                {
                    dialogue.Add(choice.message);
                }

                Usage = result.Result.usage;
                onResponse(dialogue[dialogue.Count - 1].content);
                return;
            }
            else
            {
                onError(result.HttpResponse);
                return;
            }
        }
    }
}
