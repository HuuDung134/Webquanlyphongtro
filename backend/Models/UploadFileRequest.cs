using Microsoft.AspNetCore.Http;

namespace DoAnCoSo.Models
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; }
    }
}
