using System;

namespace CCCS.Web.Models.Documents
{
    public class FileUploadResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string DateUploaded { get; set; }
        public string ViewLink { get; set; }
        public string Path { get; set; }
        public int FileId { get; set; }
    }
}
