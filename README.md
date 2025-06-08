# EasyAIConnector - 可能是最简单的.NET AI连接器 Probably the easiest .NET AI Connector

## How to use 如何使用

```csharp
Console.OutputEncoding = System.Text.Encoding.UTF8;
var c = new EasyAIConnector.AIConnector(new Uri("https://api.deepseek.com"), "sk-1234567890");
var rsp = await c.AskAsync("你好", "deepseek-chat", Console.Write);
```
控制台输出：
```
你好！😊 很高兴见到你！有什么我可以帮你的吗？
```

对，没了

更多示例[这里](/Test/Program.cs)

## 许可证
基于MIT License
