using Moq;
using Moq.Protected;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks;

public class MockHttpClient : HttpClient
{
    public MockHttpClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : base(CreateMockMessageHandler(handler).Object)
    {
        
    }

    private static Mock<HttpMessageHandler> CreateMockMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(handler);

        return mockHttpMessageHandler;
    }
}
