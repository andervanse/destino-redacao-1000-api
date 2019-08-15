
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{
    public class PostagemRepository : IPostagemRepository
    {
        private readonly DynamoDbContext _context;
        private readonly ILogger<RevisaoRepository> _logger;

        public PostagemRepository(
            DynamoDbContext context, 
            ILogger<RevisaoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<Response<IEnumerable<Postagem>>> ObterPostagensAsync(CategoriaPostagem categoria)
        {
            QueryRequest qryRequest = new QueryRequest
            {
                TableName = _context.TableName,
                KeyConditionExpression = "#tipo = :tipo",
                ExpressionAttributeNames = new Dictionary<string, string> {
                    { "#tipo", "tipo" },
                    { "#id", "id" },
                    { "#dtAtualizacao", "dt-atualizacao" },
                    { "#autorId", "autor-id" },
                    { "#autorEmail", "autor-email" },
                    { "#ordem", "ordem" },
                    { "#titulo", "titulo" },
                    { "#texto", "texto" },
                    { "#imagemUrl", "url-imagem" },
                    { "#cat", "categoria" }
                },
                FilterExpression = "#cat = :cat",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":tipo", new AttributeValue { S = "postagem" } },
                    { ":cat", new AttributeValue { S = categoria.ToString() } },
                },
                ProjectionExpression = "#id, #ordem, #dtAtualizacao, #autorId,  #autorEmail, #titulo, #texto, #imagemUrl, #cat"
            };

            var resp = new Response<IEnumerable<Postagem>>();
            QueryResponse response = null;

            using (var client = this._context.GetClientInstance())
            {
                try
                {
                    response = await client.QueryAsync(qryRequest);

                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        var msg = "Falha ao obter postagens.";
                        resp.ErrorMessages.Add(msg);
                        _logger.LogError(msg);
                    }
                }
                catch (Exception e)
                {
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                }
            }

            List<Postagem> postagens = ExtractFileFrom(response.Items);
            resp.Return = postagens;
            return resp;
        }

        public async Task<Response<Postagem>> SalvarAsync(Postagem postagem)
        {
            var response = new Response<Postagem>();

            if (postagem == null)
            {
                response.ErrorMessages.Add("Arquivo obrigatório.");
                return response;
            }

            using (var client = this._context.GetClientInstance())
            {
                try
                {
                    StringBuilder updateExpr = new StringBuilder("SET ");
                    var exprAttrValues = new Dictionary<string, AttributeValue>();
                    var exprAttrNames = new Dictionary<string, string>();

                    if (postagem.Id < 1)
                    {
                        postagem.Id = (Int32)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                    
                    postagem.DataAtualizacao = DateTime.Now;
                    exprAttrValues.Add(":dtAtualizacao", new AttributeValue { S = postagem.DataAtualizacao.ToString("dd/MM/yyyy hh:mm:ss") });
                    updateExpr.Append(" #dtAtualizacao = :dtAtualizacao,");
                    exprAttrNames.Add("#dtAtualizacao", "dt-atualizacao");

                    if (postagem.Autor != null)
                    {
                        exprAttrValues.Add(":autorId", new AttributeValue { N = postagem.Autor.Id.ToString() });
                        updateExpr.Append(" #autorId = :autorId,");
                        exprAttrNames.Add("#autorId", "autor-id");

                        exprAttrValues.Add(":autorEmail", new AttributeValue { S = postagem.Autor.Email });
                        updateExpr.Append(" #autorEmail = :autorEmail,");
                        exprAttrNames.Add("#autorEmail", "autor-email");
                    }

                    if (!String.IsNullOrEmpty(postagem.Titulo))
                    {
                        exprAttrValues.Add(":titulo", new AttributeValue { S = postagem.Titulo });
                        updateExpr.Append(" #titulo = :titulo,");
                        exprAttrNames.Add("#titulo", "titulo");
                    }

                    if (!String.IsNullOrEmpty(postagem.Texto))
                    {
                        exprAttrValues.Add(":texto", new AttributeValue { S = postagem.Texto });
                        updateExpr.Append(" #texto = :texto,");
                        exprAttrNames.Add("#texto", "texto");
                    }

                    if (!String.IsNullOrEmpty(postagem.UrlImagem))
                    {
                        exprAttrValues.Add(":url", new AttributeValue { S = postagem.UrlImagem });
                        updateExpr.Append(" #url = :url,");
                        exprAttrNames.Add("#url", "url-imagem");
                    }

                    exprAttrValues.Add(":cat", new AttributeValue { S = postagem.Categoria.ToString() });
                    updateExpr.Append(" #cat = :cat,");
                    exprAttrNames.Add("#cat", "categoria");

                    var request = new UpdateItemRequest
                    {
                        TableName = _context.TableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            { "tipo", new AttributeValue { S = $"postagem" } },
                            { "id", new AttributeValue { N = postagem.Id.ToString() } }
                        },
                        ExpressionAttributeNames = exprAttrNames,
                        ExpressionAttributeValues = exprAttrValues,
                        UpdateExpression = updateExpr.ToString().Substring(0, updateExpr.ToString().Length - 1)
                    };

                    var updResp = await client.UpdateItemAsync(request);
                }
                catch (Exception e)
                {
                    response.Return = postagem;
                    response.ErrorMessages.Add("Falha ao salvar postagem.");
                    response.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                }
                return response;
            }
        }

        public async Task<Response<Postagem>> ExcluirAsync(Postagem postagem)
        {
            var resp = new Response<Postagem>();

            using (var client = this._context.GetClientInstance())
            {
                DeleteItemRequest request = new DeleteItemRequest
                {
                    TableName = _context.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { N = postagem.Id.ToString() } },
                        { "tipo", new AttributeValue { S = "postagem" } }
                    }
                };

                try
                {
                    DeleteItemResponse response = await client.DeleteItemAsync(request);

                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        resp.ErrorMessages.Add("Falha ao deletar postagem.");
                        _logger.LogError("Falha ao deletar postagem.");
                    }
                }
                catch (Exception e)
                {
                    resp.ErrorMessages.Add(e.Message);
                    _logger.LogError(e.Message);
                }

                return resp;
            }
        }   

        private List<Postagem> ExtractFileFrom(List<Dictionary<string, AttributeValue>> dictionary)
        {
            List<Postagem> list = new List<Postagem>();
            Postagem postagem = null;

            foreach (var item in dictionary)
            {
                postagem = new Postagem();

                foreach (KeyValuePair<string, AttributeValue> kvp in item)
                {
                    string attributeName = kvp.Key;
                    AttributeValue value = kvp.Value;

                    if (attributeName == "id")
                    {
                        int id = 0;
                        int.TryParse(value.N, out id);
                        postagem.Id = id;
                    }
                    else if (attributeName == "autor-id")
                    {
                        int autorId = 0;
                        int.TryParse(value.N, out autorId);
                        postagem.Autor.Id = autorId;
                    } 
                    else if (attributeName == "autor-email")
                    {
                        postagem.Autor.Email = value.S;
                    }                                       
                    else if (attributeName == "titulo")
                    {
                        postagem.Titulo = value.S;
                    }
                    else if (attributeName == "texto")
                    {
                        postagem.Texto = value.S;
                    }
                    else if (attributeName == "url-imagem")
                    {
                        postagem.UrlImagem = value.S;
                    }
                    else if (attributeName == "categoria")
                    {
                        Object categoria = null;
                        Enum.TryParse(typeof(CategoriaPostagem), value.S, true, out categoria);

                        if (categoria != null)
                           postagem.Categoria = (CategoriaPostagem)categoria;
                    }                    
                    else if (attributeName == "dt-atualizacao")
                    {
                        DateTime dataAtualizacao;
                        DateTime.TryParseExact(value.S,
                                                "dd/MM/yyyy hh:mm:ss",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out dataAtualizacao);
                        postagem.DataAtualizacao = dataAtualizacao;
                    }
                }
                list.Add(postagem);
            }
            return list;
        }             
    }
}