# CareerPath AI — Career Guidance Portal

## Run exactly as requested

### Step 1 — Start the backend
Open PowerShell/Command Prompt inside:

`Backend`

Run:

```powershell
dotnet restore
dotnet run
```

The backend is configured to run at:

`http://localhost:5050`

Keep this terminal running.

### Step 2 — Open the frontend by double-click
Now go to:

`Frontend\index.html`

and **double-click it**.

The browser will automatically redirect the `file://` page to:

`http://localhost:5050/`

So you do NOT need Live Server, `npm`, Python HTTP server, or a separate frontend command.

### Important
- Backend = only thing you need to run.
- Frontend files remain in a separate `Frontend` folder.
- Double-clicking `Frontend/index.html` after the backend is running loads the complete styled website.
- API calls, CSS and JavaScript are served by ASP.NET Core.
- If you double-click before starting the backend, the browser cannot connect; start the backend first.
