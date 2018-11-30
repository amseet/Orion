using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Orion.DB;
using Orion.DB.Models;

namespace Orion.Web.Views
{
    public class TripDataController : Controller
    {
        private readonly SqlContext _context;

        public TripDataController(SqlContext context)
        {
            _context = context;
        }

        // GET: TripData
        public async Task<IActionResult> Index()
        {
            return View(await _context.TripData.ToListAsync());
        }

        // GET: TripData/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tripDataModel = await _context.TripData
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (tripDataModel == null)
            {
                return NotFound();
            }

            return View(tripDataModel);
        }

        // GET: TripData/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TripData/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TripId,Pickup_Datetime,Dropoff_Datetime,Passenger_Count,Trip_Distance,Pickup_Longitude,Pickup_Latitude,Dropoff_Longitude,Dropoff_Latitude,Fare_Amount")] TripDataModel tripDataModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tripDataModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tripDataModel);
        }

        // GET: TripData/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tripDataModel = await _context.TripData.FindAsync(id);
            if (tripDataModel == null)
            {
                return NotFound();
            }
            return View(tripDataModel);
        }

        // POST: TripData/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TripId,Pickup_Datetime,Dropoff_Datetime,Passenger_Count,Trip_Distance,Pickup_Longitude,Pickup_Latitude,Dropoff_Longitude,Dropoff_Latitude,Fare_Amount")] TripDataModel tripDataModel)
        {
            if (id != tripDataModel.TripId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tripDataModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripDataModelExists(tripDataModel.TripId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tripDataModel);
        }

        // GET: TripData/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tripDataModel = await _context.TripData
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (tripDataModel == null)
            {
                return NotFound();
            }

            return View(tripDataModel);
        }

        // POST: TripData/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tripDataModel = await _context.TripData.FindAsync(id);
            _context.TripData.Remove(tripDataModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TripDataModelExists(int id)
        {
            return _context.TripData.Any(e => e.TripId == id);
        }
    }
}
