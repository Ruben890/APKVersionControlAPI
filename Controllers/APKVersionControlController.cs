using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.QueryParameters;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace APKVersionControlAPI.Controllers
{
    [ApiController]
    [Route("api/APKVersionControl")]
    public class APKVersionControlController : ControllerBase
    {
        private readonly IAPKVersionControlServices _aPKVersionControl;
        public APKVersionControlController(IAPKVersionControlServices aPKVersionControl)
        {
            _aPKVersionControl = aPKVersionControl;
        }


        [HttpPost("upload-apk")]
        public async Task<IActionResult> UploadApkFile([FromForm] IFormFile file)
        {
            var response = new BaseResponse();
            try
            {
                return Ok(_aPKVersionControl.UploadApkFile(file));
            }
            catch (Exception ex)
            {
                response.Messeges = ex.Message;
                return BadRequest(response);
            }
        }

    }
}
