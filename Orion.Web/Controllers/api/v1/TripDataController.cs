using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Orion.DB;
using Orion.DB.Models;

namespace Orion.Web.Controllers.api.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TripDataController : ControllerBase
    {
        private readonly SqlContext _context;

        public TripDataController(SqlContext context)
        {
            _context = context;
        }

        // GET: api/TripData
        [HttpGet]
        public IEnumerable<TripDataModel> GetTripData()
        {
            return _context.TripData.Take(10);
        }

        // GET: api/TripData/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTripDataModel([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tripDataModel = await _context.TripData.FindAsync(id);

            if (tripDataModel == null)
            {
                return NotFound();
            }

            return Ok(tripDataModel);
        }

        [HttpGet("TimeRange")]
        public string TimeRange(string startTime, string endTime, int size = 10, int offset = 0)
        {
            DateTime sTime = DateTime.Parse(startTime);
            DateTime eTime = DateTime.Parse(endTime);

            var query = _context.TripData.AsNoTracking().Where(x => x.Trip_Date >= sTime.Date  && x.Trip_Date <= eTime.Date
                                                    && x.Trip_Hour >= sTime.Hour && x.Trip_Hour <= eTime.Hour );

            return JsonConvert.SerializeObject(query.Skip(offset).Take(size).ToArray());
        }

        // PUT: api/TripData/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTripDataModel([FromRoute] int id, [FromBody] TripDataModel tripDataModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tripDataModel.TripId)
            {
                return BadRequest();
            }

            _context.Entry(tripDataModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TripDataModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TripData
        [HttpPost]
        public async Task<IActionResult> PostTripDataModel([FromBody] TripDataModel tripDataModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.TripData.Add(tripDataModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTripDataModel", new { id = tripDataModel.TripId }, tripDataModel);
        }

        // DELETE: api/TripData/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTripDataModel([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tripDataModel = await _context.TripData.FindAsync(id);
            if (tripDataModel == null)
            {
                return NotFound();
            }

            _context.TripData.Remove(tripDataModel);
            await _context.SaveChangesAsync();

            return Ok(tripDataModel);
        }

        private bool TripDataModelExists(int id)
        {
            return _context.TripData.Any(e => e.TripId == id);
        }
    }
}