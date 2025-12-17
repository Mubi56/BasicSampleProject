using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paradigm.Contract.Interface;
using Paradigm.Data;
using Paradigm.Data.Model;
using Paradigm.Server.Interface;

namespace Paradigm.Server.Application
{
    [Route("api/[controller]")]
    public class AttachmentController : Server.ControllerBase
    {
        private readonly object balanceLock = new object();
        private DbContextBase _dbContext;
        private readonly IResponse _response;
        private readonly ICryptoService _crypto;
        private readonly IGeneralService _general;

        public AttachmentController(IGeneralService general, IDomainContextResolver resolver, ICryptoService crypto, ILocalizationService localization, IResponse resp, DbContextBase dbContext) : base(resolver, localization)
        {
            _dbContext = dbContext;
            _response = resp;
            _crypto = crypto;
            _general = general;
        }

        [HttpPost]
        [Route("UploadFile")]
        public async Task<object> UploadFile()
        {
            if (Request.ContentLength > 0)
            {
                //Get File Name and Type
                var filename = this.Request.Headers["X-File-Name"];
                var fileType = this.Request.Headers["X-File-Type"];
                var filePath = this.Request.Headers["X-File-Path"];

                //Change name file name
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                if (fileNameWithoutExtension.Length > 25)
                {
                    fileNameWithoutExtension = fileNameWithoutExtension.Substring(0, 25);
                }
                fileNameWithoutExtension = Regex.Replace(fileNameWithoutExtension, @"[^a-zA-Z0-9 _-]", "");
                fileNameWithoutExtension = fileNameWithoutExtension.Replace(" ", "");
                fileNameWithoutExtension = fileNameWithoutExtension.Replace(")", "").Replace("(", "").Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
                fileNameWithoutExtension = fileNameWithoutExtension.Replace("&", "and");
                var fileNameWithTimestamp = fileNameWithoutExtension + DateTime.Now.Ticks + Path.GetExtension(filename);

                //Create path
                string pth = filePath.ToString();
                string checkPath = String.IsNullOrEmpty(pth) ? checkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot") : checkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pth);

                //Check Path
                if (!Directory.Exists(checkPath))
                {
                    DirectoryInfo di = Directory.CreateDirectory(checkPath);
                }

                string path = null;
                if (String.IsNullOrEmpty(pth))
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileNameWithTimestamp);
                }
                else
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pth, fileNameWithTimestamp);
                }

                //Create File
                using (var destinationStream = new BinaryWriter(new FileStream(path, FileMode.Create)))
                {
                    long? length = this.Request.ContentLength;
                    int bufferSize = 2048;
                    int readCount;
                    byte[] buffer = new byte[2048];
                    readCount = await Request.Body.ReadAsync(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        destinationStream.Write(buffer, 0, readCount);
                        readCount = await Request.Body.ReadAsync(buffer, 0, bufferSize); ;
                    }
                }

                _response.Success = Constants.ResponseSuccess;
                _response.Message = "File Uploaded";
                _response.Data = fileNameWithTimestamp;
                return Ok(_response);
            }
            else
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = "No File Selected";
                return Ok(_response);
            }
        }

        [HttpGet]
        [Route("DownloadFile")]
        public async Task<object> DownloadFile(string filename, string folderPath)
        {

            if (filename == null)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.FileNameRequired;
                return Ok(_response);
            }

            string path = null;
            if (String.IsNullOrEmpty(folderPath))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filename);
            }
            else
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderPath, filename);
            }

            try
            {
                byte[] bytes = await System.IO.File.ReadAllBytesAsync(path);
                var file = new FileContentResult(bytes, new MediaTypeHeaderValue("application/octet").ToString())
                {
                    FileDownloadName = filename
                };
                return file;
            }
            catch
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.FileNotFound;
                return Ok(_response);
            }

        }
        [HttpGet]
        [Route("DeleteFile")]
        public async Task<object> DeleteFile(string filename, string folderName)
        {

            if (string.IsNullOrEmpty(filename))
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = "File Name Required";
                return Ok(_response);
            }

            string path = null;
            if (String.IsNullOrEmpty(folderName))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filename);
            }
            else
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName, filename);
            }

            try
            {
                byte[] bytes = await System.IO.File.ReadAllBytesAsync(path);
                System.IO.File.Delete(path);
                _response.Success = Constants.ResponseSuccess;
                _response.Message = "File Deleted";
                return Ok(_response);
            }
            catch
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = "File not found";
                return Ok(_response);
            }
        }

    }
}