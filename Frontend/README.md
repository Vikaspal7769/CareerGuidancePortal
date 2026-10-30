# CareerPath AI — Frontend Only

Start the Backend first with `dotnet run`.
Then double-click `index.html`.

The frontend redirects `file://` to `http://localhost:5050/`, so no separate frontend server is required.

API endpoints used:
- POST /api/auth/register
- POST /api/auth/login
- GET/PUT /api/profile
- GET /api/recommendations
- GET /api/courses
- GET /api/colleges
- GET /api/mentors
- POST /api/mentors/{id}/request
- GET /api/notifications
