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

        public ExternalScalerService(IHttpClientFactory httpClientFactory, ILogger<ExternalScalerService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GetMetricsSpec is for constructing the HPA object. If the ScaledObject changes KEDA needs to change the HPA too.
        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef scaledObject, ServerCallContext context)
        {
            int targetSize;
            int defaultTargetSize = 1;
            string targetSizeInString;
            if (scaledObject.ScalerMetadata.TryGetValue(_targetSizeKey, out targetSizeInString))
            {
                _logger.LogInformation($"GetMetrics: targetValue string found, and it's :{targetSizeInString}");
                if(int.TryParse(targetSizeInString, out targetSize))
                {
                    _logger.LogInformation($"GetMetrics: targetSize is parsed to: {targetSize}");
                }
                else 
                {
                    _logger.LogWarning($"GetMetrics: targetSize couldn't be parsed to long, will default to: {defaultTargetSize}");
                    targetSize = defaultTargetSize;
                }
            }
            else
            {
                _logger.LogWarning($"GetMetrics: targetSize is not found in the metadat, will default to: {defaultTargetSize}");
                targetSize = defaultTargetSize;
            }
            var metricSpecResponse = new GetMetricSpecResponse();
            metricSpecResponse.MetricSpecs.Add(new MetricSpec()
            {
                MetricName = _metricNameString, //If this scaler handles more than one metric, then this changes. In our case it doesn't.
                TargetSize = targetSize
            });

            return Task.FromResult(metricSpecResponse);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            string urlOfService;
            var foundUrl = request.ScaledObjectRef.ScalerMetadata.TryGetValue("urlOfService", out urlOfService);
            _logger.LogInformation($"GetMetrics url: {urlOfService}");
            if (!foundUrl)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "There is no urlOfService parameter"));


            _logger.LogInformation($"GetMetrics: about to call service: {urlOfService}");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, urlOfService);
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
