using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{
    public class AWSUploadFile : IUploadFile
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _log;

        public AWSUploadFile(
            IConfiguration configuration,
            ILoggerFactory logger)
        {
            _configuration = configuration;
            _log = logger.CreateLogger("UploadFileAWS");
        }

        public async Task<string> UploadFileAsync(Usuario usuario, Stream fileStream, string keyName)
        {
            string urlLocation = null;

            try
            {
                using (var client = new AmazonS3Client(RegionEndpoint.SAEast1))
                {
                    var fileLocation = GetFileLocation(usuario, keyName);
                    var bucketUrl = _configuration["Website:S3BucketUrl"];
                    urlLocation = $"{bucketUrl}/{fileLocation}";

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _configuration["Website:S3Bucket"],
                        Key = fileLocation,
                        InputStream = fileStream,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    putRequest.Headers.ExpiresUtc = DateTime.Now.AddDays(60);
                    var response = await client.PutObjectAsync(putRequest);
                    _log.LogInformation($"Uploaded object {putRequest.Key}. Request Id: {response.ResponseMetadata.RequestId}");                    
                }
            }
            catch (AmazonS3Exception e)
            {
                _log.LogError("Error encountered on server when writing an object. Message:'{0}'", e.Message);
            }
            catch (Exception e)
            {
                _log.LogError("Unknown error encountered on server when writing an object. Message:'{0}'", e.Message);
            }

            return urlLocation;
        }

        public async Task<string> DeleteFileAsync(Usuario usuario, string keyName)
        {
            string urlLocation = null;

            try
            {
                using (var client = new AmazonS3Client(RegionEndpoint.SAEast1))
                {
                    var bucketName = _configuration["Website:S3Bucket"];
                    var fileLocation = GetFileLocation(usuario, keyName);
                    var resp = await client.DeleteObjectAsync(bucketName, fileLocation);
                }
            }
            catch (AmazonS3Exception e)
            {
                _log.LogError("Error encountered when deleting object. Message:'{0}'", e.Message);

            }
            catch (Exception e)
            {
                _log.LogError("Unknown error encountered on server when deleting object. Message:'{0}'", e.Message);
            }
            return urlLocation;
        }

        private string GetFileLocation(Usuario usuario, string keyName)
        {
            return $"usuarios/{usuario.Id}/{keyName}";
        }
    }

}