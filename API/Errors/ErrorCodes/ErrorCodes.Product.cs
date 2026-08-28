namespace API.Errors;

public static partial class ErrorCodes
{
    public static class Product
    {
        public const string NotFound = "PRODUCT_NOT_FOUND";
        public const string CreateError = "PRODUCT_CREATE_ERROR";
        public const string UpdateError = "PRODUCT_UPDATE_ERROR";
        public const string DeleteError = "PRODUCT_DELETE_ERROR";
        public const string IdMismatch = "PRODUCT_ID_MISMATCH";
    }
}
