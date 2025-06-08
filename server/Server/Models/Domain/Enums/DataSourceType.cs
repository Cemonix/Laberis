namespace server.Models.Domain.Enums;

public enum DataSourceType
{
    MINIO_BUCKET,
    S3_BUCKET,
    GSC_BUCKET,
    AZURE_BLOB_STORAGE,
    LOCAL_DIRECTORY,
    DATABASE,
    API,
    OTHER
}
