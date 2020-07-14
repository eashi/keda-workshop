using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Externalscaler;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace my_scaler
{
    public class ExternalScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _targetSizeKey = "targetSize";
        private static int _targetSize = 1; //Dangerous, not thread safe! We need to be static so it can be preserved between requests of New and GetMetrics
        private static string _urlOfService; //Dangerous, not thread safe!

        public ExternalScalerService(IHttpClientFactory httpClientFactory, ILogger<GreeterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GetMetricsSpec is for constructing the HPA object. If the ScaledObject changes KEDA needs to change the HPA too. 
        // This method is usually called after New, it also depends on the ScaledObject metadata sent to New before, e.g. Target.
        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef scaledObject, ServerCallContext context)
        {
            var metricSpecResponse = new GetMetricSpecResponse();
            _logger.LogInformation($"GetMetricSpec: _targetSize: {_targetSize}");
            metricSpecResponse.MetricSpecs.Add(new MetricSpec()
            {
                MetricName = "mymetric", //If this scaler handles more than one metric, then this changes. In our case it doesn't.
                TargetSize = _targetSize
            });

            return Task.FromResult(metricSpecResponse);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"GetMetrics: about to call service: {_urlOfService}");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, _urlOfService);
            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(httpRequest);
            int result = 30;
            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"GetMetrics: response was: {responseContent}");

            int.TryParse(responseContent, out result);

            var metricResponse = new GetMetricsResponse();
            metricResponse.MetricValues.Add(new MetricValue
            {
                MetricName = "mymetric",
                MetricValue_ = result
            });
            return metricResponse;
        }

        public override Task<IsActiveResponse> IsActive(ScaledObjectRef scaledObject, ServerCallContext context)
        {
            return Task.FromResult(new IsActiveResponse { Result = true });
        }

        public override Task<global::Google.Protobuf.WellKnownTypes.Empty> Close(ScaledObjectRef request, ServerCallContext context)
        {
            return Task.FromResult<Google.Protobuf.WellKnownTypes.Empty>(new Google.Protobuf.WellKnownTypes.Empty());
        }

        public override Task<global::Google.Protobuf.WellKnownTypes.Empty> New(NewRequest request, ServerCallContext context)
        {
            string strTargetSize;
            if (request.Metadata.TryGetValue(_targetSizeKey, out strTargetSize))
            {
                _logger.LogInformation($"New: targetValue string found, and it's :{strTargetSize}");
                int.TryParse(strTargetSize, out _targetSize);
                _logger.LogInformation($"New: after assignment _targetSize now is: {_targetSize}");
            }
            else
            {
                _logger.LogWarning($"New: targetSize is not found in the metadat");
            }
            var foundUrl = request.Metadata.TryGetValue("urlOfService", out _urlOfService);
            _logger.LogInformation($"New called with url: {_urlOfService}");
            if (!foundUrl)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "There is no urlOfService parameter"));

            return Task.FromResult<Google.Protobuf.WellKnownTypes.Empty>(new Google.Protobuf.WellKnownTypes.Empty());
        }


    }
}
