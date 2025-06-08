using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EasyAIConnector
{


    public class AIConnector
    {
        string mainPath = "";
        List<string> context = new();
        string ApiURI = "";
        string Token = "";
        public AIConnector(Uri apiURI, string token, string role = "你是一个乐于助人的AI助手")
        {
            context.Add(JsonSerializer.Serialize(new Chat { content = role, role = "system" }));
            ApiURI = apiURI.ToString();
            Token = token;
        }

        public async Task<string> AskAsync(string question, string model, StreamWriter Writer)
        {
            return await AskAsync(question, model, (s) => Writer.Write(s));
        }

        public async Task<string> AskAsync(string question, string model, Action<string> WriteMethod)
        {
            context.Add(JsonSerializer.Serialize(new Chat { content = question, role = "user" }));
            var contextstr = string.Concat<string>(context.Select(s => s += ",\r\n"));
            var body =
$$$"""""
{          
  "messages": [
    {{{contextstr.Substring(0, contextstr.Length - 3)}}}
  ],
  "model": "{{{model}}}",
  "max_tokens": 4096,
  "stop": null,
  "stream": true

}
""""";
            HttpClient client = new();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApiURI + "/v1/chat/completions")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            StringBuilder contentBuilder = new(), thinkBuilder = new();
            int thinked = 0;
            using (var reader = new StreamReader(stream ?? throw new ArgumentNullException("stream")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
                    Debug.WriteLine($"Received:{line}");
                    var json = line["data: ".Length..];
                    if (json == "[DONE]") break;

                    var chunk = JsonSerializer.Deserialize<ResponseChunk>(json);
                    var delta = chunk?.choices.FirstOrDefault()?.delta;

                    if (delta == null) continue;

                    if (!string.IsNullOrEmpty(delta.reasoning_content))
                    {
                        if (thinked == 0)
                        {
                            thinkBuilder.Append("<think>");
                            WriteMethod("<think>");
                            thinked++;
                        }
                        thinkBuilder.Append(delta.reasoning_content);
                        WriteMethod(delta.reasoning_content);
                    }

                    if (!string.IsNullOrEmpty(delta.content))
                    {
                        if (thinked == 1)
                        {
                            thinkBuilder.Append("</think>");
                            WriteMethod("</think>");

                            thinked++;
                        }
                        contentBuilder.Append(delta.content);

                        WriteMethod(delta.content);
                    }
                }
            }
            context.Add(JsonSerializer.Serialize(new Chat { content = contentBuilder.ToString(), role = "assistant" }));

            return thinkBuilder.ToString() + contentBuilder.ToString();
        }


        public void Reset(string role = "你是一个乐于助人的AI助手")
        {
            context.Clear();
            context.Add(
$$"""
{
    "content": "{{role}}",
    "role": "system"
}
"""
    );
        }





        class ResponseChunk
        {
            public List<Choice> choices { get; set; }
        }

        class Choice
        {
            public Delta delta { get; set; }
        }

        class Delta
        {
            public string? reasoning_content { get; set; }
            public string content { get; set; }
        }

        class Chat
        {
            public string content { get; set; }
            public string role { get; set; }
        }
    }
}
