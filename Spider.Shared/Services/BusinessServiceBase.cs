using Microsoft.EntityFrameworkCore;
using Spider.Shared.Interfaces;
using Spider.Shared.Exceptions;
using Spider.Shared.Resources;
using Spider.Shared.Extensions;
using Azure;
using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Serilog;

namespace Spider.Shared.Services
{
    public class BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;
        private readonly BlobContainerClient _blobContainerClient;

        public BusinessServiceBase(IApplicationDbContext context, BlobContainerClient blobContainerClient)
        {
            _context = context;
            _blobContainerClient = blobContainerClient;
        }

        public async Task<T> GetInstanceAsync<T, ID>(ID id, int? version) 
            where T : class, IBusinessObject<ID>
            where ID : struct
        {
            return await _context.WithTransactionAsync(async () =>
            {
                T poco = await _context.DbSet<T>().FindAsync(id);

                if (poco == null)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabase);

                if (version.HasValue && poco.Version != version)
                    throw new BusinessException(SharedTerms.ConcurrencyException);

                return poco;
            });
        }

        public async Task<T> GetInstanceAsync<T, ID>(ID id) 
            where T : class, IReadonlyObject<ID>
            where ID : struct
        {
            return await _context.WithTransactionAsync(async () =>
            {
                T poco = await _context.DbSet<T>().FindAsync(id);

                if (poco == null)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabase);

                return poco;
            });
        }

        protected internal async Task CheckVersionAsync<T, ID>(ID id, int version) 
            where T : class, IBusinessObject<ID> 
            where ID : struct
        {
            await _context.WithTransactionAsync(async () =>
            {
                int dbVersion = await _context.DbSet<T>().Where(x => x.Id.Equals(id)).Select(x => x.Version).SingleOrDefaultAsync();

                if (dbVersion != version)
                    throw new BusinessException(SharedTerms.ConcurrencyException);
            });
        }

        public async Task DeleteEntityAsync<T, ID>(ID id) where T : class, IBusinessObject<ID> where ID : struct // https://www.c-sharpcorner.com/article/equality-operator-with-inheritance-and-generics-in-c-sharp/
        {
            await _context.WithTransactionAsync(async () =>
            {
                int deletedRow = await _context.DbSet<T>().Where(x => x.Id.Equals(id)).ExecuteDeleteAsync();
                if (deletedRow == 0)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabaseForDeleteRequest);
            });
        }

        public async Task DeleteEntitiesAsync<T, ID>(List<ID> ids) where T : class, IBusinessObject<ID> where ID : struct
        {
            if (ids == null)
                throw new ArgumentNullException("You need to pass a list of ids to delete.");

            if (ids.Count == 0)
                return; // FT: Early return, don't make db call

            await _context.WithTransactionAsync(async () =>
            {
                await _context.DbSet<T>().Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
            });
        }


        /// <summary>
        /// </summary>
        /// <returns>Newly generated file name</returns>
        protected async Task<string> UploadFileAsync(string fileName, string objectType, string objectProperty, string objectId, Stream content)
        {
            string fileExtension = GetFileExtensionFromFileName(fileName);

            // TODO FT: Delete class name and prop name if you don't need it
            // TODO FT: Validate if user has changed ContentType to something we don't handle
            string blobName = $"{objectId}-{Guid.NewGuid()}.{fileExtension}";

            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(content);

            Dictionary<string, string> tags = new()
            {
                { "objectType", $"{objectType}" },
                { "objectProperty", $"{objectProperty}" },
                { "objectId", $"{objectId}" },
            };

            await blobClient.SetTagsAsync(tags); // https://stackoverflow.com/questions/52769758/azure-blob-storage-authorization-permission-mismatch-error-for-get-request-wit 

            return blobName;
        }

        // uzimam id iz imena kog je poslao jer ne mogu drugacije da ga posaljem
        protected static ID GetObjectIdFromFileName<ID>(string fileName) where ID : struct
        {
            List<string> parts = fileName.Split('-').ToList();

            if (parts.Count < 2)
                throw new HackerException($"Invalid file name format ({fileName}).");

            string idPart = parts[0];

            // Try to convert the string part to the specified struct type
            if (TypeDescriptor.GetConverter(typeof(ID)).IsValid(idPart))
                return (ID)TypeDescriptor.GetConverter(typeof(ID)).ConvertFromString(idPart);

            throw new InvalidCastException($"Cannot convert '{idPart}' to {typeof(ID)}.");
        }

        protected static string GetFileExtensionFromFileName(string fileName)
        {
            List<string> parts = fileName.Split('.').ToList();

            if (parts.Count < 2) // FT: It could be only 2, it's not the same validation as spliting with '-'
                throw new HackerException($"Invalid file name format ({fileName}).");

            return parts.Last(); // FT: The file could be .abc.png
        }

        // FT: Before this in save method the authorization is being done, so we don't need to do it here also
        protected async Task DeleteNonActiveBlobs(string activeBlobName, string objectType, string objectProperty, string objectId)
        {
            if (objectId == "0") // FT: If we delete 0, we will delete the blob for multiple users/partners/etc.
                return;

            AsyncPageable<TaggedBlobItem> blobs = _blobContainerClient.FindBlobsByTagsAsync($"\"objectType\"='{objectType}' AND \"objectProperty\"='{objectProperty}' AND \"objectId\"='{objectId}'");

            await foreach (TaggedBlobItem blob in blobs)
            {
                if (blob.BlobName != activeBlobName)
                    await _blobContainerClient.DeleteBlobAsync(blob.BlobName, Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
            }
        }

        protected async Task<string> GetFileDataAsync(string key)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(key);

            Azure.Response<BlobDownloadResult> blobDownloadInfo = await blobClient.DownloadContentAsync();

            byte[] byteArray = blobDownloadInfo.Value.Content.ToArray();

            string base64 = Convert.ToBase64String(byteArray);

            return $"filename={key};base64,{base64}";
        }

    }
}
