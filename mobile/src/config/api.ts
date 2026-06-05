import Constants from 'expo-constants';
import { Platform } from 'react-native';

const API_PORT = 5221;

function normalizeBaseUrl(url: string): string {
  return url.replace(/\/$/, '');
}

/**
 * Android emulator: 10.0.2.2 → host localhost.
 * iOS simulator: localhost works.
 * Physical device (Expo Go): set EXPO_PUBLIC_API_URL in mobile/.env to your PC LAN IP.
 */
export function getApiBaseUrl(): string {
  const configured = process.env.EXPO_PUBLIC_API_URL?.trim();
  if (configured) {
    return normalizeBaseUrl(configured);
  }

  // Simulator / emulator (Expo Go on emulator counts as !isDevice on some setups; Platform helps)
  if (!Constants.isDevice) {
    const url = Platform.select({
      android: `http://10.0.2.2:${API_PORT}`,
      ios: `http://localhost:${API_PORT}`,
      default: `http://localhost:${API_PORT}`,
    })!;
    return normalizeBaseUrl(url);
  }

  throw new Error(
    'API URL not set for this phone. Copy mobile/.env.example to mobile/.env, set EXPO_PUBLIC_API_URL=http://YOUR_PC_IP:5221, run the API on all interfaces (dotnet run --launch-profile http-mobile), then restart Expo with: npx expo start -c',
  );
}

/** Dev-only: null when configured, otherwise a short setup hint. */
export function getApiSetupHint(): string | null {
  if (process.env.EXPO_PUBLIC_API_URL?.trim()) {
    return null;
  }
  if (!Constants.isDevice) {
    return null;
  }
  try {
    getApiBaseUrl();
    return null;
  } catch (e) {
    return e instanceof Error ? e.message : 'Configure EXPO_PUBLIC_API_URL in mobile/.env';
  }
}
