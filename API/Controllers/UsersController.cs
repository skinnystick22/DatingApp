using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.Dtos;
using API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
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

        // GET api/users
        [HttpGet]
        public async Task<IActionResult> GetUsersAsync()
        {
            var users = await _repository.GetUsers();

            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);
            
            return Ok(usersToReturn);
        }

        // GET api/users/{userId}
        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUserAsync(int id)
        {
            var user = await _repository.GetUser(id);

            var userToReturn = _mapper.Map<UserForDetailedDto>(user);

            return Ok(userToReturn);
        }
        
        // PUT /api/users/{userId}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserAsync(int id, UserForUpdateDto userForUpdateDto)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepository = await _repository.GetUser(id);

            _mapper.Map(userForUpdateDto, userFromRepository);

            if (await _repository.SaveAll())
                return NoContent();
            
            throw new Exception($"Updating user: {id} failed on save");
        }
    }
}