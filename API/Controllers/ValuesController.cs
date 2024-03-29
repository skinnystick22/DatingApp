﻿using System.Threading.Tasks;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ValuesController : ControllerBase
{
    private readonly DataContext _context;

    public ValuesController(DataContext context)
    {
        _context = context;
    }

    // GET api/values
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetValues()
    {
        var values = await _context.Values.ToListAsync();
        return Ok(values);
    }

    // GET api/values/{valueId}
    [Authorize(Roles = "Member")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetValue(int id)
    {
        var value = await _context.Values.FirstOrDefaultAsync(v => v.Id == id);
        return Ok(value);
    }

    // POST api/values
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/values/5
    [HttpPut("{id:int}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/values/5
    [HttpDelete("{id:int}")]
    public void Delete(int id)
    {
    }
}