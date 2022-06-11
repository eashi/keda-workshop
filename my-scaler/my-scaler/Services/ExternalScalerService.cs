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
        private const string _metricNameString = "mymetric";
        private readonly ILogger<ExternalScalerService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _targetSizeKey = "targetSize";
        private static int _targetSize = 1; //Dangerous, not thread safe! We need to be static so it can be preserved between requests of New and GetMetrics
        private static string _urlOfService; //Dangerous, not thread safe!

        public ExternalScalerService(IHttpClientFactory httpClientFactory, ILogger<ExternalScalerService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GetMetricsSpec is for constructing the HPA object. If the ScaledObject changes KEDA needs to change the HPA too. 
        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef scaledObject, ServerCallContext context)
        {
            var metricSpecResponse = new GetMetricSpecResponse();
            _logger.LogInformation($"GetMetricSpec: _targetSize: {_targetSize}");
            metricSpecResponse.MetricSpecs.Add(new MetricSpec()
            {
                MetricName = _metricNameString, //If this scaler handles more than one metric, then this changes. In our case it doesn't.
                TargetSize = _targetSize
            });

            return Task.FromResult(metricSpecResponse);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            string strTargetSize;
            if (request.ScaledObjectRef.ScalerMetadata.TryGetValue(_targetSizeKey, out strTargetSize))
            {
                _logger.LogInformation($"GetMetrics: targetValue string found, and it's :{strTargetSize}");
                int.TryParse(strTargetSize, out _targetSize);
                _logger.LogInformation($"GetMetrics: after assignment _targetSize now is: {_targetSize}");
            }
            else
            {
                _logger.LogWarning($"GetMetrics: targetSize is not found in the metadat");
            }
            var foundUrl = request.ScaledObjectRef.ScalerMetadata.TryGetValue("urlOfService", out _urlOfService);
            _logger.LogInformation($"GetMetrics url: {_urlOfService}");
            if (!foundUrl)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "There is no urlOfService parameter"));


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
                MetricName = _metricNameString,
                MetricValue_ = result
            });
            return metricResponse;
        }

        public override Task<IsActiveResponse> IsActive(ScaledObjectRef scaledObject, ServerCallContext context)
        {
            _logger.LogInformation($"IsActive: true");
            return Task.FromResult(new IsActiveResponse { Result = true });
        }

    }
}
