# Frontend Testing Guide (WPF)

## Backend discovered from `IIS_App-test2 (1).zip`
- Spring Boot API on `http://localhost:8080`.
- JWT auth endpoints: `POST /api/auth/login`, `POST /api/auth/refresh`.
- Players CRUD: `GET/POST/PATCH/DELETE /api/players`.
- XML endpoints: `GET /players`, `POST /validateAndSaveXml`.
- SOAP endpoint at `/ws`, WSDL at `/ws/players.wsdl`.
- Weather is implemented as **XML-RPC** (`WeatherService.getTemperature`) on `http://localhost:9090/RPC2`.
- Optional Relax NG module in `ng-relax` on `http://localhost:8081/validate`.

## Run backend
1. Extract zip and run Spring app (`mvn spring-boot:run`) from `IIS_App-test2`.
2. Run weather server main class `WeatherServer` (port 9090).
3. Optional: run `ng-relax` module (`mvn spring-boot:run`) for Relax NG validation.

## Run WPF app
1. Ensure `IISApp/App.config` URLs match your local backend.
2. Set `AccessMode=ReadOnly` or `FullAccess`.
3. Run:
   - `dotnet build IISApp.sln`
   - `dotnet run --project IISApp/IISApp.csproj`

Default credentials from backend source: `admin / password`.

## Screens
- Login & Tokens: authentication status.
- REST Players: GET/POST/PATCH/DELETE testing.
- XML Validate/Save: post player XML and view validation results.
- SOAP Search: SOAP search by term.
- Weather: XML-RPC weather lookup.
- Health Checks: API/SOAP/Weather/RelaxNG quick checks.

## Known mismatch
- Assignment may mention gRPC weather, but provided backend zip currently implements XML-RPC weather service.
