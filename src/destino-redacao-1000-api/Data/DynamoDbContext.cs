using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{
    public class DynamoDbContext
    {
        private readonly string _tableName;
        private readonly ILogger _log;

        public string TableName
        {
            get
            {
                return _tableName;
            }
        }

        public DynamoDbContext()
        {
        }

        public DynamoDbContext(IConfiguration configuration, ILogger<DynamoDbContext> log)
        {
            _tableName = configuration["AWS:DynamoDb:Table"];
            _log = log;
            _log.LogInformation($"TableName.: {_tableName}");
        }        

        public AmazonDynamoDBClient GetClientInstance()
        {
            var config = new AmazonDynamoDBConfig();
            config.RegionEndpoint = RegionEndpoint.SAEast1;
            config.ServiceURL = "http://dynamodb.sa-east-1.amazonaws.com";

            return new AmazonDynamoDBClient(config);
        }

        public async Task<string> GetTableAsync(string tableName)
        {            
            DescribeTableResponse response = null;
            
            try
            {
                response = await GetClientInstance().DescribeTableAsync(tableName, new System.Threading.CancellationToken());
            }
            catch(Exception e)
            {
                return e.Message;            
            }
            
            return $"Table: { response.Table }, Status: { response.Table.TableStatus }";
        }

    }
}