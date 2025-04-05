using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketManagementSystem.Models;

namespace ConcertTicketManagementSystem.Controllers
{
    [Route("api/eventManager")]
    [ApiController]
    public class ConcertTicketManager : ControllerBase
    {
        private readonly VenueContext _venueContext;
        private readonly EventContext _eventContext;
        private readonly ReservationContext _reservationContext;

        public ConcertTicketManager(VenueContext venueContext, EventContext eventContext, ReservationContext reservationContext)
        {
            _venueContext = venueContext;
            _eventContext = eventContext;
            _reservationContext = reservationContext;
        }

        [HttpGet("getVenueItems")]
        public async Task<ActionResult<IEnumerable<VenueItemDTO>>> GetVenueItems()
        {
            return await _venueContext.VenueItems
                .Select(x => VenueToDTO(x))
                .ToListAsync();
        }

        [HttpGet("getVenueItem{id}")]
        public async Task<ActionResult<VenueItemDTO>> GetVenueItem(long id)
        {
            var venueItem = await _venueContext.VenueItems.FindAsync(id);

            if (venueItem == null)
            {
                return NotFound();
            }

            return VenueToDTO(venueItem);
        }

        [HttpPut("putVenueItem{id}")]
        public async Task<IActionResult> PutVenueItem(long id, VenueItemDTO venueItemDTO)
        {
            VenueItem? venueItem = _venueContext.VenueItems.FindAsync(id).Result;
            if (venueItem == null)
            {
                return BadRequest("Venue does not exist");
            }

            List<string> validDates = new List<string>();
            List<string> invalidDates = new List<string>();

            // Validate date formatting
            string dateCheckOutput = ValidateDates(venueItemDTO.AvailableDates, out validDates, out invalidDates);

            if (validDates.Count == 0)
            {
                return BadRequest(dateCheckOutput);
            }

            // We can't change the dates to not include the date of an event at the venue
            if (venueItem.AssociatedEventIds.Count > 0)
            {
                foreach (long eventId in venueItem.AssociatedEventIds)
                {
                    EventItem? eventItem = _eventContext.EventItems.FindAsync(eventId).Result;
                    if (eventItem != null) 
                    {
                        string date = eventItem.Date;
                        if (!validDates.Contains(date))
                        {
                            return BadRequest("A venue cannot be changed such that it does not include the date of an already scheduled event.");
                        }
                        else
                        {
                            // If an event has a date already booked, we don't want to re-list it.
                            validDates.Remove(date);
                        }
                    }
                }
            }

            // Make updates
            venueItem.Name = venueItemDTO.Name;
            venueItem.Capacity = venueItemDTO.Capacity;
            venueItem.AvailableDates = validDates;

            _venueContext.VenueItems.Add(venueItem);
            _venueContext.Entry(venueItem).State = EntityState.Modified;

            try
            {
                await _venueContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VenueItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            string output = "Venue updated!";

            // Put requests with some valid and some invalid dates are allowed to pass, but we still need to alert the user
            if (invalidDates.Count > 0)
            {
                output += "\n" + dateCheckOutput;
            }

            return Ok(output); 
        }

        [HttpPost("postVenueItem")]
        public async Task<ActionResult<string>> PostVenueItem(VenueItemDTO venueItemDTO)
        {
            List<string> validDates = new List<string>();
            List<string> invalidDates = new List<string>();

            VenueItem venueItem = new VenueItem 
            {
                Name = venueItemDTO.Name,
                Capacity = venueItemDTO.Capacity,
                AvailableDates = venueItemDTO.AvailableDates,
            };

            // Validate date formatting
            string dateCheckOutput = ValidateDates(venueItem.AvailableDates, out validDates, out invalidDates);

            if (validDates.Count == 0)
            {
                return BadRequest(dateCheckOutput);
            }
            
            // Only store the valid dates
            venueItem.AvailableDates = validDates;

            _venueContext.VenueItems.Add(venueItem);
            await _venueContext.SaveChangesAsync();

            string output = "Venue saved! ID is " + venueItem.VenueId + " (Save this! You'll need it to update or delete the venue.)";

            // Post requests with some valid and some invalid dates are allowed to pass, but we still need to alert the user
            if (invalidDates.Count > 0)
            {
                output += "\n" + dateCheckOutput;
            }

            return CreatedAtAction(nameof(GetVenueItem), new { id = venueItem.VenueId }, output);
        }

        [HttpDelete("deleteVenueItem{id}")]
        public async Task<IActionResult> DeleteVenueItem(long id)
        {
            var venueItem = await _venueContext.VenueItems.FindAsync(id);
            if (venueItem == null)
            {
                return NotFound();
            }

            if(venueItem.AssociatedEventIds.Count > 0)
            {
                return BadRequest("Venue has scheduled events. Please contact the event owners before cancelling.");
            }

            _venueContext.VenueItems.Remove(venueItem);
            try
            {
                await _venueContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VenueItemExists(id))
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

        [HttpGet("getEventItems")]
        public async Task<ActionResult<IEnumerable<EventItemDTO>>> GetEventItems()
        {
            await UpdateReservations();

            return await _eventContext.EventItems
            .Select(x => EventToDTO(x))
            .ToListAsync();
        }

        [HttpGet("getEventItem{id}")]
        public async Task<ActionResult<EventItemDTO>> GetEventItem(long id)
        {
            await UpdateReservations();

            var eventItem = await _eventContext.EventItems.FindAsync(id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return EventToDTO(eventItem);
        }

        [HttpPut("putEventItem{id}")]
        public async Task<IActionResult> PutEventItem(long id, EventItemDTO eventItemDTO)
        {
            await UpdateReservations();

            EventItem eventItem = new EventItem
            {
                Name = eventItemDTO.Name,
                Date = eventItemDTO.Date,
                Description = eventItemDTO.Description,
                Capacity = eventItemDTO.Capacity,
                TicketPrices = eventItemDTO.TicketPrices,
            };

            // We need to make sure there's an event to modify
            EventItem? oldEvent = _eventContext.EventItems.FindAsync(id).Result;
            if (oldEvent == null)
            {
                return BadRequest("event does not exist");
            }

            List<string> validDates = new List<string>();
            List<string> invalidDates = new List<string>();
            List<string> eventDate = new List<string>();
            eventDate.Add(eventItem.Date);

            string dateCheckOutput = ValidateDates(eventDate, out validDates, out invalidDates);

            if (validDates.Count == 0)
            {
                return BadRequest(dateCheckOutput);
            }

            // To update an event, we need to make sure it's still valid with it's venue
            VenueItem? venue = _venueContext.VenueItems.FindAsync(oldEvent.AssociatedVenueId).Result;
            if (venue == null)
            {
                return BadRequest("Event's venue does not exist. Please delete and recreate the event");
            }

            // Check capacity against the venue's capacity
            if (eventItem.Capacity <= venue.Capacity)
            {
                // If we have a valid venue with still appropriate capacity, we need to verify dates, but only if the date is changed
                if(eventItem.Date != oldEvent?.Date)
                {
                    // Check if the new date still works for the venue
                    if (venue.AvailableDates.Contains(eventItem.Date) && oldEvent?.Date != null) 
                    {
                        // Add back the old date and remove the new date
                        venue.AvailableDates.Add(oldEvent.Date);
                        venue.AvailableDates.Remove(eventItem.Date);
                    }
                    else
                    {
                        return BadRequest("The event's venue is unavailable on the new date. Please delete and recreate the event");
                    }
                }
            }
            else
            {
                return BadRequest("The event's new capacity exceeds that of the venue. Please specify a lower capacity, or delete and recreate the event.");
            }

            if (!ValidateTicketFormatting(eventItem))
            {
                return BadRequest("Ticket pricing is incorrectly formatted. Expected 2d array with 4 columns: Name, Price, TotalCapacity, CurrentCapacity.");
            }


            // update the event
            oldEvent.Name = eventItem.Name;
            oldEvent.Date = eventItem.Date;
            oldEvent.Description = eventItem.Description;
            oldEvent.Capacity = eventItem.Capacity;
            // when updating available tickets, only adjust by the difference
            for (int i = 0; i < eventItem.TicketPrices.Length; i++)
            {
                int diff = Int32.Parse(eventItem.TicketPrices[i][2]) - Int32.Parse(oldEvent.TicketPrices[i][2]);
                // If we changed capacity
                if (diff != 0)
                {
                    if(Int32.Parse(oldEvent.TicketPrices[i][3]) + diff < 0)
                    {
                        return BadRequest("Total tickets must remain high enough to include all already-sold tickets.");
                    }
                    // Update the event, now that we've made sure we haven't subtracted too many tickets
                    oldEvent.TicketPrices[i][2] = eventItem.TicketPrices[i][2];
                    oldEvent.TicketPrices[i][3] = (Int32.Parse(oldEvent.TicketPrices[i][3])+diff).ToString();
                }
            }

            _eventContext.EventItems.Add(oldEvent);
            _eventContext.Entry(oldEvent).State = EntityState.Modified;

            try
            {
                await _eventContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Update the venue item
            if (venue != null)
            {
                _venueContext.VenueItems.Add(venue);
                _venueContext.Entry(venue).State = EntityState.Modified;

                try
                {
                    await _venueContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueItemExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return Ok("Event updated!");
        }

        [HttpPost("postEventItem")]
        public async Task<ActionResult<string>> PostEventItem(EventItemDTO eventItemDTO)
        {
            List<string> validDates = new List<string>();
            List<string> invalidDates = new List<string>();
            List<string> eventDate = new List<string>();

            EventItem eventItem = new EventItem
            {
                Name = eventItemDTO.Name,
                Date = eventItemDTO.Date,
                Description = eventItemDTO.Description,
                Capacity = eventItemDTO.Capacity,
                TicketPrices = eventItemDTO.TicketPrices,
            };

            eventDate.Add(eventItem.Date);

            // Validate date formatting
            string dateCheckOutput = ValidateDates(eventDate, out validDates, out invalidDates);

            // There's only one date for an event, so if it's not valid we can't proceed.
            if (validDates.Count == 0)
            {
                return BadRequest(dateCheckOutput);
            }
            
            // Try to find a venue to associate with the event, giving preference to a specified venue
            VenueItem? venue = FindValidVenue(eventItemDTO).Result;
            if (venue == null)
            {
                return BadRequest("No valid venues were found! Check your venue details, requested date, or capacity.");
            }

            if(!ValidateTicketFormatting(eventItem))
            {
                return BadRequest("Ticket pricing is incorrectly formatted. Expected 2d array with 4 columns: Name, Price, TotalCapacity, CurrentCapacity.");
            }

            //if the event did not get scheduled at the requested venue we need to inform the user
            string output = "";
            if (venue.VenueId != eventItemDTO.Venue?.VenueId && venue.Name != eventItemDTO.Venue?.Name)
            {
                output += "Requested venue was not available with given requirements. Venue: " + venue.Name + " chosen instead.\n";
            }

            eventItem.AssociatedVenueId = venue.VenueId;

            _eventContext.EventItems.Add(eventItem);
            await _eventContext.SaveChangesAsync();

            // Add the event to the venue as well
            venue.AssociatedEventIds.Add(eventItem.EventId);
            venue.AvailableDates.Remove(eventItem.Date);

            _venueContext.VenueItems.Add(venue);
            _venueContext.Entry(venue).State = EntityState.Modified;

            try
            {
                await _venueContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VenueItemExists(venue.VenueId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            output += "Event saved! ID is " + eventItem.EventId + " (Save this! You'll need it to update or delete the event.)";

            return CreatedAtAction(nameof(GetEventItem), new { id = eventItem.EventId }, output);
        }

        [HttpDelete("deleteEventItem{id}")]
        public async Task<IActionResult> DeleteEventItem(long id)
        {
            var eventItem = await _eventContext.EventItems.FindAsync(id);
            if (eventItem == null)
            {
                return NotFound();
            }

            await UpdateReservations();

            // We need to check if there are already purchased tickets
            string[][] tickets = eventItem.TicketPrices;
            // Iterate through each ticket type
            for (int i = 0; i < tickets.Length; i++)
            {
                // Check if tickets are already sold for the ticket type
                if (Int32.Parse(tickets[i][3]) < Int32.Parse(tickets[i][2]))
                {
                    return BadRequest("Tickets have already been purchased or reserverd for this event. Cancellation cannot proceed.");
                }
            }

            // Venue needs to be updated for cancelled events
            var venueItem = await _venueContext.VenueItems.FindAsync(eventItem.AssociatedVenueId);
            if (venueItem != null)
            {
                venueItem.AssociatedEventIds.Remove(eventItem.EventId);
                // Add back the date the event was
                venueItem.AvailableDates.Add(eventItem.Date);

                _venueContext.VenueItems.Add(venueItem);
                _venueContext.Entry(venueItem).State = EntityState.Modified;
                try
                {
                    await _venueContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueItemExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            _eventContext.EventItems.Remove(eventItem);
            try
            {
                await _eventContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventItemExists(id))
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

        [HttpGet("getTicketsForEvent{id}")]
        public async Task<ActionResult<List<string>>> GetTicketsForEvent(long id)
        {
            await UpdateReservations();

            var eventItem = await _eventContext.EventItems.FindAsync(id);

            if (eventItem == null)
            {
                return NotFound();
            }

            string[][] prices = eventItem.TicketPrices;
            List<string> tickets = new List<string>();
            for (int i = 0; i < eventItem.TicketPrices.Length; i++)
            {
                tickets.Add(prices[i][0] + ": $" + prices[i][1] + " - " + prices[i][3] + " available.");
            }

            return tickets;
        }

        [HttpPost("reserveTicket{id}/{duration}/{ticketType}/{reservingUser}")]
        public async Task<IActionResult> ReserveTicket(long id, int duration, string ticketType, string reservingUser)
        {
            await UpdateReservations();

            var eventItem = await _eventContext.EventItems.FindAsync(id);
            if (eventItem == null)
            {
                return NotFound();
            }
            if(duration > 15 || duration <= 0)
            {
                return BadRequest("Tickets cannot be reserved for more than 15 minutes or less than 1 minute");
            }
            
            //Find the requested ticket type
            string[][] tickets = eventItem.TicketPrices;
            int requestedTicket = -1;
            for(int i = 0; i < tickets.Length; i++)
            {
                // check if the ticket is of the requested type
                if (tickets[i][0].Equals(ticketType, StringComparison.CurrentCultureIgnoreCase))
                {
                    int availableTickets = 0;
                    if (Int32.TryParse(tickets[i][3], out availableTickets))
                    {
                        // Only allow reservations on available tickets
                        if(availableTickets > 0)
                        {
                            // Note the matching type
                            requestedTicket = i;

                            //Make the reservation
                            ReservationItem res = new ReservationItem();
                            res.eventID = id;
                            res.ticketType = requestedTicket;
                            res.reservationEnds = DateTime.Now.AddMinutes(duration);
                            res.reservingUser = reservingUser;
                            _reservationContext.Add(res);
                            await _reservationContext.SaveChangesAsync();

                            // Adjust available tickets
                            eventItem.TicketPrices[i][3] = (availableTickets - 1).ToString();

                            _eventContext.EventItems.Add(eventItem);
                            _eventContext.Entry(eventItem).State = EntityState.Modified;
                            try
                            {
                                await _eventContext.SaveChangesAsync();
                            }
                            catch (DbUpdateConcurrencyException)
                            {
                                if (!EventItemExists(id))
                                {
                                    return NotFound();
                                }
                                else
                                {
                                    throw;
                                }
                            }

                            return CreatedAtAction(nameof(ReserveTicket), new { id = res.reservationId }, "Reservation saved! Reservation ID: " + res.reservationId);
                        }
                    }
                }
            }
             return BadRequest("Requested ticket not available for event");            
        }

        [HttpDelete("deleteReservationItem{id}/{user}")]
        public async Task<IActionResult> DeleteReservationItem(long id, string user)
        {
            await UpdateReservations();

            var reservation = await _reservationContext.ReservationItem.FindAsync(id);
            if (reservation == null)
            {
                return BadRequest("Reservation not found or expired");
            }
            else
            {
                if (!reservation.reservingUser.Equals(user, StringComparison.CurrentCultureIgnoreCase)) 
                {
                    return BadRequest("Only the reserving user may cancel their reservation");
                }
                await CancelReservation(reservation, user);
            }
            
            return NoContent();
        }
        
        [HttpPost("buyTicker{id}")]
        public async Task<IActionResult> BuyTicket(long id, string ticketType, string user)
        {
            //First, make sure the event is valid
            var eventItem = await _eventContext.EventItems.FindAsync(id);
            if(eventItem == null)
            {
                return BadRequest("Event not found!");
            }

            await UpdateReservations();

            // First, check if the user has a reservation for the specified event
            List<ReservationItem> reservations = _reservationContext.ReservationItem.ToListAsync().Result;
            foreach (ReservationItem reservation in reservations)
            {
                // Check for the user's reservations
                if (reservation.reservingUser.Equals(user, StringComparison.CurrentCultureIgnoreCase) )
                {
                    // Then check if the reservation is for this event
                    if(reservation.eventID == id)
                    {
                        // This means the user does have a reservation, so we can proceed with payment and delete the reservation.
                        if(MockPayMethod(user))
                        {
                            _reservationContext.ReservationItem.Remove(reservation);

                            await _reservationContext.SaveChangesAsync();

                            return CreatedAtAction(nameof(BuyTicket), new { id = reservation.reservationId }, "Ticket purchased via reservation! Enjoy the show!");
                        }
                    }
                    
                }
            }
            // If they don't have a reservation they can still buy a ticket, assuming one is available. 
            string[][] tickets = eventItem.TicketPrices;
            for (int i = 0; i < tickets.Length; i++)
            {
                // check if the ticket is of the requested type
                if (tickets[i][0].Equals(ticketType, StringComparison.CurrentCultureIgnoreCase))
                {
                    int availableTickets = 0;
                    if (Int32.TryParse(tickets[i][3], out availableTickets))
                    {
                        // If there are tickets available, purchase one 
                        if (availableTickets > 0)
                        {
                            if(MockPayMethod(user))
                            {
                                // Adjust available tickets
                                eventItem.TicketPrices[i][3] = (availableTickets - 1).ToString();

                                _eventContext.EventItems.Add(eventItem);
                                _eventContext.Entry(eventItem).State = EntityState.Modified;
                                try
                                {
                                    await _eventContext.SaveChangesAsync();
                                }
                                catch (DbUpdateConcurrencyException)
                                {
                                    if (!EventItemExists(id))
                                    {
                                        return NotFound();
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }

                                return CreatedAtAction(nameof(BuyTicket), null, "Ticket purchased! Enjoy the show!");
                            }
                        }
                    }
                }
            }

            return NoContent();
        }

        private bool VenueItemExists(long id)
        {
            return _venueContext.VenueItems.Any(e => e.VenueId == id);
        }
        private bool EventItemExists(long id)
        {
            return _eventContext.EventItems.Any(e => e.EventId == id);
        }

        public string ValidateDates(List<string> dates, out List<string> validDates, out List<string> invalidDates)
        {
            validDates = new List<string>();
            invalidDates = new List<string>();
            for (int i = 0; i < dates.Count; i++)
            {
                // parse the dates, accepting well-formed dates that aren't in the past Ex: 2026-02-14
                if (DateOnly.TryParse(dates[i], out DateOnly result) == true && DateOnly.Parse(dates[i]).CompareTo(DateOnly.FromDateTime(DateTime.Now)) >= 0)
                {
                    validDates.Add(dates[i]);
                }
                else
                {
                    invalidDates.Add(dates[i]);
                }
            }

            string output = "";
            if (invalidDates.Count > 0)
            {
                output += "Some dates provided were invalid or in the past. Expected date format is YYYY-MM-DD. Invalid dates: \n";

                foreach (string badDate in invalidDates)
                {
                    output += badDate + "\n";
                }
            }

            return output;
        }

        private async Task<VenueItem?> FindValidVenue(EventItemDTO eventItem) {
            List<VenueItem> venues = await _venueContext.VenueItems.ToListAsync();

            // If a venue is specified, check to see if we find it in our database
            if (eventItem.Venue != null)
            {
                //First go off of the given ID, if there is one TODO check what happens if we don't pass ID
                
                VenueItem? venue = await _venueContext.VenueItems.FindAsync(eventItem.Venue.VenueId);
                if (venue != null)
                {
                    if (ValidateDateAndCapacity(eventItem,venue))
                    {
                        return venue;
                    }
                    
                }

                //If we don't find based on ID, we can search based on name
                foreach (VenueItem venueItem in venues)
                {
                    if(venueItem.Name == eventItem.Venue.Name)
                    {
                        if(ValidateDateAndCapacity(eventItem, venueItem))
                        {
                            return venueItem;
                        }
                    }
                }
            }

            if (eventItem.Venue == null)
            {
                foreach (VenueItem venue in venues)
                {
                    if(ValidateDateAndCapacity(eventItem, venue))
                    {
                        return venue;
                    }
                }
            }

            return null;
        }

        public bool ValidateDateAndCapacity(EventItemDTO eventItem, VenueItem venueItem) 
        {
            // Check capacity first, for performance
            if (eventItem.Capacity <= venueItem.Capacity)
            {
                foreach (string possibleDate in venueItem.AvailableDates)
                {
                    if (possibleDate == eventItem.Date)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private bool ValidateTicketFormatting(EventItem eventItem)
        {
            string[][] prices = eventItem.TicketPrices;
            if (eventItem.TicketPrices.Length == 0)
            {
                return false;
            }

            int availableCapacity = eventItem.Capacity;

            // Validate ticket types
            for (int i = 0; i < prices.Length; i++)
            {
                // Required format is Name, Price, TotalAvailable, CurrentAvailable
                if (prices[i].Length != 4)
                {
                    return false;
                }

                // Check price validity
                int price = 0;
                if (!Int32.TryParse(prices[i][2], out price))
                {
                    return false;
                }
                if (price < 0)
                {
                    return false;
                }

                // Check formatting of total and available tickets
                int requiredTickets = 0;
                int availableTickets = 0;
                if (!Int32.TryParse(eventItem.TicketPrices[i][2], out requiredTickets))
                {
                    return false;
                }
                if (!Int32.TryParse(eventItem.TicketPrices[i][3], out availableTickets))
                {
                    return false;
                }

                // Check Available Capacity against total capacity
                if(requiredTickets < availableTickets)
                {
                    return false;
                }

                // Check total tickets against capacity
                if (availableCapacity < requiredTickets)
                {
                    return false;
                }
                else
                {
                    availableCapacity -= requiredTickets;
                }
            }

            return true;
        }

        private async Task UpdateReservations()
        {
            List<ReservationItem> reservations = _reservationContext.ReservationItem.ToListAsync().Result;
            foreach (ReservationItem reservation in reservations)
            {
                // Cancel reservations that have lapsed
                if (reservation.reservationEnds.CompareTo(DateTime.Now) <= 0) 
                {
                    await CancelReservation(reservation, reservation.reservingUser);                    
                }
            }
        }

        private async Task CancelReservation(ReservationItem reservation, string user)
        {
            // Update the event to re-add the tickets to be available
            var eventItem = await _eventContext.EventItems.FindAsync(reservation.eventID);
            if (eventItem != null)
            {
                int availableTickets = Int32.Parse(eventItem.TicketPrices[reservation.ticketType][3]);
                eventItem.TicketPrices[reservation.ticketType][3] = (availableTickets + 1).ToString();

                _eventContext.EventItems.Add(eventItem);
                _eventContext.Entry(eventItem).State = EntityState.Modified;
                await _eventContext.SaveChangesAsync();
            }

            _reservationContext.ReservationItem.Remove(reservation);

            await _reservationContext.SaveChangesAsync();
        }

        private bool MockPayMethod(string user)
        {
            // This is where we'd go to the payment processing system, if that was part of the requirements
            // Instead, we just return true assuming payments are valid
            return true;
        }

        private static VenueItemDTO VenueToDTO(VenueItem venue) =>
            new VenueItemDTO
            {
                Id = venue.VenueId,
                Name = venue.Name,
                Capacity = venue.Capacity,
                AvailableDates = venue.AvailableDates,
            };

        private static EventItemDTO EventToDTO(EventItem eventItem) =>
            new EventItemDTO
            {
                Id = eventItem.EventId,
                Name = eventItem.Name,
                Date = eventItem.Date,
                Description = eventItem.Description,
                Capacity = eventItem.Capacity,
                TicketPrices = eventItem.TicketPrices,
            };
    }
}
