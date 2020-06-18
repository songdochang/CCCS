<%@ WebHandler Language="C#" Class="Download" %>

using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.UI;

public class Download : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {

        string filePath = context.Request.QueryString["f"];
        //filePath = context.Server.MapPath("~/" + filePath);
        string fileName = Path.GetFileName(filePath);
        FileInfo fi = new FileInfo(filePath);

        HttpResponse response = context.Response;

        response.ClearContent();
        response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName);
        response.AppendHeader("Content-Length", fi.Length.ToString());
        response.ContentType = ReturnType(fi.Extension);
        response.TransmitFile(filePath);
        response.End();
    }

    private string ReturnType(string fileExtension)
    {
        switch (fileExtension)
        {
            case ".htm":
            case ".html":
            case ".log":
                return "text/HTML";
            case ".txt":
                return "text/plain";
            case ".doc":
                return "application/ms-word";
            case ".tiff":
            case ".tif":
                return "image/tiff";
            case ".asf":
                return "video/x-ms-asf";
            case ".avi":
                return "video/avi";
            case ".zip":
                return "application/zip";
            case ".xls":
            case ".csv":
                return "application/vnd.ms-excel";
            case ".gif":
                return "image/gif";
            case ".jpg":
            case "jpeg":
                return "image/jpeg";
            case ".bmp":
                return "image/bmp";
            case ".wav":
                return "audio/wav";
            case ".mp3":
                return "audio/mpeg3";
            case ".mpg":
            case "mpeg":
                return "video/mpeg";
            case ".rtf":
                return "application/rtf";
            case ".asp":
                return "text/asp";
            case ".pdf":
                return "application/pdf";
            case ".fdf":
                return "application/vnd.fdf";
            case ".ppt":
                return "application/mspowerpoint";
            case ".dwg":
                return "image/vnd.dwg";
            case ".msg":
                return "application/msoutlook";
            case ".xml":
            case ".sdxl":
                return "application/xml";
            case ".xdp":
                return "application/vnd.adobe.xdp+xml";
            default:
                return "application/octet-stream";
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}