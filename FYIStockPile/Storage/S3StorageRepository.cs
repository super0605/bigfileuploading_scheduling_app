using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FYIStockPile.Storage.Model;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FYIStockPile.Storage
{
    public class S3StorageRepository
    {
        private LiteDatabase Database;
        public bool IsRunning = false;

        public S3StorageRepository(LiteDatabase db)
        {
            this.Database = db;
        }

        public async Task<bool> UploadAsync(AppRepository app, StorageRequest storageRequest)
        {

            using (AmazonS3Client client = GetS3Client(app))
            {
                try
                {
                    string bucket = app.Settings.BucketName;

                    if (string.IsNullOrEmpty(bucket)) throw new Exception("Application settings incomplete");

                    StorageCredentials credentials = new StorageCredentials()
                    {
                        BucketName = bucket,
                    };

                    PutObjectRequest putRequest = new PutObjectRequest
                    {
                        BucketName = $"{credentials.BucketName}",
                        Key = $"{storageRequest.Type}/{storageRequest.Name}",
                        //ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
                    };

                    if (storageRequest.InputStream != null)
                    {
                        putRequest.InputStream = storageRequest.InputStream;
                    }
                    else if (!string.IsNullOrEmpty(storageRequest.Path))
                    {
                        putRequest.FilePath = storageRequest.Path;
                    }
                    else
                    {
                        putRequest.ContentBody = storageRequest.Content;
                    }

                    if (string.IsNullOrEmpty(putRequest.ContentBody) && string.IsNullOrEmpty(putRequest.FilePath) && putRequest.InputStream == null)
                    {
                        return false;
                    }

                    if (!string.IsNullOrEmpty(storageRequest.EncryptionKeyId))
                    {
                        putRequest.ServerSideEncryptionKeyManagementServiceKeyId = storageRequest.EncryptionKeyId;
                    }

                    var response = await client.PutObjectAsync(putRequest);
                    return true;
                }
                catch (AmazonS3Exception amazonS3Exception)
                {
                    if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                    {
                        throw new Exception("Check the provided AWS Credentials.");
                    }

                    throw new Exception("Error occurred: " + amazonS3Exception.Message);
                }
                catch (Exception e)
                {
                    throw new Exception("Error occurred: " + e.Message);
                }
            }
        }

        public bool ValidateMigrateKey(string migrateKey, AppRepository app)
        {
            bool result = false;
            using (AmazonS3Client client = GetS3Client(app))
            {
                try
                {
                    ListBucketsResponse response = client.ListBuckets();
                    foreach (S3Bucket b in response.Buckets)
                    {
                        Console.WriteLine("{0}\t{1}", b.BucketName, b.CreationDate);
                        if (b.BucketName == migrateKey)
                        {
                            result = true;
                            break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return result;
        }

        private AmazonS3Client GetS3Client(AppRepository app)
        {
            // ... some code
        }

        private AmazonS3Config UseProxyS3Config(string Host, string Port, string UserName, string Password)
        {
            AmazonS3Config S3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2,
                ProxyHost = Host,
                ProxyPort = Int16.Parse(Port),
                ProxyCredentials = new NetworkCredential
                {
                    UserName = UserName,
                    Password = Password
                }
            };

            return S3Config;
        }
    }
}
