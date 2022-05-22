using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.Helpers;
using API.Models;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Route("api/users/{userId:int}/photos")]
[ApiController]
public class PhotosController : ControllerBase
{
    private readonly IDatingRepository _repository;
    private readonly IMapper _mapper;
    private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
    private readonly Cloudinary _cloudinary;

    public PhotosController(IDatingRepository repository, IMapper mapper,
        IOptions<CloudinarySettings> cloudinaryConfig)
    {
        _repository = repository;
        _mapper = mapper;
        _cloudinaryConfig = cloudinaryConfig;

        var account = new Account(_cloudinaryConfig.Value.CloudName, _cloudinaryConfig.Value.ApiKey,
            _cloudinaryConfig.Value.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }
        
    private bool IsUnauthorized(int userId)
    {
        if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return true;
        return false;
    }

    // GET api/users/{userId}/photos/{photoId}
    [HttpGet("{id}", Name = "GetPhoto")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var photoFromRepository = await _repository.GetPhoto(id);
        var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepository);

        return Ok(photo);
    }

    // POST api/users/{userId}/photos
    [HttpPost]
    public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm] PhotoForCreationDto photoForCreationDto)
    {
        if (IsUnauthorized(userId))
            return Unauthorized();

        var userFromRepository = await _repository.GetUser(userId, true);
        var file = photoForCreationDto.File;
        var uploadResult = new ImageUploadResult();

        if (file.Length > 0)
        {
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.Name, stream),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                };

                uploadResult = _cloudinary.Upload(uploadParams);
            }
        }

        photoForCreationDto.Url = uploadResult.Url.ToString();
        photoForCreationDto.PublicId = uploadResult.PublicId;

        var photo = _mapper.Map<Photo>(photoForCreationDto);

        if (!userFromRepository.Photos.Any(u => u.IsMain))
            photo.IsMain = true;

        userFromRepository.Photos.Add(photo);

        if (await _repository.SaveAll())
        {
            var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
            return CreatedAtRoute("GetPhoto", new {UserId = userId, Id = photo.Id}, photoToReturn);
        }

        return BadRequest("Could not add the photo");
    }

    // POST api/users/{userId}/photos/{photoId/setMain}
    [HttpPost("{id:int}/setMain")]
    public async Task<IActionResult> SetMainPhoto(int userId, int id)
    {
        if (IsUnauthorized(userId))
            return Unauthorized();

        var user = await _repository.GetUser(userId, true);

        if (user.Photos.All(p => p.Id != id))
            return Unauthorized();

        var photoFromRepository = await _repository.GetPhoto(id);
        if (photoFromRepository.IsMain)
            return BadRequest("This is already the main photo.");

        var currentMainPhoto = await _repository.GetMainPhotoForUser(userId);
        currentMainPhoto.IsMain = false;
        photoFromRepository.IsMain = true;

        if (await _repository.SaveAll())
            return NoContent();

        return BadRequest("Could not set photo to main");
    }

    // DELETE api/users/{userId}/photos/{photoId}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePhoto(int userId, int id)
    {
        if (IsUnauthorized(userId))
            return Unauthorized();

        var user = await _repository.GetUser(userId, true);

        if (user.Photos.All(p => p.Id != id))
            return Unauthorized();

        var photoFromRepository = await _repository.GetPhoto(id);
        if (photoFromRepository.IsMain)
            return BadRequest("Cannot delete main photo.");

        if (photoFromRepository.PublicId != null)
        {
            var deleteParameters = new DeletionParams(photoFromRepository.PublicId);
            var result = await _cloudinary.DestroyAsync(deleteParameters);

            if (result.Result == "ok")
                _repository.Delete(photoFromRepository);
        }

        if (photoFromRepository.PublicId == null)
            _repository.Delete(photoFromRepository);

        if (await _repository.SaveAll())
            return Ok();

        return BadRequest("Failed to delete the photo");
    }
}