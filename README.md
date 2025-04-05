# NDC ConcertSystem

## Thank you for taking the time to review my concert ticket reservation system! 
- The majority of the operational code can be found in ConcertTicketManagementSystem/Controllers/ConcertTicketManager.cs
- An image list of the available HTTP methods is available in the ConcertTicketManagementSystem top level folder
- Unit tests are included in ConcertTicketManagementSystemTests/Controllers/ConcertTicketManagerTests.cs
- Sample well-formed requests are available in ConcertTicketManagementSystem/ConcertTicketManagementSystem.http

## Sample workflow:
- Create a venue with a venue post request
- Create an event on that venue using a postEventItem request
- Modify that event to be on another available day for the venue using an putEventItem request
- Buy a ticket using the buyTicket post request
- Reserve a ticket using reserveTicket post request
- Observe the changes to the event using the getEventItems request
- Experiment further! 

## Some notes on the behaviors/limitations of the system:
- The system uses exclusively in-memory storage
- Many outputs include string-based descriptive outputs. For an actual web API implementation only important data would be returned.
- The reserved tickets data store only updates when certain APIs are called. In a full implementation there would probably be a background task or scheduled reminders to update it.
- Changing the venue of an event directly is currently not supported, but this can still be accomplished by deleting and recreating the event
- When updating an event we assume the ticket types (General Admission, Front Row, etc.) are unchanged, and only the counts are modified

## Some future features I'd implement given more time:
- Persistent storage
- More unit tests
- An option to create/delete multiple venues/events/reservations/tickets with one request
- Security:
  -  User authentication
  -  Further data sanitization/input checking
