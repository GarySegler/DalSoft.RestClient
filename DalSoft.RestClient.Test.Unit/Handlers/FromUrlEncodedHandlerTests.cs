using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DalSoft.RestClient.Handlers;
using NUnit.Framework;

namespace DalSoft.RestClient.Test.Unit.Handlers
{
    public class FromUrlEncodedHandlerTests
    {
        private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";

        [Test]
        public async Task Test()
        {
            HttpRequestMessage actualRequest = null;
            var httpClientWrapper = new HttpClientWrapper
            (
                new Config(new FormUrlEncodedHander(), new UnitTestHandler(request => actualRequest = request))
            );

            
            await httpClientWrapper.Send(HttpMethod.Post, new Uri("http://test.test"),
                new Dictionary<string, string> { { "Content-Type", FormUrlEncodedContentType } }, new
                {
                    hello = new
                    {
                        complex= new
                        {
                            @is="OK"
                        }
                    }
                });

            string formUrlEncoded = actualRequest.Content.ReadAsStringAsync().Result;
                
        }
    }
}

