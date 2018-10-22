using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CsvHelper;
using Flux.flux.dto;
using Flux.Flux.Client;
using Flux.Flux.Core;
using Flux.Flux.Options;
using Newtonsoft.Json;

namespace Flux.Flux
{
    public class FluxClient : IFluxClient
    {
        private readonly DefaultClientIO _client;
        private readonly FluxConnectionOptions _options;

        public FluxClient(FluxConnectionOptions options)
        {
            _options = options ?? throw new ArgumentException("options");
            _client = new DefaultClientIO(options);
        }

        // TODO parse response from csv to list of flux tables
        public async Task<List<FluxTable>> Query(string query)
        {
            try
            {
                var responseHttp = await _client.DoRequest(FluxService.Query(
                                FluxService.CreateBody(FluxService.GetDefaultDialect(), query)))
                                .ConfigureAwait(false);
                
                RaiseForInfluxError(responseHttp);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            return null;
        }

        /** <summary>
         * Check service health.
         * </summary>
         */
        public async Task<bool> Ping()
        {
            try
            {
                var responseHttp = await _client.DoRequest(FluxService.Ping()).ConfigureAwait(false);
                
                RaiseForInfluxError(responseHttp);
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return false;
            }
        }

        public string Version()
        {
            throw new NotImplementedException();
        }

        internal struct ErrorsWrapper
        {
            public IReadOnlyList<string> Errors;
        }

        internal static void RaiseForInfluxError(RequestResult resultRequest)
        {
            var statusCode = resultRequest.StatusCode;

            if (statusCode >= 200 && statusCode < 300)
            {
                return;
            }

            var wrapper = resultRequest.ResponseContent.Length > 1
                            ? JsonConvert.DeserializeObject<ErrorsWrapper>(resultRequest.ResponseContent)
                            : new ErrorsWrapper();

            var response = new QueryErrorResponse(statusCode, wrapper.Errors);

            var message = InfluxException.GetErrorMessage(resultRequest);

            if (message != null)
            {
                throw new InfluxException(response);
            }

            throw new HttpException(response);
        }
    }
}