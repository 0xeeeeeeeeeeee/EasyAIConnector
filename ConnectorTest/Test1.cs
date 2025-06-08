using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasyAIConnector;

namespace ConnectorTest
{
    [TestClass]
    public sealed class Test1
    {
        private const string DummyApiUri = "https://dummyapi.com/";
        private const string DummyToken = "dummy_token";

        [TestMethod]
        public void Reset_ShouldInitializeContextWithSystemRole()
        {
            // Arrange
            var connector = new AIConnector(DummyApiUri, DummyToken, "测试角色");

            // Act
            connector.Reset("新角色");

            // Assert
            // 通过反射访问私有 context 字段
            var contextField = typeof(AIConnector).GetField("context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var context = contextField?.GetValue(connector) as System.Collections.Generic.List<string>;
            Assert.IsNotNull(context);
            Assert.AreEqual(1, context.Count);
            StringAssert.Contains(context[0], "新角色");
            StringAssert.Contains(context[0], "\"role\": \"system\"");
        }

        [TestMethod]
        public async Task AskAsync_ShouldAppendUserMessageAndReturnResult()
        {
            // Arrange
            var connector = new AIConnector(DummyApiUri, DummyToken);
            var output = new StringBuilder();

            // 使用委托捕获输出
            string question = "你好，AI！";
            string model = "gpt-test";
            bool writeCalled = false;

            // 使用 Moq 或自定义 HttpClient 可进一步完善此测试
            // 这里只测试 context 追加和 WriteMethod 被调用
            Task<string> task = connector.AskAsync(question, model, s => { output.Append(s); writeCalled = true; });

            // 因为没有真实 API，期望抛出异常
            await Assert.ThrowsExceptionAsync<System.Net.Http.HttpRequestException>(async () => await task);

            // Assert
            var contextField = typeof(AIConnector).GetField("context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var context = contextField?.GetValue(connector) as System.Collections.Generic.List<string>;
            Assert.IsNotNull(context);
            // 应该有 system + user 两条
            Assert.AreEqual(2, context.Count);
            StringAssert.Contains(context[1], question);
            var chat = JsonSerializer.Deserialize<Chat>(context[1]);
            Assert.AreEqual(question, chat.content);
            Assert.IsTrue(writeCalled);
        }
    }
}
