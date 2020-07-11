using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Externalscaler;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace my_scaler
{
    public class ExternalScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly ILogger<GreeterService> _logger;
        public ExternalScalerService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef scaledObject, ServerCallContext context)
        {
            var metricSpecResponse = new GetMetricSpecResponse();
            metricSpecResponse.MetricSpecs.Add(new MetricSpec()
            {
                MetricName = "mymetric",
                TargetSize = 10
            });

            return Task.FromResult(metricSpecResponse);
        }

        public override Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var metricResponse = new GetMetricsResponse();
            metricResponse.MetricValues.Add(new MetricValue
            {
                MetricName = "mymetric",
                MetricValue_ = 30
            });
            return Task.FromResult(metricResponse);
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
            return Task.FromResult<Google.Protobuf.WellKnownTypes.Empty>(new Google.Protobuf.WellKnownTypes.Empty());
        }


    }
}
