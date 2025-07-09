# TicketBlaster Event Search and Discovery

This document describes the comprehensive Event Search and Discovery functionality implemented for the TicketBlaster platform.

## Overview

The Event Search and Discovery system provides powerful features for users to find and explore events:

- **Advanced Search**: Full-text search with filtering, sorting, and faceted navigation
- **Discovery Features**: Trending events, personalized recommendations, and curated content
- **Location-Based Search**: Find events near you using geolocation
- **Real-time Analytics**: Track event views, interactions, and performance metrics

## Architecture

### Services

1. **EventSearchService** (`IEventSearchService`)
   - Handles all search operations with advanced filtering
   - Provides faceted search capabilities
   - Tracks popular search terms

2. **EventDiscoveryService** (`IEventDiscoveryService`)
   - Manages trending and recommended events
   - Handles personalized content based on user preferences
   - Tracks event interactions and analytics

3. **EventService** (`IEventService`)
   - Basic CRUD operations for events
   - Event lifecycle management (publish, cancel, etc.)
   - Event statistics and reporting

### Controllers

1. **EventSearchController** (`/api/eventsearch`)
   - Search endpoints with various filtering options
   - Quick search and specialized searches

2. **EventDiscoveryController** (`/api/eventdiscovery`)
   - Discovery content and trending events
   - Personalized recommendations
   - Event statistics and analytics

3. **EventController** (`/api/event`)
   - Basic event management endpoints
   - Event lifecycle operations

## API Endpoints

### Search Endpoints

#### Advanced Search
```
POST /api/eventsearch/search
```
Request body:
```json
{
  "searchTerm": "concert",
  "categoryIds": [1, 2],
  "startDate": "2024-01-01",
  "endDate": "2024-12-31",
  "location": "New York",
  "minPrice": 0,
  "maxPrice": 100,
  "tags": ["music", "rock"],
  "isVirtual": false,
  "sortBy": "StartDate",
  "sortDirection": "Ascending",
  "pageNumber": 1,
  "pageSize": 20
}
```

#### Quick Search
```
GET /api/eventsearch/quick?q=concert&categoryId=1&page=1&size=20
```

#### Location-Based Search
```
GET /api/eventsearch/nearby?latitude=40.7128&longitude=-74.0060&radius=50
```

#### Specialized Searches
- `GET /api/eventsearch/by-date?startDate=2024-01-01&endDate=2024-01-31`
- `GET /api/eventsearch/by-price?minPrice=0&maxPrice=50`
- `GET /api/eventsearch/virtual`
- `GET /api/eventsearch/free`

### Discovery Endpoints

#### Main Discovery Content
```
GET /api/eventdiscovery?latitude=40.7128&longitude=-74.0060
```
Returns personalized discovery sections including:
- Featured Events
- Trending Now
- Near You
- This Week
- Virtual Events
- Free Events
- Recommended for You
- Just Added
- Last Chance

#### Trending Events
```
GET /api/eventdiscovery/trending?categoryId=1&period=Last7Days&count=10
```

#### Personalized Recommendations
```
GET /api/eventdiscovery/recommended?count=10&type=Hybrid
```
(Requires authentication)

#### Similar Events
```
GET /api/eventdiscovery/similar/{eventId}?count=5
```

#### Event Statistics
```
GET /api/eventdiscovery/statistics/{eventId}
```
(Requires organizer authorization)

### Event Management Endpoints

- `GET /api/event/{id}` - Get event details
- `GET /api/event` - Get public events
- `GET /api/event/category/{categoryId}` - Events by category
- `GET /api/event/featured` - Featured events
- `POST /api/event` - Create event (organizer only)
- `PUT /api/event/{id}` - Update event (organizer only)
- `DELETE /api/event/{id}` - Delete event (organizer only)
- `POST /api/event/{id}/publish` - Publish event
- `POST /api/event/{id}/cancel` - Cancel event

## Search Features

### Filtering Options
- **Text Search**: Search in title, description, location, and tags
- **Category**: Single or multiple category filtering
- **Date Range**: Filter by event date range
- **Location**: Text-based location search or radius search
- **Price Range**: Filter by ticket price range
- **Tags**: Filter by event tags
- **Event Type**: Virtual, in-person, or hybrid events
- **Status**: Filter by event status

### Sorting Options
- Start Date (ascending/descending)
- End Date
- Title (alphabetical)
- Price (lowest/highest)
- Popularity (based on ticket sales)
- Distance (for location-based searches)
- Relevance (for text searches)
- Created Date
- Updated Date

### Faceted Search
The search results include facets for refining searches:
- **Categories**: With event counts
- **Price Ranges**: Predefined ranges with counts
- **Date Ranges**: Today, Tomorrow, This Week, etc.
- **Locations**: Top cities with event counts
- **Popular Tags**: Most used tags
- **Event Types**: Virtual, In-Person, Featured counts

## Discovery Features

### Trending Events Algorithm
Trending score is calculated based on:
- Recent order activity (40% weight)
- Total popularity (20% weight)
- Urgency - events happening soon (20% weight)
- Featured status (20% weight)

### Recommendation Engine
Recommendations are based on:
- User's past event attendance
- Preferred categories
- Attended event tags
- Event popularity
- Similar events algorithm

### Similar Events
Similarity is calculated using:
- Same category (40% weight)
- Tag similarity (30% weight)
- Same organizer (10% weight)
- Price range similarity (10% weight)
- Event type similarity (10% weight)

## Analytics and Tracking

### Event Views
- Track each event view with optional user ID
- Used for calculating conversion rates

### Event Interactions
Supported interaction types:
- `save` - User saved the event
- `share` - User shared the event
- `like` - User liked the event
- `interested` - User marked as interested
- `going` - User plans to attend

### Event Statistics
Available metrics:
- Total views
- Save/share counts
- Tickets sold
- Revenue
- Conversion rate
- Daily view trends
- Daily ticket sales

## Performance Optimizations

1. **Caching**: Trending events are cached for 15 minutes
2. **Pagination**: All list endpoints support pagination
3. **Efficient Queries**: Uses Entity Framework Include for eager loading
4. **Indexed Fields**: Database indexes on frequently searched fields

## Security

- Authentication required for:
  - Personalized recommendations
  - Recording interactions
  - Event management operations
  
- Authorization policies:
  - `OrganizerOnly`: For event creation and management
  - Event ownership verification for updates/deletes

## Future Enhancements

1. **Elasticsearch Integration**: For more powerful full-text search
2. **Machine Learning**: Improved recommendation algorithms
3. **Real-time Updates**: WebSocket notifications for inventory changes
4. **Advanced Analytics**: More detailed event performance metrics
5. **Saved Searches**: Allow users to save and subscribe to searches
6. **A/B Testing**: Test different recommendation algorithms
7. **Social Features**: Follow organizers, friend recommendations

## Example Usage

### Search for Music Events in New York
```javascript
const response = await fetch('/api/eventsearch/search', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    searchTerm: 'music',
    location: 'New York',
    categoryIds: [1], // Music category
    sortBy: 'StartDate',
    pageSize: 20
  })
});

const results = await response.json();
// results.results - array of events
// results.facets - available filters
// results.totalCount - total matching events
```

### Get Personalized Recommendations
```javascript
const response = await fetch('/api/eventdiscovery/recommended?count=10', {
  headers: { 'Authorization': 'Bearer ' + token }
});

const recommendations = await response.json();
```

### Track Event View
```javascript
await fetch('/api/eventdiscovery/view/123', {
  method: 'POST'
});
```

## Testing

To test the search and discovery functionality:

1. Create sample events with various categories, prices, and locations
2. Test search with different filter combinations
3. Verify facet counts match filtered results
4. Test pagination with large result sets
5. Verify trending algorithm with simulated orders
6. Test recommendations with user history
7. Verify location-based search accuracy

## Dependencies

- Entity Framework Core for data access
- Memory caching for performance
- ASP.NET Core for API endpoints
- Authentication/Authorization middleware

## Configuration

Add to `appsettings.json`:
```json
{
  "Search": {
    "MaxResultsPerPage": 100,
    "DefaultPageSize": 20,
    "CacheDurationMinutes": 15
  }
}
```