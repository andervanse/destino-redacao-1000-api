using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{
    public class RevisaoRepository : IRevisaoRepository
    {
        private readonly DynamoDbContext _context;
        private readonly ILogger _logger;

        public RevisaoRepository(DynamoDbContext context, ILogger<RevisaoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Response<Revisao>> SalvarAsync(Revisao revisao)
        {
            var resp = new Response<Revisao>();

            if (revisao.Arquivo == null)
            {
                resp.ErrorMessages.Add("Arquivo obrigatório.");
                return resp;
            }

            using (var client = this._context.GetClientInstance())
            {
                try
                {
                    StringBuilder updExp = new StringBuilder("SET ");
                    var exprAttrValues = new Dictionary<string, AttributeValue>();
                    var exprAttrNames = new Dictionary<string, string>();

                    if (revisao.Id < 1)
                    {
                        revisao.Id = (Int32)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }

                    revisao.DataPrevista = DateTime.Now.AddDays(4);
                    exprAttrValues.Add(":dtPrev", new AttributeValue { S = revisao.DataPrevista.ToString("dd/MM/yyyy hh:mm:ss") });
                    updExp.Append(" #dtPrev = :dtPrev,");
                    exprAttrNames.Add("#dtPrev", "dt-prevista");

                    exprAttrValues.Add(":assinanteId", new AttributeValue { N = revisao.AssinanteId.ToString() });
                    updExp.Append(" #assinanteId = :assinanteId,");
                    exprAttrNames.Add("#assinanteId", "assinante-id");

                    exprAttrValues.Add(":assinanteEmail", new AttributeValue { S = revisao.AssinanteEmail });
                    updExp.Append(" #assinanteEmail = :assinanteEmail,");
                    exprAttrNames.Add("#assinanteEmail", "assinante-email");

                    exprAttrValues.Add(":revId", new AttributeValue { N = revisao.RevisorId.ToString() });
                    updExp.Append(" #revId = :revId,");
                    exprAttrNames.Add("#revId", "revisor-id");

                    if (revisao.StatusRevisao.HasValue) 
                    {
                        exprAttrValues.Add(":status", new AttributeValue { S = revisao.StatusRevisao.Value.ToString() });
                        updExp.Append(" #status = :status,");
                        exprAttrNames.Add("#status", "status");
                    }

                    if (revisao.RevisaoIdRef.HasValue) 
                    {
                        exprAttrValues.Add(":revIdRef", new AttributeValue { N = revisao.RevisaoIdRef.Value.ToString() });
                        updExp.Append(" #revIdRef = :revIdRef,");
                        exprAttrNames.Add("#revIdRef", "revisao-id-ref");
                    }

                    revisao.Arquivo.DataAtualizacao = DateTime.Now;
                    exprAttrValues.Add(":dtAt", new AttributeValue { S = revisao.Arquivo.DataAtualizacao.ToString("dd/MM/yyyy hh:mm:ss") });
                    updExp.Append(" #dtAt = :dtAt,");
                    exprAttrNames.Add("#dtAt", "dt-atualizacao");

                    if (!String.IsNullOrEmpty(revisao.Arquivo.Nome))
                    {
                        exprAttrValues.Add(":nome", new AttributeValue { S = revisao.Arquivo.Nome });
                        updExp.Append(" #nome = :nome,");
                        exprAttrNames.Add("#nome", "nome");
                    }

                    if (!String.IsNullOrEmpty(revisao.Arquivo.Url))
                    {
                        exprAttrValues.Add(":url", new AttributeValue { S = revisao.Arquivo.Url });
                        updExp.Append(" #url = :url,");
                        exprAttrNames.Add("#url", "url");
                    }

                    exprAttrValues.Add(":tpArq", new AttributeValue { S = revisao.Arquivo.TipoArquivo.ToString() });
                    updExp.Append(" #tpArq = :tpArq,");
                    exprAttrNames.Add("#tpArq", "tp-arquivo");

                    if (!String.IsNullOrEmpty(revisao.Comentario))
                    {
                        exprAttrValues.Add(":comentario", new AttributeValue { S = revisao.Comentario });
                        updExp.Append(" #comentario = :comentario,");
                        exprAttrNames.Add("#comentario", "comentario");
                    }

                    var request = new UpdateItemRequest
                    {
                        TableName = _context.TableName,
                        Key = new Dictionary<string, AttributeValue>
                            {
                                { "tipo", new AttributeValue { S = $"revisao" } },
                                { "id", new AttributeValue { N = revisao.Id.ToString() } }
                            },

                        ExpressionAttributeNames = exprAttrNames,
                        ExpressionAttributeValues = exprAttrValues,
                        UpdateExpression = updExp.ToString().Substring(0, updExp.ToString().Length - 1)
                    };

                    var updResp = await client.UpdateItemAsync(request);
                    resp.Return = revisao;

                    return resp;
                }
                catch (Exception e)
                {
                    resp.Return = revisao;
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                    return resp;
                }
            }
        }

        public async Task<Response<List<Revisao>>> ObterNovasRevisoesAsync(Usuario usuario)
        {
            var resp = new Response<List<Revisao>>();
            QueryResponse response = null;

            using (var client = this._context.GetClientInstance())
            {
                QueryRequest request = new QueryRequest
                {
                    TableName = _context.TableName,
                    KeyConditionExpression = "#tipo = :tipo",
                    ExpressionAttributeNames = new Dictionary<string, string> {
                        { "#id", "id" },
                        { "#tipo", "tipo" },
                        { "#nome", "nome" },
                        { "#usrId", "assinante-id" },
                        { "#dtAt", "dt-atualizacao" },
                        { "#url", "url" },
                        { "#comentario", "comentario" },
                        { "#dtPrev", "dt-prevista" },
                        { "#status", "status" },
                        { "#revisorId", "revisor-id" }
                    },
                    FilterExpression = "#status = :status AND #revisorId = :revisorId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":tipo", new AttributeValue { S = "revisao" } },
                        { ":status", new AttributeValue { S = "NovaRevisao"} },
                        { ":revisorId", new AttributeValue { N = "0" } }
                    },

                    ProjectionExpression = "#id, #tipo, #nome, #usrId, #dtAt, #url, #comentario, #dtPrev, #status, #revisorId"
                };

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

            List<Revisao> revisoes = ExtractFileFrom(response.Items);
            resp.Return = revisoes;
            return resp;
        }

        public async Task<Response<List<Revisao>>> ObterRevisoesPendentesAsync(Usuario usuario)
        {
            var resp = new Response<List<Revisao>>();
            QueryResponse response = null;

            using (var client = this._context.GetClientInstance())
            {
                QueryRequest request = new QueryRequest
                {
                    TableName = _context.TableName,
                    KeyConditionExpression = "#tipo = :tipo",
                    ExpressionAttributeNames = new Dictionary<string, string> {
                        { "#id", "id" },
                        { "#tipo", "tipo" },
                        { "#nome", "nome" },
                        { "#usrId", "assinante-id" },
                        { "#dtAt", "dt-atualizacao" },
                        { "#url", "url" },
                        { "#comentario", "comentario" },
                        { "#dtPrev", "dt-prevista" },
                        { "#status", "status" },
                        { "#tpArq", "tp-arquivo" },
                        { "#revIdRef", "revisao-id-ref" }, 
                        { "#revisorId", "revisor-id" }
                    },
                    FilterExpression = "#revisorId = :revisorId AND (#status = :status OR #tpArq = :tpArq)",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":tipo", new AttributeValue { S = "revisao" } },
                        { ":status", new AttributeValue { S = "EmRevisao"} },
                        { ":tpArq", new AttributeValue { S = "Correcao"} },
                        { ":revisorId", new AttributeValue { N = usuario.Id.ToString() } }
                    },

                    ProjectionExpression = "#id, #tipo, #nome, #usrId, #dtAt, #url, #comentario, #dtPrev, #status, #tpArq, #revIdRef, #revisorId"
                };

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

            List<Revisao> revisoes = ExtractFileFrom(response.Items);
            resp.Return = revisoes;
            return resp;
        }
        public async Task<Response<List<Revisao>>> ObterRevisoesAssinanteAsync(Usuario usuario)
        {
            var resp = new Response<List<Revisao>>();
            QueryResponse response = null;

            using (var client = this._context.GetClientInstance())
            {
                QueryRequest request = new QueryRequest
                {
                    TableName = _context.TableName,
                    KeyConditionExpression = "#tipo = :tipo",
                    ExpressionAttributeNames = new Dictionary<string, string> {
                        { "#id", "id" },
                        { "#tipo", "tipo" },
                        { "#nome", "nome" },
                        { "#assinanteId", "assinante-id" },
                        { "#dtAt", "dt-atualizacao" },
                        { "#url", "url" },
                        { "#comentario", "comentario" },
                        { "#dtPrev", "dt-prevista" },
                        { "#status", "status" },
                        { "#tpArq", "tp-arquivo" },
                        { "#revIdRef", "revisao-id-ref" }, 
                        { "#revisorId", "revisor-id" }
                    },
                    FilterExpression = "#assinanteId = :assinanteId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":tipo", new AttributeValue { S = "revisao" } },
                        { ":assinanteId", new AttributeValue { N = usuario.Id.ToString() } }
                    },

                    ProjectionExpression = "#id, #tipo, #nome, #dtAt, #url, #comentario, #dtPrev, #status, #tpArq, #revIdRef, #revisorId"
                };

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

            List<Revisao> revisoes = ExtractFileFrom(response.Items);
            resp.Return = revisoes;
            return resp;
        }

        public async Task<Response<Revisao>> AtualizarRevisorAsync(Revisao revisao)
        {
            var resp = new Response<Revisao>();

            using (var client = this._context.GetClientInstance())
            {
                try
                {
                    var request = new UpdateItemRequest
                    {
                        TableName = _context.TableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            { "tipo", new AttributeValue { S = $"revisao" } },
                            { "id", new AttributeValue { N = revisao.Id.ToString() } }
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            { "#usrId", "assinante-id" },
                            { "#status", "status" },
                            { "#revId", "revisor-id" }
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            {":usrId", new AttributeValue { N = revisao.AssinanteId.ToString() }},
                            {":status", new AttributeValue { S = revisao.StatusRevisao.ToString() }},
                            {":revId", new AttributeValue { N = revisao.RevisorId.ToString() }}
                        },
                        UpdateExpression = "SET #usrId = :usrId, #status = :status, #revId = :revId"
                    };

                    var updResp = await client.UpdateItemAsync(request);
                    resp.Return = revisao;
                    return resp;
                }
                catch (Exception e)
                {
                    resp.Return = revisao;
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                    return resp;
                }
            }
        }
        
        public async Task<Response<Revisao>> DeletarAsync(Revisao revisao)
        {
            var resp = new Response<Revisao>();
            DeleteItemResponse response = null;

            using (var client = this._context.GetClientInstance())
            {
                DeleteItemRequest request = new DeleteItemRequest
                {
                    TableName = _context.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { N = revisao.Id.ToString() } },
                        { "tipo", new AttributeValue { S = "revisao" } }
                    },
                    ConditionExpression = "#status = :status",
                    ExpressionAttributeNames = new Dictionary<string, string> {
                        { "#status", "status" }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":status", new AttributeValue { S = StatusRevisao.NovaRevisao.ToString() } }
                    }
                };

                try
                {
                    response = await client.DeleteItemAsync(request);
                }
                catch (Exception e)
                {
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                }

                return resp;
            }
        }

        private QueryRequest ObterRevisaoQueryRequest(Usuario usuario)
        {
            string keyExpr = "#tipo = :t";

            return new QueryRequest
            {
                TableName = _context.TableName,
                KeyConditionExpression = keyExpr,
                ExpressionAttributeNames = new Dictionary<string, string> {
                    { "#id", "id" },
                    { "#tipo", "tipo" },
                    { "#nome", "nome" },
                    { "#usrId", "assinante-id" },
                    { "#dtAt", "dt-atualizacao" },
                    { "#url", "url" },
                    { "#comentario", "comentario" },
                    { "#dtPrev", "dt-prevista" },
                    { "#status", "status" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":t", new AttributeValue { S = "revisao" } }
                },
                ProjectionExpression = "#id, #tipo, #nome, #usrId, #dtAt, #url, #comentario, #dtPrev, #status"
            };
        }
        
        private List<Revisao> ExtractFileFrom(List<Dictionary<string, AttributeValue>> dictionary)
        {
            List<Revisao> list = new List<Revisao>();
            Revisao revisao = null;

            foreach (var item in dictionary)
            {
                revisao = new Revisao();

                foreach (KeyValuePair<string, AttributeValue> kvp in item)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;

                    if (attributeName == "id")
                    {
                        revisao.Id = int.Parse(value.N);
                    }
                    else if (attributeName == "nome")
                    {
                        revisao.Arquivo.Nome = value.S;
                    }
                    else if (attributeName == "url")
                    {
                        revisao.Arquivo.Url = value.S;
                    }
                    else if (attributeName == "comentario")
                    {
                        revisao.Comentario = value.S;
                    }
                    else if (attributeName == "assinante-id")
                    {
                        int id = 0;
                        int.TryParse(value.N, out id);
                        revisao.AssinanteId = id;
                    }
                    else if (attributeName == "assinante-email")
                    {
                        revisao.AssinanteEmail = value.S;
                    }                    
                    else if (attributeName == "revisor-id")
                    {
                        int id = 0;
                        int.TryParse(value.N, out id);
                        revisao.RevisorId = id;
                    }
                    else if (attributeName == "status")
                    {
                        Object st = null;
                        Enum.TryParse(typeof(StatusRevisao), value.S, true, out st);
                        revisao.StatusRevisao = (StatusRevisao)st;
                    }
                    else if (attributeName == "tp-arquivo")
                    {
                        Object st = null;
                        Enum.TryParse(typeof(TipoArquivo), value.S, true, out st);
                        revisao.Arquivo.TipoArquivo = (TipoArquivo)st;
                    }                    
                    else if (attributeName == "dt-prevista")
                    {
                        DateTime dtPrev;
                        DateTime.TryParseExact(value.S,
                                                "dd/MM/yyyy hh:mm:ss",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out dtPrev);
                        revisao.DataPrevista = dtPrev;
                    }
                    else if (attributeName == "dt-atualizacao")
                    {
                        DateTime dtAtual;
                        DateTime.TryParseExact(value.S,
                                            "dd/MM/yyyy hh:mm:ss",
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.None,
                                            out dtAtual);
                        revisao.Arquivo.DataAtualizacao = dtAtual;
                    }
                }
                list.Add(revisao);
            }
            return list;
        }

    }
}