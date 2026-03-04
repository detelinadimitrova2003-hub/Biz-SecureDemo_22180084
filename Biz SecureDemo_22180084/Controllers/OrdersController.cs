using BizSecureDemo_22180084.Data;
using BizSecureDemo_22180084.Models;
using BizSecureDemo_22180084.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BizSecureDemo_22180084.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderVm vm)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index", "Home");

        var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        _db.Orders.Add(new Order
        {
            UserId = uid,
            Title = vm.Title,
            Amount = vm.Amount
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Home");
    }
	public async Task<IActionResult> Details(int id)
	{
		var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		// Добавяме проверка: търсим поръчката по нейния Id И по UserId на логнатия потребител
		var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);

		if (order == null) return NotFound(); // Ако не е наша, връщаме 404
		return View(order);
	}



}
