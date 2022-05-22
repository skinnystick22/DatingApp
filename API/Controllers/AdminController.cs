using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Dtos;
using API.Helpers;
using API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;
    private readonly Cloudinary _cloudinary;

    public AdminController(DataContext context, UserManager<User> userManager,
        IOptions<CloudinarySettings> cloudinaryConfiguration)
    {
        _context = context;
        _userManager = userManager;

        var account = new Account(cloudinaryConfiguration.Value.CloudName, cloudinaryConfiguration.Value.ApiKey,
            cloudinaryConfiguration.Value.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("usersWithRoles")]
    public async Task<IActionResult> GetUsersWithRoles()
    {
        var userList = await _context.Users.OrderBy(x => x.UserName)
            .Select(user => new
            {
                Id = user.Id,
                UserName = user.UserName,
                Roles = (from userRole in user.UserRoles
                    join role in _context.Roles
                        on userRole.RoleId
                        equals role.Id
                    select role.Name).ToList()
            }).ToListAsync();

        return Ok(userList);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("editRoles/{userName}")]
    public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
    {
        var user = await _userManager.FindByNameAsync(userName);
        var userRoles = await _userManager.GetRolesAsync(user);
        var selectedRoles = roleEditDto.RoleNames;
        // null coalescing operator ??
        selectedRoles ??= new string[] { };
        var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to add to roles.");

        result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to remove the roles");

        return Ok(await _userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photosForModeration")]
    public async Task<IActionResult> GetPhotosForModeration()
    {
        var photos = await _context.Photos.Include(u => u.User)
            .IgnoreQueryFilters()
            .Where(p => p.IsApproved == false)
            .Select(u => new
            {
                Id = u.Id,
                UserName = u.User.UserName,
                Url = u.Url,
                IsApproved = u.IsApproved
            }).ToListAsync();

        return Ok(photos);
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approvePhoto/{photoId:int}")]
    public async Task<IActionResult> ApprovePhoto(int photoId)
    {
        var photo = await GetPhotoById(photoId);

        photo.IsApproved = true;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("rejectPhoto/{photoId:int}")]
    public async Task<IActionResult> RejectPhoto(int photoId)
    {
        var photo = await GetPhotoById(photoId);

        if (photo.IsMain)
            return BadRequest("You cannot reject the main photo.");

        if (photo.PublicId != null)
        {
            var deletionParams = new DeletionParams(photo.PublicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok")
                _context.Photos.Remove(photo);
        }

        if (photo.PublicId == null)
            _context.Photos.Remove(photo);

        await _context.SaveChangesAsync();
        return Ok();
    }

    private async Task<Photo> GetPhotoById(int photoId)
    {
        var photo = await _context.Photos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == photoId);
        return photo;
    }
}