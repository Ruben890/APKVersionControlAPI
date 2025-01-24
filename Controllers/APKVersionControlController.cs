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


        /// <summary>
        /// Descarga un archivo APK basado en la versión y el nombre proporcionados.
        /// </summary>
        /// <param name="parameters">Parámetros que incluyen IsDownload, Version y Name.</param>
        /// <returns>El archivo APK solicitado.</returns>
        /// <remarks>
        /// IsDownload debe ser true para proceder con la descarga.
        /// Los parámetros Name y Version deben coincidir exactamente con los nombres proporcionados por el endpoint GetAllApk.
        /// </remarks>
        [HttpGet("DownloadApkFile")]
        public IActionResult DownloadApkFile([FromQuery] GenericParameters parameters)
        {
            var response = new BaseResponse();
            try
            {

                if (string.IsNullOrWhiteSpace(parameters.Name))
                {
                    response.Messages = "The 'Name' parameter is required and cannot be empty or whitespace.";
                    return BadRequest(response);
                }

                // Buscar el archivo
                string filePath = _aPKVersionControl.FindFileForDownload(parameters);

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
        public IActionResult DeleteApkFile([FromQuery] GenericParameters parameters)
        {
            var response = new BaseResponse();
            try
            {

                if (string.IsNullOrWhiteSpace(parameters.Name))
                {
                    response.Messages = "The 'Name' parameter is required and cannot be empty or whitespace.";
                    return BadRequest(response);
                }

                _aPKVersionControl.DeleteApkFile(parameters);

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
