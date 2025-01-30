using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.QueryParameters;
using Microsoft.AspNetCore.Mvc;

namespace APKVersionControlAPI.Controllers
{
    [Route("api/APKVersionControl")]
    [ApiController]
    public class APKVersionControlController : ControllerBase
    {
        private readonly IAPKVersionControlServices _aPKVersionControl;
        public APKVersionControlController(IAPKVersionControlServices aPKVersionControl)
        {
            _aPKVersionControl = aPKVersionControl;
        }


        [HttpPost("UploadApkFile")]
        public IActionResult UploadApkFile(IFormFile File, [FromQuery] string? Client)
        {
            var response = new BaseResponse();
            try
            {
                response.Messages = _aPKVersionControl.UploadApkFile(File, Client).Result!;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Messages = ex.Message;
                return BadRequest(response);
            }
        }


        [HttpGet("GetAllApk")]
        public async Task<IActionResult> GetAllApk([FromQuery] GenericParameters parameters)
        {
            var response = new BaseResponse();
            try
            {
                response.Details = await _aPKVersionControl.GetApkFiles(parameters);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Messages = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("DownloadApkFile")]
        public async Task<IActionResult> DownloadApkFile([FromQuery] DownloadParameters parameters)
        {
            var response = new BaseResponse();
            try
            {


                // Buscar el archivo
                string filePath = await _aPKVersionControl.FindFileForDownload(parameters);

                // Verificar si el archivo existe
                if (!System.IO.File.Exists(filePath))
                {
                    response.Messages = "File not found.";
                    return NotFound(response);
                }

                // Devolver el archivo para descargar
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/vnd.android.package-archive", Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                response.Messages = ex.Message;
                return BadRequest(response);
            }
        }


        [HttpDelete("DeleteApkFile")]
        public async Task<IActionResult> DeleteApkFile([FromQuery] int Id)
        {
            var response = new BaseResponse();
            try
            {

                if (string.IsNullOrWhiteSpace(Id.ToString()))
                {
                    response.Messages = "The 'Id' parameter is required and cannot be empty or whitespace.";
                    return BadRequest(response);
                }

                await _aPKVersionControl.DeleteApkFile(Id);

                return Ok(response.Messages = "The APK file was deleted successfully.");
            }
            catch (Exception ex)
            {
                response.Messages = ex.Message;
                return BadRequest(response);
            }
        }
    }
}
