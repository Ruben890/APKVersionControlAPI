using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Shared;
using APKVersionControlAPI.Shared.Dto;
using APKVersionControlAPI.Shared.QueryParameters;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

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


        [HttpPost("upload-apk")]
        public IActionResult UploadApkFile([FromForm] FileDto request)
        {
            var response = new BaseResponse();
            try
            {
                return Ok(_aPKVersionControl.UploadApkFile(request.File!));
            }
            catch (Exception ex)
            {
                response.Messeges = ex.Message;
                return BadRequest(response);
            }
        }

    }
}
