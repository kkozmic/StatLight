using System.Collections.Generic;
using System.IO;
using System.Net;
using Moq;
using NUnit.Framework;
using StatLight.Core.Common;
using StatLight.Core.Common.Logging;
using StatLight.Core.Configuration;
using StatLight.Core.Serialization;
using StatLight.Core.WebBrowser;
using StatLight.Core.WebServer;
using StatLight.Core.WebServer.XapHost;

namespace StatLight.Core.Tests.WebServer.CustomHost
{
    [TestFixture]
    public class TestServiceEngineTests : FixtureBase
    {
        private InMemoryWebServer _inMemoryWebServer;

        private string _baseUrl;
        private WebClient _webClient;
        private byte[] _hostXap;
        private string _serializedConfiguration;
        private Mock<IPostHandler> _mockPostHandler;
        private ResponseFactory _responseFactory;

        protected override void Before_all_tests()
        {
            base.Before_all_tests();

            var webServerLocation = new WebServerLocation(TestLogger, 38881);
            var consoleLogger = new ConsoleLogger(LogChatterLevels.Full);
            _hostXap = new byte[] { 5, 4, 2, 1, 4 };
            var clientConfig = new ClientTestRunConfiguration(UnitTestProviderType.MSTest, new List<string>(), "", 1, WebBrowserType.SelfHosted, string.Empty, new WindowGeometry(), new List<string>());
            _serializedConfiguration = clientConfig.Serialize();

            var dummyServerTestRunConfiguration = GetDummyServerTestRunConfiguration();
            var statLightConfiguration = new StatLightConfiguration(clientConfig, dummyServerTestRunConfiguration);
            var currentStatLightConfiguration = new CurrentStatLightConfiguration(statLightConfiguration);

            _responseFactory = new ResponseFactory(currentStatLightConfiguration);
            _mockPostHandler = new Mock<IPostHandler>();
            _inMemoryWebServer = new InMemoryWebServer(consoleLogger, webServerLocation, _responseFactory, _mockPostHandler.Object, base.TestEventPublisher);
            _webClient = new WebClient();

            _baseUrl = webServerLocation.BaseUrl.ToString();

            _inMemoryWebServer.Start();
        }

        private ServerTestRunConfiguration GetDummyServerTestRunConfiguration()
        {
            var config = new ServerTestRunConfiguration(xapHost: () => _hostXap,
                                                                   xapToTest: "",
                                                                   xapHostType: XapHostType.MSTest2010April,
                                                                   queryString: "foo",
                                                                   forceBrowserStart: false,
                                                                   windowGeometry: new WindowGeometry(),
                                                                   isPhoneRun: false
                                                                   );
            return config;
        }

        [SetUp]
        public void Setup()
        {
            _responseFactory.Reset();
        }

        protected override void After_all_tests()
        {
            base.After_all_tests();

            _inMemoryWebServer.Stop();
        }

        [Test]
        public void Should_server_the_ClientAccessPolicy_file()
        {
            GetString(StatLightServiceRestApi.ClientAccessPolicy)
                .ShouldEqual(StatLight.Core.Properties.Resources.ClientAccessPolicy);
        }

        [Test]
        public void Should_server_the_CrossDomain_file()
        {
            GetString(StatLightServiceRestApi.CrossDomain)
                .ShouldEqual(StatLight.Core.Properties.Resources.CrossDomain);
        }

        [Test]
        public void Should_server_the_GetHtmlTestPage_file()
        {
            var expectedFile = new TestPage(instanceId: 0, windowless: "false").ToString();
            GetString(StatLightServiceRestApi.GetHtmlTestPage)
                .ShouldEqual(expectedFile);
        }

        [Test]
        public void Should_serve_the_GetTestPageHostXap_file()
        {
            _webClient.DownloadData(GetUrl(StatLightServiceRestApi.GetTestPageHostXap))
                .ShouldEqual(_hostXap);
        }

        [Test]
        public void Should_serve_the_GetTestRunConfiguration_file()
        {
            _webClient.DownloadString(GetUrl(StatLightServiceRestApi.GetTestRunConfiguration))
                .ShouldEqual(_serializedConfiguration);
        }

        //[Test]
        //public void Should_accept_postedMessages()
        //{
        //    const string messageWritten = "Hello World!";
        //    PostMessage(messageWritten);

        //    _mockPostHandler.Verify(v => v.Handle(It.Is<Stream>(x=> x.StreamToString() == messageWritten)));
        //}

        private void PostMessage(string messageWritten)
        {
            byte[] data = messageWritten.ToByteArray();
            Stream openWrite = _webClient.OpenWrite(GetUrl(StatLightServiceRestApi.PostMessage));
            openWrite.Write(data, 0, data.Length);
            openWrite.Close();
        }

        private string GetString(string path)
        {
            var url = GetUrl(path);
            return _webClient.DownloadString(url);
        }

        private string GetUrl(string path)
        {
            return _baseUrl + path;
        }
    }
}
