using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Manager")]
public class ParkingController : Controller
{

    private readonly ApplicationDbContext _context;


    public ParkingController(ApplicationDbContext context)
    {
        _context = context;
    }



    public IActionResult Index()
    {

        var parking =
        _context.ParkingSpots
        .Include(x => x.Flat)
        .ToList();


        return View(parking);

    }




    public IActionResult Create()
    {

        return View();

    }




    [HttpPost]
    public async Task<IActionResult> Create(ParkingSpot model)
    {


        _context.ParkingSpots.Add(model);

        await _context.SaveChangesAsync();


        return RedirectToAction("Index");

    }



}