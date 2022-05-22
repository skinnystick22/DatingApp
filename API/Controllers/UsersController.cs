using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.Dtos;
using API.Helpers;
using API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ServiceFilter(typeof(LogUserActivity))]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IDatingRepository _repository;
    private readonly IMapper _mapper;

    public UsersController(IDatingRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
        
    private bool IsUnauthorized(int userId)
    {
        if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return true;
        return false;
    }

    // GET api/users
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserParams userParams)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var userFromRepository = await _repository.GetUser(currentUserId, true);
        userParams.UserId = currentUserId;

        if (string.IsNullOrEmpty(userParams.Gender))
            userParams.Gender = userFromRepository.Gender == "male" ? "female" : "male";

        var users = await _repository.GetUsers(userParams);
        var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);

        Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

        return Ok(usersToReturn);
    }

    // GET api/users/{id}
    [HttpGet("{userId:int}", Name = "GetUser")]
    public async Task<IActionResult> GetUser(int userId)
    {
        var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == userId; // Returns the user Id
        var user = await _repository.GetUser(userId, isCurrentUser);
        var userToReturn = _mapper.Map<UserForDetailedDto>(user);

        return Ok(userToReturn);
    }

    // PUT /api/users/{id}
    [HttpPut("{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, UserForUpdateDto userForUpdateDto)
    {
        if (IsUnauthorized(userId))
            return Unauthorized();

        var userFromRepository = await _repository.GetUser(userId, true);

        _mapper.Map(userForUpdateDto, userFromRepository);

        if (await _repository.SaveAll())
            return NoContent();

        throw new Exception($"Updating user: {userId} failed on save");
    }

    // POST /api/users/{id}/like/{recipientId}
    [HttpPost("{userId:int}/like/{recipientId:int}")]
    public async Task<IActionResult> LikeUser(int userId, int recipientId)
    {
        if (IsUnauthorized(userId))
            return Unauthorized();

        var like = await _repository.GetLike(userId, recipientId);
        if (like != null)
            return BadRequest("You already like this user.");

        if (await _repository.GetUser(recipientId, true) == null)
            return NotFound();

        like = new Like
        {
            LikerId = userId,
            LikeeId = recipientId
        };
        _repository.Add(like);

        if (await _repository.SaveAll())
            return Ok();

        return BadRequest("Failed to Like user");
    }
}