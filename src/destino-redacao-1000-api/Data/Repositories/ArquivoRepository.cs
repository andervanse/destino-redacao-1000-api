using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{
    public class ArquivoRepository :IArquivoRepository
    {
        private readonly DynamoDbContext _context;
        private readonly ILogger _logger;

        public ArquivoRepository(DynamoDbContext context, ILogger<ArquivoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Response<Arquivo>> SalvarAsync(Arquivo arquivo)
        {
            var resp = new Response<Arquivo>();

            using (var client = this._context.GetClientInstance())
            {
                try
                {
                    StringBuilder updExp = new StringBuilder("SET ");
                    var exprAttrValues = new Dictionary<string, AttributeValue>();
                    var exprAttrNames = new Dictionary<string, string>();

                    if (arquivo.Id < 1)
                    {
                        arquivo.Id = (Int32)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }

                    arquivo.DataAtualizacao = DateTime.Now;
                    exprAttrValues.Add(":dtAt", new AttributeValue { S = arquivo.DataAtualizacao.ToString("dd/MM/yyyy hh:mm:ss") });
                    updExp.Append(" #dtAt = :dtAt,");
                    exprAttrNames.Add("#dtAt", "dt-atualizacao");                                               

                    if (arquivo.Id <= 0)
                    {
                        exprAttrValues.Add(":arquivoId", new AttributeValue { S = arquivo.Id.ToString() });
                        updExp.Append(" #arquivoId = :arquivoId,");
                        exprAttrNames.Add("#arquivoId", "arquivo-id");
                    }  

                    if (!String.IsNullOrEmpty(arquivo.Url))
                    {
                        exprAttrValues.Add(":url", new AttributeValue { S = arquivo.Url });
                        updExp.Append(" #url = :url,");
                        exprAttrNames.Add("#url", "url");                        
                    }

                    var request = new UpdateItemRequest
                    {
                        TableName = _context.TableName,
                        Key = new Dictionary<string, AttributeValue>
                            {
                                { "tipo", new AttributeValue { S = $"arquivo-{arquivo.UsuarioId}" } },
                                { "id", new AttributeValue { N = arquivo.Id.ToString() } }
                            },

                        ExpressionAttributeNames = exprAttrNames,
                        ExpressionAttributeValues = exprAttrValues,
                        UpdateExpression = updExp.ToString().Substring(0, updExp.ToString().Length - 1)
                    };

                    var updResp = await client.UpdateItemAsync(request);
                    resp.Return = arquivo;

                    return resp;
                }
                catch (Exception e)
                {
                    resp.Return = arquivo;
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                    return resp;
                }
            }
        }

        public async Task<Response<List<Arquivo>>> ObterListaAsync(Usuario usuario)
        {
            var resp = new Response<List<Arquivo>>();
            QueryResponse response = await ObterArquivoResponseAsync<List<Arquivo>>(usuario, resp);
            List<Arquivo> lstUser = ExtractFileFrom(response.Items);
            resp.Return = lstUser;
            return resp;
        }

        public async Task<Response<Arquivo>> ObterArquivoAsync(Usuario usuario, String urlArquivo)
        {
            var resp = new Response<Arquivo>();
            QueryResponse response = await ObterArquivoResponseAsync<Arquivo>(usuario, resp);
            List<Arquivo> lstArquivo = ExtractFileFrom(response.Items);

            if (lstArquivo.Count > 0)
                resp.Return = lstArquivo[0];
            else
                resp.Messages.Add("Nenhum registro encontrado.");

            return resp;
        }

        private async Task<QueryResponse> ObterArquivoResponseAsync<T>(Usuario usuario, Response<T> resp)
        {
            var attrName = String.Empty;
            var attrValue = new AttributeValue();
            Dictionary<string, string> attributes = new Dictionary<string, string> { { "#tipo", "tipo" } };

            if (usuario.Id > 0)
            {
                attrName = "id";
                attrValue.N = usuario.Id.ToString();
            }                        

            attributes.Add($"#{attrName}", $":{attrName}");
            QueryResponse response = null;

            using (var client = this._context.GetClientInstance())
            {
                QueryRequest request = ObterArquivoQueryRequest(attrName, attrValue);

                try
                {
                    response = await client.QueryAsync(request);
                }
                catch (Exception e)
                {
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                }
            }

            return response;
        }

        private QueryRequest ObterArquivoQueryRequest(string attrName, AttributeValue attrValue)
        {
            string filterExpr = null;
            string keyExpr = "#tipo = :t";

            if (attrName == "id") {
                keyExpr = "#tipo = :t AND #id = :id";
            } else {
                filterExpr = $"#{attrName} = :{attrName}";
            }

            if (attrName == "nome")
                filterExpr = $"begins_with(#{attrName}, :{attrName})";

            return new QueryRequest
            {
                TableName = _context.TableName,
                KeyConditionExpression = keyExpr,
                FilterExpression = filterExpr,
                ExpressionAttributeNames = new Dictionary<string, string> {
                        { "#id", "id" },
                        { "#tipo", "tipo" },
                        { "#nome", "nome" },
                        { "#dtAt", "dt-atualizacao" },
                        { "#url", "url" }
                    },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                         { ":t", new AttributeValue { S = "arquivo" } },
                         { $":{attrName}", attrValue }
                    },
                ProjectionExpression = "#id, #tipo, #nome, #dtAt, #url"
            };
        }

        private List<Arquivo> ExtractFileFrom(List<Dictionary<string, AttributeValue>> dictionary)
        {
            List<Arquivo> list = new List<Arquivo>();
            Arquivo arquivo = null;

            foreach (var item in dictionary)
            {
                arquivo = new Arquivo();

                foreach (KeyValuePair<string, AttributeValue> kvp in item)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;

                    if (attributeName == "id")
                    {
                        arquivo.Id = int.Parse(value.N);
                    }
                    else if (attributeName == "nome")
                    {
                        arquivo.Nome = value.S;
                    }                  
                    else if (attributeName == "url")
                    {
                        arquivo.Url = value.S;
                    }                    
                    else if (attributeName == "dt-atualizacao")
                    {
                        DateTime dtAtual;
                        DateTime.TryParse(value.S, out dtAtual);
                        arquivo.DataAtualizacao = dtAtual;
                    }                    
                }
                list.Add(arquivo);
            }
            return list;
        }
    }
}