using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;

namespace Speech2Text
{
    public static class Program
    { 
        class Translation
        {
            public string? text;
            public string? to;
        }
        class Translations
        {
            public Translation[]? translations;
        }

     
        private static readonly string key = "7b7ee590b8944871a03aa37b8a27293c";
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";
        private static readonly string location = "westeurope";

        static async Task<string> Translate(string textToTranslate)
        {
            // Input and output languages are defined as parameters.
            string route = "/translate?api-version=3.0&from=ru&to=fr&to=de&to=en";
            
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                // location required if you're using a multi-service or regional (not global) resource.
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async static Task ShowAvailableVoices(this SpeechSynthesizer synthesizer,  string voiceLocale)
        {
            // Gets a list of voices.
            using var result = await synthesizer.GetVoicesAsync("");

            if (result.Reason == ResultReason.VoicesListRetrieved)
            {
                Console.WriteLine("Voices found:");
                /*foreach (var voice in result.Voices)
                {
                    Console.WriteLine(voice.Name);
                    Console.WriteLine($" Gender: {voice.Gender}");
                    Console.WriteLine($" Locale: {voice.Locale}");
                    Console.WriteLine($" Path:   {voice.VoicePath}");
                }*/

                // To find a voice that supports a specific locale, for example:

                string? voiceName = null;
                
                foreach (var voice in result.Voices)
                {
                    if (voice.Locale.Equals(voiceLocale))
                    {
                        voiceName = voice.Name;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(voiceName))
                {
                    Console.WriteLine($"Found {voiceLocale} voice: {voiceName}");
                }

            }
            else if (result.Reason == ResultReason.Canceled)
            {
                Console.Error.WriteLine($"CANCELED: ErrorDetails=\"{result.ErrorDetails}\"");
            }

        }

        async static Task Main(string[] args)
        {          



            var speechConfig = SpeechConfig.FromSubscription("d5b68beeaf7c418786f5ce154597f181", "westeurope");
            speechConfig.SpeechRecognitionLanguage = "ru-RU";
            speechConfig.SpeechSynthesisLanguage = "en-US";

            using var synthesizer = new SpeechSynthesizer(speechConfig, AudioConfig.FromDefaultSpeakerOutput());

            await synthesizer.ShowAvailableVoices("de-DE");
           

            //await synthesizer.StartSpeakingTextAsync("This is test message");


            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            speechRecognizer.Recognized += async (s, e) =>
            {
                Console.WriteLine(e.Result.Text);
                var answer = await Translate(e.Result.Text);
                var json = JsonConvert.DeserializeObject<Translations[]>(answer);
                string en = json[0].translations[2].text;
                string de = json[0].translations[1].text;
                string fr = json[0].translations[0].text;

                Console.WriteLine($"EN: {en}");
                Console.WriteLine($"DE: {de}");
                Console.WriteLine($"FR: {fr}");

                speechConfig.SpeechSynthesisLanguage = "en-US";
                await synthesizer.StartSpeakingTextAsync(en);

                speechConfig.SpeechSynthesisLanguage = "de-DE";
                await synthesizer.StartSpeakingTextAsync(de);

                speechConfig.SpeechSynthesisLanguage = "fr-FR";
                await synthesizer.StartSpeakingTextAsync(fr);
            };

            Console.WriteLine("Speak, please ...");
            await speechRecognizer.StartContinuousRecognitionAsync();
            
            Console.ReadKey();

            speechRecognizer?.StopContinuousRecognitionAsync();


        }

    }
}
