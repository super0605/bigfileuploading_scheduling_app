using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FYIStockPile.Storage.Model
{
    public class StorageRequest
    {
        /// <summary>
        /// The path to the file to upload
        /// Do not set InputStream or Content if you use this
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of request
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The KMS EncryptionKey to be used, this is optional
        /// </summary>
        public string EncryptionKeyId { get; set; }

        /// <summary>
        /// A Input stream of the file to be uploaded
        /// Do not set Path or Content if you use this
        /// </summary>
        public Stream InputStream { get; set; }

        /// <summary>
        /// The Text content to be uploaded
        /// Do not set Path or InputStream if you use this
        /// </summary>
        public string Content { get; set; }

        public override string ToString()
        {
            return $@"Path: {Path}
                      Name: {Name}
                      Type: {Type}
                      EncryptionKeyId: {EncryptionKeyId}
                    ";
        }
    }
}
