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
        public IActionResult UploadApkFile(IFormFile File)
        {
            var response = new BaseResponse();
            try
            {
                response.Messeges = _aPKVersionControl.UploadApkFile(File).Result!;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Messeges = ex.Message;
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
                response.Messeges = ex.Message;
                return BadRequest(response);
            }
        }

    }
}
