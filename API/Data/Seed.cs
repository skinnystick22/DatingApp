using System.Collections.Generic;
using System.IO;
using System.Linq;
using API.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace API.Data;

public static class Seed
{
    public static void SeedUsers(UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        if (!userManager.Users.Any())
        {
            var userData = File.ReadAllText("Data/UserSeedData.json");
            var users = JsonConvert.DeserializeObject<List<User>>(userData);

            // Create some roles
            var roles = new List<Role>
            {
                new() {Name = "Member"},
                new() {Name = "Admin"},
                new() {Name = "Moderator"},
                new() {Name = "VIP"}
            };

            foreach (var role in roles)
            {
                roleManager.CreateAsync(role).Wait();
            }

            foreach (var user in users)
            {
                user.Photos.SingleOrDefault().IsApproved = true;
                userManager.CreateAsync(user, "password").Wait();
                userManager.AddToRoleAsync(user, "Member").Wait();
            }

            // Create Admin user
            var adminUser = new User {UserName = "Admin"};
            var result = userManager.CreateAsync(adminUser, "password").Result;

            if (result.Succeeded)
            {
                var admin = userManager.FindByNameAsync("Admin").Result;
                var adminRoles = new string[] {"Admin", "Moderator"};
                userManager.AddToRolesAsync(admin, adminRoles).Wait();
            }
        }
    }
}