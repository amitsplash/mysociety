# MySociety Mobile (Expo / React Native)

Consumes the MySociety ASP.NET Core API.

## Prerequisites

- Node.js 18+
- API running (physical phone / Expo Go):  
  `dotnet run --project ../src/Api/MySociety.Api.csproj --launch-profile http-mobile`  
  (listens on all interfaces so your phone can connect)
- API on emulator only: `--launch-profile http` is enough
- Expo Go app on your phone, or Android emulator

## Run

```bash
cd mobile
npm install
npm start
```

Press `a` for Android emulator or scan QR with Expo Go.

## API URL

| Environment | URL |
|-------------|-----|
| Android emulator | `http://10.0.2.2:5221` (default) |
| iOS simulator | `http://localhost:5221` |
| Physical device (Expo Go) | **Required:** `EXPO_PUBLIC_API_URL` in `mobile/.env` + API on `http-mobile` profile |

Copy `mobile/.env.example` to `mobile/.env` and set your PC's Wi‑Fi IP (`ipconfig` on Windows). Restart Expo after changing `.env`:

```bash
npx expo start -c
```

Without this, login spins forever — the app was calling `10.0.2.2` (emulator-only) or `localhost` (the phone itself), not your PC.

## Demo login

- Phone: `9000000001`
- Password: `Password123!`

## Screens

1. Login / Create group  
2. Dashboard (pull to refresh, group summary)  
3. Members / Add member  
4. Add expense / Expense approval  
5. Ledger (balance highlight, transaction history)  
6. Contributions (generate & pay)

## UX polish

- Toast notifications (success / error) instead of intrusive alerts  
- Pull-to-refresh on all list screens  
- Loading spinners on buttons during submit  
- Empty states with helpful messages  
- Status badges (Pending, Paid, Approved, etc.)  
- INR currency formatting  
- Consistent theme and navigation styling
