using Events.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Controller;

[ApiController]
[Microsoft.AspNetCore.Components.Route("api/[controller]")]
public class AdminController: ControllerBase
{
    public ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        this._context = context;
    }
    
    

}