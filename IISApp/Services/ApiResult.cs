using System.Net;

namespace IISApp.Services
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public HttpStatusCode StatusCode { get; set; }
    }
}
