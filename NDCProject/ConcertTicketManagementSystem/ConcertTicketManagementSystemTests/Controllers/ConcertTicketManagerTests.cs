using ConcertTicketManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace ConcertTicketManagementSystem.Controllers.Tests
{
    [TestClass()]
    public class ConcertTicketManagerTests
    {
        ConcertTicketManager? _controller;

        [TestInitialize]
        public void InitializeTests()
        {
            if(_controller == null)
            {
                DbContextOptions<VenueContext> venueOptions = new DbContextOptions<VenueContext>();
                VenueContext _venueContext = new VenueContext(venueOptions);
                DbContextOptions<EventContext> eventOptions = new DbContextOptions<EventContext>();
                EventContext _eventContext = new EventContext(eventOptions);
                DbContextOptions<ReservationContext> reservationOptions = new DbContextOptions<ReservationContext>();
                ReservationContext _reservationContext = new ReservationContext(reservationOptions);

                _controller = new ConcertTicketManager(_venueContext, _eventContext, _reservationContext);
            }
        }

        [TestMethod()]
        public async Task PostVenueTest()
        {
            if (_controller == null)
            {
                Assert.Fail();
            }

            // Setup
            VenueItemDTO venueItem = new VenueItemDTO
            {
                Name = "test",
                Capacity = 5,
                AvailableDates = ["2026-02-152"],
            };

            ActionResult<string> result = await _controller.PostVenueItem(venueItem);
            Assert.IsTrue(result.Result?.GetType() == typeof(BadRequestObjectResult));
        }

        [TestMethod()]
        public async Task PostEventTest()
        {
            if (_controller == null)
            {
                Assert.Fail();
            }

            // Setup
            EventItemDTO eventItem = new EventItemDTO
            {
                Name = "test",
                Capacity = 10,
                Date = "2026-02-155",
                TicketPrices = []
            };

            ActionResult<string> result = await _controller.PostEventItem(eventItem);
            Assert.IsTrue(result.Result?.GetType() == typeof(BadRequestObjectResult));
        }
    }
}