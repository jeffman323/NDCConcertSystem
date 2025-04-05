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
        public void ConcertTicketManagerTest()
        {
            if (_controller == null)
            {
                Assert.Fail();
            }

            // Validate Dates
            List<string> dates = new List<string>();
            dates.Add("2026-01-02");
            dates.Add("2026-01-023");
            List<string> validDates = new List<string>();
            List<string> invalidDates = new List<string>();
            _controller.ValidateDates(dates, out validDates, out invalidDates);
            Assert.IsTrue(validDates.Count == 1);
            Assert.IsTrue(invalidDates.Count == 1);
        }

        [TestMethod()]
        public void ValidDatesTest()
        {
            if (_controller == null)
            {
                Assert.Fail();
            }

            // Setup
            List<string> dates = new List<string>();
            dates.Add("2026-01-02");
            dates.Add("2026-01-023");
            List<string> validDates = new List<string>();
            List<string> invalidDates = new List<string>();
            
            _controller.ValidateDates(dates, out validDates, out invalidDates);

            Assert.IsTrue(validDates.Count == 1);
            Assert.IsTrue(invalidDates.Count == 1);
        }


        [TestMethod()]
        public void ValidDateAndCapacityTest()
        {
            if (_controller == null)
            {
                Assert.Fail();
            }

            // Setup
            VenueItem venueItem = new VenueItem
            {
                Name = "test",
                Capacity = 5,
                AvailableDates = ["2026-02-15"],
            };
            EventItemDTO eventItem = new EventItemDTO
            {
                Name = "test",
                Capacity = 10,
                Date = "2026-02-15",
                TicketPrices = []
            };

            // Fail on capacity
            Assert.IsFalse(_controller.ValidateDateAndCapacity(eventItem, venueItem));

            venueItem.Capacity = 10;

            // Pass for good match
            Assert.IsTrue(_controller.ValidateDateAndCapacity(eventItem, venueItem));

            venueItem.AvailableDates.RemoveAt(0);

            // fail on date
            Assert.IsFalse(_controller.ValidateDateAndCapacity(eventItem, venueItem));
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
            Assert.IsTrue(result.Result?.GetType() == typeof(Microsoft.AspNetCore.Mvc.BadRequestObjectResult));
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
            Assert.IsTrue(result.Result?.GetType() == typeof(Microsoft.AspNetCore.Mvc.BadRequestObjectResult));
        }
    }
}